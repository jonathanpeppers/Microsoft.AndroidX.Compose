using Android.Runtime;
using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;
using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

internal sealed class FlowOverflowContent(Action<FlowOverflowScope, IComposer> render)
{
    internal static FlowOverflowContent FromNode(ComposableNode content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new((_, composer) => content.Render(composer));
    }

    internal static FlowOverflowContent FromFactory(Func<FlowOverflowScope, ComposableNode> content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new((scope, composer) =>
            (content(scope) ?? throw new InvalidOperationException("Flow overflow indicator returned no content."))
                .Render(composer));
    }

    internal static FlowOverflowContent FromAction(Action<FlowOverflowScope> content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new((scope, _) => content(scope));
    }

    internal IFunction3 Wrap(IComposer composer, bool horizontal,
        [CallerLineNumber] int line = 0, [CallerFilePath] string file = "") =>
        ComposableLambdas.Wrap3(composer, (handle, current) =>
        {
            FlowOverflowScope counts;
            if (horizontal)
            {
                var peer = Java.Lang.Object.GetObject<IFlowRowOverflowScope>(handle, JniHandleOwnership.DoNotTransfer)
                    ?? throw new InvalidOperationException("FlowRow overflow callback supplied no scope.");
                counts = new(() => peer.TotalItemCount, () => peer.ShownItemCount);
            }
            else
            {
                var peer = Java.Lang.Object.GetObject<IFlowColumnOverflowScope>(handle, JniHandleOwnership.DoNotTransfer)
                    ?? throw new InvalidOperationException("FlowColumn overflow callback supplied no scope.");
                counts = new(() => peer.TotalItemCount, () => peer.ShownItemCount);
            }
            using var scope = RenderContext.PushScope(handle, horizontal ? ScopeKind.Row : ScopeKind.Column);
            using var context = ComposableContext.Enter(current);
            render(counts, current);
        }, line, file);
}
