using global::AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>
    /// Construct an <c>androidx.compose.ui.text.input.TextFieldValue</c>
    /// — the text + caret-selection + IME-composition triple that drives
    /// the <see cref="TextField(MutableState{global::AndroidX.Compose.UI.Text.Input.TextFieldValue})"/>
    /// overload. Hand-bridged because the Kotlin primary ctor is stripped
    /// from the binding (the <c>selection: TextRange</c> param is a
    /// <c>@JvmInline value class</c>; see issue #204).
    /// </summary>
    public static global::AndroidX.Compose.UI.Text.Input.TextFieldValue NewTextFieldValue(
        string text = "",
        long selection = 0L,
        global::AndroidX.Compose.UI.Text.TextRange? composition = null)
        => ComposeBridges.NewTextFieldValueImpl(text, selection, composition);

    /// <summary>
    /// Convenience overload: <c>composer.NewTextFieldValue(...)</c> for
    /// call sites already inside a composable that prefer the same
    /// dotted-on-the-composer shape as <see cref="Remember{T}(IComposer, Func{T}, int, string)"/>.
    /// The composer is unused — this just forwards to
    /// <see cref="NewTextFieldValue(string, long, global::AndroidX.Compose.UI.Text.TextRange?)"/>
    /// — but it keeps demos consistent with the "everything Compose-related
    /// hangs off <c>c</c>" idiom. Outside composition (button callbacks,
    /// ViewModels) call the static form directly, exactly as Kotlin's
    /// <c>TextFieldValue("")</c> ctor isn't <c>@Composable</c>.
    /// </summary>
    public static global::AndroidX.Compose.UI.Text.Input.TextFieldValue NewTextFieldValue(
        this IComposer composer,
        string text = "",
        long selection = 0L,
        global::AndroidX.Compose.UI.Text.TextRange? composition = null)
        => NewTextFieldValue(text, selection, composition);

    /// <summary>
    /// Compose's <c>derivedStateOf { calculation() }</c>: returns a
    /// read-only <see cref="DerivedState{T}"/> whose value is lazily
    /// computed by <paramref name="calculation"/>. Compose tracks which
    /// state values <paramref name="calculation"/> reads and only re-runs
    /// it when one of them changes. Cache the returned instance via
    /// <see cref="Remember{T}(IComposer, Func{T}, int, string)"/> so it
    /// survives recomposition.
    /// </summary>
    public static DerivedState<T> DerivedStateOf<T>(Func<T> calculation)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        var jcw = new ObjectFunction0(() => MutableState<T>.ToJava(calculation()));
        var state = SnapshotStateKt.DerivedStateOf(jcw);
        return new DerivedState<T>(state);
    }

    /// <summary>
    /// Convenience overload: <c>composer.DerivedStateOf(() =&gt; ...)</c> for
    /// call sites that prefer the dotted-on-the-composer shape. The
    /// composer is unused — this just forwards to
    /// <see cref="DerivedStateOf{T}(Func{T})"/>. Wrap the result in
    /// <see cref="Remember{T}(IComposer, Func{T}, int, string)"/> to cache
    /// it across recompositions.
    /// </summary>
    public static DerivedState<T> DerivedStateOf<T>(this IComposer composer, Func<T> calculation)
        => DerivedStateOf(calculation);
}
