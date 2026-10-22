<#
.SYNOPSIS
    One-time setup: creates a local self-signed code-signing certificate for Sages of Ozvaram
    dev builds and trusts it, so Smart App Control stops blocking freshly-built, unsigned
    SagesOfOzvaram.exe binaries.

.DESCRIPTION
    Run this ONCE per machine (per Windows user account, really - it installs into the CURRENT
    USER's certificate stores, not the machine-wide ones, so it needs no admin elevation).

    1. Creates a self-signed Code Signing certificate in Cert:\CurrentUser\My (if one with the
       expected Subject doesn't already exist there) - the private key never leaves that store;
       nothing is ever written to disk as a .pfx.
    2. Exports just the PUBLIC half to a temporary .cer file.
    3. Imports that .cer into Cert:\CurrentUser\Root (Trusted Root CAs) and
       Cert:\CurrentUser\TrustedPublisher (Trusted Publishers) via certutil, which - unlike
       PowerShell's own Import-Certificate - doesn't pop a GUI confirmation dialog, so this runs
       cleanly from a script/CI with no one at the keyboard to click through it.
    4. Deletes the temporary .cer file.

    After this, Scripts\Sign-Build.ps1 (wired into SagesOfOzvaram.csproj as a post-build step -
    see the SignExecutable target) signs the built .exe automatically on every `dotnet build`/
    `dotnet run`, and Windows will recognize it as coming from a trusted, known publisher instead
    of an unknown one.
#>

$ErrorActionPreference = 'Stop'

$certSubject = 'CN=Insomnium-Eye Dev Code Signing'
$friendlyName = 'Insomnium-Eye Dev Code Signing'

$existing = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $certSubject } |
    Select-Object -First 1

if ($existing) {
    Write-Host "Dev code-signing certificate already exists (thumbprint $($existing.Thumbprint)) - reusing it."
    $cert = $existing
} else {
    Write-Host "Creating a new self-signed code-signing certificate ($certSubject)..."
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $certSubject `
        -FriendlyName $friendlyName `
        -CertStoreLocation Cert:\CurrentUser\My `
        -KeyUsage DigitalSignature `
        -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddYears(10)
    Write-Host "Created certificate with thumbprint $($cert.Thumbprint)."
}

$tempCerPath = Join-Path $env:TEMP "InsomniumEyeDevCodeSigning.cer"
Export-Certificate -Cert $cert -FilePath $tempCerPath | Out-Null

try {
    Write-Host "Trusting the certificate's public key (Root + TrustedPublisher, current user only)..."
    certutil -user -addstore -f "Root" $tempCerPath | Out-Null
    certutil -user -addstore -f "TrustedPublisher" $tempCerPath | Out-Null
} finally {
    Remove-Item $tempCerPath -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Done. The dev certificate is installed and trusted for this Windows user account."
Write-Host "From now on, every 'dotnet build'/'dotnet run' signs SagesOfOzvaram.exe automatically"
Write-Host "(see the SignExecutable target in SagesOfOzvaram.csproj / Scripts\Sign-Build.ps1)."
