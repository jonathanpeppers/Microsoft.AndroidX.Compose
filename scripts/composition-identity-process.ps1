param(
    [Parameter(Mandatory = $true)][string] $Adb,
    [Parameter(Mandatory = $true)][string] $Serial,
    [ValidateSet('loop', 'selective-nested')][string] $Scenario = 'loop'
)

$ErrorActionPreference = 'Stop'
$package = 'net.compose.devicetests'
$component = "$package/net.compose.devicetests.CompositionIdentityProcessActivity"
$runId = [guid]::NewGuid().ToString()

function Invoke-Adb {
    $output = & $Adb -s $Serial @args
    if ($LASTEXITCODE -ne 0) { throw "adb failed: $args`n$output" }
    $output
}

function Wait-Snapshot([scriptblock] $Condition, [string] $Description) {
    $deadline = (Get-Date).AddSeconds(30)
    do {
        $json = & $Adb -s $Serial shell run-as $package cat files/composition-identity-process.json 2>$null
        if ($LASTEXITCODE -eq 0) {
            $snapshot = $json | ConvertFrom-Json
            if ($snapshot.RunId -eq $runId -and (& $Condition $snapshot)) { return $snapshot }
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    throw "Timed out: $Description. Last snapshot: $json"
}

# Requires an exclusive device lease and an already-installed, self-contained DeviceTests APK.
Invoke-Adb shell am start -W -n $component --es runId $runId --es scenario $Scenario | Out-Null
$initialCount = if ($Scenario -eq 'loop') { 8 } else { 3 }
$finalCount = if ($Scenario -eq 'loop') { 4 } else { 5 }
$initial = Wait-Snapshot { param($s) @($s.Values.PSObject.Properties).Count -eq $initialCount } 'initial composition'
Invoke-Adb shell am start -W -n $component --es command mutate | Out-Null
if ($Scenario -eq 'loop') {
    $mutated = Wait-Snapshot {
        param($s)
        $s.Phase -eq 1 -and $s.Values.permanent -eq 1100 -and
            $s.Values.'loop-0' -eq 1200 -and $s.Values.'loop-1' -eq 1201 -and
            $s.Values.'loop-2' -eq 1202 -and @($s.Values.PSObject.Properties).Count -eq 4
    } 'distinct saved values after removing preceding calls'
} else {
    $inserted = Wait-Snapshot {
        param($s)
        $s.Phase -eq 15 -and $s.Values.'loop-1' -eq 1201 -and
            $s.Values.'loop-3' -eq 1203 -and @($s.Values.PSObject.Properties).Count -eq 5
    } 'earlier children inserted under both surviving nested parents'
    Invoke-Adb shell am start -W -n $component --es command seed-added | Out-Null
    $mutated = Wait-Snapshot {
        param($s)
        $s.Values.'loop-0' -eq 1200 -and $s.Values.'loop-1' -eq 1201 -and
            $s.Values.'loop-2' -eq 1202 -and $s.Values.'loop-3' -eq 1203
    } 'distinct saved values after selective insertion'
}

Invoke-Adb shell input keyevent KEYCODE_HOME | Out-Null
$saved = Wait-Snapshot { param($s) $s.Saved } 'Android OnSaveInstanceState'
Invoke-Adb shell am kill $package | Out-Null
$deadline = (Get-Date).AddSeconds(15)
do {
    $process = Invoke-Adb shell "pidof $package || true"
    if (-not $process) { break }
    Start-Sleep -Milliseconds 200
} while ((Get-Date) -lt $deadline)
if ($process) { throw "Background package did not exit; refusing to force-stop or clear task state." }

Invoke-Adb shell am task focus $initial.TaskId | Out-Null
$restored = Wait-Snapshot {
    param($s)
    $s.Restored -and $s.ProcessId -ne $initial.ProcessId -and
        $s.PreviousProcessId -eq $initial.ProcessId -and
        $s.TaskId -eq $initial.TaskId -and @($s.Values.PSObject.Properties).Count -eq $finalCount
} 'original task restored in a new process'
foreach ($property in $mutated.Values.PSObject.Properties) {
    if ($restored.Values.($property.Name) -ne $property.Value) {
        throw "Saveable state changed for $($property.Name): $($restored | ConvertTo-Json -Compress)"
    }
    if ($restored.OrdinaryValues.($property.Name) -ne 0) {
        throw "Ordinary state unexpectedly survived process death for $($property.Name)."
    }
}
Invoke-Adb shell am start -W -n $component --es command finish | Out-Null
"PASS ($Scenario): task $($restored.TaskId), PID $($initial.ProcessId) -> $($restored.ProcessId); all $finalCount saveable values restored, ordinary state reset."
