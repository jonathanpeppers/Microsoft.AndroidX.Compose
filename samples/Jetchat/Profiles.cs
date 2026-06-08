namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Seed profile data for the demo. Mirrors upstream's
/// <c>com.example.compose.jetchat.data.meProfile</c> and
/// <c>colleagueProfile</c> values from <c>FakeData.kt</c>.
/// </summary>
public static class Profiles
{
    /// <summary>Example "me" profile.</summary>
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

    /// <summary>Example colleague profile.</summary>
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
    /// Resolve a profile by id. C# port of the lookup branch in
    /// upstream's <c>ProfileViewModel.setUserId</c>.
    /// </summary>
    public static ProfileScreenState GetById(string? userId) =>
        // Workaround for simplicity
        userId == MeProfile.UserId || userId == MeProfile.DisplayName
            ? MeProfile
            : ColleagueProfile;
}
