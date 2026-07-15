using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Creates remembered mutable state.</summary>
    public static MutableState<T> MutableStateOf<T>(
        T initial,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.MutableStateOf(
            ComposableContext.Current, initial, line, file);

    /// <summary>Creates remembered integer state.</summary>
    public static MutableNumberState<int> MutableStateOf(
        int initial,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.MutableStateOf(
            ComposableContext.Current, initial, line, file);

    /// <summary>Creates remembered long state.</summary>
    public static MutableNumberState<long> MutableStateOf(
        long initial,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.MutableStateOf(
            ComposableContext.Current, initial, line, file);

    /// <summary>Creates remembered floating-point state.</summary>
    public static MutableNumberState<float> MutableStateOf(
        float initial,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.MutableStateOf(
            ComposableContext.Current, initial, line, file);

    /// <summary>Creates remembered double-precision state.</summary>
    public static MutableNumberState<double> MutableStateOf(
        double initial,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.MutableStateOf(
            ComposableContext.Current, initial, line, file);

    /// <summary>Creates a derived state value.</summary>
    public static DerivedState<T> DerivedStateOf<T>(Func<T> calculation) =>
        ComposeExtensions.DerivedStateOf(calculation);
}
