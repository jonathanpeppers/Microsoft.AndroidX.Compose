namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// A single chat message. Mirrors upstream's
/// <c>com.example.compose.jetchat.conversation.Message</c> data class
/// (author / content / timestamp), minus the image-attachment fields
/// — we render emoji avatars instead of bitmap drawables.
/// </summary>
public sealed record Message(string Author, string Content, string Timestamp, string AuthorAvatar);
