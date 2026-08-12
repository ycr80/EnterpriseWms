param([string]$DotnetPath = "")

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path $PSScriptRoot -Parent

if (-not $DotnetPath) {
    $workspaceSdk = Join-Path $projectRoot "..\..\.tools\dotnet10\dotnet.exe"
    if (Test-Path -LiteralPath $workspaceSdk) {
        $DotnetPath = (Resolve-Path -LiteralPath $workspaceSdk).Path
    }
    else {
        $DotnetPath = (Get-Command dotnet.exe -ErrorAction Stop).Source
    }
}

$version = & $DotnetPath --version
if ($LASTEXITCODE -ne 0 -or [int]($version.Split('.')[0]) -lt 10) {
    throw ".NET 10 SDK is required. Current version: $version"
}

Push-Location $projectRoot
try {
    & $DotnetPath restore "EnterpriseWms.sln" --configfile "NuGet.Config"
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

    & $DotnetPath build "EnterpriseWms.sln" --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Solution build failed." }

    & $DotnetPath test "EnterpriseWms.sln" --no-build
    if ($LASTEXITCODE -ne 0) { throw "Test suite failed." }
}
finally {
    Pop-Location
}

Write-Host "EnterpriseWms build and tests completed successfully." -ForegroundColor Green
