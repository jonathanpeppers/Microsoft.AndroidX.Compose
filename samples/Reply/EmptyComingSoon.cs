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
                new Text("Screen under construction")
                {
                    FontSize   = 18,
                    FontWeight = FontWeight.SemiBold,
                    Color      = Color.FromPacked(scheme.Primary),
                    Modifier   = Modifier.FillMaxWidth(),
                },
                Spacer.Height(8),
                new Text("This screen is still under construction. This sample will help you learn about adaptive layouts in Jetpack Compose")
                {
                    FontSize = 14,
                    Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                    Modifier = Modifier.Padding(horizontal: 16),
                },
            };
        });
}
