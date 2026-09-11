using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

internal sealed class NavHostGraph
{
    readonly NavDestinationRegistration[] _routes;

    internal NavGraphBuilderLambda Builder { get; }

    internal NavHostGraph(IReadOnlyList<NavDestination> destinations)
    {
        _routes = new NavDestinationRegistration[destinations.Count];
        for (int i = 0; i < destinations.Count; i++)
            _routes[i] = new(destinations[i]);
        Builder = new(graphBuilder =>
        {
            foreach (var route in _routes)
                route.RegisterInto(graphBuilder);
        });
    }

    internal Dictionary<string, NavDestinationContent> Capture(IReadOnlyList<NavDestination> destinations)
    {
        var content = new Dictionary<string, NavDestinationContent>(StringComparer.Ordinal);
        foreach (var destination in destinations)
            content[destination.Route] = destination.Content;
        return content;
    }

    internal void Publish(Dictionary<string, NavDestinationContent> content)
    {
        foreach (var route in _routes)
        {
            if (content.TryGetValue(route.Route, out var latest))
                route.Publish(latest);
        }
    }

    internal void PublishAfterComposition(IComposer composer, Dictionary<string, NavDestinationContent> content)
    {
        // The native side-effect adapter can outlive its invocation. Consume
        // its payload so it cannot retain a host or obsolete destination tree.
        Dictionary<string, NavDestinationContent>? pending = content;
        composer.SideEffect(() =>
        {
            var latest = pending
                ?? throw new InvalidOperationException("NavHost content was already published.");
            pending = null;
            Publish(latest);
        });
    }

    internal void Clear()
    {
        foreach (var route in _routes)
            route.Clear();
    }

    internal Action CreateCleanup() => Clear;
}
