using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

// An immutable publication of the factory or static-child list, not the host tree.
internal sealed class NavDestinationContent(
    Func<NavBackStackEntry, ComposableNode>? factory,
    ComposableNode[] children)
{
    internal void Render(IntPtr entryHandle, IComposer composer)
    {
        if (factory is not null)
        {
            var entry = entryHandle == IntPtr.Zero
                ? null
                : Java.Lang.Object.GetObject<AndroidX.Navigation.NavBackStackEntry>(
                    entryHandle, JniHandleOwnership.DoNotTransfer);
            if (entry is null)
                throw new InvalidOperationException(
                    "Compose Navigation invoked a destination with a null back-stack entry.");
            RenderChild(factory(new NavBackStackEntry(entry)), 0, composer);
        }
        else
        {
            for (int i = 0; i < children.Length; i++)
                RenderChild(children[i], i, composer);
        }
    }

    static void RenderChild(ComposableNode child, int index, IComposer composer)
    {
        composer.StartReplaceableGroup(CompositionGroupKey.Compute(index, child.GetType()));
        try { child.Render(composer); }
        finally { composer.EndReplaceableGroup(); }
    }
}
