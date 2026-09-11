using System.Collections;

namespace AndroidX.Compose;

/// <summary>
/// A single destination registered with a <see cref="NavHost"/>. The
/// route string is the unique key the host uses to match
/// <see cref="NavController.Navigate(string)"/> calls; the body is
/// the @Composable content shown while this destination sits on top
/// of the back stack.
///
/// <para>
/// Two construction shapes — collection-init for static content,
/// or a <c>Func&lt;NavBackStackEntry, ComposableNode&gt;</c> for
/// dynamic content that needs to read route arguments:
/// </para>
/// <code>
/// // Static — children from the latest successful host render
/// new NavDestination("home")
/// {
///     new Text("Home"),
/// }
///
/// // Dynamic — read the {id} placeholder from the back-stack entry
/// new NavDestination("user/{id}", entry =&gt;
/// {
///     var id = entry.Arguments?.GetString("id") ?? "?";
///     return new Text($"User #{id}");
/// })
/// </code>
///
/// <para>
/// Mirrors Kotlin's <c>NavGraphBuilder.composable(route) { backStackEntry -&gt; ... }</c>.
/// Named <c>NavDestination</c> rather than <c>Composable</c> to avoid
/// confusion with the unrelated <see cref="ComposableNode"/> base class
/// and Kotlin's <c>@Composable</c> annotation — this type isn't a UI
/// node, it's a route registration.
/// Add instances to a <see cref="NavHost"/> via collection-initializer
/// — they're not normal <see cref="ComposableNode"/>s, since their
/// content is composed inside the NavHost's per-route subcomposition,
/// not the surrounding tree.
/// </para>
/// <para>
/// A host render publishes the latest factory or static children for each
/// route. Captured values and callbacks can therefore be replaced without
/// changing the navigation graph. Publication invalidates visible destination
/// content; inactive destinations use the latest content when revisited.
/// Changes to static children through <see cref="Add(ComposableNode?)"/> are
/// published on the next host render, not immediately.
/// </para>
/// </summary>
public sealed class NavDestination : IEnumerable
{
    readonly List<ComposableNode> _staticChildren = new();
    readonly Func<NavBackStackEntry, ComposableNode>? _factory;
    NavDestinationContent? _content;

    internal NavDestinationContent Content => _content ??= new(_factory, [.. _staticChildren]);

    /// <summary>
    /// Register a destination with a static child tree. Add children
    /// via collection-initializer syntax.
    /// </summary>
    public NavDestination(string route)
    {
        ArgumentNullException.ThrowIfNull(route);
        Route = route;
    }

    /// <summary>
    /// Register a destination whose content depends on the
    /// <see cref="NavBackStackEntry"/> — typically used to read
    /// route placeholders (e.g. <c>{id}</c> in <c>"user/{id}"</c>)
    /// from the entry's <see cref="NavBackStackEntry.Arguments"/>.
    /// </summary>
    public NavDestination(string route, Func<NavBackStackEntry, ComposableNode> content)
    {
        ArgumentNullException.ThrowIfNull(route);
        Route    = route;
        ArgumentNullException.ThrowIfNull(content);
        _factory = content;
    }

    /// <summary>The route string used to navigate to this destination.</summary>
    public string Route { get; }

    /// <summary>
    /// Add a child to the static content tree. Required by C#'s
    /// collection-initializer syntax. Calling this on a destination
    /// constructed with the dynamic-content factory throws.
    /// </summary>
    public void Add(ComposableNode? child)
    {
        if (_factory is not null)
            throw new InvalidOperationException(
                "NavDestination was constructed with a dynamic content factory; collection-init children are not supported.");
        if (child is not null)
        {
            _staticChildren.Add(child);
            _content = null;
        }
    }

    /// <summary>
    /// Collection-initializer overload that exists only to give a clear
    /// error message — Kotlin's <c>composable(route) { ... }</c> has no
    /// per-destination <c>Modifier</c> slot, so silently accepting one
    /// would be misleading. Wrap the destination's content in a
    /// <see cref="Box"/> / <see cref="Column"/> / <see cref="Row"/> with
    /// the modifier, or set the modifier on the surrounding
    /// <see cref="NavHost"/> instead.
    /// </summary>
    public void Add(Modifier modifier) =>
        throw new InvalidOperationException(
            "Compose Navigation's composable() route has no Modifier slot. " +
            "Apply the Modifier to a wrapping Box/Column/Row inside the destination, " +
            "or to the surrounding NavHost.");

    IEnumerator IEnumerable.GetEnumerator() => _staticChildren.GetEnumerator();
}
