namespace AndroidX.Compose;

/// <summary>Identifies a pane in a Material 3 list-detail scaffold.</summary>
public enum AdaptivePaneRole
{
    /// <summary>The pane containing the list of selectable items.</summary>
    List,

    /// <summary>The pane containing details for the selected item.</summary>
    Detail,

    /// <summary>An optional pane containing additional context.</summary>
    Extra,
}
