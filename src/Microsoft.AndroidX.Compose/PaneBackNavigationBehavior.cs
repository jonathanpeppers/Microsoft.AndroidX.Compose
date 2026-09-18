namespace AndroidX.Compose;

/// <summary>Controls how list-detail pane navigation consumes Back.</summary>
public enum PaneBackNavigationBehavior
{
    /// <summary>Remove only the latest pane destination.</summary>
    PopLatest,

    /// <summary>Pop until the visible pane arrangement changes.</summary>
    PopUntilScaffoldValueChange,

    /// <summary>Pop until the focused pane destination changes.</summary>
    PopUntilCurrentDestinationChange,

    /// <summary>Pop until the destination content key changes.</summary>
    PopUntilContentChange,
}
