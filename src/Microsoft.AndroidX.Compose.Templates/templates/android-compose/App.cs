using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace MyApplication;

/// <summary>Builds the application's root composition.</summary>
internal static class App
{
    /// <summary>Creates the root Compose node tree.</summary>
    internal static ComposableNode Build(IComposer composer)
    {
        var currentDestination = composer.RememberSaveable(
            () => new MutableNumberState<int>(0));

        return AppTheme.Build(
            composer,
            BuildNavigationSuite(currentDestination));
    }

    static NavigationSuiteScaffold BuildNavigationSuite(
        MutableNumberState<int> currentDestination)
    {
        var navigationSuite = new NavigationSuiteScaffold
        {
            Content = new Scaffold
            {
                Modifier = Modifier.FillMaxSize(),
                Body = new Text("Hello Android!"),
            },
        };
        for (int index = 0; index < AppDestination.All.Count; index++)
        {
            int destinationIndex = index;
            var destination = AppDestination.All[index];
            navigationSuite.Add(new NavigationSuiteItem(
                selected: currentDestination.Value == destinationIndex,
                onClick: () => currentDestination.Value = destinationIndex)
            {
                Icon = new Icon(destination.Icon, destination.Label),
                Label = new Text(destination.Label),
            });
        }
        return navigationSuite;
    }
}
