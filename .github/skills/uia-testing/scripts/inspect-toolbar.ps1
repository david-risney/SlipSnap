<#
.SYNOPSIS
    Inspect a SlipSnap toolbar window: buttons, position, size, opacity.
.PARAMETER WindowName
    Name/title of the window to inspect. Default: "SlipSnap Toolbar"
#>
param([string]$WindowName = "SlipSnap Toolbar")

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$proc = Get-Process -Name SlipSnap -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { @{ error = 'SlipSnap not running' } | ConvertTo-Json; exit 1 }

# Find all windows for this process
$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$allWindows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)

$toolbars = @()
foreach ($win in $allWindows) {
    if ($win.Current.Name -ne $WindowName) { continue }

    $rect = $win.Current.BoundingRectangle

    # Find all buttons
    $btnCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)
    $buttons = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCondition)

    $btnList = @()
    foreach ($btn in $buttons) {
        $btnRect = $btn.Current.BoundingRectangle
        $btnList += @{
            name       = $btn.Current.Name
            isEnabled  = $btn.Current.IsEnabled
            isOffscreen = $btn.Current.IsOffscreen
            bounds     = @{
                x = [int]$btnRect.X; y = [int]$btnRect.Y
                width = [int]$btnRect.Width; height = [int]$btnRect.Height
            }
        }
    }

    # Determine edge from position
    Add-Type -AssemblyName System.Windows.Forms
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    $edge = 'unknown'
    if ([int]$rect.X -le $screen.X + 5) { $edge = 'Left' }
    elseif ([int]($rect.X + $rect.Width) -ge $screen.Right - 5) { $edge = 'Right' }
    elseif ([int]$rect.Y -le $screen.Y + 5) { $edge = 'Top' }
    elseif ([int]($rect.Y + $rect.Height) -ge $screen.Bottom - 5) { $edge = 'Bottom' }

    $toolbars += @{
        handle     = $win.Current.NativeWindowHandle
        edge       = $edge
        bounds     = @{ x = [int]$rect.X; y = [int]$rect.Y; width = [int]$rect.Width; height = [int]$rect.Height }
        buttons    = $btnList
        buttonCount = $btnList.Count
        isOffscreen = $win.Current.IsOffscreen
    }
}

@{ toolbarCount = $toolbars.Count; toolbars = $toolbars } | ConvertTo-Json -Depth 5
