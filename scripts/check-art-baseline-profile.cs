// Run from the repository root:
// dotnet run scripts/check-art-baseline-profile.cs -- app.apk [app.aab|package.nupkg ...]

using System.IO.Compression;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("Usage: dotnet run scripts/check-art-baseline-profile.cs -- <apk|aab|nupkg> [other files ...]");
    return args.Length == 0 ? 2 : 0;
}

bool passed = true;
foreach (string path in args)
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"{path}: file not found");
        passed = false;
        continue;
    }

    using ZipArchive archive = ZipFile.OpenRead(path);
    bool current = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".apk" => CheckBinaryProfiles(archive, "assets/dexopt/", requireStored: true),
        ".aab" => CheckBinaryProfiles(
            archive,
            "BUNDLE-METADATA/com.android.tools.build.profiles/",
            requireStored: false),
        ".nupkg" => CheckPackage(archive),
        _ => Unsupported(path),
    };
    Console.WriteLine($"{path}: {(current ? "passed" : "failed")}");
    passed &= current;
}

return passed ? 0 : 1;

static bool CheckBinaryProfiles(ZipArchive archive, string directory, bool requireStored)
{
    bool profile = CheckBinaryProfile(
        archive,
        directory + "baseline.prof",
        "pro\0"u8,
        requireStored);
    bool metadata = CheckBinaryProfile(
        archive,
        directory + "baseline.profm",
        "prm\0"u8,
        requireStored);
    return profile && metadata;
}

static bool CheckBinaryProfile(
    ZipArchive archive,
    string entryName,
    ReadOnlySpan<byte> magic,
    bool requireStored)
{
    ZipArchiveEntry? entry = archive.GetEntry(entryName);
    if (entry is null)
    {
        Console.Error.WriteLine($"  missing {entryName}");
        return false;
    }
    if (requireStored && entry.CompressedLength != entry.Length)
    {
        Console.Error.WriteLine(
            $"  {entryName} is compressed ({entry.CompressedLength} bytes from {entry.Length})");
        return false;
    }

    Span<byte> actual = stackalloc byte[4];
    using Stream stream = entry.Open();
    if (stream.Read(actual) != actual.Length || !actual.SequenceEqual(magic))
    {
        Console.Error.WriteLine($"  {entryName} has invalid profile magic");
        return false;
    }
    return true;
}

static bool CheckPackage(ZipArchive archive)
{
    string[] required =
    [
        "buildTransitive/Microsoft.AndroidX.Compose.props",
        "buildTransitive/Microsoft.AndroidX.Compose.targets",
        "buildTransitive/Microsoft.AndroidX.Compose.pro",
        "buildTransitive/Microsoft.AndroidX.Compose.baseline-prof.txt",
    ];
    bool passed = true;
    foreach (string entryName in required)
    {
        if (archive.GetEntry(entryName) is not null)
            continue;
        Console.Error.WriteLine($"  missing {entryName}");
        passed = false;
    }

    ZipArchiveEntry? profile = archive.GetEntry(required[^1]);
    if (profile is null)
        return false;
    using var reader = new StreamReader(profile.Open());
    int rules = 0;
    bool composeRule = false;
    while (reader.ReadLine() is { } line)
    {
        string rule = line.Trim();
        if (rule.Length == 0 || rule.StartsWith('#'))
            continue;
        rules++;
        composeRule |= rule.Contains("Landroidx/compose/", StringComparison.Ordinal);
    }
    if (rules < 1_000 || !composeRule)
    {
        Console.Error.WriteLine(
            $"  Baseline Profile is unexpectedly small or contains no Compose rules ({rules} rules)");
        passed = false;
    }
    return passed;
}

static bool Unsupported(string path)
{
    Console.Error.WriteLine($"{path}: expected an APK, AAB, or NuGet package");
    return false;
}
