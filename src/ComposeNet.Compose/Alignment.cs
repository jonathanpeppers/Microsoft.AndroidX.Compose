using AndroidX.Compose.UI;

namespace ComposeNet;

/// <summary>
/// C# wrapper around the <c>androidx.compose.ui.Alignment</c> singletons
/// exposed by Kotlin's <c>Alignment.Companion</c> object. Each public
/// static property forwards to the bound
/// <c>AndroidX.Compose.UI.IAlignment.Companion</c> singleton and caches
/// the wrapper for the lifetime of the process.
///
/// Use these with <see cref="Modifier.Align(ComposeNet.Alignment)"/>
/// inside a <see cref="Box"/> child to position the child within the
/// parent. Use the nested <see cref="Vertical"/> singletons inside a
/// <see cref="Row"/> and the nested <see cref="Horizontal"/> singletons
/// inside a <see cref="Column"/>.
/// </summary>
public sealed class Alignment
{
    internal IAlignment Java { get; }

    Alignment(IAlignment java) => Java = java;

    static Alignment? s_topStart, s_topCenter, s_topEnd, s_centerStart, s_center, s_centerEnd, s_bottomStart, s_bottomCenter, s_bottomEnd;

    /// <summary><c>Alignment.TopStart</c> — top-left in LTR, top-right in RTL.</summary>
    public static Alignment TopStart => s_topStart ??= new Alignment(IAlignment.Companion.TopStart);
    /// <summary><c>Alignment.TopCenter</c> — top-center.</summary>
    public static Alignment TopCenter => s_topCenter ??= new Alignment(IAlignment.Companion.TopCenter);
    /// <summary><c>Alignment.TopEnd</c> — top-right in LTR, top-left in RTL.</summary>
    public static Alignment TopEnd => s_topEnd ??= new Alignment(IAlignment.Companion.TopEnd);
    /// <summary><c>Alignment.CenterStart</c> — vertically centered, leading edge.</summary>
    public static Alignment CenterStart => s_centerStart ??= new Alignment(IAlignment.Companion.CenterStart);
    /// <summary><c>Alignment.Center</c> — centered both axes.</summary>
    public static Alignment Center => s_center ??= new Alignment(IAlignment.Companion.Center);
    /// <summary><c>Alignment.CenterEnd</c> — vertically centered, trailing edge.</summary>
    public static Alignment CenterEnd => s_centerEnd ??= new Alignment(IAlignment.Companion.CenterEnd);
    /// <summary><c>Alignment.BottomStart</c> — bottom-left in LTR, bottom-right in RTL.</summary>
    public static Alignment BottomStart => s_bottomStart ??= new Alignment(IAlignment.Companion.BottomStart);
    /// <summary><c>Alignment.BottomCenter</c> — bottom-center.</summary>
    public static Alignment BottomCenter => s_bottomCenter ??= new Alignment(IAlignment.Companion.BottomCenter);
    /// <summary><c>Alignment.BottomEnd</c> — bottom-right in LTR, bottom-left in RTL.</summary>
    public static Alignment BottomEnd => s_bottomEnd ??= new Alignment(IAlignment.Companion.BottomEnd);

    /// <summary>
    /// Vertical-only alignment singletons (<c>Alignment.Vertical</c>) for
    /// use with <see cref="Modifier.Align(ComposeNet.Alignment.Vertical)"/>
    /// inside a <see cref="Row"/>.
    /// </summary>
    public sealed class Vertical
    {
        internal IAlignmentVertical Java { get; }
        Vertical(IAlignmentVertical java) => Java = java;

        static Vertical? s_top, s_center, s_bottom;

        /// <summary><c>Alignment.Top</c> — align to the top of the row.</summary>
        public static Vertical Top => s_top ??= new Vertical(IAlignment.Companion.Top);
        /// <summary><c>Alignment.CenterVertically</c> — align to the vertical center.</summary>
        public static Vertical CenterVertically => s_center ??= new Vertical(IAlignment.Companion.CenterVertically);
        /// <summary><c>Alignment.Bottom</c> — align to the bottom of the row.</summary>
        public static Vertical Bottom => s_bottom ??= new Vertical(IAlignment.Companion.Bottom);
    }

    /// <summary>
    /// Horizontal-only alignment singletons (<c>Alignment.Horizontal</c>)
    /// for use with <see cref="Modifier.Align(ComposeNet.Alignment.Horizontal)"/>
    /// inside a <see cref="Column"/>.
    /// </summary>
    public sealed class Horizontal
    {
        internal IAlignmentHorizontal Java { get; }
        Horizontal(IAlignmentHorizontal java) => Java = java;

        static Horizontal? s_start, s_center, s_end;

        /// <summary><c>Alignment.Start</c> — align to the leading edge.</summary>
        public static Horizontal Start => s_start ??= new Horizontal(IAlignment.Companion.Start);
        /// <summary><c>Alignment.CenterHorizontally</c> — align to the horizontal center.</summary>
        public static Horizontal CenterHorizontally => s_center ??= new Horizontal(IAlignment.Companion.CenterHorizontally);
        /// <summary><c>Alignment.End</c> — align to the trailing edge.</summary>
        public static Horizontal End => s_end ??= new Horizontal(IAlignment.Companion.End);
    }
}
