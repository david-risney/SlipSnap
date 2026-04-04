<#
.SYNOPSIS
    Find all SlipSnap windows via UI Automation. Returns JSON array of window info.
#>
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process -Name SlipSnap -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) {
    @{ error = 'SlipSnap is not running' } | ConvertTo-Json
    exit 1
}

$root = [System.Windows.Automation.AutomationElement]::RootElement
$condition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$proc.Id)

$windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $condition)

$results = @()
foreach ($win in $windows) {
    $rect = $win.Current.BoundingRectangle
    $results += @{
        name           = $win.Current.Name
        automationId   = $win.Current.AutomationId
        className      = $win.Current.ClassName
        nativeHandle   = $win.Current.NativeWindowHandle
        bounds         = @{
            x      = [int]$rect.X
            y      = [int]$rect.Y
            width  = [int]$rect.Width
            height = [int]$rect.Height
        }
        isOffscreen    = $win.Current.IsOffscreen
    }
}

@{ processId = $proc.Id; windowCount = $results.Count; windows = $results } | ConvertTo-Json -Depth 4
