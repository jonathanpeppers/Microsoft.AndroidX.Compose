using Microsoft.AndroidX.Compose.Maui.Sample.Pages;

namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>
/// Root MAUI <see cref="Shell"/>. Hosts a single <c>HomePage</c> as
/// the gallery index; demo pages are registered as routes here so
/// <see cref="HomePage"/> can navigate via
/// <see cref="Shell.GoToAsync(string)"/> with the demo's route name.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>Construct the shell + register demo routes.</summary>
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("counter",        typeof(CounterPage));
        Routing.RegisterRoute("buttons",        typeof(ButtonsPage));
        Routing.RegisterRoute("labels",         typeof(LabelsPage));
        Routing.RegisterRoute("entries",        typeof(EntriesPage));
        Routing.RegisterRoute("image-aspects",  typeof(ImageAspectsPage));
        Routing.RegisterRoute("image-sources",  typeof(ImageSourcesPage));
        Routing.RegisterRoute("modifiers",      typeof(ModifiersPage));
        Routing.RegisterRoute("toggles",        typeof(TogglesPage));
        Routing.RegisterRoute("theme",          typeof(ThemePage));
    }
}
