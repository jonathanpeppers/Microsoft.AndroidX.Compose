using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ExposedDropdownMenu</c> — the popup half of an
/// <see cref="ExposedDropdownMenuBox"/>. Hosts
/// <see cref="DropdownMenuItem"/> children and reads the enclosing
/// <see cref="ExposedDropdownMenuBox"/>'s scope handle from
/// <see cref="RenderContext"/>; rendering this outside an
/// <see cref="ExposedDropdownMenuBox"/> throws
/// <see cref="System.InvalidOperationException"/>.
/// </summary>
public sealed class ExposedDropdownMenu : ComposableContainer
{
    readonly bool _expanded;
    readonly System.Action _onDismissRequest;

    public ExposedDropdownMenu(bool expanded, System.Action onDismissRequest)
    {
        _expanded = expanded;
        _onDismissRequest = onDismissRequest;
    }

    internal override void Render(IComposer composer)
    {
        var scope = RenderContext.CurrentScope;
        if (scope == System.IntPtr.Zero)
            throw new System.InvalidOperationException(
                "ExposedDropdownMenu must be rendered inside an ExposedDropdownMenuBox " +
                "so it can resolve the menu-anchor scope.");

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var content   = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));

        int defaults = (int)ExposedDropdownMenuDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)ExposedDropdownMenuDefault.Modifier;

        ComposeBridges.ExposedDropdownMenu(
            scope:            scope,
            expanded:         _expanded,
            onDismissRequest: onDismiss,
            modifier:         modifier,
            defaults:         defaults,
            content:          content,
            composer:         composer);
    }
}
