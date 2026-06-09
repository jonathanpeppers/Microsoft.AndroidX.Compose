namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>
/// Immutable state for the profile screen. C# port of upstream's
/// <c>com.example.compose.jetchat.profile.ProfileScreenState</c>.
/// </summary>
public sealed record ProfileScreenState(
    string  UserId,
    int?    Photo,
    string  Name,
    string  Status,
    string  DisplayName,
    string  Position,
    string  Twitter,
    string? TimeZone,
    string? CommonChannels)
{
    /// <summary>Whether this profile represents the local user — matches upstream's <c>isMe()</c> helper.</summary>
    public bool IsMe() => UserId == Profiles.MeProfile.UserId;
}
