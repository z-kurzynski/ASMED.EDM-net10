$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2('A:\EDMP1\asmedp1_tls_p12.p12', 'asNZOZmed')
Write-Host "Subject:    " $cert.Subject
Write-Host "Issuer:     " $cert.Issuer
Write-Host "HasPrivKey: " $cert.HasPrivateKey
Write-Host "NotAfter:   " $cert.NotAfter
Write-Host ""

$chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
$chain.Build($cert) | Out-Null
Write-Host "Chain length: $($chain.ChainElements.Count)"
Write-Host "Chain status: $($chain.ChainStatus.Status)"
foreach ($elem in $chain.ChainElements) {
	Write-Host "  - Subject: $($elem.Certificate.Subject)"
	Write-Host "    Issuer:  $($elem.Certificate.Issuer)"
}
