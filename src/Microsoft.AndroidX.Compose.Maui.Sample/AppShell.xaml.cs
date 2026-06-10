namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>
/// Root MAUI <see cref="Shell"/>. Matches the <c>dotnet new maui</c>
/// template so the <c>Style TargetType="Shell"</c> resource in
/// <c>Resources/Styles/Styles.xaml</c> applies (white bar + black title
/// text in light mode). The <c>NavigationPage</c> style in that file uses
/// a near-white <c>BarTextColor</c> for light mode and is unusable in
/// practice — the template has been Shell-only for many releases.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>Construct the shell.</summary>
    public AppShell()
    {
        InitializeComponent();
    }
}
