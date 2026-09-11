using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.ComposableMethods;

/// <summary>Independent saved child state under repeated parents and selectively visible children.</summary>
public static class ConditionalIdentityDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "composable-conditional-identity",
        CategoryId: "composable-methods",
        Title: "Conditional child identity",
        Description: "Count B, then show A. Each fixed parent keeps its own saved child state.",
        Build: static _ => new ComposableDemoAdapter(() => Parent()));

    /// <summary>Renders two fixed parent occurrences, initially showing only B's child.</summary>
    [Composable]
    public static void Parent()
    {
        var visible = RememberSaveable(() => new MutableNumberState<int>(2));
        Column(() =>
        {
            Text("Count B, then show A: B keeps its value and A starts at zero.");
            Text("Hiding a child forgets that child's state, not its sibling's.");
            Button(() => visible.Value ^= 1, () => Text("Toggle A"));
            Button(() => visible.Value ^= 2, () => Text("Toggle B"));
            for (int i = 0; i < 2; i++)
                RepeatedParent(i, (visible.Value & (1 << i)) != 0);
        });
    }

    /// <summary>Keeps the loop occurrence present when its child is hidden.</summary>
    [Composable]
    public static void RepeatedParent(int index, bool visible)
    {
        if (visible)
            Child(index == 0 ? "A" : "B");
    }

    /// <summary>Owns a saved count independently of the other parent's child.</summary>
    [Composable]
    public static void Child(string label)
    {
        var count = RememberSaveable(() => new MutableNumberState<int>(0));
        Button(() => count.Value++, () => Text($"{label}: {count.Value}"));
    }
}
