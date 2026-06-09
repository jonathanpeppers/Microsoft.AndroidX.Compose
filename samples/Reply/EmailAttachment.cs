namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// An attachment on an <see cref="Email"/>. Port of upstream's
/// <c>EmailAttachment</c> data class.
/// </summary>
public sealed class EmailAttachment
{
    public EmailAttachment(int resId, string contentDesc)
    {
        ResId       = resId;
        ContentDesc = contentDesc;
    }

    /// <summary>Drawable resource id of the attachment image.</summary>
    public int ResId { get; }

    /// <summary>Accessibility content description.</summary>
    public string ContentDesc { get; }
}
