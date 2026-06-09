using global::AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// Top-level composition utilities — the C# parity of every helper Kotlin's
/// IR rewrite would normally inject through the implicit <c>$composer</c>
/// parameter on a <c>@Composable</c> function.
///
/// Every method that needs the active composer takes <see cref="IComposer"/>
/// as its receiver (<c>this IComposer composer</c>) so call sites read
/// <c>composer.Remember(...)</c>, <c>composer.LaunchedEffect(...)</c>,
/// <c>composer.StringResource(R.string.foo)</c> — exactly the
/// <c>$composer.remember(...)</c> shape Compose ships under the hood. The
/// composer is always handed to the body explicitly by the
/// <see cref="ComposableLambda2"/> / <see cref="ComposableLambda3"/> /
/// <see cref="ComposableLambda4"/> adapters, by
/// <see cref="ComposableNode.Render(IComposer)"/> overrides, and by the
/// <see cref="SetContent(global::AndroidX.Activity.ComponentActivity, Func{IComposer, ComposableNode})"/>
/// entry point — no ambient <c>[ThreadStatic]</c> lookup, no base class
/// required.
///
/// Methods that don't read a slot (<see cref="MutableStateOf{T}"/>,
/// <see cref="MutableIntStateOf"/>, <see cref="DerivedStateOf{T}"/>,
/// <see cref="NewTextFieldValue"/>, …) stay as plain statics on this class
/// — the composer is genuinely irrelevant to them.
/// </summary>
public static partial class ComposeExtensions
{
}
