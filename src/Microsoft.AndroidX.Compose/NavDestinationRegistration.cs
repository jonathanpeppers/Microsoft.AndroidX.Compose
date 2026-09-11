using AndroidX.Navigation;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

internal sealed class NavDestinationRegistration
{
    readonly MutableManagedState<NavDestinationContent?> _content;
    readonly IFunction3 _callback;

    internal string Route { get; }

    internal NavDestinationRegistration(NavDestination destination)
    {
        Route = destination.Route;
        _content = new(destination.Content);
        _callback = ComposableLambdas.InstantiateNavComposable((entry, composer) =>
        {
            // Cleared registrations may still be held by an external controller.
            // Reading the state here subscribes the deferred destination scope,
            // not the parent that publishes content after applying composition.
            _content.Value?.Render(entry, composer);
        });
    }

    internal void Publish(NavDestinationContent content) => _content.Value = content;

    internal void Clear() => _content.Value = null;

    internal void RegisterInto(NavGraphBuilder graphBuilder) =>
        ComposeBridges.NavGraphBuilderComposable(
            navGraphBuilder: ((Java.Lang.Object)graphBuilder).Handle,
            route: Route,
            arguments: null,
            deepLinks: null,
            content: _callback);
}
