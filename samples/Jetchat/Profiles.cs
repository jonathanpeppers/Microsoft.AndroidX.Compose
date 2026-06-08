namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Seed profile data for the demo. Mirrors upstream's
/// <c>com.example.compose.jetchat.data.meProfile</c> and
/// <c>colleagueProfile</c> values from <c>FakeData.kt</c>; lookups by
/// user-id fall back to the colleague when the id is unknown, matching
/// the workaround in upstream's <c>ProfileViewModel.setUserId</c>.
/// </summary>
public static class Profiles
{
    /// <summary>The local user's profile — selected when the back-stack user-id is <c>"me"</c>.</summary>
    public static readonly ProfileScreenState MeProfile = new(
        UserId:         "me",
        Photo:          Resource.Drawable.avatar_ali,
        Name:           "Ali Conors",
        Status:         "Online",
        DisplayName:    "aliconors",
        Position:       "Senior Android Dev at Yearin\nGoogle Developer Expert",
        Twitter:        "twitter.com/aliconors",
        TimeZone:       "In your timezone",
        CommonChannels: null);

    /// <summary>Sample colleague profile — returned for any non-<c>"me"</c> user id.</summary>
    public static readonly ProfileScreenState ColleagueProfile = new(
        UserId:         "12345",
        Photo:          Resource.Drawable.avatar_someone_else,
        Name:           "Taylor Brooks",
        Status:         "Away",
        DisplayName:    "taylor",
        Position:       "Senior Android Dev at Openlane",
        Twitter:        "twitter.com/taylorbrookscodes",
        TimeZone:       "12:25 AM local time (Eastern Daylight Time)",
        CommonChannels: "2");

    /// <summary>
    /// Resolve a profile by id. Returns <see cref="MeProfile"/> when
    /// the id matches <c>"me"</c> or the me profile's display name;
    /// every other id (including <c>null</c>) maps to
    /// <see cref="ColleagueProfile"/> — same workaround upstream's
    /// <c>ProfileViewModel</c> uses.
    /// </summary>
    public static ProfileScreenState GetById(string? userId) =>
        userId == MeProfile.UserId || userId == MeProfile.DisplayName
            ? MeProfile
            : ColleagueProfile;
}
