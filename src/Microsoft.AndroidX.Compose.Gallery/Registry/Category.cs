namespace AndroidX.Compose.Gallery.Registry;

/// <summary>
/// One row on the gallery's home screen and one filter bucket in
/// search. Demos reference a category by <see cref="Id"/>; the home
/// screen renders categories in the order they appear in
/// <see cref="Catalog.Categories"/>.
/// </summary>
/// <param name="Id">
/// Stable slug used as the navigation key (e.g. <c>"buttons"</c>) and
/// the foreign key on <see cref="Demo.CategoryId"/>. Lower-case, no
/// spaces; never localized.
/// </param>
/// <param name="Title">Display title — short noun phrase.</param>
/// <param name="Subtitle">
/// One-line description shown under the title on the home cards.
/// </param>
/// <param name="Glyph">
/// Single-character emoji or symbol used as a lightweight leading
/// glyph in the home cards and search results. Avoids dragging in
/// extended-icon Compose dependencies just for the category index.
/// </param>
public sealed record Category(
    string Id,
    string Title,
    string Subtitle,
    string Glyph);
