<#
.SYNOPSIS
    Stop all SlipSnap processes.
#>
Get-Process -Name SlipSnap -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
@{ stopped = $true } | ConvertTo-Json
