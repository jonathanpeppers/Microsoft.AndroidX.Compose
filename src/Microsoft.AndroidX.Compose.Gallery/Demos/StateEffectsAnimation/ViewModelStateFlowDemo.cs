using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>UDF pattern — ViewModel + MutableStateFlow + collectAsStateWithLifecycle.</summary>
public static class ViewModelStateFlowDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-viewmodel-stateflow",
        CategoryId:  "state-effects",
        Title:       "ViewModel + StateFlow (UDF)",
        Description: "A CounterViewModel exposes a MutableStateFlow<int>; the composable collects it and renders the current value. Increment / Reset post mutations through the VM.",
        Build:       c =>
        {
            var vm = c.ViewModel(() => new CounterViewModel());
            int count = vm.Count.CollectAsStateWithLifecycle().Value;

            return new Column
            {
                Modifier.Companion.Padding(16),
                new Text($"Count: {count}")
                {
                    FontSize   = 22,
                    FontWeight = FontWeight.SemiBold,
                },
                new Spacer { Modifier = Modifier.Companion.Height(12) },
                new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                {
                    new Button(onClick: () => vm.Increment()) { new Text("Increment") },
                    new Button(onClick: () => vm.Reset())     { new Text("Reset") },
                    new Button(onClick: () => _ = vm.AddInBackgroundAsync(5)) { new Text("+5 (async)") },
                },
            };
        });

    sealed class CounterViewModel : ViewModel
    {
        readonly MutableStateFlow<int> _count = new(0);

        public IStateFlow<int> Count => _count;

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
