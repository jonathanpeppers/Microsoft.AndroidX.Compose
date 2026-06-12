namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Exercises the Phase 2 Slice 11 semantics bridge —
/// <see cref="SemanticProperties.HintProperty"/> /
/// <see cref="SemanticProperties.DescriptionProperty"/> /
/// <see cref="SemanticProperties.HeadingLevelProperty"/> + the
/// view-level <see cref="VisualElement.AutomationId"/> are translated
/// into Compose <c>Modifier.Semantics { … }</c> +
/// <c>Modifier.TestTag(…)</c> by
/// <c>Microsoft.AndroidX.Compose.Maui.Platform.SemanticsBridge</c>.
/// See <c>docs/maui-backend.md</c> for the mapping table.
/// </summary>
public partial class SemanticsPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public SemanticsPage()
    {
        InitializeComponent();
    }
}
