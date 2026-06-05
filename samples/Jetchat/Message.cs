namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// A single chat message. Mirrors upstream's
/// <c>com.example.compose.jetchat.conversation.Message</c> data class
/// (author / content / timestamp), minus the inline image-attachment
/// field. The author avatar is a drawable resource id rendered through
/// the Phase 7 <see cref="ComposeNet.Image"/> facade — same shape as
/// upstream's <c>painterResource(R.drawable.someone_else)</c>.
/// </summary>
public sealed record Message(string Author, string Content, string Timestamp, int AuthorAvatarRes);
