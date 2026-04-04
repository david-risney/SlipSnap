<#
.SYNOPSIS
    Set a control value in the Settings window.
.PARAMETER ControlName
    The name/content of the control to modify.
.PARAMETER ControlType
    Type: checkbox, radio, slider
.PARAMETER Value
    For checkbox: true/false. For radio: ignored (selects it). For slider: numeric value.
#>
param(
    [Parameter(Mandatory)][string]$ControlName,
    [Parameter(Mandatory)][ValidateSet('checkbox','radio','slider')][string]$ControlType,
    [string]$Value
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$proc = Get-Process -Name SlipSnap -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { @{ error = 'SlipSnap not running' } | ConvertTo-Json; exit 1 }

$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)

$settingsWin = $null
foreach ($w in $windows) {
    if ($w.Current.Name -match 'Settings') { $settingsWin = $w; break }
}
if (-not $settingsWin) { @{ error = 'Settings window not found' } | ConvertTo-Json; exit 1 }

$nameCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, $ControlName)

switch ($ControlType) {
    'checkbox' {
        $typeCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::CheckBox)
        $andCond = New-Object System.Windows.Automation.AndCondition($nameCond, $typeCond)
        $el = $settingsWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $andCond)
        if ($el) {
            $toggle = $el.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
            $desired = [bool]::Parse($Value)
            $current = $toggle.Current.ToggleState -eq 'On'
            if ($current -ne $desired) { $toggle.Toggle() }
            @{ success = $true; control = $ControlName; newValue = $desired } | ConvertTo-Json
        } else {
            @{ error = "Checkbox '$ControlName' not found" } | ConvertTo-Json
        }
    }
    'radio' {
        $typeCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::RadioButton)
        $andCond = New-Object System.Windows.Automation.AndCondition($nameCond, $typeCond)
        $el = $settingsWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $andCond)
        if ($el) {
            $sel = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
            $sel.Select()
            @{ success = $true; control = $ControlName; selected = $true } | ConvertTo-Json
        } else {
            @{ error = "Radio button '$ControlName' not found" } | ConvertTo-Json
        }
    }
    'slider' {
        $typeCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Slider)
        $el = $settingsWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $typeCond)
        if ($el) {
            $range = $el.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern)
            $range.SetValue([double]$Value)
            @{ success = $true; control = 'slider'; newValue = [double]$Value } | ConvertTo-Json
        } else {
            @{ error = 'Slider not found' } | ConvertTo-Json
        }
    }
}
