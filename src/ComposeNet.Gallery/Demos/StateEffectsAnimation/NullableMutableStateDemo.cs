using AndroidX.Compose.Runtime;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>
/// <see cref="MutableState{T}"/> and <see cref="CompositionLocal{T}"/>
/// with a nullable value type (<c>long?</c>) — the "nothing selected
/// yet" sentinel pattern. Toggling between <c>null</c> and a non-null
/// value round-trips through the JVM box without throwing
/// <c>NotSupportedException</c> (regression test for issue #173).
/// </summary>
public static class NullableMutableStateDemo
{
    static readonly CompositionLocal<long?> LocalSelectedId =
        CompositionLocal.Of<long?>(() => null);

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-nullable-value-types",
        CategoryId:  "state-effects",
        Title:       "MutableState<long?> + nullable Local",
        Description: "Nullable primitives (long?, int?, bool?) round-trip through MutableState and CompositionLocal without unbox errors. Toggle the selected ID between null and a value.",
        Build:       () =>
        {
            var selectedId = Compose.Remember(() => new MutableState<long?>(null));
            var taps       = Compose.Remember(() => new MutableNumberState<int>(0));

            return new Column
            {
                new Text($"selectedId.Value: {selectedId.Value?.ToString() ?? "<null>"}"),
                new Text($"taps:             {taps.Value}"),
                new Row
                {
                    new Button(onClick: () =>
                    {
                        taps.Value++;
                        selectedId.Value = 100L + taps.Value;
                    })
                    { new Text("Select next") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => selectedId.Value = null)
                        { new Text("Clear") },
                },
                new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                new Text("Below: a CompositionLocal<long?> reads null by default, then a provider supplies the current selectedId:"),
                new SelectedIdLabel(),
                new CompositionLocalProvider
                {
                    LocalSelectedId.Provides(selectedId.Value),
                    new Column
                    {
                        Modifier.Companion.Padding(8),
                        new SelectedIdLabel(),
                    },
                },
            };
        });

    sealed class SelectedIdLabel : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var id = LocalSelectedId.GetCurrent(composer);
            new Text($"  LocalSelectedId.Current: {id?.ToString() ?? "<null>"}").Render(composer);
        }
    }
}
