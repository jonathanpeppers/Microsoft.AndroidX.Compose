using System.Collections.Specialized;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor          = AndroidX.Compose.Color;
using ComposeFontWeight     = AndroidX.Compose.FontWeight;
using ComposeOutlinedTextField = AndroidX.Compose.OutlinedTextField;
using ComposeText           = AndroidX.Compose.Text;
using ComposeTextStyle      = AndroidX.Compose.TextStyle;
using MauiPicker            = Microsoft.Maui.Controls.Picker;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="MauiPicker"/> handler that renders through Jetpack
/// Compose's Material 3 <see cref="ExposedDropdownMenuBox"/> +
/// <see cref="ExposedDropdownMenu"/>. Replaces MAUI's stock
/// <c>AppCompatSpinner</c>-based handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>The trigger is a read-only <see cref="ComposeOutlinedTextField"/>
/// labelled with <see cref="MauiPicker.Title"/> and showing the
/// currently-selected item. Tapping the trailing ▼ <see cref="IconButton"/>
/// toggles the popup menu, which lists each entry from
/// <see cref="Microsoft.Maui.IPicker.Items"/> as a
/// <see cref="DropdownMenuItem"/>. Selecting an item writes back to
/// <see cref="Microsoft.Maui.IPicker.SelectedIndex"/> so MAUI's standard
/// property pipeline (data binding, behaviors, validation) fires
/// normally; the resulting <c>SelectedIndexChanged</c> event re-enters
/// the mapper, but the <see cref="MutableState{T}"/> equality check
/// short-circuits the loop just like
/// <see cref="EntryHandler.OnValueChanged(string)"/>.</para>
///
/// <para><see cref="Microsoft.Maui.IPicker.Items"/> is an
/// <see cref="IList{T}"/> of strings — Compose's
/// <see cref="MutableState{T}"/> doesn't model collection types
/// directly. Use the <em>version-counter</em> pattern (same trick as
/// <see cref="LayoutHandler"/>'s padding slot): bump
/// <see cref="_itemsVersion"/> from <see cref="MapItemsSource(PickerHandler, MauiPicker)"/>
/// and from <see cref="OnItemsCollectionChanged(object?, NotifyCollectionChangedEventArgs)"/>,
/// then read <c>VirtualView.Items</c> live inside <see cref="BuildNode(IComposer)"/>.</para>
///
/// <para>Concrete <see cref="MauiPicker"/> exposes
/// <see cref="MauiPicker.ItemsSource"/> on top of the <c>IPicker</c>
/// surface; data-binding flows through that property. The mapper key
/// is the bare string <c>"ItemsSource"</c> (the property doesn't live
/// on the interface, same trick as <c>"Children"</c> on
/// <see cref="LayoutHandler"/>).</para>
/// </remarks>
public partial class PickerHandler : ComposeElementHandler<IPicker>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IPicker"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IPicker, PickerHandler> Mapper =
        new PropertyMapper<IPicker, PickerHandler>(ViewHandler.ViewMapper)
        {
            // ItemsSource is on the concrete Picker, not IPicker; use
            // the bare-string key. Bumping _itemsVersion invalidates
            // the dropdown menu's child list (Compose smart-skips
            // siblings whose state is unchanged).
            ["ItemsSource"]                           = MapItemsSource,
            [nameof(IPicker.SelectedIndex)]           = MapSelectedIndex,
            [nameof(IPicker.Title)]                   = MapTitle,
            [nameof(IPicker.TitleColor)]              = MapTitleColor,
            [nameof(ITextStyle.TextColor)]            = MapTextColor,
            [nameof(ITextStyle.Font)]                 = MapFont,
            [nameof(IView.HorizontalLayoutAlignment)] = MapHorizontalLayoutAlignment,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IPicker, PickerHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<int>     _itemsVersion = new(0);
    readonly MutableState<int>     _selectedIndex = new(-1);
    readonly MutableState<string>  _title         = new(string.Empty);
    readonly MutableState<long?>   _titleColor    = new((long?)null);
    readonly MutableState<long?>   _textColor     = new((long?)null);
    readonly MutableState<int?>    _fontSize      = new((int?)null);
    readonly MutableState<bool>    _bold          = new(false);
    readonly MutableState<bool>    _open          = new(false);
    readonly MutableState<bool>    _fillWidth     = new(false);

    INotifyCollectionChanged? _subscribedItems;

    /// <summary>Construct a handler with the default mappers.</summary>
    public PickerHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public PickerHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    protected override void ConnectHandler(ComposeView platformView)
    {
        base.ConnectHandler(platformView);
        SubscribeItems();
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        UnsubscribeItems();
        base.DisconnectHandler(platformView);
    }

    /// <inheritdoc/>
    public override void SetVirtualView(IView view)
    {
        // Re-bind the items subscription to the new VirtualView whenever
        // the handler is reused (MAUI sometimes recycles handlers across
        // virtual views; defensive against tear-down/re-attach cycles).
        // base.SetVirtualView walks the property mapper, which runs
        // MapItemsSource → UnsubscribeItems(); SubscribeItems(); so we
        // don't need to manually re-subscribe afterwards.
        UnsubscribeItems();
        base.SetVirtualView(view);
    }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView;
        ArgumentNullException.ThrowIfNull(virtualView);

        // Subscribe to the version slot so external Items mutations
        // recompose this subtree even when the live IList reference
        // is identity-stable.
        _ = _itemsVersion.Value;

        var picker = virtualView;
        var items = picker.Items;
        var selectedIndex = _selectedIndex.Value;
        var title = _title.Value;
        var packedTextColor  = _textColor.Value;
        var packedTitleColor = _titleColor.Value;
        var size = _fontSize.Value;
        var bold = _bold.Value;
        var fill = _fillWidth.Value;
        var isOpen = _open.Value;

        var displayValue = selectedIndex >= 0 && selectedIndex < items.Count
            ? items[selectedIndex] ?? string.Empty
            : string.Empty;

        var trigger = new ComposeOutlinedTextField(displayValue, _ => { /* read-only */ })
        {
            ReadOnly   = true,
            SingleLine = true,
            TrailingIcon = new IconButton(onClick: () => _open.Value = !_open.Value)
            {
                new ComposeText(isOpen ? "▲" : "▼"),
            },
        };
        if (!string.IsNullOrEmpty(title))
        {
            trigger.Label = packedTitleColor.HasValue
                ? new ComposeText(title) { Color = new ComposeColor(packedTitleColor.Value) }
                : new ComposeText(title);
        }
        if (packedTextColor.HasValue || size.HasValue || bold)
        {
            trigger.TextStyle = new ComposeTextStyle
            {
                Color      = packedTextColor.HasValue ? new ComposeColor(packedTextColor.Value) : null,
                FontSize   = size.HasValue   ? new Sp(size.Value) : null,
                FontWeight = bold ? ComposeFontWeight.Bold : null,
            };
        }
        // Combines the layout-fill (when set) with the cross-cutting view
        // properties (Opacity, Translation, Scale, Rotation, IsVisible,
        // Clip, Shadow). The dropdown menu is rendered into the same
        // ComposeView, so the modifier wraps the ExposedDropdownMenuBox via
        // its trigger anchor.
        var outer = (fill ? Modifier.FillMaxWidth() : Modifier.Companion)
            .ApplyViewProperties(virtualView)
            .ApplyGestures(virtualView, MauiContext);
        trigger.PrependModifier(outer);

        var menu = new ExposedDropdownMenu(
            expanded:         isOpen,
            onDismissRequest: () => _open.Value = false);
        for (int i = 0; i < items.Count; i++)
        {
            // Capture i so the click closure points at this row's index.
            var index = i;
            var label = items[i] ?? string.Empty;
            menu.Add(new DropdownMenuItem(
                text:    new ComposeText(label),
                onClick: () => OnItemSelected(index)));
        }

        return new ExposedDropdownMenuBox(
            expanded:         isOpen,
            onExpandedChange: v => _open.Value = v)
        {
            trigger,
            menu,
        };
    }

    void OnItemSelected(int index)
    {
        // Update Compose state synchronously so the trigger label
        // reflects the new selection on the next frame; the equality
        // short-circuit on MutableState<int> breaks the
        // MapSelectedIndex feedback loop, just like EntryHandler.
        _selectedIndex.Value = index;
        _open.Value          = false;
        if (VirtualView is { } picker)
            picker.SelectedIndex = index;
    }

    void SubscribeItems()
    {
        if (VirtualView is { } picker
            && picker.Items is INotifyCollectionChanged ncc)
        {
            _subscribedItems = ncc;
            ncc.CollectionChanged += OnItemsCollectionChanged;
        }
    }

    void UnsubscribeItems()
    {
        if (_subscribedItems is { } ncc)
        {
            ncc.CollectionChanged -= OnItemsCollectionChanged;
            _subscribedItems = null;
        }
    }

    void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // External mutation to the bound list — bump the version slot
        // so BuildNode re-reads VirtualView.Items.
        _itemsVersion.Value++;
    }

    /// <summary>
    /// Bump the items-version slot so the menu's child list rebuilds.
    /// The mapper key is the bare string <c>"ItemsSource"</c> (defined
    /// only on the concrete <see cref="MauiPicker"/>; the interface
    /// only exposes the live <see cref="IPicker.Items"/> list). The
    /// property pipeline still hands us the <see cref="IPicker"/>
    /// virtualView, and <see cref="IPicker.Items"/> reflects whatever
    /// MAUI just rebuilt from the new <c>ItemsSource</c>. We
    /// re-subscribe to the new list's
    /// <see cref="INotifyCollectionChanged"/> events so future
    /// mutations also recompose.
    /// </summary>
    public static void MapItemsSource(PickerHandler handler, IPicker picker)
    {
        handler.UnsubscribeItems();
        handler.SubscribeItems();
        handler._itemsVersion.Value++;
    }

    /// <summary>Map <see cref="IPicker.SelectedIndex"/> to the Compose selection slot.</summary>
    public static void MapSelectedIndex(PickerHandler handler, IPicker picker) =>
        handler._selectedIndex.Value = picker.SelectedIndex;

    /// <summary>Map <see cref="IPicker.Title"/> to the Compose label slot.</summary>
    public static void MapTitle(PickerHandler handler, IPicker picker) =>
        handler._title.Value = picker.Title ?? string.Empty;

    /// <summary>Map <see cref="IPicker.TitleColor"/> to the floating-label colour slot.</summary>
    public static void MapTitleColor(PickerHandler handler, IPicker picker) =>
        handler._titleColor.Value = ColorMapping.ToPackedLong(picker.TitleColor);

    /// <summary>Map <see cref="ITextStyle.TextColor"/> to the Compose <c>TextStyle.Color</c> slot.</summary>
    public static void MapTextColor(PickerHandler handler, IPicker picker) =>
        handler._textColor.Value = ColorMapping.ToPackedLong(picker.TextColor);

    /// <summary>Map <see cref="ITextStyle.Font"/> (size + bold) to Compose <c>TextStyle</c> slots.</summary>
    public static void MapFont(PickerHandler handler, IPicker picker)
    {
        var font = picker.Font;
        handler._fontSize.Value = font.Size > 0 ? (int)font.Size : null;
        handler._bold.Value     = (font.Weight & Microsoft.Maui.FontWeight.Bold)
            == Microsoft.Maui.FontWeight.Bold;
    }

    /// <summary>
    /// Map <see cref="IView.HorizontalLayoutAlignment"/> to
    /// <c>Modifier.fillMaxWidth()</c> when the picker asks to fill its
    /// slot — same parity rule as the other field-shaped controls.
    /// </summary>
    public static void MapHorizontalLayoutAlignment(PickerHandler handler, IPicker picker) =>
        handler._fillWidth.Value = picker.HorizontalLayoutAlignment
            == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
}
