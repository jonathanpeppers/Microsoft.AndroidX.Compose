using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

// Deliberately bypasses content publication: only Kotlin can choose the winner.
sealed class NavDuplicateRegistrationProbe : ComposableNode
{
    readonly NavHostGraph _graph = new([
        new NavDestination("home", _ => new NavContentProbe("home", "home: Account 0", static () => { })),
        new NavDestination("home", _ => new NavContentProbe("home", "home: Account 10", static () => { })),
    ]);

    public override void Render(IComposer composer)
    {
        var controller = ComposeBridges.RememberNavController(composer);
        composer.DisposableEffect(_graph.Builder, _graph.CreateCleanup);
        ComposeBridges.NavHost(controller, "home", null, null, _graph.Builder, composer);
    }
}
