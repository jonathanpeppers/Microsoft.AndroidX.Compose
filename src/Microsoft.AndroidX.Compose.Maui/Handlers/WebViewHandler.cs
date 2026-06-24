using Android.Webkit;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using AViewGroup = Android.Views.ViewGroup;
using AWebView = Android.Webkit.WebView;
using StockWebViewHandler = Microsoft.Maui.Handlers.WebViewHandler;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.WebView"/> handler that folds
/// the platform <see cref="AWebView"/> into the page's single Compose
/// composition via <see cref="AndroidView"/> interop. Replaces MAUI's
/// stock <c>WebViewHandler</c> when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para><b>Why subclass the stock handler.</b> The <c>WebViewHandler</c>
/// pipeline (page-load callbacks, cookies sync, source loading, JS
/// evaluation) is large and deeply coupled to
/// <see cref="Microsoft.Maui.Platform.MauiWebView"/> + its
/// <c>MauiWebViewClient</c> / <c>MauiWebChromeClient</c>, all of which
/// require a stock <see cref="Microsoft.Maui.Handlers.WebViewHandler"/>
/// reference. Re-implementing that machinery would duplicate hundreds
/// of lines for zero benefit since the same Android <c>WebView</c>
/// renders either way. Instead this handler derives from the stock
/// handler so every Source / UserAgent / Settings / WebViewClient /
/// WebChromeClient mapper, every command (GoBack, GoForward, Reload,
/// Eval, EvaluateJavaScriptAsync), and all cookie-sync logic continues
/// to target the real <c>MauiWebView</c> unchanged. The Compose
/// integration is purely additive: <see cref="BuildNode"/> wraps the
/// existing <see cref="ViewHandler.PlatformView"/> in an
/// <c>AndroidView</c> so the same composition that owns the page hosts
/// the WebView, instead of leaving it as a fallback
/// <c>ToPlatform()</c> island.</para>
///
/// <para><b>Cross-cutting <c>Modifier</c> propagation.</b> The stock
/// handler does no Compose-side work, so on a Compose-backed page the
/// fallback <see cref="ComposeWalker"/> path applies only
/// <c>WidthRequest</c> / <c>HeightRequest</c> — <see cref="IView.Opacity"/>,
/// rotation, scale, translation, clip, shadow, and semantics all
/// silently drop. This handler routes the same view through
/// <see cref="Platform.ModifierBridge.ApplyViewProperties"/> +
/// <see cref="Platform.SemanticsBridge.ApplySemantics"/> on the
/// outermost <see cref="Modifier"/> chain so those properties propagate
/// uniformly with every other Compose-folded leaf in the page.</para>
///
/// <para><b>Re-mapped statics.</b> The <see cref="Mapper"/> /
/// <see cref="CommandMapper"/> fields below re-declare the stock keys
/// (forwarding to <c>StockWebViewHandler.MapXxx</c>) for two reasons:
/// (1) <see cref="PropertyMapper{TVirtualView,TViewHandler}"/> is
/// invariant in <c>TViewHandler</c>, so the stock static fields
/// (typed <c>IPropertyMapper&lt;IWebView, IWebViewHandler&gt;</c>) can't
/// be reused directly with our concrete handler type as
/// <c>TViewHandler</c>; (2) the
/// <see cref="Hosting.AppHostBuilderExtensions.RemapForCompose"/>
/// pipeline appends a <c>BumpViewProperties</c> hook to
/// <c>ViewHandler.ViewMapper</c>, and pinning our static field to
/// <c>ViewHandler.ViewMapper</c> guarantees the appended hook fires
/// for this handler too. Each forwarder body is a one-liner that
/// invokes the inherited stock static, so the heavy lifting still
/// lives in MAUI core.</para>
/// </remarks>
public partial class WebViewHandler : StockWebViewHandler, IComposeHandler
{
    /// <summary>
    /// Property mapper forwarding every stock <c>WebView</c> key to the
    /// inherited <see cref="StockWebViewHandler"/> static methods.
    /// Chained on <see cref="ViewHandler.ViewMapper"/> so the
    /// cross-cutting <c>BumpViewProperties</c> hooks installed by
    /// <see cref="Hosting.AppHostBuilderExtensions.RemapForCompose"/>
    /// fire for this handler.
    /// </summary>
    public new static IPropertyMapper<IWebView, WebViewHandler> Mapper =
        new PropertyMapper<IWebView, WebViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IWebView.Source)]            = MapSource,
            [nameof(IWebView.UserAgent)]         = MapUserAgent,
            [nameof(AWebView.Settings)]          = MapWebViewSettings,
            // String literals (not `nameof(WebViewClient)`) so the
            // maui-coverage script's regex — which only captures
            // qualified `nameof(X.Y)` patterns — picks these keys up
            // (stock MAUI decompiles to literal strings and matches
            // the same way).
            ["WebViewClient"]                    = MapWebViewClient,
            ["WebChromeClient"]                  = MapWebChromeClient,
        };

    /// <summary>
    /// Command mapper forwarding every stock <c>WebView</c> command
    /// (navigation, JS eval) to the inherited <see cref="StockWebViewHandler"/>
    /// static methods.
    /// </summary>
    public new static CommandMapper<IWebView, WebViewHandler> CommandMapper =
        new(ViewCommandMapper)
        {
            [nameof(IWebView.GoBack)]                  = MapGoBack,
            [nameof(IWebView.GoForward)]               = MapGoForward,
            [nameof(IWebView.Reload)]                  = MapReload,
            [nameof(IWebView.Eval)]                    = MapEval,
            [nameof(IWebView.EvaluateJavaScriptAsync)] = MapEvaluateJavaScriptAsync,
        };

    // Bumped whenever a cross-cutting IView property changes
    // (Opacity / Translation / Scale / Rotation / Clip / Shadow /
    // Visibility / Semantics / AutomationId). BuildNode reads it
    // inside the composition so the next pass re-runs
    // ApplyViewProperties + ApplySemantics with live values. Same
    // version-counter pattern as every other IComposeHandler in
    // this project — struct-valued properties can't sit in
    // MutableState<T> directly.
    readonly MutableState<int> _viewPropertiesVersion = new(0);

    /// <summary>Construct a handler with the default mappers.</summary>
    public WebViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public WebViewHandler(IPropertyMapper? mapper = null, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc cref="IComposeHandler.BuildNode(IComposer)"/>
    public ComposableNode BuildNode(IComposer composer)
    {
        // Subscribe to the cross-cutting view-properties version slot
        // so any Opacity / Translation / Scale / Rotation / Clip /
        // Shadow / Visibility / Semantics / AutomationId change
        // recomposes this leaf.
        _ = _viewPropertiesVersion.Value;

        var view = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on WebViewHandler.");
        var platformView = PlatformView
            ?? throw new InvalidOperationException("PlatformView not set on WebViewHandler.");

        // Size from WidthRequest / HeightRequest (same matrix as
        // ComposeWalker's fallback). Falls back to FillMaxSize so the
        // WebView occupies whatever bounded slot the parent gives it
        // (the typical case for a single WebView per page).
        Modifier modifier = (view.Width, view.Height) switch
        {
            ( >= 0d, >= 0d ) => Modifier.Size(new Dp((float)view.Width), new Dp((float)view.Height)),
            ( >= 0d, _    ) => Modifier.Width(new Dp((float)view.Width)),
            ( _,    >= 0d ) => Modifier.FillMaxWidth().Height(new Dp((float)view.Height)),
            _                => Modifier.FillMaxSize(),
        };
        modifier = modifier.ApplyViewProperties(view).ApplySemantics(view);

        return new AndroidView(
            factory: _ =>
            {
                // The platform WebView is a long-lived View we don't
                // want to re-create per recomposition (it'd reset the
                // page, lose cookies, etc.). Detach from any prior
                // parent before Compose adopts it as its child —
                // covers the case where MAUI initially attached the
                // view to a stock parent before this handler folded
                // into a Compose page.
                if (platformView.Parent is AViewGroup oldParent)
                    oldParent.RemoveView(platformView);
                return platformView;
            })
        {
            Modifier = modifier,
        };
    }

    /// <inheritdoc/>
    void IComposeHandler.BumpViewPropertiesVersion() => _viewPropertiesVersion.Value++;

    // ---- Forwarders --------------------------------------------------------
    //
    // PropertyMapper<TVirtualView, TViewHandler> is invariant in
    // TViewHandler, so we can't drop the stock static methods (typed
    // against IWebViewHandler) straight into our typed-against-
    // WebViewHandler mapper. Each forwarder takes our concrete
    // handler — which IS an IWebViewHandler via the base class — and
    // delegates to the stock static so the heavy lifting (cookie sync,
    // navigating-canceled, source resolution, etc.) stays in MAUI core.

    /// <summary>Map <see cref="IWebView.Source"/> via the stock handler's <see cref="StockWebViewHandler.MapSource"/>.</summary>
    public static void MapSource(WebViewHandler handler, IWebView webView) =>
        StockWebViewHandler.MapSource(handler, webView);

    /// <summary>Map <see cref="IWebView.UserAgent"/> via the stock handler's <see cref="StockWebViewHandler.MapUserAgent"/>.</summary>
    public static void MapUserAgent(WebViewHandler handler, IWebView webView) =>
        StockWebViewHandler.MapUserAgent(handler, webView);

    /// <summary>Map <see cref="AWebView.Settings"/> via the stock handler's <see cref="StockWebViewHandler.MapWebViewSettings"/>.</summary>
    public static void MapWebViewSettings(WebViewHandler handler, IWebView webView) =>
        StockWebViewHandler.MapWebViewSettings(handler, webView);

    /// <summary>Map <see cref="WebViewClient"/> via the stock handler's <see cref="StockWebViewHandler.MapWebViewClient"/>.</summary>
    public static void MapWebViewClient(WebViewHandler handler, IWebView webView) =>
        StockWebViewHandler.MapWebViewClient(handler, webView);

    /// <summary>Map <see cref="WebChromeClient"/> via the stock handler's <see cref="StockWebViewHandler.MapWebChromeClient"/>.</summary>
    public static void MapWebChromeClient(WebViewHandler handler, IWebView webView) =>
        StockWebViewHandler.MapWebChromeClient(handler, webView);

    /// <summary>Map <see cref="IWebView.GoBack"/> via the stock handler's <see cref="StockWebViewHandler.MapGoBack"/>.</summary>
    public static void MapGoBack(WebViewHandler handler, IWebView webView, object? arg) =>
        StockWebViewHandler.MapGoBack(handler, webView, arg);

    /// <summary>Map <see cref="IWebView.GoForward"/> via the stock handler's <see cref="StockWebViewHandler.MapGoForward"/>.</summary>
    public static void MapGoForward(WebViewHandler handler, IWebView webView, object? arg) =>
        StockWebViewHandler.MapGoForward(handler, webView, arg);

    /// <summary>Map <see cref="IWebView.Reload"/> via the stock handler's <see cref="StockWebViewHandler.MapReload"/>.</summary>
    public static void MapReload(WebViewHandler handler, IWebView webView, object? arg) =>
        StockWebViewHandler.MapReload(handler, webView, arg);

    /// <summary>Map <see cref="IWebView.Eval"/> via the stock handler's <see cref="StockWebViewHandler.MapEval"/>.</summary>
    public static void MapEval(WebViewHandler handler, IWebView webView, object? arg) =>
        StockWebViewHandler.MapEval(handler, webView, arg);

    /// <summary>Map <see cref="IWebView.EvaluateJavaScriptAsync"/> via the stock handler's <see cref="StockWebViewHandler.MapEvaluateJavaScriptAsync"/>.</summary>
    public static void MapEvaluateJavaScriptAsync(WebViewHandler handler, IWebView webView, object? arg) =>
        StockWebViewHandler.MapEvaluateJavaScriptAsync(handler, webView, arg);
}
