using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Top-level composition utilities — the C# parity of every helper Kotlin's
/// IR rewrite would normally inject through the implicit <c>$composer</c>
/// parameter on a <c>@Composable</c> function.
///
/// The tree-style methods that need the active composer take
/// <see cref="IComposer"/> as their receiver
/// (<c>this IComposer composer</c>) so call sites read
/// <c>composer.Remember(...)</c>, <c>composer.LaunchedEffect(...)</c>,
/// <c>composer.StringResource(R.string.foo)</c> — exactly the
/// <c>$composer.remember(...)</c> shape Compose ships under the hood. The
/// composer is handed to the implementation explicitly by the
/// <see cref="ComposableLambda2"/> / <see cref="ComposableLambda3"/> /
/// <see cref="ComposableLambda4"/> adapters, by
/// <see cref="ComposableNode.Render(IComposer)"/> overrides, and by the
/// <see cref="SetContent(AndroidX.Activity.ComponentActivity, Func{IComposer, ComposableNode})"/>
/// entry point. Composerless methods additionally expose that
/// explicit value through <see cref="ComposableContext"/> for the duration
/// of synchronous composable calls. Deferred callbacks outside that scope
/// must use an explicit composer-bearing boundary.
///
/// Helpers that don't read composition state remain plain static factories;
/// they do not expose a misleading <c>this IComposer</c> receiver.
/// </summary>
public static partial class ComposeExtensions
{
}
