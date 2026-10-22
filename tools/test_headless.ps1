param(
    [int]$TimeoutSeconds = 120,
    [string]$GameDir = "D:\steam\steamapps\common\Slay the Spire 2",
    [switch]$Force
)

# Runs Slay the Spire 2 with the headless display server, waits for the mod's
# self-check lines, then stops ONLY the instance it started.
#
# It refuses to run while the game is already open, because the headless
# instance shares settings.save and godot.log with the real client.
$exe = Join-Path $GameDir "SlayTheSpire2.exe"
$log = Join-Path ([System.IO.Path]::GetTempPath()) ("MadScientist-headless-" + [guid]::NewGuid() + ".log")

$running = Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue
if ($running -and -not $Force) {
    Write-Host "Slay the Spire 2 is already running (pid $($running.Id -join ', '))."
    Write-Host "Close it first, or pass -Force to run a second headless instance anyway."
    exit 2
}

$start = Get-Date
$proc = Start-Process -FilePath $exe -ArgumentList @("--headless", "--log-file", "`"$log`"") -WorkingDirectory $GameDir -PassThru
Write-Host "headless pid=$($proc.Id), waiting for startup..."

$found = $false
while (((Get-Date) - $start).TotalSeconds -lt $TimeoutSeconds) {
    Start-Sleep -Seconds 3
    if (Test-Path $log) {
        $line = Select-String -Path $log -Pattern "\[MadScientist\] \[Diag\] OK startup checks complete" -ErrorAction SilentlyContinue
        if ($line) { $found = $true; break }
    }
    if ($proc.HasExited) { break }
}
Start-Sleep -Seconds 2
if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 1

Write-Host "--- mod log ---"
Select-String -Path $log -Pattern "\[MadScientist\]|Skipping loading mod MadScientist" |
    Select-Object -ExpandProperty Line
Write-Host "--- errors mentioning MadScientist ---"
Select-String -Path $log -Pattern "MadScientist" |
    Where-Object { $_.Line -match "ERROR|Exception|failed" } |
    Select-Object -ExpandProperty Line

if (-not $found) { Write-Host "FAIL: startup checks did not complete successfully. Log: $log"; exit 1 }
$errors = Select-String -Path $log -Pattern "MadScientist" | Where-Object { $_.Line -match "ERROR|Exception|failed|MISSING" }
if ($errors) { Write-Host "FAIL: errors in startup log: $log"; exit 1 }
Write-Host "PASS: startup/resource checks only; gameplay regression tests must be run separately. Log: $log"
exit 0
