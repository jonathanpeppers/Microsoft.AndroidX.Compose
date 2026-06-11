using AndroidX.Compose;
using AndroidX.Compose.UI.Platform;
using Microsoft.Maui.Handlers;
using StockPageHandler = Microsoft.Maui.Handlers.PageHandler;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Page"/> handler that owns
/// the <em>single</em> <see cref="ComposeView"/> per page. Every
/// other handler in <c>Microsoft.AndroidX.Compose.Maui</c> implements
/// <see cref="IComposeHandler"/> so its render folds into the
/// composition this handler installs — collapsing what used to be
/// one <see cref="ComposeView"/> per leaf into one per page.
/// </summary>
/// <remarks>
/// <para>Replaces MAUI's stock <see cref="StockPageHandler"/> via
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// Unlike the stock handler — which uses a <c>ContentViewGroup</c>
/// whose <c>OnMeasure</c>/<c>OnLayout</c> route through MAUI's
/// <c>ICrossPlatformLayout</c> on the <see cref="IContentView"/> and
/// recurse into <see cref="IContentView.PresentedContent"/>'s stock
/// platform view — we make the <see cref="ComposeView"/> itself the
/// platform view. Standard Android measure / layout then sizes it to
/// fill whatever container Shell / Navigation put the page in, and
/// Compose owns the entire content tree's layout (via the walker that
/// dispatches into each <see cref="IComposeHandler"/> child).</para>
///
/// <para>Side-effect: <see cref="Microsoft.Maui.Controls.ContentPage"/>
/// properties that live on the cross-platform side (<c>Padding</c>,
/// <c>BackgroundColor</c>) no longer flow through the stock
/// <c>ContentViewHandler</c> mappers — they have to be applied via
/// Compose modifiers inside the composition. Acceptable: anything
/// hosted in this composition is by definition the Compose-backed
/// surface and prefers <c>Modifier.padding(…)</c>/<c>.background(…)</c>
/// over MAUI's struct-typed equivalents.</para>
///
/// <para>From the consumer's perspective nothing changes — they
/// don't see Compose, they don't pick a "Compose root", and their
/// XAML / C# stays pure MAUI.</para>
/// </remarks>
public partial class PageHandler : ViewHandler<IContentView, ComposeView>
{
    /// <summary>
    /// Property mapper that intercepts <see cref="IContentView.PresentedContent"/>
    /// changes and routes them through the Compose walker.
    /// </summary>
    public static IPropertyMapper<IContentView, PageHandler> Mapper =
        new PropertyMapper<IContentView, PageHandler>(ViewHandler.ViewMapper)
        {
            ["Content"] = MapContent,
        };

    /// <summary>Command mapper (inherits the base view commands; no extras).</summary>
    public static CommandMapper<IContentView, PageHandler> CommandMapper =
        new(ViewHandler.ViewCommandMapper);

    /// <summary>Construct a handler with the default mappers.</summary>
    public PageHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public PageHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    protected override ComposeView CreatePlatformView()
    {
        var compose = new ComposeView(Context!);
        // Force MATCH_PARENT so whichever container Shell / Navigation
        // attaches the page to (FrameLayout, ViewPager, …) sizes us
        // to fill, regardless of its own LayoutParams defaults.
        compose.LayoutParameters = new global::Android.Views.ViewGroup.LayoutParams(
            global::Android.Views.ViewGroup.LayoutParams.MatchParent,
            global::Android.Views.ViewGroup.LayoutParams.MatchParent);
        return compose;
    }

    /// <summary>
    /// Push the page's <see cref="IContentView.PresentedContent"/>
    /// through <see cref="ComposeWalker"/> and install the resulting
    /// node into <see cref="PlatformView"/>'s composition.
    /// </summary>
    public static void MapContent(PageHandler handler, IContentView page)
    {
        var compose = handler.PlatformView
            ?? throw new InvalidOperationException(
                "PlatformView should have been set by the base ViewHandler.");
        var context = handler.MauiContext
            ?? throw new InvalidOperationException(
                "MauiContext should have been set by the base ViewHandler.");

        var content = page.PresentedContent;
        if (content is null)
        {
            compose.SetContent(_ => new Box());
            return;
        }

        compose.SetContent(c =>
        {
            var node = ComposeWalker.Render(content, c, context);
            var root = new Box { Modifier = Modifier.FillMaxSize() };
            root.Add(node);
            return root;
        });
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }
}
