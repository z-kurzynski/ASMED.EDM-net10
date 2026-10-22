<#
.SYNOPSIS
	Kopia zapasowa bazy MySQL: mysqldump -> zip -> kopia na drugi nosnik -> rotacja.

.DESCRIPTION
	Dane logowania czytane sa z pliku opcji MySQL (--defaults-extra-file),
	dzieki czemu haslo nie pojawia sie w linii polecen ani w Harmonogramie zadan.
	Plik tworzy install-backup-task.ps1.

	Kod wyjscia: 0 = OK, 1 = blad kopii, 2 = kopia OK, ale nie udalo sie skopiowac na drugi nosnik.

.EXAMPLE
	.\backup-mysql.ps1 -Database asmed_local -DefaultsFile D:\MySQL\Backup\credentials\asmed_local.cnf -CopyTo E:\ASMED_Backup
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)] [string] $Database,
	[Parameter(Mandatory = $true)] [string] $DefaultsFile,
	[string] $BackupDir = 'D:\MySQL\Backup',
	[string] $CopyTo = '',
	[int]    $KeepDays = 30,
	[int]    $MinKeep = 7,
	[string] $Label = '',
	[string] $MySqlBin = 'C:\Program Files\MySQL\MySQL Server 26.10\bin'
)

$ErrorActionPreference = 'Stop'
if (-not $Label) { $Label = $Database }

$logDir = Join-Path $BackupDir 'logs'
New-Item -ItemType Directory -Force -Path $BackupDir, $logDir | Out-Null
$logFile = Join-Path $logDir ('backup_{0}.log' -f (Get-Date -Format 'yyyy-MM'))

function Write-Log {
	param([string] $Message, [string] $Level = 'INFO')
	$line = '{0} [{1}] [{2}] {3}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level, $Label, $Message
	Add-Content -Path $logFile -Value $line -Encoding UTF8
	Write-Host $line
}

# Usuwa kopie starsze niz KeepDays, ale zawsze zostawia MinKeep najnowszych
# (gdy kopie przestana sie wykonywac, stare nie znikna wszystkie).
function Remove-OldBackups {
	param([string] $Dir)
	$cutoff = (Get-Date).AddDays(-$KeepDays)
	Get-ChildItem -Path $Dir -Filter ('{0}_*.zip' -f $Label) -File |
		Sort-Object LastWriteTime -Descending |
		Select-Object -Skip $MinKeep |
		Where-Object { $_.LastWriteTime -lt $cutoff } |
		ForEach-Object {
			Remove-Item -LiteralPath $_.FullName -Force
			Write-Log ('Usunieto stara kopie: {0}' -f $_.FullName)
		}
}

$stamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
# Unikalna nazwa gwarantuje, ze sprzatanie po bledzie nie usunie istniejacej kopii.
$suffix = ''
$n = 1
do {
	$sqlFile = Join-Path $BackupDir ('{0}_{1}{2}.sql' -f $Label, $stamp, $suffix)
	$zipFile = [IO.Path]::ChangeExtension($sqlFile, '.zip')
	$suffix = '_{0}' -f $n++
} while ((Test-Path -LiteralPath $sqlFile) -or (Test-Path -LiteralPath $zipFile))
$exitCode = 0

try {
	$mysqldump = Join-Path $MySqlBin 'mysqldump.exe'
	if (-not (Test-Path -LiteralPath $mysqldump))    { throw "Nie znaleziono $mysqldump" }
	if (-not (Test-Path -LiteralPath $DefaultsFile)) { throw "Nie znaleziono pliku z danymi logowania: $DefaultsFile" }

	Write-Log "Start zrzutu bazy '$Database'"
	$sw = [Diagnostics.Stopwatch]::StartNew()

	# --defaults-extra-file musi byc pierwszym argumentem.
	# --result-file zapisuje UTF-8 bez posrednictwa PowerShell (brak konwersji do UTF-16).
	$dumpArgs = @(
		"--defaults-extra-file=$DefaultsFile",
		'--single-transaction',
		'--quick',
		'--routines',
		'--triggers',
		'--no-tablespaces',
		'--set-gtid-purged=OFF',
		'--hex-blob',
		'--default-character-set=utf8mb4',
		"--result-file=$sqlFile",
		$Database
	)

	# W PowerShell 5.1 komunikaty na stderr przy 'Stop' przerywalyby skrypt.
	$ErrorActionPreference = 'Continue'
	$dumpOutput = & $mysqldump @dumpArgs 2>&1 | ForEach-Object { "$_" }
	$dumpExit = $LASTEXITCODE
	$ErrorActionPreference = 'Stop'

	foreach ($line in $dumpOutput) { if ($line) { Write-Log $line 'WARN' } }
	if ($dumpExit -ne 0) { throw "mysqldump zakonczyl sie kodem $dumpExit" }
	if (-not (Test-Path -LiteralPath $sqlFile)) { throw 'mysqldump nie utworzyl pliku zrzutu' }

	$lastLine = Get-Content -LiteralPath $sqlFile -Tail 1 -Encoding UTF8
	if ($lastLine -notmatch 'Dump completed') { throw 'Zrzut jest niekompletny (brak znacznika "Dump completed")' }

	# ZipFile zamiast Compress-Archive: szybszy i bez limitu 2 GB w PowerShell 5.1.
	Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
	$zip = [IO.Compression.ZipFile]::Open($zipFile, [IO.Compression.ZipArchiveMode]::Create)
	try {
		[void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
			$zip, $sqlFile, (Split-Path $sqlFile -Leaf), [IO.Compression.CompressionLevel]::Optimal)
	}
	finally { $zip.Dispose() }
	Remove-Item -LiteralPath $sqlFile -Force

	$sw.Stop()
	$sizeMb = [math]::Round((Get-Item -LiteralPath $zipFile).Length / 1MB, 2)
	Write-Log ('Kopia OK: {0} ({1} MB, {2:N0} s)' -f $zipFile, $sizeMb, $sw.Elapsed.TotalSeconds)

	if ($CopyTo) {
		try {
			if (-not (Test-Path -LiteralPath $CopyTo)) { throw "Nosnik niedostepny: $CopyTo" }
			Copy-Item -LiteralPath $zipFile -Destination $CopyTo -Force
			Write-Log "Skopiowano na drugi nosnik: $CopyTo"
			Remove-OldBackups -Dir $CopyTo
		}
		catch {
			Write-Log ('Nie udalo sie skopiowac na drugi nosnik: {0}' -f $_.Exception.Message) 'ERROR'
			$exitCode = 2
		}
	}

	Remove-OldBackups -Dir $BackupDir
}
catch {
	Write-Log $_.Exception.Message 'ERROR'
	foreach ($f in @($sqlFile, $zipFile)) {
		if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force -ErrorAction SilentlyContinue }
	}
	$exitCode = 1
}

exit $exitCode
