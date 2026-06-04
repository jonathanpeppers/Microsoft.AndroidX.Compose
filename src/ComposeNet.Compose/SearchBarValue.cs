namespace ComposeNet;

/// <summary>
/// Mirror of Kotlin's <c>androidx.compose.material3.SearchBarValue</c>
/// enum — the visibility state of a <see cref="SearchBar"/> pair.
/// Currently unused in the facade (the wrapper always defaults
/// <c>initialValue</c> to <see cref="Collapsed"/> at the JNI boundary)
/// but exposed so future overloads can accept an initial value.
/// </summary>
public enum SearchBarValue
{
    /// <summary>The search bar is collapsed and only the input field bar is visible.</summary>
    Collapsed = 0,
    /// <summary>The search bar is expanded and the results popup is visible.</summary>
    Expanded = 1,
}
