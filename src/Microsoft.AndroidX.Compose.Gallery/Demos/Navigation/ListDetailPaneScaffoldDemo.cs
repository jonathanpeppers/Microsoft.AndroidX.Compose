using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>Fold-aware Material 3 list-detail navigation with stable content keys.</summary>
public static class ListDetailPaneScaffoldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-list-detail-pane",
        CategoryId:  "navigation",
        Title:       "List-detail pane scaffold",
        Description: "Navigable list/detail panes that react to window width and separating or occluding hinges.",
        Build:       c =>
        {
            var navigator =
                c.RememberListDetailPaneScaffoldNavigator<long>();
            var scope = c.RememberCoroutineScope();
            long selected = navigator.CurrentPane == AdaptivePaneRole.Detail
                ? navigator.CurrentContentKey
                : 1L;

            var list = new Column(
                verticalArrangement: Arrangement.SpacedBy(8.Dp()))
            {
                Modifier = Modifier.FillMaxSize().Padding(16),
            };
            list.Add(new Text(
                $"List pane\nPartitions: {navigator.ScaffoldDirective.MaxHorizontalPartitions}"));
            for (long id = 1; id <= 4; id++)
            {
                long current = id;
                list.Add(new Button(
                    () => Run(scope, ct => navigator.NavigateToAsync(
                        AdaptivePaneRole.Detail,
                        current,
                        ct)))
                {
                    new Text($"Open message {current}"),
                });
            }

            var detail = new Column(
                verticalArrangement: Arrangement.SpacedBy(12.Dp()))
            {
                Modifier = Modifier.FillMaxSize().Padding(16),
            };
            detail.Add(new Text($"Detail pane\nMessage {selected}"));
            detail.Add(new Text(
                $"Focused pane: {navigator.CurrentPane?.ToString() ?? "none"}"));
            detail.Add(new Button(
                () => Run(scope, ct => navigator.NavigateBackAsync(
                    cancellationToken: ct)),
                enabled: navigator.CanNavigateBack())
            {
                new Text("Back to list"),
            });

            return new NavigableListDetailPaneScaffold<long>(navigator)
            {
                Modifier = Modifier.FillMaxWidth().Height(420),
                ListPane = list,
                DetailPane = detail,
            };
        });

    static async void Run(
        CoroutineScope scope,
        Func<CancellationToken, Task> action)
    {
        try
        {
            await scope.Launch(action);
        }
        catch (OperationCanceledException)
        {
            // Leaving the demo cancels this await; Kotlin may finish the transition.
        }
    }
}
