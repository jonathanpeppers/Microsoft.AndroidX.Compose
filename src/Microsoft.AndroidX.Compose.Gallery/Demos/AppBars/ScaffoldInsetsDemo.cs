using Android.Content;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.AppBars;

/// <summary>Launches an edge-to-edge Scaffold inset ownership demonstration.</summary>
public static class ScaffoldInsetsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "appbars-scaffold-insets",
        CategoryId: "app-bars-tabs",
        Title: "Scaffold content insets",
        Description: "Default, zero, and excluded insets with a real IME and selector.",
        Build: _ => new Button(() =>
        {
            var context = Android.App.Application.Context;
            context.StartActivity(new Intent(context, typeof(ScaffoldInsetsDemoActivity))
                .AddFlags(ActivityFlags.NewTask));
        })
        {
            new Text("Open edge-to-edge inset demo"),
        });
}
