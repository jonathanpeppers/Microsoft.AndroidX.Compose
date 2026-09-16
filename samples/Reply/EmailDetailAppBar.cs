namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Full-screen toolbar for the <see cref="ReplyEmailDetail"/> screen. Port of
/// upstream's <c>EmailDetailAppBar</c>.
/// </summary>
public static class EmailDetailAppBar
{
    /// <summary>Build the email-detail app bar.</summary>
    public static ComposableNode Build(Email email, Action onBackPressed) =>
        new Composed(c =>
        {
            var scheme = c.ColorScheme();
            var toolbar = new Surface
            {
                Color = Color.FromPacked(scheme.InverseOnSurface),
                Modifier = Modifier.FillMaxWidth(),
            };
            var layout = new Box
            {
                Modifier.FillMaxWidth().StatusBarsPadding().Height(64),
            };
            var backButton = new Surface
            {
                Shape = Shape.Circle(),
                Color = Color.FromPacked(scheme.Surface),
                Modifier = Modifier.Align(Alignment.CenterStart).Padding(8),
            };
            backButton.Add(new IconButton(onClick: onBackPressed)
            {
                new Icon(
                    Resource.Drawable.ic_arrow_back,
                    c.StringResource(Resource.String.reply_back))
                {
                    Modifier = Modifier.Size(14),
                    Tint = Color.FromPacked(scheme.OnSurface),
                },
            });
            layout.Add(backButton);
            var title = new Column(
                verticalArrangement: null,
                horizontalAlignment: Alignment.Horizontal.CenterHorizontally);
            title.Add(Modifier.Align(Alignment.Center));
            title.Add(new Text(email.Subject)
            {
                Color = Color.FromPacked(scheme.OnSurfaceVariant),
                Modifier = Modifier.Semantics(
                    c.StringResource(Resource.String.reply_email_detail_title)),
            }.WithTypography(ReplyTypography.TitleMedium));
            title.Add(new Text(c.StringResource(
                Resource.String.reply_messages,
                email.Threads.Count))
            {
                Color = Color.FromPacked(scheme.Outline),
                Modifier = Modifier.Padding(top: 4),
            }.WithTypography(ReplyTypography.LabelMedium));
            layout.Add(title);
            var more = new IconButton(onClick: NoOp);
            more.Add(Modifier.Align(Alignment.CenterEnd));
            more.Add(new Icon(
                Resource.Drawable.ic_more_vert,
                c.StringResource(Resource.String.reply_more_options))
            {
                Tint = Color.FromPacked(scheme.OnSurfaceVariant),
            });
            layout.Add(more);
            toolbar.Add(layout);
            return toolbar;
        });

    static void NoOp() { }
}
