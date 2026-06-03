using Android.Runtime;
using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI;
using Java.Interop;

namespace ComposeNet;

/// <summary>
/// Foundation <c>Spacer</c> composable — an empty layout used to
/// reserve space (typically via a sizing modifier such as
/// <c>Modifier.Size(8)</c> or <c>Modifier.Width(16)</c>) between
/// siblings inside a <see cref="Row"/> or <see cref="Column"/>.
/// </summary>
/// <remarks>
/// Unlike most Compose composables, Kotlin's <c>Spacer(modifier)</c>
/// has NO default for its modifier parameter — the JVM method has no
/// <c>$default</c> companion. The wrapper materialises
/// <c>Modifier.Companion</c> (the empty modifier) when the caller
/// doesn't supply a chain so the binding receives a non-null value.
/// </remarks>
public sealed class Spacer : ComposableNode
{
    /// <summary>Empty Spacer (zero-sized). Apply a sizing modifier to give it presence.</summary>
    public Spacer() { }

    /// <summary>Convenience overload that sets <see cref="ComposableNode.Modifier"/>.</summary>
    public Spacer(Modifier modifier) => Modifier = modifier;

    internal override void Render(IComposer composer)
    {
        var modifier = BuildModifier()
            ?? Java.Lang.Object.GetObject<IModifier>(
                ComposeBridges.ModifierCompanionInstance(),
                JniHandleOwnership.TransferLocalRef)!;

        try
        {
            SpacerKt.Spacer(modifier, composer, 0);
        }
        finally
        {
            GC.KeepAlive(modifier);
            GC.KeepAlive(composer);
        }
    }
}
