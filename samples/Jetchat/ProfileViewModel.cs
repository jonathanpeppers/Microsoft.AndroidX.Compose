using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// C# port of upstream's <c>ProfileViewModel</c>. Holds the resolved
/// <see cref="ProfileScreenState"/> behind a
/// <see cref="MutableState{T}"/> of string — Compose's snapshot system
/// only safely shuttles JVM-convertible primitives, so the user id is
/// the snapshot-tracked field and <see cref="UserData"/> is a derived
/// read-only projection through <see cref="Profiles.GetById(string?)"/>.
/// </summary>
public sealed class ProfileViewModel
{
    /// <summary>
    /// Snapshot-tracked user id — observed by the profile screen so a
    /// drawer/avatar tap recomposes the profile body with the new
    /// identity.
    /// </summary>
    public MutableState<string> UserId { get; } = new(Profiles.MeProfile.UserId);

    /// <summary>The currently-loaded profile, derived from <see cref="UserId"/>.</summary>
    public ProfileScreenState UserData => Profiles.GetById(UserId.Value);

    /// <summary>
    /// Set the user id to display. Falls back to
    /// <see cref="Profiles.MeProfile"/>'s id when <paramref name="newUserId"/>
    /// is <c>null</c>, matching upstream's workaround.
    /// </summary>
    public void SetUserId(string? newUserId) =>
        UserId.Value = newUserId ?? Profiles.MeProfile.UserId;
}
