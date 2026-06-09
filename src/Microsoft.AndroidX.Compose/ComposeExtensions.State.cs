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
}
