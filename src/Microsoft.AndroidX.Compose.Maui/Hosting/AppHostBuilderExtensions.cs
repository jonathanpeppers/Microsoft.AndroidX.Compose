using Microsoft.AndroidX.Compose.Maui.Handlers;
using Microsoft.AndroidX.Compose.Maui.Platform;
using MauiActivityIndicator = Microsoft.Maui.Controls.ActivityIndicator;
using MauiBorder = Microsoft.Maui.Controls.Border;
using MauiBoxView = Microsoft.Maui.Controls.BoxView;
using MauiButton = Microsoft.Maui.Controls.Button;
using MauiCheckBox = Microsoft.Maui.Controls.CheckBox;
using MauiCollectionView = Microsoft.Maui.Controls.CollectionView;
using MauiContentView = Microsoft.Maui.Controls.ContentView;
using MauiDatePicker = Microsoft.Maui.Controls.DatePicker;
using MauiEditor = Microsoft.Maui.Controls.Editor;
using MauiEntry = Microsoft.Maui.Controls.Entry;
using MauiHorizontalStackLayout = Microsoft.Maui.Controls.HorizontalStackLayout;
using MauiImage = Microsoft.Maui.Controls.Image;
using MauiImageButton = Microsoft.Maui.Controls.ImageButton;
using MauiIndicatorView = Microsoft.Maui.Controls.IndicatorView;
using MauiLabel = Microsoft.Maui.Controls.Label;
using MauiPage = Microsoft.Maui.Controls.Page;
using MauiPicker = Microsoft.Maui.Controls.Picker;
using MauiProgressBar = Microsoft.Maui.Controls.ProgressBar;
using MauiRadioButton = Microsoft.Maui.Controls.RadioButton;
using MauiRefreshView = Microsoft.Maui.Controls.RefreshView;
using MauiScrollView = Microsoft.Maui.Controls.ScrollView;
using MauiSearchBar = Microsoft.Maui.Controls.SearchBar;
using MauiSlider = Microsoft.Maui.Controls.Slider;
using MauiStepper = Microsoft.Maui.Controls.Stepper;
using MauiSwitch = Microsoft.Maui.Controls.Switch;
using MauiTimePicker = Microsoft.Maui.Controls.TimePicker;
using MauiVerticalStackLayout = Microsoft.Maui.Controls.VerticalStackLayout;

namespace Microsoft.AndroidX.Compose.Maui.Hosting;

/// <summary>
/// <see cref="MauiAppBuilder"/> extensions that switch MAUI's Android
/// renderers over to Jetpack Compose by registering Compose-backed
/// replacements for the stock view handlers.
/// </summary>
public static class AppHostBuilderExtensions
{
    /// <summary>
    /// Registers the Compose-backed handlers shipped by
    /// <c>Microsoft.AndroidX.Compose.Maui</c>. Call <i>after</i>
    /// <c>UseMauiApp&lt;TApp&gt;()</c> so our handlers overwrite the stock
    /// AppCompat / Material handlers in MAUI's registry (last
    /// <c>AddHandler</c> per virtual-view type wins).
    /// </summary>
    /// <remarks>
    /// <para>Layout coverage in this release:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="MauiPage"/> →
    ///     <see cref="PageHandler"/> owns the <em>single</em>
    ///     <c>ComposeView</c> per page and drives the composition
    ///     for everything below.</description></item>
    ///   <item><description><see cref="MauiVerticalStackLayout"/> /
    ///     <see cref="MauiHorizontalStackLayout"/> →
    ///     <see cref="LayoutHandler"/> emits a Compose
    ///     <c>Column</c> / <c>Row</c>.</description></item>
    ///   <item><description><see cref="MauiScrollView"/> →
    ///     <see cref="ScrollViewHandler"/> wraps its content in
    ///     <c>Modifier.verticalScroll</c> / <c>horizontalScroll</c>.</description></item>
    ///   <item><description>Leaves
    ///     (<see cref="MauiLabel"/> / <see cref="MauiButton"/> /
    ///     <see cref="MauiEntry"/> / <see cref="MauiEditor"/> /
    ///     <see cref="MauiSearchBar"/> / <see cref="MauiImage"/> /
    ///     <see cref="MauiImageButton"/> / <see cref="MauiCheckBox"/> /
    ///     <see cref="MauiSwitch"/> / <see cref="MauiRadioButton"/> /
    ///     <see cref="MauiPicker"/> / <see cref="MauiDatePicker"/> /
    ///     <see cref="MauiTimePicker"/> /
    ///     <see cref="MauiSlider"/> / <see cref="MauiStepper"/> /
    ///     <see cref="MauiProgressBar"/> /
    ///     <see cref="MauiActivityIndicator"/>) fold into the
    ///     enclosing composition via
    ///     <see cref="IComposeHandler"/>.</description></item>
    ///   <item><description>Visual containers
    ///     (<see cref="MauiBorder"/> / <see cref="MauiBoxView"/> /
    ///     <see cref="MauiContentView"/>) render through Compose's
    ///     <c>Box</c> with stroke / fill / clip modifier
    ///     chains.</description></item>
    ///   <item><description>Pull-to-refresh + indicator dots
    ///     (<see cref="MauiRefreshView"/> / <see cref="MauiIndicatorView"/>):
    ///     <see cref="MauiRefreshView"/> wraps a single child
    ///     through Material 3 <c>PullToRefreshBox</c>;
    ///     <see cref="MauiIndicatorView"/> synthesises a Compose
    ///     <c>Row</c> of dot tiles. CarouselView ↔ IndicatorView
    ///     two-way wiring lands in Phase 3.</description></item>
    ///   <item><description>Lists
    ///     (<see cref="MauiCollectionView"/>):
    ///     <see cref="CollectionViewHandler"/> folds the list into the
    ///     page composition as a Compose <see cref="LazyColumn{T}"/>,
    ///     <see cref="LazyRow{T}"/>, or <see cref="LazyVerticalGrid{T}"/>
    ///     (chosen by
    ///     <see cref="Microsoft.Maui.Controls.StructuredItemsView.ItemsLayout"/>)
    ///     instead of MAUI's stock <c>RecyclerView</c>-per-cell-island
    ///     pattern. Each item template is materialised through
    ///     <see cref="ComposeWalker"/>, so Compose-folded leaves inside
    ///     cells inherit the page's <c>MaterialTheme</c> and snapshot
    ///     graph. Selection / <c>ScrollTo</c> / grouping / sticky
    ///     headers are follow-up slices.</description></item>
    /// </list>
    ///
    /// <para>Layout types not in the list above (Grid, AbsoluteLayout,
    /// FlexLayout, StackLayout) keep the stock MAUI
    /// <see cref="Microsoft.Maui.Handlers.LayoutHandler"/>. When they
    /// appear above one of our Compose-backed handlers in the tree the
    /// Compose-backed handler degrades to a per-leaf <c>ComposeView</c>
    /// (the leaf creates its own composition because there's no parent
    /// composer to fold into). When they appear <i>below</i> a
    /// converted parent — or anywhere a non-converted control surfaces
    /// (CollectionView, BoxView, customer renderers) — they're
    /// hosted via <c>AndroidView { factory = child.ToPlatform(MauiContext) }</c>
    /// inside the page composition, so MAUI's normal handler resolution
    /// keeps working.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = MauiApp.CreateBuilder()
    ///     .UseMauiApp&lt;App&gt;()
    ///     .UseAndroidXCompose();
    /// </code>
    /// </example>
    public static MauiAppBuilder UseAndroidXCompose(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register a DispatchProxy-backed IAlertManagerSubscription so
        // MAUI's per-window AlertManager.Subscribe() picks us up
        // before falling back to AlertRequestHelper. Renders
        // DisplayAlert / DisplayActionSheet / DisplayPromptAsync via
        // Compose AlertDialog / ModalBottomSheet instead of stock
        // AppCompat dialogs. See ComposeAlertManagerSubscription's
        // remarks for the version-pinning hazard.
        ComposeAlertManagerSubscription.Register(builder.Services);

        // Slice 8: install the cross-cutting view-property bumpers on
        // ViewHandler.ViewMapper *before* per-handler registration so
        // every Compose-backed handler picks up Opacity / Translation
        // / Scale / Rotation / IsVisible / Clip / Shadow recomposition
        // through the shared mapper. Idempotent.
        RemapForCompose();

        // Process-wide bridge between MAUI's RequestedTheme /
        // UserAppTheme and the Compose-side MaterialTheme. Resolved
        // by PageHandler.MapContent — see ThemeManager for resolution
        // rules and trade-offs.
        builder.Services.AddSingleton<ThemeManager>();

        builder.ConfigureMauiHandlers(handlers =>
        {
            // Root: the only handler that creates a ComposeView.
            handlers.AddHandler<MauiPage,                   PageHandler>();

            // Containers — folded into the page composition.
            handlers.AddHandler<MauiVerticalStackLayout,    LayoutHandler>();
            handlers.AddHandler<MauiHorizontalStackLayout,  LayoutHandler>();
            handlers.AddHandler<MauiScrollView,             ScrollViewHandler>();

            // Leaves.
            handlers.AddHandler<MauiLabel,                  LabelHandler>();
            handlers.AddHandler<MauiButton,                 ButtonHandler>();
            handlers.AddHandler<MauiEntry,                  EntryHandler>();
            handlers.AddHandler<MauiEditor,                 EditorHandler>();
            handlers.AddHandler<MauiSearchBar,              SearchBarHandler>();
            handlers.AddHandler<MauiImage,                  ImageHandler>();
            handlers.AddHandler<MauiImageButton,            ImageButtonHandler>();
            handlers.AddHandler<MauiCheckBox,               CheckBoxHandler>();
            handlers.AddHandler<MauiSwitch,                 SwitchHandler>();
            handlers.AddHandler<MauiRadioButton,            RadioButtonHandler>();
            handlers.AddHandler<MauiPicker,                 PickerHandler>();
            handlers.AddHandler<MauiDatePicker,             DatePickerHandler>();
            handlers.AddHandler<MauiTimePicker,             TimePickerHandler>();

            // Phase 2 Slice 4 — value & progress leaves.
            handlers.AddHandler<MauiSlider,                 SliderHandler>();
            handlers.AddHandler<MauiStepper,                StepperHandler>();
            handlers.AddHandler<MauiProgressBar,            ProgressBarHandler>();
            handlers.AddHandler<MauiActivityIndicator,      ActivityIndicatorHandler>();

            // Visual containers — render through Compose Box.
            handlers.AddHandler<MauiBorder,                 BorderHandler>();
            handlers.AddHandler<MauiBoxView,                BoxViewHandler>();
            // ContentView collides with stock MAUI's ContentViewHandler;
            // last AddHandler wins, so register ours last.
            handlers.AddHandler<MauiContentView,            ContentViewHandler>();

            // Phase 2 Slice 12 — pull-to-refresh + carousel-position
            // dot strip. RefreshView wraps a single child through
            // PullToRefreshBox; IndicatorView synthesises a Row of Box
            // dots. CarouselView two-way wiring lands in Phase 3.
            handlers.AddHandler<MauiRefreshView,            RefreshViewHandler>();
            handlers.AddHandler<MauiIndicatorView,          IndicatorViewHandler>();

            // Phase 3 Slice 1 — CollectionView folded into the page
            // composition as LazyColumn / LazyRow / LazyVerticalGrid
            // (dispatched by ItemsLayout) instead of the stock
            // per-cell ComposeView island.
            handlers.AddHandler<MauiCollectionView,         CollectionViewHandler>();
        });

        return builder;
    }

    static bool s_remapped;

    /// <summary>
    /// Layer Compose-side recomposition triggers onto
    /// <see cref="ViewHandler.ViewMapper"/> for every cross-cutting
    /// <see cref="IView"/> visual / transform property
    /// (<c>Visibility</c>, <c>Opacity</c>, <c>Translation</c>,
    /// <c>Scale</c>, <c>Rotation</c>, <c>Anchor</c>, <c>Clip</c>,
    /// <c>Shadow</c>). The base mapper entries continue to dispatch
    /// to the platform <c>UpdateXxx</c> extensions on <c>ComposeView</c>
    /// — those are no-ops for our folded leaves, since the
    /// <c>ComposeView</c> is detached when a Compose-aware parent
    /// hosts the handler — and the appended hook bumps the handler's
    /// shared view-properties version slot so the next composition
    /// pass re-reads via <see cref="Platform.ModifierBridge.ApplyViewProperties"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Why <c>AppendToMapping</c> on the global mapper.</b>
    /// <c>ViewHandler.ViewMapper</c> is the static base mapper that
    /// every handler inherits via the <c>PropertyMapper</c> ctor's
    /// chained-defaults call. Mutating it once here adds the bump
    /// hook to every handler in the app domain at once — including
    /// future handlers we add in later slices. Non-Compose handlers
    /// see the appended hook too; the inner <c>handler is
    /// IComposeHandler</c> guard keeps the cost to a single type
    /// check on those.</para>
    ///
    /// <para><b>Idempotency.</b> The static <see cref="s_remapped"/>
    /// flag prevents double-registration when a host calls
    /// <see cref="UseAndroidXCompose"/> twice (e.g. from a unit-test
    /// fixture that rebuilds the app). The hook always runs after
    /// the original mapper, never replaces it — the platform-side
    /// <c>UpdateOpacity</c> still executes against the (detached)
    /// <c>ComposeView</c>.</para>
    /// </remarks>
    static void RemapForCompose()
    {
        if (s_remapped) return;
        s_remapped = true;

        var mapper = Microsoft.Maui.Handlers.ViewHandler.ViewMapper;

        // Each cross-cutting IView visual / transform property gets a
        // dedicated AppendToMapping call so future per-property logic
        // (e.g. swap Visibility to a measure-policy short-circuit when
        // the layout adapter ships) has a clear single-point edit.
        mapper.AppendToMapping(nameof(IView.Visibility),    BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.Opacity),       BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.TranslationX),  BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.TranslationY),  BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.Scale),         BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.ScaleX),        BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.ScaleY),        BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.Rotation),      BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.RotationX),     BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.RotationY),     BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.AnchorX),       BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.AnchorY),       BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.Clip),          BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.Shadow),        BumpViewProperties);

        // Phase 2 Slice 11 — semantics. SemanticProperties.Hint /
        // Description / HeadingLevel and IView.AutomationId all flow
        // through `SemanticsBridge.ApplySemantics(view)` on the same
        // version slot ApplyViewProperties uses, so a runtime mutation
        // recomposes the leaf and rebuilds the merged-semantics block.
        mapper.AppendToMapping(nameof(IView.Semantics),     BumpViewProperties);
        mapper.AppendToMapping(nameof(IView.AutomationId),  BumpViewProperties);
    }

    static void BumpViewProperties(IViewHandler handler, IView view)
    {
        if (handler is IComposeHandler compose)
            compose.BumpViewPropertiesVersion();
    }
}
