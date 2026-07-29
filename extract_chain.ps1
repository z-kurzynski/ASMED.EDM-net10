# Wczytaj PEM z wszystkimi certyfikatami
$pemContent = Get-Content 'A:\EDMP1\tls-dabb57f1ca3f419baff49edadf81a600.pem' -Raw

# Rozdziel na poszczególne certyfikaty
$certs = $pemContent -split '(?=-----BEGIN CERTIFICATE-----)'
$certs = $certs | Where-Object { $_ -match 'BEGIN CERTIFICATE' }

Write-Host "Znaleziono $($certs.Count) certyfikatów w PEM"

# Zapisz każdy certyfikat osobno
for ($i = 0; $i -lt $certs.Count; $i++) {
	$certPem = $certs[$i].Trim()
	$outFile = "A:\EDMP1\cert_$i.cer"

	# Konwertuj PEM → DER (binary)
	$certLines = ($certPem -split "`n") | Where-Object { $_ -notmatch '-----' }
	$certBase64 = $certLines -join ''
	$certBytes = [Convert]::FromBase64String($certBase64)
	[IO.File]::WriteAllBytes($outFile, $certBytes)

	# Odczytaj Subject/Issuer
	$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($outFile)
	Write-Host "[$i] Subject: $($cert.Subject)"
	Write-Host "    Issuer:  $($cert.Issuer)"
	Write-Host "    Saved:   $outFile"
	Write-Host ""
}
