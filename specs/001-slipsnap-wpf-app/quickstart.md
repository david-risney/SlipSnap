# Quickstart: SlipSnap WPF Floating Toolbar

**Feature**: 001-slipsnap-wpf-app  
**Date**: 2026-04-02

## Prerequisites

- .NET 8 SDK (with Windows desktop workload)
- Windows 10 22H2+ or Windows 11
- Windows SDK (for `signtool.exe` — UIAccess signed deployment only)
- Visual Studio 2022 or VS Code with C# Dev Kit

## Create the Solution

```powershell
# From repository root
dotnet new sln -n SlipSnap -o src
dotnet new wpf -n SlipSnap -o src/SlipSnap --framework net8.0-windows10.0.22621.0
dotnet sln src/SlipSnap.sln add src/SlipSnap/SlipSnap.csproj

# Unit test project
dotnet new xunit -n SlipSnap.Tests -o tests/SlipSnap.Tests --framework net8.0-windows10.0.22621.0
dotnet sln src/SlipSnap.sln add tests/SlipSnap.Tests/SlipSnap.Tests.csproj
dotnet add tests/SlipSnap.Tests reference src/SlipSnap/SlipSnap.csproj

# E2E test project
dotnet new xunit -n SlipSnap.E2ETests -o tests/SlipSnap.E2ETests --framework net8.0-windows10.0.22621.0
dotnet sln src/SlipSnap.sln add tests/SlipSnap.E2ETests/SlipSnap.E2ETests.csproj
dotnet add tests/SlipSnap.E2ETests reference src/SlipSnap/SlipSnap.csproj
```

## Add NuGet Packages

```powershell
# Main app
dotnet add src/SlipSnap package WPF-UI --version 4.2.0
dotnet add src/SlipSnap package Slions.VirtualDesktop.WPF --version 6.9.2
dotnet add src/SlipSnap package Serilog.Sinks.File
dotnet add src/SlipSnap package Serilog.Extensions.Logging

# Unit tests
dotnet add tests/SlipSnap.Tests package FluentAssertions

# E2E tests
dotnet add tests/SlipSnap.E2ETests package FlaUI.UIA3 --version 5.0.0
dotnet add tests/SlipSnap.E2ETests package FluentAssertions
```

## Configure Project

In `src/SlipSnap/SlipSnap.csproj`:
```xml
<PropertyGroup>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ApplicationManifest>app.manifest</ApplicationManifest>
</PropertyGroup>
```

## Build & Run (Development)

```powershell
# Build
dotnet build src/SlipSnap.sln

# Run (without UIAccess - toolbar works but won't appear above fullscreen apps)
dotnet run --project src/SlipSnap

# Run unit tests
dotnet test tests/SlipSnap.Tests

# Run E2E tests (requires app to be launchable)
dotnet test tests/SlipSnap.E2ETests
```

## Signed Deployment (UIAccess)

```powershell
# Publish self-contained
dotnet publish src/SlipSnap -c Release -r win-x64 --self-contained -o publish

# Sign exe (requires code-signing cert — see prototype/README.md for cert setup)
$thumbprint = (Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq "CN=SlipSnap Dev" }).Thumbprint
& "signtool.exe" sign /fd SHA256 /sha1 $thumbprint publish\SlipSnap.exe

# Deploy to Program Files (triggers UAC)
Start-Process pwsh -Verb RunAs -ArgumentList '-Command',
    "Copy-Item 'publish\*' 'C:\Program Files\SlipSnap\' -Recurse -Force"

# Launch with UIAccess
& "C:\Program Files\SlipSnap\SlipSnap.exe"
```

## Verify

1. Toolbar appears on the left edge of the monitor (default configuration).
2. Right-click tray icon → Settings opens the modern Fluent Design dialog.
3. Click Start Menu button → Windows Start Menu opens.
4. Click Next Desktop button → host virtual desktop switches.
5. All tests pass: `dotnet test src/SlipSnap.sln`
