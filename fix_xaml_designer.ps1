$xamlFiles = Get-ChildItem "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI" -Filter "*.xaml" -Recurse
$stats = @{ fixed = 0; skipped = 0; alreadyOk = 0 }
foreach ($file in $xamlFiles) {
	$raw = Get-Content $file.FullName -Raw -Encoding UTF8
	if ($raw -match '<Application\s' -or $raw -match '<ResourceDictionary') { $stats.skipped++; continue }
	$changed = $false
	$content = $raw
	$needsMc  = $content -notmatch 'xmlns:mc='
	$needsD   = $content -notmatch 'xmlns:d='
	$needsIgn = $content -notmatch 'mc:Ignorable'
	$needsDtc = $content -notmatch 'd:IsDesignTimeCreatable'
	if ($needsMc -or $needsD) {
		$inject = ''
		if ($needsMc) { $inject += "`r`n    xmlns:mc=`"http://schemas.openxmlformats.org/markup-compatibility/2006`"" }
		if ($needsD)  { $inject += "`r`n    xmlns:d=`"http://schemas.microsoft.com/expression/blend/2008`"" }
		$content = $content -replace '(xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml")', "`$1$inject"
		$changed = $true
	}
	if ($needsIgn -and ($content -match 'xmlns:d=')) {
		$content = $content -replace '(xmlns:d="http://schemas.microsoft.com/expression/blend/2008")', ('$1' + "`r`n    mc:Ignorable=`"d`"")
		$changed = $true
	}
	if ($needsDtc -and ($content -match 'mc:Ignorable="d"')) {
		$content = $content -replace '(mc:Ignorable="d")', ('$1' + "`r`n    d:IsDesignTimeCreatable=`"False`"")
		$changed = $true
	}
	if ($changed) {
		[System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
		$stats.fixed++
		Write-Host "OK: $($file.Name)" -ForegroundColor Green
	} else {
		$stats.alreadyOk++
		Write-Host "Juz ok: $($file.Name)" -ForegroundColor DarkGray
	}
}
Write-Host ""
Write-Host "Poprawiono: $($stats.fixed)   Pominieto: $($stats.skipped)   Juz ok: $($stats.alreadyOk)" -ForegroundColor Cyan
