using System.Windows.Input;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposePullToRefreshBox = AndroidX.Compose.PullToRefreshBox;
using MauiRefreshView         = Microsoft.Maui.Controls.RefreshView;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="MauiRefreshView"/> handler that renders through
/// Jetpack Compose's Material 3 <see cref="ComposePullToRefreshBox"/>.
/// Replaces MAUI's stock <c>SwipeRefreshLayout</c>-backed handler when
/// the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para><see cref="IRefreshView.IsRefreshing"/> is two-way:
/// MAUI → Compose flows through <see cref="MapIsRefreshing"/>, and the
/// pull gesture flows back through the <c>onRefresh</c> callback —
/// which writes <see cref="IRefreshView.IsRefreshing"/> = <c>true</c>
/// before invoking <see cref="IRefreshView.Command"/>. The mapper
/// re-fires with the same value, but the
/// <see cref="MutableState{T}"/> equality short-circuit breaks the
/// loop without needing a `_suppressMauiWrite` flag (mirrors
/// <see cref="EntryHandler.OnValueChanged"/>).</para>
///
/// <para>The consumer is responsible for clearing
/// <see cref="IRefreshView.IsRefreshing"/> back to <c>false</c> when
/// their async reload completes — same contract as MAUI's stock
/// handler. Wiring an automatic timer here would race the consumer's
/// own state machine.</para>
///
/// <para><see cref="IRefreshView.Content"/> is walked through the same
/// <see cref="ComposeWalker"/> recursion used by
/// <see cref="LayoutHandler"/> / <see cref="ContentViewHandler"/>, so
/// nested Compose-backed views fold into the parent composition.</para>
///
/// <para><see cref="IRefreshView.RefreshColor"/> isn't wired yet: the
/// current C# <c>PullToRefreshBox</c> facade doesn't expose
/// <c>containerColor</c> / <c>contentColor</c> slots. Until it does,
/// the spinner uses Material 3's default theme tint. Tracked as a
/// follow-up for the next slice.</para>
/// </remarks>
public partial class RefreshViewHandler : ComposeElementHandler<IRefreshView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IRefreshView"/>
    /// property changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IRefreshView, RefreshViewHandler> Mapper =
        new PropertyMapper<IRefreshView, RefreshViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IRefreshView.IsRefreshing)] = MapIsRefreshing,
            ["Content"]                          = MapContent,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IRefreshView, RefreshViewHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<bool> _isRefreshing  = new(false);
    // Bumped whenever Content swaps so BuildNode reads the live
    // PresentedContent reference (IView itself doesn't fit in
    // MutableState<T>; same trick as ContentViewHandler).
    readonly MutableState<int>  _contentVersion = new(0);

    /// <summary>Construct a handler with the default mappers.</summary>
    public RefreshViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public RefreshViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        // Read the version slot so recompositions trigger when Content
        // swaps (Content as a property doesn't live in MutableState<T>).
        _ = _contentVersion.Value;

        var view    = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on RefreshViewHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on RefreshViewHandler.");

        var box = new ComposePullToRefreshBox(
            isRefreshing: _isRefreshing.Value,
            onRefresh:    () =>
            {
                // Mirror MAUI's contract: pulling triggers the refresh
                // and the consumer (or their Command handler) is
                // responsible for clearing IsRefreshing when the work
                // completes. Set both sides; the mapper re-fires with
                // the same value and the MutableState<bool> equality
                // short-circuit breaks the loop.
                _isRefreshing.Value = true;
                view.IsRefreshing   = true;

                if ((view as MauiRefreshView)?.Command is ICommand cmd)
                {
                    var arg = (view as MauiRefreshView)?.CommandParameter;
                    if (cmd.CanExecute(arg))
                        cmd.Execute(arg);
                }
            });

        // FillMaxSize so the gesture region matches MAUI's full-bleed
        // RefreshView semantics, plus ApplyViewProperties for Opacity /
        // Translation / Scale / Rotation / IsVisible / Clip / Shadow.
        box.PrependModifier(Modifier.FillMaxSize().ApplyViewProperties(view));

        if (view.Content is { } content)
            box.Add(c => ComposeWalker.Render(content, c, context));

        return box;
    }

    /// <summary>
    /// Map <see cref="IRefreshView.IsRefreshing"/> to the Compose busy
    /// slot. The <see cref="MutableState{T}"/> equality short-circuit
    /// breaks the two-way feedback loop when the pull gesture writes
    /// the same value back through <see cref="IRefreshView"/>.
    /// </summary>
    public static void MapIsRefreshing(RefreshViewHandler handler, IRefreshView view) =>
        handler._isRefreshing.Value = view.IsRefreshing;

    /// <summary>Bump the content version slot so <see cref="BuildNode"/> re-walks.</summary>
    public static void MapContent(RefreshViewHandler handler, IRefreshView _) =>
        handler._contentVersion.Value++;
}
