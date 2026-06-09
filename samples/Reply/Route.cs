namespace Microsoft.AndroidX.Compose.Samples.Reply;

/// <summary>
/// Top-level destination routes recognised by the <see cref="NavHost"/>.
/// Port of upstream's <c>Route</c> sealed interface — collapsed to a
/// static class because we only consume the string values.
/// </summary>
public static class Route
{
    /// <summary>The Inbox screen.</summary>
    public const string Inbox = "Inbox";

    /// <summary>The Articles screen — placeholder content.</summary>
    public const string Articles = "Articles";

    /// <summary>The Direct Messages screen — placeholder content.</summary>
    public const string DirectMessages = "DirectMessages";

    /// <summary>The Groups screen — placeholder content.</summary>
    public const string Groups = "Groups";

    /// <summary>The Email Detail screen — takes an <c>{emailId}</c> placeholder.</summary>
    public const string EmailDetailPattern = "EmailDetail/{emailId}";

    /// <summary>Build the Email Detail route for a specific email id.</summary>
    public static string EmailDetail(long emailId) => $"EmailDetail/{emailId}";
}
