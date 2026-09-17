namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// "Screen under construction" placeholder shown on the Articles,
/// Direct Messages, and Groups tabs. Port of upstream's
/// <c>EmptyComingSoon</c>.
/// </summary>
public static class EmptyComingSoon
{
    /// <summary>Build the centered "coming soon" column.</summary>
    public static ComposableNode Build() =>
        new Composed(c =>
        {
            var scheme = c.ColorScheme();
            return new Column(verticalArrangement: Arrangement.Center)
            {
                Modifier.FillMaxSize().Padding(8),
                new Text(c.StringResource(Resource.String.reply_empty_title))
                {
                    Color      = Color.FromPacked(scheme.Primary),
                    Modifier   = Modifier.FillMaxWidth(),
                }.WithTypography(ReplyTypography.TitleMedium),
                Spacer.Height(8),
                new Text(c.StringResource(Resource.String.reply_empty_subtitle))
                {
                    Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                    Modifier = Modifier.Padding(horizontal: 16),
                }.WithTypography(ReplyTypography.BodyMedium),
            };
        });
}
