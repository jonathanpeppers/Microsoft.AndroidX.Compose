using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Holds the chat's mutable message log and channel metadata. Mirrors
/// upstream's <c>ConversationUiState</c>, but uses our
/// <see cref="ObservableList{T}"/> instead of Kotlin's
/// <c>SnapshotStateList</c> (which isn't bound).
/// </summary>
public sealed class ConversationUiState
{
    public string ChannelName { get; }
    public int ChannelMembers { get; }
    public ObservableList<Message> Messages { get; }

    public ConversationUiState(string channelName, int channelMembers, IEnumerable<Message> initial)
    {
        ChannelName = channelName;
        ChannelMembers = channelMembers;
        Messages = new ObservableList<Message>(initial);
    }

    /// <summary>Append a new user-authored message to the log.</summary>
    public void AddMessage(string author, string content, string avatar, string timestamp)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        Messages.Add(new Message(author, content.Trim(), timestamp, avatar));
    }
}
