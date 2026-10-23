# Issue 2 checks on Windows

Use PowerShell 7, an existing .NET 9 SDK, and locally installed game/BaseLib assemblies:

```powershell
./tests/run_issue2.ps1 `
  -DotnetPath 'C:/path/to/dotnet.exe' `
  -GameDirectory 'C:/path/to/Slay the Spire 2' `
  -BaseLibAssembly 'C:/path/to/BaseLib.dll'
python tests/validate_resources.py
```

The offline runner downloads nothing, restores process environment variables on exit, and writes local build outputs/logs under .validation and test bin/obj directories. It does not launch or install a mod. Results: **84 contract assertions**, **655 static resource assertions**, and **0 compiler errors / 18 existing nullable warnings** against installed public-beta v0.111.0 and BaseLib 3.4.7. The original Python validator separately passed **4/4** checks.

Contract tests link actual production implementations against narrow game doubles and real Harmony. tests/InstalledApi compiles all production C# against installed game, Harmony, GodotSharp and BaseLib assemblies. It omits Godot generators, mod analyzers and packing. Its DLL is a compile-check artifact, not an installable release.

## Full DLL/PCK build and checks

The complete MadScientist.csproj build now succeeds with **exit 0, 0 errors / 20 warnings**, including the Godot SDK, localization analyzer, PCK packer and scene appender. The two STS003 fusion-prefix recommendations remain visible to preserve the original stored model IDs; the other 18 warnings concern nullability.

The successful command was run from the repository root using an existing .NET 9.0.316 SDK, Python 3.13.14, and all declared package versions. Set the following path variables to your installed tools and assemblies, then run in a disposable PowerShell session:

```powershell
$dotnet = 'C:/path/to/dotnet.exe'
$python = 'C:/path/to/python.exe'
$game = 'C:/path/to/Slay the Spire 2'
$baseLib = 'C:/path/to/BaseLib.dll'
$env:DOTNET_CLI_HOME = "$PWD/.validation/dotnet-home"
$env:NUGET_PACKAGES = "$PWD/.validation/nuget"
$env:APPDATA = "$PWD/.validation/appdata"
$env:LOCALAPPDATA = "$PWD/.validation/localappdata"
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
# First restore declared packages from the official nuget.org source if uncached.
& $dotnet restore MadScientist.csproj "-p:Sts2Path=$game" --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE) { throw 'Restore failed' }
& $dotnet build MadScientist.csproj -c Release --no-restore `
  "-p:Sts2Path=$game" "-p:ModsPath=$PWD/.validation/deploy" "-p:PythonPath=$python"
if ($LASTEXITCODE) { throw 'Full build failed' }
& $python tests/validate_release.py .validation/deploy/MadScientist
if ($LASTEXITCODE) { throw 'Pack integrity failed' }
& $dotnet run --project tests/RuntimeLocalization `
  "-p:RestoreConfigFile=$PWD/tests/NuGet.Offline.Config" -- `
  "$game/data_sts2_windows_x86_64" $baseLib `
  .validation/deploy/MadScientist/MadScientist.dll MadScientist/localization
if ($LASTEXITCODE) { throw 'Runtime localization checks failed' }
```

Release integrity passed: **430 entry checksums, 206 image import mappings, 16 source resources, 2 scenes**, plus DLL header and manifest equality. Managed runtime checks passed **47 assertions**, invoking the actual installed game ID resolver and BaseLib prefix postfix, checking both languages and the native active/spent smart-description keys. The checker constructs no game session and does not exercise native saves. Detailed results and sanitized logs are in docs/issue-2-validation.md and docs/issue-2-evidence/.

The 63-card matrix explicitly leaves game play, upgrade, copy/fusion, save/reload and multiplayer unverified. Maintainers must perform those checks. Do not run the existing tools/test_headless.ps1 on official user data; use verified disposable user-data and Steam-cloud isolation. Multiplayer needs a second client. Managed key checks and pack integrity do not demonstrate correct rendered localization or layout.
