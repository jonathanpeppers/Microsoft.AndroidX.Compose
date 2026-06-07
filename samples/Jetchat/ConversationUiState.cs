using System.Collections.Generic;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Per-channel UI state for the conversation. Holds a dictionary of
/// channel-name → message log plus the currently-selected channel name
/// (which the drawer toggles). Both backing fields participate in the
/// Compose snapshot system, so drawer taps trigger recomposition of
/// the title and the message list.
/// </summary>
/// <remarks>
/// Each channel keeps its own <see cref="MutableStateList{T}"/> of
/// messages and its own member count. Tapping <c>composers</c> in the
/// drawer surfaces the composers log; tapping <c>droidcon-nyc</c>
/// swaps to that log. Sending a message appends to the
/// <see cref="CurrentChannel"/> only.
/// </remarks>
public sealed class ConversationUiState
{
    /// <summary>Per-channel mutable state: message log + static member count.</summary>
    public sealed class ChannelState
    {
        /// <summary>Channel-display name (e.g. <c>composers</c>).</summary>
        public string Name { get; }

        /// <summary>Headcount displayed in the top app bar subtitle.</summary>
        public int Members { get; }

        /// <summary>Snapshot-aware message log for this channel.</summary>
        public MutableStateList<Message> Messages { get; }

        /// <summary>Construct a channel with a seed message list.</summary>
        public ChannelState(string name, int members, IEnumerable<Message> seed)
        {
            Name     = name;
            Members  = members;
            Messages = new MutableStateList<Message>(seed);
        }
    }

    readonly MutableState<string> _currentChannel;

    /// <summary>All channels keyed by name. Read-only after construction.</summary>
    public IReadOnlyDictionary<string, ChannelState> Channels { get; }

    /// <summary>
    /// Name of the currently-selected channel. Backed by a
    /// <see cref="MutableState{T}"/>; assigning a new value triggers
    /// recomposition of any subtree that reads it.
    /// </summary>
    public string CurrentChannel
    {
        get => _currentChannel.Value;
        set => _currentChannel.Value = value;
    }

    /// <summary>The <see cref="ChannelState"/> entry for <see cref="CurrentChannel"/>.</summary>
    public ChannelState Current => Channels[CurrentChannel];

    /// <summary>Construct with a seeded set of channels and an initial selection.</summary>
    public ConversationUiState(IEnumerable<ChannelState> channels, string initialChannel)
    {
        var dict = new Dictionary<string, ChannelState>();
        foreach (var ch in channels) dict[ch.Name] = ch;
        Channels = dict;
        _currentChannel = new MutableState<string>(initialChannel);
    }

    /// <summary>Append a new message to the currently-selected channel.</summary>
    public void AddMessage(string author, string content, int avatarRes, string timestamp)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        Current.Messages.Add(new Message(author, content.Trim(), timestamp, avatarRes));
    }
}
