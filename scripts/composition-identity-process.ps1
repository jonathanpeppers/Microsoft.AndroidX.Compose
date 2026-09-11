param(
    [Parameter(Mandatory = $true)][string] $Adb,
    [Parameter(Mandatory = $true)][string] $Serial
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
Invoke-Adb shell am start -W -n $component --es runId $runId | Out-Null
$initial = Wait-Snapshot { param($s) @($s.Values.PSObject.Properties).Count -eq 8 } 'initial loop composition'
Invoke-Adb shell am start -W -n $component --es command mutate | Out-Null
$mutated = Wait-Snapshot {
    param($s)
    $s.Phase -eq 1 -and $s.Values.permanent -eq 1100 -and
        $s.Values.'loop-0' -eq 1200 -and $s.Values.'loop-1' -eq 1201 -and
        $s.Values.'loop-2' -eq 1202 -and @($s.Values.PSObject.Properties).Count -eq 4
} 'distinct saved values after removing preceding calls'

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
        $s.TaskId -eq $initial.TaskId -and @($s.Values.PSObject.Properties).Count -eq 4
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
"PASS: task $($restored.TaskId), PID $($initial.ProcessId) -> $($restored.ProcessId); all four saveable values restored, ordinary state reset."
