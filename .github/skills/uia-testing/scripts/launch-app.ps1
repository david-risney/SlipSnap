<#
.SYNOPSIS
    Build and launch SlipSnap, return process info as JSON.
#>
param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$repoRoot = git rev-parse --show-toplevel 2>$null
if ($repoRoot) { $repoRoot = $repoRoot -replace '/', '\' } else { $repoRoot = Resolve-Path "$PSScriptRoot\..\..\..\.." }

Get-Process -Name SlipSnap -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

if (-not $NoBuild) {
    $sln = Join-Path $repoRoot 'src\SlipSnap.sln'
    dotnet build $sln --verbosity quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }
}

$exe = Get-ChildItem (Join-Path $repoRoot 'src\SlipSnap\bin\Debug') -Recurse -Filter 'SlipSnap.exe' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $exe) { Write-Error 'SlipSnap.exe not found'; exit 1 }

$proc = Start-Process -FilePath $exe.FullName -PassThru
Start-Sleep -Seconds 3

@{ success = $true; pid = $proc.Id; exe = $exe.FullName } | ConvertTo-Json
