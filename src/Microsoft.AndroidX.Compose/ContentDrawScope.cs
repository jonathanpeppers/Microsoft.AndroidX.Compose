using AndroidX.Compose.UI.Graphics.Drawscope;

namespace AndroidX.Compose;

/// <summary>
/// Draw scope supplied by <see cref="ModifierExtensions.DrawWithContent(Modifier, Action{ContentDrawScope})"/>.
/// </summary>
public sealed class ContentDrawScope : DrawScope
{
    readonly IContentDrawScope _jvm;

    internal ContentDrawScope(IContentDrawScope jvm) : base(jvm) => _jvm = jvm;

    /// <summary>Draws the modified composable's content at the current point in the callback.</summary>
    public void DrawContent() => _jvm.DrawContent();
}
