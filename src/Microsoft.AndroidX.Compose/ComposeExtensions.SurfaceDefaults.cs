using System.Runtime.CompilerServices;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    internal static (long Color, long ContentColor) SurfaceColors(
        this IComposer composer, Color? color, Color? contentColor, SurfaceDefault defaults,
        [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
    {
        // contentColorFor adds a group. Keep it away from Surface's following
        // inline remember slots when the caller changes which colors are omitted.
        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            long resolvedColor = (defaults & SurfaceDefault.Color) != 0
                ? Material3.MaterialTheme.Instance.GetColorScheme(composer, 0).Surface
                : color.GetValueOrDefault().ToPacked();
            long resolvedContent = (defaults & SurfaceDefault.ContentColor) != 0
                ? ColorSchemeKt.ContentColorFor(resolvedColor, composer, 0)
                : contentColor.GetValueOrDefault().ToPacked();
            return (resolvedColor, resolvedContent);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
