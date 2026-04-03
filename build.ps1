<#
.SYNOPSIS
    SlipSnap build helper.
.DESCRIPTION
    Shortcuts for common dev tasks. Run without arguments for help.
.EXAMPLE
    .\build.ps1 build
    .\build.ps1 test
    .\build.ps1 run
    .\build.ps1 watch
    .\build.ps1 publish
    .\build.ps1 clean
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet('build', 'test', 'run', 'watch', 'clean', 'publish')]
    [string]$Command
)

$ErrorActionPreference = 'Stop'
$sln = 'src/SlipSnap.sln'
$proj = 'src/SlipSnap'
$testProj = 'tests/SlipSnap.Tests'

switch ($Command) {
    'build'   { dotnet build $sln }
    'test'    { dotnet test $testProj --verbosity minimal }
    'run'     { dotnet run --project $proj }
    'watch'   { dotnet watch test --project $testProj --verbosity minimal }
    'clean'   { dotnet clean $sln }
    'publish' { dotnet publish $proj -c Release --self-contained false -o publish }
    default   {
        Write-Host 'Usage: .\build.ps1 <command>' -ForegroundColor Cyan
        Write-Host ''
        Write-Host '  build    Build the solution'
        Write-Host '  test     Run unit tests'
        Write-Host '  run      Launch the app'
        Write-Host '  watch    Run tests in watch mode'
        Write-Host '  clean    Clean build outputs'
        Write-Host '  publish  Publish release build to ./publish'
    }
}
