using Android.Runtime;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI;
using AndroidX.Compose.UI.Graphics;

namespace AndroidX.Compose;

internal static class FabStyleDefaults
{
    internal static (IModifier? Modifier, IShape? Shape, long Container, long Content, FloatingActionButtonElevation? Elevation) Resolve(
        IModifier? modifier, Shape? shape, System.Func<IComposer, int, IShape> shapeFactory,
        Color? containerColor, Color? contentColor, FloatingActionButtonElevation? elevation,
        bool defaultModifier, bool defaultShape, bool defaultContainer, bool defaultContent, bool defaultElevation, IComposer composer)
    {
        var resolvedModifier = defaultModifier ? Modifier.BuildEmpty() : modifier;
        IShape? resolvedShape;
        composer.StartReplaceableGroup(CompositionGroupKey.Compute(0, typeof(FabStyleDefaults)));
        try
        {
            resolvedShape = defaultShape ? shapeFactory(composer, 0) : shape?.JavaCast<IShape>();
        }
        finally
        {
            composer.EndReplaceableGroup();
        }

        long container;
        composer.StartReplaceableGroup(CompositionGroupKey.Compute(1, typeof(FabStyleDefaults)));
        try
        {
            container = defaultContainer
                ? FloatingActionButtonDefaults.Instance.GetContainerColor(composer, 0)
                : containerColor?.ToPacked() ?? 0L;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }

        long content;
        composer.StartReplaceableGroup(CompositionGroupKey.Compute(2, typeof(FabStyleDefaults)));
        try
        {
            content = defaultContent
                ? ColorSchemeKt.ContentColorFor(container, composer, 0)
                : contentColor?.ToPacked() ?? 0L;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }

        composer.StartReplaceableGroup(CompositionGroupKey.Compute(3, typeof(FabStyleDefaults)));
        try
        {
            if (defaultElevation)
                elevation = FloatingActionButtonDefaults.Instance.Elevation(0, 0, 0, 0, composer,
                    p5: 0, _changed: (int)FloatingActionButtonElevationDefault.All);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
        return (resolvedModifier, resolvedShape, container, content, elevation);
    }
}
