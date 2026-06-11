using D = AndroidX.Compose.Gallery.Demos;

namespace AndroidX.Compose.Gallery.Registry;

/// <summary>
/// Global catalog: every <see cref="Demo"/> in the gallery and every
/// <see cref="Category"/> they belong to. Adding a new demo is two
/// edits — a new file under <c>Demos/&lt;Category&gt;/</c> and a new
/// line in <see cref="Demos"/>.
/// </summary>
public static class Catalog
{
    /// <summary>
    /// All categories, in the order they appear on the home screen
    /// and in the navigation drawer. Order matters; reordering here
    /// reorders the UI.
    /// </summary>
    public static readonly IReadOnlyList<Category> Categories =
    [
        new("text-inputs",         "Text & inputs",          "Text styling, TextField variants",             "✏️"),
        new("buttons",             "Buttons",                "Filled, icon, chip, FAB, tooltip",             "🔘"),
        new("selection",           "Selection",              "Checkbox, switch, slider, segmented",          "☑️"),
        new("containers",          "Containers",             "Card, Surface, Box, Column, Row, Flow",        "📦"),
        new("lists-grids",         "Lists & grids",          "LazyColumn, LazyGrid, PullToRefresh",          "📋"),
        new("carousels-paging",    "Carousels & paging",     "HorizontalPager, M3 carousels",                "🎠"),
        new("app-bars-tabs",       "App bars & tabs",        "TopAppBar variants, BottomAppBar, TabRow",     "🧭"),
        new("navigation",          "Navigation",             "NavHost, drawers, rails",                      "🗺️"),
        new("dialogs-sheets",      "Dialogs & sheets",       "AlertDialog, ModalSheet, pickers, menus",      "💬"),
        new("search",              "Search",                 "SearchBar, ExpandedFullScreen, Docked",        "🔍"),
        new("modifiers",           "Modifiers",              "Shapes, transforms, gestures, semantics",      "🎛️"),
        new("state-effects",       "State, effects, anim.",  "Remember, side effects, animation",            "✨"),
        new("locals-misc",         "CompositionLocal & misc","Locals, progress, image, icon",                "🧩"),
        new("theming",             "Theming",                "Custom palette, shapes, typography, icons",    "🎨"),
    ];

    /// <summary>
    /// All registered demos. Order within each category is preserved
    /// when rendered on the category screen — keep related demos
    /// adjacent.
    /// </summary>
    public static readonly IReadOnlyList<Demo> Demos =
    [
        // ---- Text & inputs ----
        D.TextInputs.TextStylingDemo.Demo,
        D.TextInputs.FontWeightStyleFamilyDemo.Demo,
        D.TextInputs.ColorAndAlignmentDemo.Demo,
        D.TextInputs.OverflowAndClampingDemo.Demo,
        D.TextInputs.TextFieldSlotsDemo.Demo,
        D.TextInputs.TextFieldCursorPlacementDemo.Demo,
        D.TextInputs.OutlinedTextFieldDemo.Demo,
        D.TextInputs.SecureTextFieldDemo.Demo,
        D.TextInputs.OutlinedSecureTextFieldDemo.Demo,
        D.TextInputs.SelectionDemo.Demo,
        D.TextInputs.TextStyleOverrideDemo.Demo,
        D.TextInputs.PasswordFieldDemo.Demo,
        D.TextInputs.NumericKeyboardDemo.Demo,

        // ---- Buttons ----
        D.Buttons.HelloCounterDemo.Demo,
        D.Buttons.FillStylesDemo.Demo,
        D.Buttons.ColorOverridesDemo.Demo,
        D.Buttons.IconButtonsDemo.Demo,
        D.Buttons.IconToggleButtonsDemo.Demo,
        D.Buttons.ChipsDemo.Demo,
        D.Buttons.FloatingActionButtonsDemo.Demo,
        D.Buttons.TooltipsDemo.Demo,

        // ---- Selection ----
        D.Selection.CheckboxDemo.Demo,
        D.Selection.TriStateCheckboxDemo.Demo,
        D.Selection.SwitchDemo.Demo,
        D.Selection.RadioButtonGroupDemo.Demo,
        D.Selection.SliderDemo.Demo,
        D.Selection.RangeSliderDemo.Demo,
        D.Selection.SingleChoiceSegmentedButtonsDemo.Demo,
        D.Selection.MultiChoiceSegmentedButtonsDemo.Demo,

        // ---- Containers ----
        D.Containers.CardVariantsDemo.Demo,
        D.Containers.RoundedCornerShapeDemo.Demo,
        D.Containers.BrushDemo.Demo,
        D.Containers.SurfaceDemo.Demo,
        D.Containers.BoxAlignmentDemo.Demo,
        D.Containers.ColumnRowArrangementsDemo.Demo,
        D.Containers.DpArithmeticDemo.Demo,
        D.Containers.SpacerDemo.Demo,
        D.Containers.DividerDemo.Demo,
        D.Containers.FlowRowFlowColumnDemo.Demo,
        D.Containers.BoxWithConstraintsDemo.Demo,

        // ---- Lists & grids ----
        D.ListsGrids.LazyColumnLongDemo.Demo,
        D.ListsGrids.LazyColumnContentPaddingDemo.Demo,
        D.ListsGrids.LazyListScrollStateDemo.Demo,
        D.ListsGrids.LazyRowDemo.Demo,
        D.ListsGrids.LazyVerticalGridFixedDemo.Demo,
        D.ListsGrids.LazyVerticalGridAdaptiveDemo.Demo,
        D.ListsGrids.LazyVerticalStaggeredGridDemo.Demo,
        D.ListsGrids.PullToRefreshBoxDemo.Demo,

        // ---- Carousels & paging ----
        D.CarouselsPaging.HorizontalUncontainedCarouselDemo.Demo,
        D.CarouselsPaging.HorizontalMultiBrowseCarouselDemo.Demo,
        D.CarouselsPaging.HorizontalCenteredHeroCarouselDemo.Demo,
        D.CarouselsPaging.HorizontalPagerDemo.Demo,

        // ---- App bars & tabs ----
        D.AppBars.CenterAlignedTopAppBarDemo.Demo,
        D.AppBars.MediumFlexibleTopAppBarDemo.Demo,
        D.AppBars.LargeFlexibleTopAppBarDemo.Demo,
        D.AppBars.PinnedScrollBehaviorDemo.Demo,
        D.AppBars.EnterAlwaysScrollBehaviorDemo.Demo,
        D.AppBars.BottomAppBarActionsDemo.Demo,
        D.AppBars.BottomAppBarWithFabDemo.Demo,
        D.AppBars.FlexibleBottomAppBarDemo.Demo,
        D.AppBars.PrimaryScrollableTabRowDemo.Demo,
        D.AppBars.SecondaryScrollableTabRowDemo.Demo,

        // ---- Navigation ----
        D.Navigation.NavHostRouteArgsDemo.Demo,
        D.Navigation.BottomNavOptionsDemo.Demo,
        D.Navigation.BackHandlerDemo.Demo,
        D.Navigation.NavigationDrawerItemDemo.Demo,
        D.Navigation.ModalDrawerDemo.Demo,
        D.Navigation.DismissibleDrawerDemo.Demo,
        D.Navigation.PermanentDrawerDemo.Demo,
        D.Navigation.WideNavigationRailDemo.Demo,
        D.Navigation.ModalWideNavigationRailDemo.Demo,

        // ---- Dialogs & sheets ----
        D.DialogsSheets.AlertDialogDemo.Demo,
        D.DialogsSheets.ModalBottomSheetDemo.Demo,
        D.DialogsSheets.DatePickerDialogDemo.Demo,
        D.DialogsSheets.DateRangePickerDialogDemo.Demo,
        D.DialogsSheets.TimePickerDialogDemo.Demo,
        D.DialogsSheets.DropdownMenuDemo.Demo,
        D.DialogsSheets.ExposedDropdownMenuBoxDemo.Demo,

        // ---- Search ----
        D.Search.SearchBarPairDemo.Demo,
        D.Search.DockedSearchBarDemo.Demo,
        D.Search.DockedSearchBarQueryDemo.Demo,

        // ---- Modifiers ----
        D.Modifiers.ShapesAndShadowDemo.Demo,
        D.Modifiers.RotateScaleAlphaDemo.Demo,
        D.Modifiers.ToggleableSelectableSemanticsDemo.Demo,
        D.Modifiers.SemanticsBuilderDemo.Demo,
        D.Modifiers.FocusRequesterDemo.Demo,
        D.Modifiers.CombinedClickableDemo.Demo,
        D.Modifiers.DetectTapGesturesDemo.Demo,
        D.Modifiers.DraggableOffsetDemo.Demo,
        D.Modifiers.DragAndDropTargetDemo.Demo,
        D.Modifiers.GraphicsLayerDemo.Demo,
        D.Modifiers.FlowRowScopeDispatchDemo.Demo,
        D.Modifiers.MinimumInteractiveComponentSizeDemo.Demo,

        // ---- State, effects, animation ----
        D.StateEffectsAnimation.RememberSaveableDemo.Demo,
        D.StateEffectsAnimation.ProduceStateTickerDemo.Demo,
        D.StateEffectsAnimation.SnapshotFlowDemo.Demo,
        D.StateEffectsAnimation.MutableStateCollectionsDemo.Demo,
        D.StateEffectsAnimation.StateFactoriesDemo.Demo,
        D.StateEffectsAnimation.NullableMutableStateDemo.Demo,
        D.StateEffectsAnimation.DerivedStateDemo.Demo,
        D.StateEffectsAnimation.LaunchedEffectDemo.Demo,
        D.StateEffectsAnimation.DisposableEffectDemo.Demo,
        D.StateEffectsAnimation.SideEffectDemo.Demo,
        D.StateEffectsAnimation.AnimatedVisibilityDemo.Demo,
        D.StateEffectsAnimation.EnterExitTransitionsDemo.Demo,
        D.StateEffectsAnimation.CrossfadeDemo.Demo,
        D.StateEffectsAnimation.AnimatedContentDemo.Demo,
        D.StateEffectsAnimation.ViewModelStateFlowDemo.Demo,
        D.StateEffectsAnimation.KotlinStateFlowDemo.Demo,

        // ---- CompositionLocal & misc ----
        D.LocalsMisc.LocalContextDemo.Demo,
        D.LocalsMisc.BuiltInCompositionLocalsDemo.Demo,
        D.LocalsMisc.CustomCompositionLocalDemo.Demo,
        D.LocalsMisc.ComposeViewInteropDemo.Demo,
        D.LocalsMisc.WindowSizeClassDemo.Demo,
        D.LocalsMisc.CircularProgressIndicatorDemo.Demo,
        D.LocalsMisc.LinearProgressIndicatorDemo.Demo,
        D.LocalsMisc.ImageDemo.Demo,
        D.LocalsMisc.ImageContentScaleDemo.Demo,
        D.LocalsMisc.IconDemo.Demo,

        // ---- Theming ----
        D.Theming.CustomColorSchemeDemo.Demo,
        D.Theming.CustomShapesDemo.Demo,
        D.Theming.CustomTypographyDemo.Demo,
        D.Theming.MaterialIconsDemo.Demo,
    ];

    /// <summary>
    /// Look up a category by <see cref="Category.Id"/>. Returns
    /// <c>null</c> when no such category exists (e.g. a stale deep
    /// link).
    /// </summary>
    public static Category? FindCategory(string? id) =>
        id is null ? null : Categories.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Look up a demo by <see cref="Demo.Id"/>. Returns <c>null</c>
    /// when no such demo exists.
    /// </summary>
    public static Demo? FindDemo(string? id) =>
        id is null ? null : Demos.FirstOrDefault(d => d.Id == id);

    /// <summary>
    /// Every demo declared with <see cref="Demo.CategoryId"/> equal to
    /// <paramref name="categoryId"/>, in registration order.
    /// </summary>
    public static IEnumerable<Demo> DemosByCategory(string categoryId) =>
        Demos.Where(d => d.CategoryId == categoryId);

    /// <summary>
    /// Case-insensitive <c>Contains</c> match across each demo's
    /// title and description plus its parent category title. An
    /// empty / whitespace-only query returns every demo.
    /// </summary>
    public static IEnumerable<Demo> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Demos;

        var needle = query.Trim();
        return Demos.Where(d =>
            d.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
            d.Description.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
            (FindCategory(d.CategoryId)?.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));
    }
}
