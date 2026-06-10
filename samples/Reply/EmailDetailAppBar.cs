namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Top app bar for the <see cref="ReplyEmailDetail"/> screen. Port of
/// upstream's <c>EmailDetailAppBar</c>.
/// </summary>
public static class EmailDetailAppBar
{
    /// <summary>Build the email-detail app bar.</summary>
    public static TopAppBar Build(Email email, Action onBackPressed) =>
        new()
        {
            Title = new Composed(c =>
            {
                var scheme = MaterialTheme.CurrentColorScheme(c);
                return new Column
                {
                    Modifier.Companion.FillMaxWidth(),
                    new Text(email.Subject)
                    {
                        FontSize = 16,
                        Color    = scheme.OnSurfaceVariant,
                    },
                    new Text($"{email.Threads.Count} Messages")
                    {
                        FontSize = 12,
                        Color    = scheme.Outline,
                        Modifier = Modifier.Companion.Padding(top: 4, bottom: 0, start: 0, end: 0),
                    },
                };
            }),
            NavigationIcon = new FilledIconButton(onClick: onBackPressed)
            {
                Modifier.Companion.Padding(8),
                new Icon(Resource.Drawable.ic_arrow_back, "Back")
                {
                    Modifier = Modifier.Companion.Size(14),
                },
            },
            Actions = new Composed(c =>
            {
                var scheme = MaterialTheme.CurrentColorScheme(c);
                return new IconButton(onClick: NoOp)
                {
                    new Icon(Resource.Drawable.ic_more_vert, "More options")
                    {
                        TintArgb = scheme.OnSurfaceVariant,
                    },
                };
            }),
        };

    static void NoOp() { }
}
