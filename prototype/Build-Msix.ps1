<#
.SYNOPSIS
    Builds SlipSnap and packages it as an MSIX for local development/testing.
.DESCRIPTION
    1. Creates a self-signed code-signing certificate (if not already present)
    2. Builds the WPF app
    3. Packages it as MSIX using makeappx
    4. Signs the MSIX with the dev certificate
.PARAMETER Install
    If specified, also installs the MSIX package after building.
#>
param(
    [switch]$Install
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot

# --- Config ---
$certSubject = "CN=SlipSnap Dev"
$certFriendlyName = "SlipSnap Dev Code Signing"
$sdkBin = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64"
$makeappx = Join-Path $sdkBin "makeappx.exe"
$signtool = Join-Path $sdkBin "signtool.exe"

# --- Ensure cert exists ---
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -eq $certSubject } | Select-Object -First 1

if (-not $cert) {
    Write-Host "Creating self-signed code-signing certificate..." -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $certSubject `
        -FriendlyName $certFriendlyName `
        -CertStoreLocation Cert:\CurrentUser\My `
        -NotAfter (Get-Date).AddYears(3)

    # Export cert — must be trusted as root CA for UIAccess validation
    $certFile = Join-Path $repoRoot "dev-cert.cer"
    Export-Certificate -Cert $cert -FilePath $certFile | Out-Null
    Write-Host "Certificate exported to $certFile" -ForegroundColor Yellow
    Write-Host "To trust it for UIAccess + MSIX sideloading, run as admin:" -ForegroundColor Yellow
    Write-Host "  Import-Certificate -FilePath '$certFile' -CertStoreLocation Cert:\LocalMachine\Root" -ForegroundColor Yellow
    Write-Host "  Import-Certificate -FilePath '$certFile' -CertStoreLocation Cert:\LocalMachine\TrustedPeople" -ForegroundColor Yellow
} else {
    Write-Host "Using existing certificate: $($cert.Thumbprint)" -ForegroundColor Green
}

$thumbprint = $cert.Thumbprint

# --- Build app ---
Write-Host "`nBuilding SlipSnap..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained -o "$repoRoot\publish" 2>&1
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# --- Prepare MSIX layout ---
$layoutDir = Join-Path $repoRoot "msix-layout"
if (Test-Path $layoutDir) { Remove-Item $layoutDir -Recurse -Force }
New-Item -ItemType Directory -Path $layoutDir | Out-Null
New-Item -ItemType Directory -Path "$layoutDir\Assets" | Out-Null

# Copy published app
Copy-Item "$repoRoot\publish\*" $layoutDir -Recurse

# Sign the exe itself — UIAccess requires Authenticode on the executable, not just on the MSIX
Write-Host "`nSigning executable for UIAccess..." -ForegroundColor Cyan
& $signtool sign /fd SHA256 /sha1 $thumbprint /td SHA256 "$layoutDir\SlipSnap.exe"
if ($LASTEXITCODE -ne 0) { throw "signtool (exe) failed" }

# Copy manifest and assets
Copy-Item "$repoRoot\Package\AppxManifest.xml" $layoutDir
Copy-Item "$repoRoot\Package\Assets\*" "$layoutDir\Assets\"

# --- Package MSIX ---
$msixPath = Join-Path $repoRoot "SlipSnap.msix"
if (Test-Path $msixPath) { Remove-Item $msixPath }

Write-Host "`nPackaging MSIX..." -ForegroundColor Cyan
& $makeappx pack /d $layoutDir /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "makeappx failed" }

# --- Sign MSIX ---
Write-Host "`nSigning MSIX..." -ForegroundColor Cyan
& $signtool sign /fd SHA256 /sha1 $thumbprint /td SHA256 $msixPath
if ($LASTEXITCODE -ne 0) { throw "signtool failed" }

Write-Host "`nMSIX package created: $msixPath" -ForegroundColor Green

# --- Optional install ---
if ($Install) {
    Write-Host "`nInstalling MSIX package..." -ForegroundColor Cyan
    Add-AppxPackage -Path $msixPath
    Write-Host "Installed! Launch SlipSnap from the Start menu." -ForegroundColor Green
}
