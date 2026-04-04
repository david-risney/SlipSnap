<#
.SYNOPSIS
    Open the Settings dialog by launching a second instance with --settings flag.
    The running instance handles the flag (or the new instance opens settings on startup).
#>
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process -Name SlipSnap -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { @{ error = 'SlipSnap not running' } | ConvertTo-Json; exit 1 }

# Launch a second instance with --settings flag to trigger settings dialog
try { $exe = $proc.MainModule.FileName } catch { $exe = $null }
if (-not $exe) {
    # Fallback: find the built exe
    $exe = Join-Path $PSScriptRoot '..\..\..\..\src\SlipSnap\bin\Debug\net8.0-windows10.0.22621.0\SlipSnap.exe'
    $exe = (Resolve-Path $exe -ErrorAction SilentlyContinue).Path
}

if ($exe -and (Test-Path $exe)) {
    Start-Process $exe -ArgumentList '--settings'
} else {
    @{ error = "Could not find SlipSnap.exe"; path = $exe } | ConvertTo-Json; exit 1
}

Start-Sleep -Seconds 3

# Check if settings window opened
$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
$settingsOpen = $false
foreach ($w in $windows) {
    if ($w.Current.Name -match 'Settings') { $settingsOpen = $true; break }
}

# Also check any new SlipSnap processes (the --settings instance may be a new PID)
if (-not $settingsOpen) {
    $allProcs = Get-Process -Name SlipSnap -ErrorAction SilentlyContinue
    foreach ($p in $allProcs) {
        $pCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
        $pWindows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $pCond)
        foreach ($w in $pWindows) {
            if ($w.Current.Name -match 'Settings') { $settingsOpen = $true; break }
        }
        if ($settingsOpen) { break }
    }
}

@{ success = $settingsOpen; settingsWindowOpen = $settingsOpen } | ConvertTo-Json
