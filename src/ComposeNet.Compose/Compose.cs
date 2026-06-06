using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Top-level composition utilities that need to be callable from anywhere
/// inside a composition pass — not just from <see cref="ComposeActivity"/>
/// subclasses. Use these from extracted helper composables, custom
/// <see cref="ComposableNode"/> subclasses, etc.
/// </summary>
public static class Compose
{
    /// <summary>
    /// Compose's <c>remember { factory() }</c>, backed by the active
    /// composer's slot table. Returns the value of <paramref name="factory"/>
    /// the first time this call site is reached, then the cached value on
    /// subsequent recompositions.
    ///
    /// The slot is keyed by <c>HashCode.Combine(line, file)</c> from the
    /// <see cref="CallerLineNumberAttribute"/> / <see cref="CallerFilePathAttribute"/>
    /// fill-ins so two call sites in different files (or different lines)
    /// never share a slot — and so the same call site reached repeatedly
    /// across recompositions does share its slot, returning the cached
    /// value.
    ///
    /// Wrapped in <c>StartReplaceableGroup</c> / <c>EndReplaceableGroup</c>
    /// so the slot belongs to its own group; sibling positional grouping
    /// (see <see cref="ComposableContainer.RenderChildren"/>) then keeps
    /// repeated helper calls in a parent container from sharing slots.
    ///
    /// Must be called inside a composition (i.e. on the thread currently
    /// running a <see cref="ComposableLambda2"/>/<c>3</c>/<c>4</c> body, or
    /// inside <c>Render</c> on a node reached from one of those). Otherwise
    /// throws <see cref="System.InvalidOperationException"/>.
    /// </summary>
    public static T Remember<T>(
        System.Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.Remember<T> must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(System.HashCode.Combine(line, file));
        try
        {
            if (composer.RememberedValue() is RememberHolder existing)
                return (T)existing.Value!;
            var value = factory();
            composer.UpdateRememberedValue(new RememberHolder(value));
            return value;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
