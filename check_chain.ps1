$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2('C:\Users\zk\Downloads\P1\tls-client.p12', 'asNZOZmed')
Write-Host "Subject:    " $cert.Subject
Write-Host "Issuer:     " $cert.Issuer
Write-Host "HasPrivKey: " $cert.HasPrivateKey
Write-Host "NotAfter:   " $cert.NotAfter
Write-Host ""

$chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
$chain.Build($cert) | Out-Null
Write-Host "Chain length: $($chain.ChainElements.Count)"
foreach ($elem in $chain.ChainElements) {
	Write-Host "  - $($elem.Certificate.Subject)"
}
