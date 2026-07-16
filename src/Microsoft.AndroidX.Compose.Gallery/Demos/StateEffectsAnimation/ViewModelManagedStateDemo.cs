using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>UDF pattern with thread-safe managed state owned by a view model.</summary>
public static class ViewModelManagedStateDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-viewmodel-managed-state",
        CategoryId:  "state-effects",
        Title:       "ViewModel + managed state",
        Description: "A CounterViewModel exposes read-only IState<int> backed by MutableManagedState<int>. Increment and Reset mutate it through the view model.",
        Build:       c =>
        {
            var vm = c.ViewModel(() => new CounterViewModel());
            int count = vm.Count.Value;

            return new Column
            {
                Modifier.Padding(16),
                new Text($"Count: {count}")
                {
                    FontSize   = 22,
                    FontWeight = FontWeight.SemiBold,
                },
                new Spacer { Modifier = Modifier.Height(12) },
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(onClick: () => vm.Increment()) { new Text("Increment") },
                    new Button(onClick: () => vm.Reset())     { new Text("Reset") },
                    new Button(onClick: () => _ = vm.AddInBackgroundAsync(5)) { new Text("+5 (async)") },
                },
            };
        });

    sealed class CounterViewModel : ViewModel
    {
        readonly MutableManagedState<int> _count = new(0);

        public IState<int> Count => _count;

        public void Increment() => _count.Update(static c => c + 1);

        public void Reset() => _count.Value = 0;

        public Task AddInBackgroundAsync(int delta) => LaunchAsync(async ct =>
        {
            try { await Task.Delay(500, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }
            _count.Update(c => c + delta);
        });
    }
}
