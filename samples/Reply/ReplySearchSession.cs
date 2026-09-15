namespace AndroidX.Compose.Samples.Reply;

// Owned by the search composition, not the retained inbox/nav state.
internal sealed class ReplySearchSession
{
    internal SearchBarState Expansion { get; } = new();
    internal SearchBarTextFieldState Input { get; } = new();

    internal static IReadOnlyList<Email> FindMatches(IReadOnlyList<Email> emails, string query)
    {
        ArgumentNullException.ThrowIfNull(emails);
        ArgumentNullException.ThrowIfNull(query);
        if (query.Length == 0)
            return [];

        return emails.Where(email =>
            email.Subject.StartsWith(query, StringComparison.OrdinalIgnoreCase) ||
            email.Sender.FullName.StartsWith(query, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    internal async Task DismissAsync(bool clearQuery, CancellationToken cancellationToken = default)
    {
        if (clearQuery)
            Input.ClearText();
        await Expansion.CollapseAsync(cancellationToken);
    }

    internal async Task SelectAsync(
        Email email, Action<long> onSelected, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(onSelected);
        onSelected(email.Id);
        await DismissAsync(clearQuery: true, cancellationToken);
    }
}
