<#
.SYNOPSIS
    Click a button on a SlipSnap toolbar by its AutomationProperties.Name.
.PARAMETER ButtonName
    The automation name of the button (e.g., "Start Menu", "Next Desktop", "Grip")
#>
param(
    [Parameter(Mandatory)][string]$ButtonName
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$proc = Get-Process -Name SlipSnap -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { @{ error = 'SlipSnap not running' } | ConvertTo-Json; exit 1 }

$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$allWindows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)

$found = $false
foreach ($win in $allWindows) {
    $nameCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $ButtonName)
    $element = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $nameCond)

    if ($element) {
        $pattern = $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        if ($pattern) {
            $pattern.Invoke()
            $found = $true
            break
        }
    }
}

@{ success = $found; button = $ButtonName } | ConvertTo-Json
