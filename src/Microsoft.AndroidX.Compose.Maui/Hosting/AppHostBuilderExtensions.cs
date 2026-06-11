using Microsoft.AndroidX.Compose.Maui.Handlers;
using Microsoft.AndroidX.Compose.Maui.Platform;
using MauiBorder = Microsoft.Maui.Controls.Border;
using MauiBoxView = Microsoft.Maui.Controls.BoxView;
using MauiButton = Microsoft.Maui.Controls.Button;
using MauiCheckBox = Microsoft.Maui.Controls.CheckBox;
using MauiContentView = Microsoft.Maui.Controls.ContentView;
using MauiEditor = Microsoft.Maui.Controls.Editor;
using MauiEntry = Microsoft.Maui.Controls.Entry;
using MauiHorizontalStackLayout = Microsoft.Maui.Controls.HorizontalStackLayout;
using MauiImage = Microsoft.Maui.Controls.Image;
using MauiImageButton = Microsoft.Maui.Controls.ImageButton;
using MauiLabel = Microsoft.Maui.Controls.Label;
using MauiPage = Microsoft.Maui.Controls.Page;
using MauiRadioButton = Microsoft.Maui.Controls.RadioButton;
using MauiScrollView = Microsoft.Maui.Controls.ScrollView;
using MauiSearchBar = Microsoft.Maui.Controls.SearchBar;
using MauiSwitch = Microsoft.Maui.Controls.Switch;
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
    ///     <see cref="MauiSwitch"/> / <see cref="MauiRadioButton"/>)
    ///     fold into the enclosing composition via
    ///     <see cref="IComposeHandler"/>.</description></item>
    ///   <item><description>Visual containers
    ///     (<see cref="MauiBorder"/> / <see cref="MauiBoxView"/> /
    ///     <see cref="MauiContentView"/>) render through Compose's
    ///     <c>Box</c> with stroke / fill / clip modifier
    ///     chains.</description></item>
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

            // Visual containers — render through Compose Box.
            handlers.AddHandler<MauiBorder,                 BorderHandler>();
            handlers.AddHandler<MauiBoxView,                BoxViewHandler>();
            // ContentView collides with stock MAUI's ContentViewHandler;
            // last AddHandler wins, so register ours last.
            handlers.AddHandler<MauiContentView,            ContentViewHandler>();
        });

        return builder;
    }
}
