using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Gallery.Registry;

/// <summary>
/// One catalog entry — a single self-contained demo of a facade or a
/// composition pattern. The <see cref="Build"/> factory is invoked
/// inside a <see cref="ComposableNode"/> wrapper so calls like
/// <c>composer.Remember(...)</c> resolve against the active composer.
/// </summary>
/// <param name="Id">
/// Stable slug used as the <c>demo/{id}</c> navigation key (e.g.
/// <c>"buttons-fill-styles"</c>). Lower-case, hyphenated.
/// </param>
/// <param name="CategoryId">
/// Foreign key into <see cref="Catalog.Categories"/> — groups demos on
/// the home / category screens and in the search index.
/// </param>
/// <param name="Title">Short title shown in lists and on the demo screen.</param>
/// <param name="Description">
/// One-line tagline shown under the title in list rows and as the
/// header text on the demo screen.
/// </param>
/// <param name="Build">
/// Factory that returns the demo's root composable. Receives the active
/// <see cref="IComposer"/> so the body can call composition primitives
/// like <c>composer.Remember(...)</c>.
/// </param>
public sealed record Demo(
    string Id,
    string CategoryId,
    string Title,
    string Description,
    Func<IComposer, ComposableNode> Build);
