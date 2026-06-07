using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Holds the chat's mutable message log, the currently selected
/// channel, and channel metadata. Mirrors upstream's
/// <c>ConversationUiState</c>, but uses our
/// <see cref="MutableStateList{T}"/> instead of Kotlin's
/// <c>SnapshotStateList</c> (which isn't bound). The
/// <see cref="CurrentChannel"/> property is backed by a
/// <see cref="MutableState{T}"/> so drawer taps trigger recomposition
/// of the top app bar title — we don't actually swap message lists per
/// channel since the seed only has one conversation.
/// </summary>
public sealed class ConversationUiState
{
    readonly MutableState<string> _currentChannel;

    public int ChannelMembers { get; }
    public MutableStateList<Message> Messages { get; }

    /// <summary>
    /// Currently selected channel, displayed in the top app bar and
    /// highlighted in the drawer. Tapping a drawer item updates this;
    /// the displayed message list does not change because the sample
    /// only seeds one conversation.
    /// </summary>
    public string CurrentChannel
    {
        get => _currentChannel.Value;
        set => _currentChannel.Value = value;
    }

    public ConversationUiState(string channelName, int channelMembers, IEnumerable<Message> initial)
    {
        _currentChannel = new MutableState<string>(channelName);
        ChannelMembers = channelMembers;
        Messages = new MutableStateList<Message>(initial);
    }

    /// <summary>Append a new user-authored message to the log.</summary>
    public void AddMessage(string author, string content, int avatarRes, string timestamp)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        Messages.Add(new Message(author, content.Trim(), timestamp, avatarRes));
    }
}
