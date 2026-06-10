namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>
/// C# port of upstream's <c>ProfileViewModel</c>. Tracks the active
/// user id and resolves it to a <see cref="ProfileScreenState"/> via
/// <see cref="Profiles.GetById(string?)"/>.
/// </summary>
public sealed class ProfileViewModel
{
    /// <summary>The active user id — observed by the profile screen.</summary>
    public MutableState<string> UserId { get; } = new(Profiles.MeProfile.UserId);

    /// <summary>The currently-loaded profile, derived from <see cref="UserId"/>.</summary>
    public ProfileScreenState UserData => Profiles.GetById(UserId.Value);

    /// <summary>Set the user id to display.</summary>
    public void SetUserId(string? newUserId) =>
        UserId.Value = newUserId ?? Profiles.MeProfile.UserId;
}
