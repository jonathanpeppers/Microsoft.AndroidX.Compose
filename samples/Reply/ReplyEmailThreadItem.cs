namespace AndroidX.Compose.Samples.Reply;

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
            var scheme = c.ColorScheme();
            var surface = new Surface
            {
                Shape = Shape.RoundedCorners(16),
                Color = Color.FromPacked(scheme.SurfaceContainerHigh),
            };
            surface.Add(Modifier.Padding(horizontal: 16, vertical: 4));
            surface.Add(new Column
            {
                Modifier.FillMaxWidth().Padding(20),
                BuildHeaderRow(email, scheme, c),
                new Text(email.Subject)
                {
                    Color = Color.FromPacked(scheme.Outline),
                    Modifier = Modifier.Padding(top: 12, bottom: 8),
                }.WithTypography(ReplyTypography.BodyMedium),
                new Text(email.Body)
                {
                    Color = Color.FromPacked(scheme.OnSurfaceVariant),
                }.WithTypography(ReplyTypography.BodyLarge),
                BuildActionRow(c, scheme),
            });
            return surface;
        });

    static Row BuildHeaderRow(
        Email email,
        AndroidX.Compose.Material3.ColorScheme scheme,
        AndroidX.Compose.Runtime.IComposer composer) =>
        new()
        {
            Modifier.FillMaxWidth(),
            ReplyProfileImage.Build(email.Sender.Avatar, email.Sender.FullName),
            new Column
            {
                Modifier
                    .Weight(1f)
                    .Padding(horizontal: 12, vertical: 4),
                new Text(email.Sender.FirstName)
                    .WithTypography(ReplyTypography.LabelMedium),
                new Text(composer.StringResource(Resource.String.reply_twenty_minutes_ago))
                {
                    Color    = Color.FromPacked(scheme.Outline),
                }.WithTypography(ReplyTypography.LabelMedium),
            },
            new IconButton(onClick: NoOp)
            {
                Modifier
                    .Clip(Shape.Circle())
                    .Background(Color.FromPacked(scheme.SurfaceVariant)),
                new Icon(
                    Resource.Drawable.ic_star_border,
                    composer.StringResource(Resource.String.reply_favorite))
                {
                    Tint = Color.FromPacked(scheme.Outline),
                },
            },
        };

    static Row BuildActionRow(
        AndroidX.Compose.Runtime.IComposer composer,
        AndroidX.Compose.Material3.ColorScheme scheme)
    {
        var colors = composer.ButtonColors(
            containerColor: Color.FromPacked(scheme.SurfaceBright));
        var reply = new Button(onClick: NoOp) { Colors = colors };
        reply.Add(Modifier.Weight(1f));
        reply.Add(new Text(composer.StringResource(Resource.String.reply_action))
        {
            Color = Color.FromPacked(scheme.OnSurface),
        });
        var replyAll = new Button(onClick: NoOp) { Colors = colors };
        replyAll.Add(Modifier.Weight(1f));
        replyAll.Add(new Text(composer.StringResource(Resource.String.reply_all))
        {
            Color = Color.FromPacked(scheme.OnSurface),
        });
        return new Row(Arrangement.SpacedBy(12.Dp()))
        {
            Modifier.FillMaxWidth().Padding(top: 20, bottom: 8),
            reply,
            replyAll,
        };
    }

    static void NoOp() { }
}
