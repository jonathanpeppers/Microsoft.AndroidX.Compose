using AndroidX.Compose.Material3;

namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>
/// Multi-color Jetchat "J" logo composable — C# port of upstream's
/// <c>JetchatIcon</c> in <c>components/JetchatIcon.kt</c>.
/// </summary>
/// <remarks>
/// <para>
/// Stacks <c>ic_jetchat_back</c> (tinted with the current scheme's
/// <see cref="ColorScheme.PrimaryContainer"/>) and <c>ic_jetchat_front</c>
/// (tinted with <see cref="ColorScheme.Primary"/>) inside a
/// <see cref="Box"/> so the icon picks up the active theme's brand
/// colors. Both source drawables ship as solid-white silhouettes; the
/// real tint is applied at the <see cref="Icon"/> call site via
/// <c>ColorFilter.tint(..., SrcIn)</c>.
/// </para>
/// <para>
/// Replaces the single-glyph <c>ic_jetchat</c> chat-bubble drawable
/// at the two upstream call sites (top app bar nav icon and drawer
/// header). Other places that still want the chat-bubble glyph
/// (e.g. the drawer channel rows) keep using <c>ic_jetchat</c>.
/// </para>
/// </remarks>
public static class JetchatIcon
{
    /// <summary>
    /// Build a Jetchat-logo composable sized to <paramref name="sizeDp"/>
    /// on each side, with an optional <paramref name="contentDescription"/>
    /// for accessibility (pass <see langword="null"/> for decorative use).
    /// </summary>
    public static ComposableNode Build(string? contentDescription, int sizeDp = 32) =>
        new Composed(c =>
        {
            var scheme = MaterialTheme.CurrentColorScheme(c);
            return new Box
            {
                Modifier.Companion.Size(sizeDp),
                new Icon(Resource.Drawable.ic_jetchat_back, null)
                {
                    Modifier = Modifier.Companion.Size(sizeDp),
                    TintArgb = scheme.PrimaryContainer,
                },
                new Icon(Resource.Drawable.ic_jetchat_front, contentDescription)
                {
                    Modifier = Modifier.Companion.Size(sizeDp),
                    TintArgb = scheme.Primary,
                },
            };
        });
}
