<#
.SYNOPSIS
	Instaluje codzienna kopie zapasowa bazy MySQL w Harmonogramie zadan Windows.

.DESCRIPTION
	1. Kopiuje backup-mysql.ps1 do katalogu skryptow na serwerze (domyslnie D:\MySQL\Scripts).
	2. Pyta o haslo i zapisuje dane logowania w pliku opcji MySQL dostepnym
	   tylko dla SYSTEM i Administratorow (haslo nie trafia do repozytorium ani do zadania).
	3. Sprawdza polaczenie z baza.
	4. Rejestruje zadanie "\ASMED\Backup MySQL - <Label>".

	Uruchom jako Administrator. Instalacje mozna powtorzyc - zadanie i plik zostana nadpisane.

.EXAMPLE
	# Baza lokalna, codziennie o 02:00, kopia na dysk USB E:
	.\install-backup-task.ps1 -CopyTo 'E:\ASMED_Backup' -RunNow

.EXAMPLE
	# Dodatkowo kopia bazy z chmury (codziennie o 03:00)
	.\install-backup-task.ps1 -Label chmura -DbHost mysql84.nq.pl -User asmed_krone -Database asmed2026_krone -Time '03:00' -CopyTo 'E:\ASMED_Backup'

.EXAMPLE
	# Kopia na NAS - zadanie musi dzialac na koncie z dostepem do udzialu sieciowego
	.\install-backup-task.ps1 -CopyTo '\\NAS\backup\asmed' -RunAsUser 'GABINET\backup'
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
	[string] $Database  = 'asmed_local',
	[string] $Label     = '',
	[string] $DbHost    = '127.0.0.1',
	[int]    $Port      = 3306,
	[string] $User      = 'asmed_backup',
	[string] $BackupDir = 'D:\MySQL\Backup',
	[string] $CopyTo    = '',
	[int]    $KeepDays  = 30,
	[string] $Time      = '02:00',
	[string] $ScriptDir = 'D:\MySQL\Scripts',
	[string] $MySqlBin  = 'C:\Program Files\MySQL\MySQL Server 26.10\bin',
	[string] $RunAsUser = '',
	[switch] $RunNow
)

$ErrorActionPreference = 'Stop'
if (-not $Label) { $Label = $Database }

$mysqlExe = Join-Path $MySqlBin 'mysql.exe'
if (-not (Test-Path -LiteralPath $mysqlExe)) { throw "Nie znaleziono $mysqlExe - sprawdz parametr -MySqlBin" }

# --- 1. Skrypt kopii --------------------------------------------------
New-Item -ItemType Directory -Force -Path $ScriptDir, $BackupDir | Out-Null
$backupScript = Join-Path $ScriptDir 'backup-mysql.ps1'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'backup-mysql.ps1') -Destination $backupScript -Force
Write-Host "Skrypt kopii: $backupScript"

# --- 2. Plik z danymi logowania -------------------------------------
$credDir = Join-Path $BackupDir 'credentials'
New-Item -ItemType Directory -Force -Path $credDir | Out-Null

# SID-y zamiast nazw grup - dzialaja w polskiej wersji Windows (Administratorzy).
$icaclsArgs = @($credDir, '/inheritance:r', '/grant:r', '*S-1-5-18:(OI)(CI)F', '*S-1-5-32-544:(OI)(CI)F')
if ($RunAsUser) { $icaclsArgs += "${RunAsUser}:(OI)(CI)R" }
& icacls.exe @icaclsArgs | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Nie udalo sie ustawic uprawnien do katalogu credentials' }

$securePwd = Read-Host -AsSecureString "Haslo MySQL dla uzytkownika '$User'@'$DbHost'"
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
try { $plainPwd = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }

$escapedPwd = $plainPwd.Replace('\', '\\').Replace('"', '\"')
$cnfContent = @"
[client]
host=$DbHost
port=$Port
user=$User
password="$escapedPwd"
default-character-set=utf8mb4
"@
$cnfFile = Join-Path $credDir ('{0}.cnf' -f $Label)
# UTF-8 bez BOM - BOM w pliku opcji MySQL powoduje bledy parsowania.
[IO.File]::WriteAllText($cnfFile, $cnfContent, (New-Object System.Text.UTF8Encoding($false)))
Remove-Variable plainPwd, escapedPwd, cnfContent
Write-Host "Dane logowania: $cnfFile (dostep: SYSTEM, Administratorzy$(if ($RunAsUser) { ", $RunAsUser" }))"

# --- 3. Test polaczenia ---------------------------------------------
$ErrorActionPreference = 'Continue'
$testOut = & $mysqlExe "--defaults-extra-file=$cnfFile" -N -e 'SELECT CONCAT(VERSION(), '' / '', DATABASE())' $Database 2>&1 | ForEach-Object { "$_" }
$testExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($testExit -ne 0) { throw "Test polaczenia nieudany: $($testOut -join ' ')" }
Write-Host "Polaczenie OK: $testOut" -ForegroundColor Green

# --- 4. Zadanie w Harmonogramie -------------------------------------
$taskArgs = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -Database "{1}" -DefaultsFile "{2}" -BackupDir "{3}" -KeepDays {4} -Label "{5}" -MySqlBin "{6}"' -f `
	$backupScript, $Database, $cnfFile, $BackupDir, $KeepDays, $Label, $MySqlBin
if ($CopyTo) { $taskArgs += ' -CopyTo "{0}"' -f $CopyTo }

$action   = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $taskArgs
$trigger  = New-ScheduledTaskTrigger -Daily -At $Time
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
				-ExecutionTimeLimit (New-TimeSpan -Hours 2) -MultipleInstances IgnoreNew
$taskName = "Backup MySQL - $Label"

$registerParams = @{
	TaskName    = $taskName
	TaskPath    = '\ASMED\'
	Action      = $action
	Trigger     = $trigger
	Settings    = $settings
	Description = "Codzienna kopia bazy '$Database' do $BackupDir$(if ($CopyTo) { " oraz $CopyTo" })"
	Force       = $true
}

if ($RunAsUser) {
	$cred = Get-Credential -UserName $RunAsUser -Message 'Haslo konta Windows, na ktorym ma dzialac zadanie kopii'
	$registerParams.User     = $cred.UserName
	$registerParams.Password = $cred.GetNetworkCredential().Password
	$registerParams.RunLevel = 'Highest'
}
else {
	$registerParams.Principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
}

Register-ScheduledTask @registerParams | Out-Null
Write-Host "Zadanie '\ASMED\$taskName' zarejestrowane - codziennie o $Time" -ForegroundColor Green

if ($RunNow) {
	Start-ScheduledTask -TaskPath '\ASMED\' -TaskName $taskName
	Write-Host "Uruchomiono kopie testowa. Wynik: $BackupDir\logs oraz Harmonogram zadan -> ASMED -> 'Wynik ostatniego uruchomienia' (0x0 = OK)."
}
