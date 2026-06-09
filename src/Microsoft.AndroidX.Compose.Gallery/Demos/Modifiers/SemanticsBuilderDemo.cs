using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>
/// Fluent <see cref="Modifier.Semantics(Action{SemanticsScope})"/>
/// builder demo — exposes Selected, Role, ContentDescription,
/// StateDescription, and OnClick(label, action) on a single chain.
/// Models a multi-select email row from the upstream Reply sample.
/// </summary>
public static class SemanticsBuilderDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-semantics-builder",
        CategoryId:  "modifiers",
        Title:       "Semantics builder (Selected/Role/OnClick)",
        Description: "Modifier.Semantics(s => s.Selected(..).Role(..).ContentDescription(..).OnClick(\"Open\", ..)). Enable TalkBack to hear the announcements.",
        Build:       () =>
        {
            var selected = ComposeRuntime.Remember(() => new MutableState<bool>(false));
            var opens    = ComposeRuntime.Remember(() => new MutableNumberState<int>(0));

            // The card has TWO accessibility actions:
            //   1. The standard click — toggles selection.
            //   2. A labelled OnClick — "Open email" in TalkBack's
            //      actions menu, increments a counter.
            // With TalkBack on, double-tap fires the labelled action.
            // Without TalkBack, the physical tap toggles selection
            // (only the Clickable handler runs in that case).
            return new Column
            {
                new Text("Tap the email card to toggle selection."),
                new Text("With TalkBack on, the same card announces "
                       + "'Tab, selected/not selected, Email from Alice...' "
                       + "and exposes an 'Open email' action you can "
                       + "trigger from the actions menu."),
                new Text("Settings → Accessibility → TalkBack to enable "
                       + "the screen reader."),

                new Card
                {
                    Modifier.Companion
                        .FillMaxWidth()
                        .Padding(8)
                        .Clickable(() => selected.Value = !selected.Value)
                        .Semantics(mergeDescendants: true, s => s
                            .Selected(selected.Value)
                            .Role(SemanticsRole.Tab)
                            .ContentDescription("Email from Alice — Reply samples PR review")
                            .StateDescription(selected.Value ? "Selected" : "Not selected")
                            .OnClick("Open email", () =>
                            {
                                opens.Value++;
                                return true;
                            })),
                    new Column
                    {
                        Modifier.Companion.Padding(12),
                        new Text(selected.Value ? "✓ Alice" : "Alice"),
                        new Text("Reply samples PR review"),
                        new Text($"'Open email' triggered {opens} time(s) "
                               + "via TalkBack actions menu."),
                    },
                },

                // ClearAndSet variant — masks all descendant semantics
                // and replaces them with our curated description, so
                // TalkBack reads a single "Three new messages" instead
                // of walking into each child Text.
                new Card
                {
                    Modifier.Companion
                        .FillMaxWidth()
                        .Padding(8)
                        .ClearAndSetSemantics(s => s
                            .Role(SemanticsRole.Button)
                            .ContentDescription("Three new messages, double-tap to open inbox")),
                    new Column
                    {
                        Modifier.Companion.Padding(12),
                        new Text("📬 3 new"),
                        new Text("Alice, Bob, Carol"),
                    },
                },
            };
        });
}
