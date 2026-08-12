param(
    [string]$DotnetPath = "",
    [string]$DatabaseName = "EnterpriseWms"
)

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

if ($DatabaseName -notmatch '^[A-Za-z0-9_]+$') {
    throw "DatabaseName may contain only letters, numbers, and underscores."
}
$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=$DatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtBytes = New-Object byte[] 48
$soapBytes = New-Object byte[] 24
$rng.GetBytes($jwtBytes)
$rng.GetBytes($soapBytes)
$rng.Dispose()
$jwtKey = [Convert]::ToBase64String($jwtBytes)
$soapKey = [Convert]::ToBase64String($soapBytes)

$apiSettings = @{
    ConnectionStrings = @{ WarehouseDb = $connectionString }
    Security = @{ JwtKey = $jwtKey; SoapApiKey = $soapKey }
} | ConvertTo-Json -Depth 3
$clientSettings = @{ ApiBaseAddress = "http://localhost:5080/"; SoapApiKey = $soapKey } | ConvertTo-Json
Set-Content -LiteralPath (Join-Path $projectRoot "src\EnterpriseWms.Api\appsettings.Local.json") -Value $apiSettings -Encoding UTF8
Set-Content -LiteralPath (Join-Path $projectRoot "src\EnterpriseWms.WinForms\client.local.json") -Value $clientSettings -Encoding UTF8

$localDbCommand = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
if (-not $localDbCommand) {
    $localDbCommand = Get-ChildItem "$env:ProgramFiles\Microsoft SQL Server" -Filter SqlLocalDB.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
}
if (-not $localDbCommand) {
    throw "SQL Server LocalDB was not found. Install the LocalDB component from Visual Studio Installer."
}
$localDbPath = if ($localDbCommand.Source) { $localDbCommand.Source } else { $localDbCommand.FullName }
& $localDbPath start MSSQLLocalDB | Out-Null

Push-Location $projectRoot
try {
    & $DotnetPath tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed." }

    & $DotnetPath restore "EnterpriseWms.sln" --configfile "NuGet.Config"
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

    & $DotnetPath dotnet-ef database update --project "src\EnterpriseWms.Infrastructure\EnterpriseWms.Infrastructure.csproj" --startup-project "src\EnterpriseWms.Api\EnterpriseWms.Api.csproj"
    if ($LASTEXITCODE -ne 0) { throw "EF Core database initialization failed." }

    & $DotnetPath build "EnterpriseWms.sln" --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Solution build failed." }

    Push-Location "src\EnterpriseWms.Api"
    try {
        & $DotnetPath ".\bin\Debug\net10.0\EnterpriseWms.Api.dll" --initialize-only
        if ($LASTEXITCODE -ne 0) { throw "Demo data initialization failed." }
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}

Write-Host "EnterpriseWms initialization completed successfully. Database: $DatabaseName" -ForegroundColor Green
