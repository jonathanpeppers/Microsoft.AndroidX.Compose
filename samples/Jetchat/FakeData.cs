
namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Seed conversation data for the demo. Same <em>structural</em> shape
/// as upstream's <c>FakeData</c>: nine messages, four authors
/// (<c>me</c>, <c>Taylor Brooks</c>, <c>John Glenn</c>,
/// <c>Shangeeth Sivan</c>), distributed across two visual "days" so the
/// hardcoded day headers ("Today" at index 2, "20 Aug" above the
/// oldest) split the list. Each message exercises one of the markup
/// tokens the future message-formatter will recognize — a code span,
/// an @mention, a URL, plain emoji, and a multi-codepoint emoji
/// sequence — so the formatter port (slice 4) has the same test cases
/// the upstream sample does. The prose is written for this port.
/// </summary>
internal static class FakeData
{
    const string MeltingFace  = "\uD83E\uDEE0";
    const string FaceInClouds = "\uD83D\uDE36\u200D\uD83C\uDF2B\uFE0F";
    const string Flamingo     = "\uD83E\uDDA9";
    const string PointRight   = "\uD83D\uDC49";
    const string PinkHeart    = "\uD83E\uDE77";

    public static List<Message> InitialMessages() => new()
    {
        new Message("me",              "Take a look at this!",                                                                                                                                  "8:07 PM"),
        new Message("me",              $"Appreciate it {PinkHeart}",                                                                                                                            "8:06 PM"),
        new Message("Taylor Brooks",   "Everything composable carries over.",                                                                                                                   "8:05 PM"),
        new Message("Taylor Brooks",   "@aliconors give `Flow.collectAsStateWithLifecycle()` a try.",                                                                                           "8:05 PM"),
        new Message("John Glenn",      $"Also fairly new to Compose {Flamingo} — the JetNews sample tracks current releases and shows a clean async-loading pattern. {PointRight} https://goo.gle/jetnews", "8:04 PM"),
        new Message("me",              $"Brand new to Compose: every tutorial I find for streaming data into composables looks out of date {MeltingFace} {FaceInClouds}. What's the current recommended approach?", "8:03 PM"),
        new Message("Shangeeth Sivan", "Has anyone tried Glance Widgets yet? It's the newer way to build home-screen widgets on Android.",                                                       "8:08 PM"),
        new Message("Taylor Brooks",   "Hadn't heard of them — when did Glance show up in the platform?",                                                                                        "8:10 PM"),
        new Message("John Glenn",      "Pretty recent addition from what I can tell.",                                                                                                          "8:12 PM"),
    };
}
