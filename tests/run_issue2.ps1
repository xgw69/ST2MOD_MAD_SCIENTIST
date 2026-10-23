param(
    [Parameter(Mandatory)][string]$DotnetPath,
    [Parameter(Mandatory)][string]$GameDirectory,
    [Parameter(Mandatory)][string]$BaseLibAssembly
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$work = Join-Path $root '.validation'
$logs = Join-Path $work 'logs'
$data = Join-Path $GameDirectory 'data_sts2_windows_x86_64'
foreach ($file in @($DotnetPath, $BaseLibAssembly, (Join-Path $data 'sts2.dll'), (Join-Path $data '0Harmony.dll'))) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Required file not found: $file" }
}
New-Item -ItemType Directory -Force $logs | Out-Null
$variables = @{
    DOTNET_CLI_HOME=(Join-Path $work 'dotnet-home'); NUGET_PACKAGES=(Join-Path $work 'nuget')
    APPDATA=(Join-Path $work 'appdata'); LOCALAPPDATA=(Join-Path $work 'localappdata')
    DOTNET_GENERATE_ASPNET_CERTIFICATE='false'; DOTNET_CLI_TELEMETRY_OPTOUT='1'; DOTNET_CLI_UI_LANGUAGE='en'
}
$previous = @{}
try {
    foreach ($key in $variables.Keys) {
        $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, $variables[$key], 'Process')
    }
    $config = Join-Path $PSScriptRoot 'NuGet.Offline.Config'
    & $DotnetPath run --project (Join-Path $PSScriptRoot 'CombatRegression/CombatRegression.csproj') "-p:HarmonyAssembly=$data/0Harmony.dll" "-p:RestoreConfigFile=$config" 2>&1 |
        Tee-Object (Join-Path $logs 'contract-tests.txt')
    if ($LASTEXITCODE -ne 0) { throw "Contract tests failed: $LASTEXITCODE" }
    & $DotnetPath build (Join-Path $PSScriptRoot 'InstalledApi/InstalledApi.csproj') -c Release --configfile $config "-p:Sts2DataDir=$data" "-p:BaseLibAssembly=$BaseLibAssembly" 2>&1 |
        Tee-Object (Join-Path $logs 'installed-api-build.txt')
    if ($LASTEXITCODE -ne 0) { throw "Installed API compile failed: $LASTEXITCODE" }
    & (Join-Path $PSScriptRoot 'validate_resources.ps1') | Tee-Object (Join-Path $logs 'resources.txt')
    Write-Output "PASS: contracts, installed-assembly compile, and static resources. No game launch, deployment, or save access. Logs: $logs"
}
finally {
    foreach ($key in $previous.Keys) { [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process') }
}
