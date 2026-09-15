namespace AndroidX.Compose;

/// <summary>Material 3 <c>FloatingActionButton</c>.</summary>
/// <remarks>
/// Leave <see cref="ContainerColor"/>, <see cref="ContentColor"/>,
/// <see cref="Elevation"/>, and <see cref="InteractionSource"/> unset for Kotlin defaults.
/// A supplied container color selects its theme content role unless ContentColor is also set;
/// transparent is an explicit color, not omission. Non-theme containers inherit the ambient
/// content color. Elevation uses the bound Material 3 elevation object (for example,
/// <c>FloatingActionButtonDefaults.Instance.BottomAppBarFabElevation(0, 0, 0, 0)</c>).
/// Remember a hoisted interaction source; null leaves interaction handling to Kotlin.
/// Do not dispose supplied Java peers while the composition uses them.
/// </remarks>
public sealed partial class FloatingActionButton;
