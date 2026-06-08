using System;
using System.Collections.Generic;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.Reply;

/// <summary>
/// Email detail screen — a top app bar plus a <see cref="LazyColumn{T}"/>
/// of <see cref="ReplyEmailThreadItem"/>s. Port of upstream's
/// <c>ReplyEmailDetail</c> for the single-pane layout.
/// </summary>
public static class ReplyEmailDetail
{
    /// <summary>Build the email detail screen.</summary>
    public static ComposableNode Build(Email email, System.Action onBackPressed) =>
        new Scaffold
        {
            TopBar = EmailDetailAppBar.Build(email, onBackPressed),
            Body   = new LazyColumn<Email>(
                items:       email.Threads,
                itemContent: e => ReplyEmailThreadItem.Build(e))
            {
                Modifier = Modifier.Companion.FillMaxSize(),
            },
        };
}
