<#
.SYNOPSIS
    Inspect all controls in the SlipSnap Settings window. Returns structured JSON.
#>
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$procs = Get-Process -Name SlipSnap -ErrorAction SilentlyContinue
if (-not $procs) { @{ error = 'SlipSnap not running' } | ConvertTo-Json; exit 1 }

$settingsWin = $null
foreach ($proc in $procs) {
    $procCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $windows) {
        if ($w.Current.Name -match 'Settings') { $settingsWin = $w; break }
    }
    if ($settingsWin) { break }
}

if (-not $settingsWin) {
    @{ error = 'Settings window not found. Open it first with open-settings.ps1' } | ConvertTo-Json
    exit 1
}

$rect = $settingsWin.Current.BoundingRectangle

# Find all checkboxes
$checkCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::CheckBox)
$checkboxes = $settingsWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, $checkCond)

$checks = @()
foreach ($cb in $checkboxes) {
    $toggle = $cb.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $checks += @{
        name      = $cb.Current.Name
        isChecked = $toggle.Current.ToggleState.ToString()
        isEnabled = $cb.Current.IsEnabled
    }
}

# Find all radio buttons
$radioCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::RadioButton)
$radios = $settingsWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, $radioCond)

$radioList = @()
foreach ($rb in $radios) {
    $sel = $rb.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $radioList += @{
        name       = $rb.Current.Name
        isSelected = $sel.Current.IsSelected
    }
}

# Find sliders
$sliderCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Slider)
$sliders = $settingsWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, $sliderCond)

$sliderList = @()
foreach ($sl in $sliders) {
    $range = $sl.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern)
    $sliderList += @{
        name    = $sl.Current.Name
        value   = $range.Current.Value
        minimum = $range.Current.Minimum
        maximum = $range.Current.Maximum
    }
}

@{
    windowBounds = @{ x = [int]$rect.X; y = [int]$rect.Y; width = [int]$rect.Width; height = [int]$rect.Height }
    checkboxes   = $checks
    radioButtons = $radioList
    sliders      = $sliderList
} | ConvertTo-Json -Depth 4
