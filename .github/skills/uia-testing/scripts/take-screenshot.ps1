<#
.SYNOPSIS
    Capture a screenshot. Can capture full screen or a specific window region.
.PARAMETER WindowHandle
    Native window handle to capture. If omitted, captures full primary screen.
.PARAMETER OutputPath
    Path to save the PNG. Defaults to .github/skills/uia-testing/screenshots/<timestamp>.png
#>
param(
    [int]$WindowHandle = 0,
    [string]$OutputPath
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$screenshotsDir = Join-Path $PSScriptRoot '..\screenshots'
New-Item -ItemType Directory -Force $screenshotsDir | Out-Null

if (-not $OutputPath) {
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputPath = Join-Path $screenshotsDir "$timestamp.png"
}

if ($WindowHandle -gt 0) {
    # Capture specific window region using UIA to get bounds
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    $el = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$WindowHandle)
    $rect = $el.Current.BoundingRectangle
    $x = [int]$rect.X; $y = [int]$rect.Y; $w = [int]$rect.Width; $h = [int]$rect.Height
} else {
    # Full screen
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $x = $screen.X; $y = $screen.Y; $w = $screen.Width; $h = $screen.Height
}

$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($x, $y, 0, 0, (New-Object System.Drawing.Size($w, $h)))
$g.Dispose()
$bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

@{ success = $true; path = (Resolve-Path $OutputPath).Path; width = $w; height = $h } | ConvertTo-Json
