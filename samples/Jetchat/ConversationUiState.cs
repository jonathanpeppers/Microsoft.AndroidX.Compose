namespace Microsoft.AndroidX.Compose.Samples.Jetchat;

/// <summary>
/// UI state for the conversation screen. One channel per state object,
/// matching upstream's
/// <c>com.example.compose.jetchat.conversation.ConversationUiState</c>:
/// channel name, member count, and a snapshot-aware
/// <see cref="MutableStateList{T}"/> of messages. New messages are
/// inserted at index 0 (newest first), so the
/// <c>LazyColumn(reverseLayout = true)</c> in the screen body pins the
/// newest message to the bottom of the viewport.
/// </summary>
public sealed class ConversationUiState
{
    /// <summary>Channel-display name (e.g. <c>#composers</c>) — already includes the leading <c>#</c>.</summary>
    public string ChannelName { get; }

    /// <summary>Channel headcount displayed in the top app bar subtitle.</summary>
    public int ChannelMembers { get; }

    /// <summary>Snapshot-aware message log. Index 0 is the newest message.</summary>
    public MutableStateList<Message> Messages { get; }

    /// <summary>Construct the state with a fixed channel and a seed message list.</summary>
    public ConversationUiState(string channelName, int channelMembers, IEnumerable<Message> initialMessages)
    {
        ChannelName    = channelName;
        ChannelMembers = channelMembers;
        Messages       = new MutableStateList<Message>(initialMessages);
    }

    /// <summary>Add a new message to the log (inserts at index 0 — newest position).</summary>
    public void AddMessage(Message msg) => Messages.Insert(0, msg);
}
