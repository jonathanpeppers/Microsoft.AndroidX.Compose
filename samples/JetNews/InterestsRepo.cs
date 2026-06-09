
namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Static data for the Interests screen — three categories of
/// togglable subscriptions: topics (grouped under sections), people,
/// and publications. Stripped down from the upstream
/// <c>FakeInterestsRepository</c>.
/// </summary>
public static class InterestsRepo
{
    /// <summary>
    /// Topics grouped by category. The Interests screen renders each
    /// category as a header with a toggleable list of topics underneath.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Topics { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Android"]     = new[] { "Jetpack Compose", "Kotlin", "Material 3", "Compose Multiplatform" },
            [".NET"]        = new[] { "C# 12", ".NET for Android", "Source generators", "Roslyn analyzers" },
            ["Tooling"]     = new[] { "MSBuild", "AGP", "Gradle", "dotnet workload" },
            ["Architecture"] = new[] { "State holders", "Snapshot system", "Navigation", "Theming" },
        };

    /// <summary>People you can follow.</summary>
    public static IReadOnlyList<string> People { get; } = new[]
    {
        "Aubrey",
        "Taylor",
        "Jordan",
        "Avery",
        "Riley",
        "Casey",
    };

    /// <summary>Publications you can subscribe to.</summary>
    public static IReadOnlyList<string> Publications { get; } = new[]
    {
        ".NET Blog",
        "Android Developers",
        "Now in Android",
        "Compose Insights",
    };
}
