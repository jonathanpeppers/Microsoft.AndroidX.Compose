namespace ComposeNet.Samples.Reply;

/// <summary>
/// A single thread item rendered in the email detail screen. Port of
/// upstream's <c>ReplyEmailThreadItem</c>.
/// </summary>
public static class ReplyEmailThreadItem
{
    /// <summary>Build the thread card for the given <see cref="Email"/>.</summary>
    public static ComposableNode Build(Email email) =>
        new Composed(c =>
        {
            var scheme = MaterialTheme.CurrentColorScheme(c);
            return new Card
            {
                Modifier.Companion
                    .Padding(horizontal: 16, vertical: 4)
                    .Background(new Color(scheme.SurfaceVariant)),
                new Column
                {
                    Modifier.Companion.FillMaxWidth().Padding(20),
                    BuildHeaderRow(email, scheme),
                    new Text(email.Subject)
                    {
                        FontSize  = 14,
                        Color     = new Color(scheme.Outline),
                        Modifier  = Modifier.Companion.Padding(top: 12, bottom: 8, start: 0, end: 0),
                    },
                    new Text(email.Body)
                    {
                        FontSize = 16,
                        Color    = new Color(scheme.OnSurfaceVariant),
                    },
                    BuildActionRow(scheme),
                },
            };
        });

    static Row BuildHeaderRow(Email email, AndroidX.Compose.Material3.ColorScheme scheme) =>
        new()
        {
            Modifier.Companion.FillMaxWidth(),
            ReplyProfileImage.Build(email.Sender.Avatar, email.Sender.FullName),
            new Column
            {
                Modifier.Companion
                    .Weight(1f)
                    .Padding(horizontal: 12, vertical: 4),
                new Text(email.Sender.FirstName)
                {
                    FontSize = 12,
                },
                new Text("20 mins ago")
                {
                    FontSize = 12,
                    Color    = new Color(scheme.Outline),
                },
            },
            new IconButton(onClick: NoOp)
            {
                Modifier.Companion
                    .Clip(Shape.Circle())
                    .Background(new Color(scheme.SurfaceVariant)),
                new Icon(Resource.Drawable.ic_star_border, "Favorite")
                {
                    TintArgb = scheme.Outline,
                },
            },
        };

    static Row BuildActionRow(AndroidX.Compose.Material3.ColorScheme scheme) =>
        new(Arrangement.SpacedBy(12))
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(top: 20, bottom: 8, start: 0, end: 0),
            new Button(onClick: NoOp)
            {
                Modifier.Companion.Weight(1f),
                new Text("Reply")
                {
                    Color = new Color(scheme.OnSurface),
                },
            },
            new Button(onClick: NoOp)
            {
                Modifier.Companion.Weight(1f),
                new Text("Reply All")
                {
                    Color = new Color(scheme.OnSurface),
                },
            },
        };

    static void NoOp() { }
}
