param(
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$projects = @(
    'src\Microsoft.AndroidX.Compose.DeviceTests\Microsoft.AndroidX.Compose.DeviceTests.csproj'
    'samples\Reply\Reply.csproj'
)
$assemblies = @(
    'src\Microsoft.AndroidX.Compose.DeviceTests\bin\Debug\net10.0-android\Microsoft.AndroidX.Compose.DeviceTests.dll'
    'samples\Reply\bin\Debug\net10.0-android\Reply.dll'
)

Get-Command ilspycmd -ErrorAction Stop | Out-Null
if (-not $SkipBuild) {
    foreach ($project in $projects) {
        & dotnet build (Join-Path $root $project) -c Debug -v:quiet
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed: $project"
        }
    }
}

foreach ($assembly in $assemblies) {
    $path = Join-Path $root $assembly
    $source = & ilspycmd --disable-updatecheck -t AndroidX.Compose.Samples.Reply.ReplyInboxScreen $path
    if ($LASTEXITCODE -ne 0) {
        throw "Could not decompile ReplyInboxScreen in $assembly"
    }
    $numericDpCall = 'ModifierExtensions\.Padding\(\s*(?:AndroidX\.Compose\.)?Modifier\.Align\(Alignment\.BottomEnd\),\s*(?:\(Dp\))?16\)'
    if (($source -join "`n") -notmatch $numericDpCall) {
        throw "The numeric inbox FAB padding did not compile to the Dp extension in $assembly"
    }

    $il = & ilspycmd --disable-updatecheck -il $path
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect IL for $assembly"
    }
    if (($il -join "`n") -match 'Modifier::Padding\(native int\)') {
        throw "A Padding(IntPtr) call remains in $assembly"
    }
    Write-Output "PASS: numeric inbox padding uses Dp in $assembly"
}
