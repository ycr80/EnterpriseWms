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

$apiProject = Join-Path $projectRoot "src\EnterpriseWms.Api\EnterpriseWms.Api.csproj"
$clientExe = Join-Path $projectRoot "src\EnterpriseWms.WinForms\bin\Debug\net48\EnterpriseWms.WinForms.exe"
$localSettings = Join-Path $projectRoot "src\EnterpriseWms.Api\appsettings.Local.json"
if (-not (Test-Path -LiteralPath $localSettings)) {
    throw "Run scripts\setup.ps1 first."
}
if (-not (Test-Path -LiteralPath $clientExe)) {
    throw "The WinForms executable is missing. Run scripts\setup.ps1 first."
}

$api = Start-Process -FilePath $DotnetPath -ArgumentList @("run", "--project", $apiProject, "--no-build", "--no-launch-profile", "--urls", "http://localhost:5080") -PassThru -WindowStyle Hidden
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        try {
            Invoke-RestMethod "http://localhost:5080/health" | Out-Null
            $ready = $true
            break
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }
    if (-not $ready) { throw "The API did not start within 20 seconds." }
    Start-Process -FilePath $clientExe -Wait
}
finally {
    if ($api -and -not $api.HasExited) { Stop-Process -Id $api.Id }
}
