using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

internal sealed class StableCollectionKeyRow(int id) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        var count = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        var button = new Button(() => count.Value++)
        {
            new Text($"Record {id}: {count.Value}"),
        };
        button.Modifier = Modifier.Width(150).Height(64 + id % 3 * 16);
        button.Render(composer);
    }
}
