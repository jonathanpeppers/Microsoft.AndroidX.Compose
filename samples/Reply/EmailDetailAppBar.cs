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
                var scheme = c.ColorScheme();
                return new Column
                {
                    Modifier.FillMaxWidth(),
                    new Text(email.Subject)
                    {
                        FontSize = 16,
                        Color    = scheme.OnSurfaceVariant,
                    },
                    new Text($"{email.Threads.Count} Messages")
                    {
                        FontSize = 12,
                        Color    = scheme.Outline,
                        Modifier = Modifier.Padding(top: 4),
                    },
                };
            }),
            NavigationIcon = new FilledIconButton(onClick: onBackPressed)
            {
                Modifier.Padding(8),
                new Icon(Resource.Drawable.ic_arrow_back, "Back")
                {
                    Modifier = Modifier.Size(14),
                },
            },
            Actions = new Composed(c =>
            {
                var scheme = c.ColorScheme();
                return new IconButton(onClick: NoOp)
                {
                    new Icon(Resource.Drawable.ic_more_vert, "More options")
                    {
                        Tint = scheme.OnSurfaceVariant,
                    },
                };
            }),
        };

    static void NoOp() { }
}
