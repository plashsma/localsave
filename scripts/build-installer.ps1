param([Parameter(Mandatory=$true)][string]$CompilerPath,
    [string]$SignToolPath='', [string]$CertificateThumbprint='',
    [string]$TimestampUrl='https://timestamp.digicert.com',
    [switch]$MachineCertificateStore, [switch]$RequireSigning)
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$signing= -not [string]::IsNullOrWhiteSpace($CertificateThumbprint)
if ($RequireSigning -and -not $signing) {throw 'A trusted signing certificate is required. No unsigned release will be built.'}
if ($signing) {
    if($CertificateThumbprint -notmatch '^[0-9A-Fa-f]{40}$') {throw 'Use the SHA-1 thumbprint of your code-signing certificate.'}
    if(-not (Test-Path -LiteralPath $SignToolPath -PathType Leaf)) {throw 'Provide the Windows SDK SignTool executable.'}
    $SignToolPath=[IO.Path]::GetFullPath($SignToolPath)
    if($SignToolPath.Contains('"') -or $SignToolPath.Contains('$')) {throw 'Unsupported SignTool path characters.'}
    if($TimestampUrl -notmatch '^https://[a-zA-Z0-9./_-]+$') {throw 'Use a valid HTTPS RFC3161 timestamp URL.'}
    $store=if($MachineCertificateStore){'Cert:\LocalMachine\My'}else{'Cert:\CurrentUser\My'}
    $certificate=Get-Item -LiteralPath "$store\$CertificateThumbprint"
    if(-not $certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date) -or $certificate.NotBefore -gt (Get-Date)) {throw 'The certificate is expired, not yet valid, or has no accessible private key.'}
    if(@($certificate.Extensions | Where-Object {$_.Oid.Value -eq '2.5.29.37'} | ForEach-Object {$_.EnhancedKeyUsages} | Where-Object {$_.Value -eq '1.3.6.1.5.5.7.3.3'}).Count -eq 0) {throw 'A code-signing certificate is required.'}
    $chain=New-Object Security.Cryptography.X509Certificates.X509Chain
    try {
        $chain.ChainPolicy.RevocationMode='Online'
        $chain.ChainPolicy.UrlRetrievalTimeout=[TimeSpan]::FromSeconds(15)
        if(-not $chain.Build($certificate)) {throw 'Certificate trust or revocation validation failed.'}
        if($certificate.Subject -eq $certificate.Issuer) {throw 'Self-signed certificates are unsuitable for public releases.'}
    } finally {$chain.Dispose()}
}
& (Join-Path $PSScriptRoot 'build.ps1')
if ($LASTEXITCODE -ne 0) {throw 'Application build failed.'}
$app=Join-Path $repoRoot 'dist\LocalSave-2.8.0.exe'
$compilerArgs=@()
if($signing) {
    $signArgs=@('sign','/sha1',$CertificateThumbprint,'/s','My','/fd','SHA256','/tr',$TimestampUrl,'/td','SHA256')
    if($MachineCertificateStore){$signArgs+='/sm'}
    & $SignToolPath @signArgs $app
    if($LASTEXITCODE -ne 0){throw 'App signing failed.'}
    & $SignToolPath verify /pa /all /tw $app
    if($LASTEXITCODE -ne 0){throw 'App signature verification failed.'}
    $machine=if($MachineCertificateStore){' /sm'}else{''}
    $command='$q'+$SignToolPath+'$q sign /sha1 '+$CertificateThumbprint+' /s My /fd SHA256 /tr '+$TimestampUrl+' /td SHA256'+$machine+' $f'
    $compilerArgs=@('/DSignBuild','/SLocalSaveSign='+$command)
}
& $CompilerPath @compilerArgs (Join-Path $repoRoot 'installer\LocalSave.iss')
if ($LASTEXITCODE -ne 0) {throw 'Installer compilation failed.'}
$setup=Join-Path $repoRoot 'dist\LocalSave-Setup-2.8.0.exe'
if($signing) {
    & $SignToolPath verify /pa /all /tw $setup
    if($LASTEXITCODE -ne 0){throw 'Installer signature verification failed.'}
    foreach($file in @($app,$setup)) {
        $signature=Get-AuthenticodeSignature -LiteralPath $file
        if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $CertificateThumbprint -or $null -eq $signature.TimeStamperCertificate){throw 'Release signature, identity or timestamp verification failed.'}
    }
} else {Write-Warning 'Unsigned preview. Signing support is prepared, but no trusted certificate was supplied.'}
foreach($file in @($app,$setup)) {
    $hash=Get-FileHash -LiteralPath $file -Algorithm SHA256
    ($hash.Hash.ToLower()+'  '+[IO.Path]::GetFileName($file)) | Set-Content -LiteralPath ([IO.Path]::ChangeExtension($file,'.sha256')) -Encoding ascii
}
Write-Output "Installer: $setup"


