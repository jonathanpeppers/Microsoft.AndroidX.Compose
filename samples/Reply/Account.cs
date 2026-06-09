namespace Microsoft.AndroidX.Compose.Samples.Reply;

/// <summary>
/// An account belonging to a user. A single user can have multiple
/// accounts. Port of upstream's <c>Account</c> data class.
/// </summary>
public sealed class Account
{
    public Account(
        long id,
        long uid,
        string firstName,
        string lastName,
        string email,
        string altEmail,
        int avatar,
        bool isCurrentAccount = false)
    {
        Id               = id;
        Uid              = uid;
        FirstName        = firstName;
        LastName         = lastName;
        Email            = email;
        AltEmail         = altEmail;
        Avatar           = avatar;
        IsCurrentAccount = isCurrentAccount;
    }

    /// <summary>Stable identity used by <c>LazyColumn</c> keys.</summary>
    public long Id { get; }

    /// <summary>User identity — multiple <see cref="Account"/>s may share a <see cref="Uid"/>.</summary>
    public long Uid { get; }

    public string FirstName { get; }
    public string LastName  { get; }
    public string Email     { get; }
    public string AltEmail  { get; }

    /// <summary>Drawable resource id for this account's avatar.</summary>
    public int Avatar { get; }

    public bool IsCurrentAccount { get; set; }

    /// <summary>Concatenation of <see cref="FirstName"/> and <see cref="LastName"/>.</summary>
    public string FullName => $"{FirstName} {LastName}";
}
