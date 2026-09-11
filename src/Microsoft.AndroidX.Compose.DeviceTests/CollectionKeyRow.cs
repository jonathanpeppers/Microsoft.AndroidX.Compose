using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class CollectionKeyRow(int item, int generation) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        var state = composer.Remember(() => new MutableNumberState<int>(0));
        var saved = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        CollectionKeysTestActivity.RowStates[item] = (state, saved);
        CollectionKeysTestActivity.Observed[item] = (generation, state.Value, saved.Value);
        new Text($"Record {item}: {state.Value}") { Modifier = Modifier.Size(100) }.Render(composer);
    }
}
