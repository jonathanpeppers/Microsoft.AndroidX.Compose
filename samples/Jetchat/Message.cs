namespace Microsoft.AndroidX.Compose.Samples.Jetchat;

/// <summary>
/// A single chat message. Mirrors upstream's
/// <c>com.example.compose.jetchat.conversation.Message</c> data class:
/// <c>author</c>, <c>content</c>, <c>timestamp</c>, optional inline
/// attachment <c>image</c>, and a derived <see cref="AuthorImage"/>
/// that resolves to <c>avatar_ali</c> for the local user and a shared
/// <c>avatar_someone_else</c> for every other author (same convention
/// upstream uses).
/// </summary>
public sealed record Message(
    string Author,
    string Content,
    string Timestamp,
    int? Image = null)
{
    /// <summary>
    /// Drawable resource id of the author's portrait. Computed from
    /// <see cref="Author"/> at read-time so seed data only has to pass
    /// the author name — same default-driven shape as upstream's Kotlin
    /// <c>authorImage = if (author == "me") R.drawable.ali else R.drawable.someone_else</c>.
    /// </summary>
    public int AuthorImage => Author == Conversation.MyName
        ? Resource.Drawable.avatar_ali
        : Resource.Drawable.avatar_someone_else;
}
