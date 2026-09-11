param(
    [Parameter(Mandatory = $true)]
    [string] $Serial,
    [string] $Adb = "adb"
)

$ErrorActionPreference = "Stop"
$package = "net.compose.devicetests"
$component = "$package/net.compose.devicetests.NavSaveableProcessTestActivity"

function Invoke-Adb {
    $output = & $Adb -s $Serial @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "adb $args failed: $output"
    }
    return ($output -join "`n")
}

function Wait-Snapshot([scriptblock] $Predicate) {
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        $json = Invoke-Adb shell "run-as $package sh -c 'test ! -f files/nav-saveable-process.json || cat files/nav-saveable-process.json'"
        if ($json) {
            $snapshot = $json | ConvertFrom-Json
            if ($snapshot.RunId -eq $runId -and (& $Predicate $snapshot)) {
                return $snapshot
            }
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Timed out waiting for navigation process probe. Last snapshot: $json"
}

# Install once before running; each scenario restores the same APK and task.
# Do not reinstall, force-stop, or clear data between save and restore.
foreach ($factory in @("false", "true")) {
    $runId = [Guid]::NewGuid().ToString("N")
    Invoke-Adb shell am start -W -n $component --es runId $runId --ez factory $factory | Write-Host
    $initial = Wait-Snapshot { param($s) $null -ne $s.Value }
    if ($initial.Restored -or $initial.Value -ne 0 -or $initial.Label -ne "Account 0" -or
        $initial.Factory -ne [bool]::Parse($factory)) {
        throw "Expected fresh navigation content and a zero-valued saveable state."
    }

    Invoke-Adb shell am start -W -n $component --es command update | Write-Host
    $changed = Wait-Snapshot { param($s) $s.Value -eq 101 -and $s.Label -eq "Account 1" }
    if ($changed.ProcessId -ne $initial.ProcessId -or $changed.TaskId -ne $initial.TaskId) {
        throw "Replacing navigation content unexpectedly replaced the process or task."
    }

    Invoke-Adb shell input keyevent KEYCODE_HOME | Write-Host
    $saved = Wait-Snapshot { param($s) $s.Saved }
    Invoke-Adb shell am kill $package | Write-Host
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        $processes = Invoke-Adb shell "pidof $package || true"
        if (-not $processes.Trim()) { break }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    if ($processes.Trim()) {
        throw "Android did not kill the background test package; no process-restoration result can be claimed."
    }

    Invoke-Adb shell am task focus $saved.TaskId | Write-Host
    $restored = Wait-Snapshot { param($s) $s.ProcessId -ne $saved.ProcessId -and $null -ne $s.Value }
    if (-not $restored.Restored -or $restored.PreviousProcessId -ne $saved.ProcessId -or
        $restored.TaskId -ne $saved.TaskId -or $restored.Factory -ne $initial.Factory -or
        $restored.Value -ne 101 -or $restored.Label -ne "Account 1") {
        throw "Updated navigation content lost its saveable state in the restored task: $($restored | ConvertTo-Json -Compress)"
    }
    $restored | ConvertTo-Json | Write-Host
    Invoke-Adb shell am start -W -n $component --es command finish | Write-Host
    Write-Host "PASS: updated navigation content (factory=$factory) restored state 101 in a new process."
}
