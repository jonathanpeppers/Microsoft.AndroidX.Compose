using global::Android.Content;
using global::AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>
/// Showcases the <c>ComposeView</c> interop entry point — hosting
/// Compose content inside an existing Android <c>View</c> hierarchy
/// (the inverse of <c>ComponentActivity.SetContent</c>, which uses a
/// <c>ComposeView</c> as the activity's root view).
/// </summary>
public static class ComposeViewInteropDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "locals-compose-view-interop",
        CategoryId:  "locals-misc",
        Title:       "ComposeView interop",
        Description: "Hosts Compose inside a native Android LinearLayout via ComposeView.SetContent — the canonical pattern for adding Compose to a legacy View-based screen.",
        Build:       _ => new Column
        {
            new Text("ComposeView is the View-hierarchy entry point for Compose. Use it when an existing Android layout (XML, fragment, RecyclerView cell) needs to host Compose content."),
            new Text(""),
            new Text("Equivalent Kotlin:"),
            new Text("    val compose = ComposeView(context)"),
            new Text("    compose.setContent { Text(\"Hi\") }"),
            new Text("    parent.addView(compose)"),
            new Text(""),
            new Text("In this repo:"),
            new Text("    var compose = new ComposeView(context);"),
            new Text("    compose.SetContent(c => new Text(\"Hi\"));"),
            new Text("    parent.AddView(compose);"),
            new Text(""),
            new LaunchInteropActivity(),
        });

    /// <summary>
    /// Reads <c>Locals.LocalContext</c> to get the host activity, then
    /// renders a <see cref="Button"/> that launches
    /// <see cref="ComposeViewActivity"/> via an explicit
    /// <see cref="Intent"/>.
    /// </summary>
    sealed class LaunchInteropActivity : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var ctx = Locals.LocalContext.GetCurrent(composer);
            new Button(onClick: () =>
            {
                var intent = new Intent(ctx, typeof(ComposeViewActivity));
                intent.AddFlags(ActivityFlags.NewTask);
                ctx.StartActivity(intent);
            })
            {
                new Text("Launch ComposeView activity"),
            }.Render(composer);
        }
    }
}
