namespace ComposeNet.Samples.JetNews;

/// <summary>
/// One inline run inside a <see cref="Paragraph"/>'s text — start/end
/// offsets into <see cref="Paragraph.Text"/> plus the
/// <see cref="MarkupType"/> to apply. Mirrors upstream's
/// <c>Markup</c> data class.
/// </summary>
/// <param name="Type">Style applied to the run.</param>
/// <param name="Start">Inclusive start offset in <see cref="Paragraph.Text"/>.</param>
/// <param name="End">Exclusive end offset in <see cref="Paragraph.Text"/>.</param>
/// <param name="Href">URL target for <see cref="MarkupType.Link"/> runs (unused
/// by upstream's renderer, kept here for future tappable-link wiring).</param>
public sealed record Markup(MarkupType Type, int Start, int End, string? Href = null);
