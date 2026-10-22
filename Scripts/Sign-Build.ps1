<#
.SYNOPSIS
    Signs the built binaries with the local dev code-signing certificate, if one's been set up
    (see Setup-DevCodeSigning.ps1). Called automatically after every build - see the
    SignExecutable target in SagesOfOzvaram.csproj.

.DESCRIPTION
    Deliberately non-fatal: if Setup-DevCodeSigning.ps1 hasn't been run on this machine yet (no
    matching cert in Cert:\CurrentUser\My), this just prints a warning and exits 0 rather than
    failing the build - so a fresh clone/CI machine without the dev cert still builds fine, it
    just won't get Smart App Control's more lenient treatment of signed binaries until someone
    runs the setup script once.

.PARAMETER Path
    A single file path to sign (the MSBuild target calls this script once per file - both the
    apphost .exe AND the managed .dll it loads need signing, since Smart App Control evaluates
    each one it loads independently; signing only the .exe left the .dll still getting blocked.
    Kept as one path per invocation rather than an array parameter: `powershell -File` doesn't
    reliably bind multiple raw command-line tokens into a single []-typed parameter the way an
    in-process array literal would).
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$certSubject = 'CN=Insomnium-Eye Dev Code Signing'

if (-not (Test-Path $Path)) {
    Write-Warning "Sign-Build.ps1: '$Path' doesn't exist - nothing to sign."
    exit 0
}

$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $certSubject } |
    Select-Object -First 1

if (-not $cert) {
    Write-Warning "Sign-Build.ps1: no dev code-signing certificate found. Run Scripts\Setup-DevCodeSigning.ps1 once to create and trust one - until then, the build is unsigned and Smart App Control may keep blocking it."
    exit 0
}

$result = Set-AuthenticodeSignature -FilePath $Path -Certificate $cert

if ($result.Status -ne 'Valid') {
    Write-Warning "Sign-Build.ps1: signing '$Path' finished with status '$($result.Status)' ($($result.StatusMessage)) - left as-is."
    exit 0
}

Write-Host "Signed $(Split-Path -Leaf $Path) with the Insomnium-Eye dev certificate."
