param(
    [Parameter(Mandatory = $true)]
    [string] $Serial,
    [string] $Adb = "adb"
)

$ErrorActionPreference = "Stop"
$package = "net.compose.devicetests"
$component = "$package/net.compose.devicetests.SaveableProcessTestActivity"
$runId = [Guid]::NewGuid().ToString("N")

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
        $json = Invoke-Adb shell "run-as $package sh -c 'test ! -f files/saveable-process.json || cat files/saveable-process.json'"
        if ($json) {
            $snapshot = $json | ConvertFrom-Json
            if ($snapshot.RunId -eq $runId -and (& $Predicate $snapshot)) {
                return $snapshot
            }
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Timed out waiting for saveable process probe. Last snapshot: $json"
}

# Install once before running. Never reinstall, force-stop, or clear app data
# between these phases: the same APK and Android's saved task are the test.
Invoke-Adb shell am start -W -n $component --es runId $runId | Write-Host
$initial = Wait-Snapshot { param($s) @($s.Values.PSObject.Properties).Count -eq 10 }
if ($initial.Restored -or @($initial.Values.PSObject.Properties | Where-Object Value -ne 0).Count) {
    throw "Expected a fresh task with ten zero-valued probes."
}

Invoke-Adb shell am start -W -n $component --es command mutate | Write-Host
$changed = Wait-Snapshot {
    param($s)
    $values = @($s.Values.PSObject.Properties | ForEach-Object Value | Sort-Object)
    ($values -join ",") -eq ((101..110) -join ",")
}
if ($changed.ProcessId -ne $initial.ProcessId -or $changed.TaskId -ne $initial.TaskId) {
    throw "Mutation unexpectedly replaced the process or task."
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
    throw "Android did not kill the background test package; no cross-process result can be claimed."
}

# Bring the existing task forward, without a new intent or a fresh activity.
Invoke-Adb shell am task focus $saved.TaskId | Write-Host
$restored = Wait-Snapshot {
    param($s)
    $s.ProcessId -ne $saved.ProcessId -and @($s.Values.PSObject.Properties).Count -eq 10
}
if (-not $restored.Restored -or $restored.PreviousProcessId -ne $saved.ProcessId -or
    $restored.TaskId -ne $saved.TaskId) {
    throw "The probe did not restore Android's saved task in a genuinely new process."
}
foreach ($property in $changed.Values.PSObject.Properties) {
    if ($restored.Values.($property.Name) -ne $property.Value) {
        throw "Saveable value '$($property.Name)' was lost: expected $($property.Value), got $($restored.Values.($property.Name))."
    }
}
$restored | ConvertTo-Json -Depth 4 | Write-Host
Invoke-Adb shell am start -W -n $component --es command finish | Write-Host
Write-Host "PASS: all ten probes restored in a new process using the same APK and saved task."
