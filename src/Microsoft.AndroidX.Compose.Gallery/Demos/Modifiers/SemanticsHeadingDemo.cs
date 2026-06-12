using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>
/// <see cref="SemanticsScope.Heading"/> — tags a node as a heading
/// so TalkBack announces "heading" alongside the content description
/// and lets the user jump between headings via the rotor / swipe.
/// </summary>
public static class SemanticsHeadingDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-semantics-heading",
        CategoryId:  "modifiers",
        Title:       "Semantics heading()",
        Description: "Modifier.Semantics { heading() } — Foundation's heading boolean. TalkBack announces \"heading\" after the content description; rotor swipe jumps between headings.",
        Build:       c =>
        {
            return new Column
            {
                new Text(
                    "Settings → Accessibility → TalkBack to hear "
                  + "the announcements. Sections marked as headings "
                  + "get a 'heading' suffix in TalkBack's voice and "
                  + "are reachable via the rotor's 'Headings' control.")
                { Modifier = Modifier.Padding(8) },

                new Text("Settings")
                {
                    Modifier = Modifier
                        .Padding(start: new Dp(8), top: new Dp(16), end: new Dp(8), bottom: new Dp(4))
                        .Semantics(mergeDescendants: true, s => s
                            .ContentDescription("Settings")
                            .Heading()),
                },

                new Text("Choose a theme") { Modifier = Modifier.Padding(8) },
                new Text("Manage notifications") { Modifier = Modifier.Padding(8) },

                new Text("Account")
                {
                    Modifier = Modifier
                        .Padding(start: new Dp(8), top: new Dp(16), end: new Dp(8), bottom: new Dp(4))
                        .Semantics(mergeDescendants: true, s => s
                            .ContentDescription("Account")
                            .Heading()),
                },

                new Text("Sign out") { Modifier = Modifier.Padding(8) },
                new Text("Delete account") { Modifier = Modifier.Padding(8) },
            };
        });
}
