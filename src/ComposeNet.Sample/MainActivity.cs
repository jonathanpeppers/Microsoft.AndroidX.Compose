using Android.OS;
using AndroidX.Compose.Material3;
using AndroidX.Compose.UI.Graphics;
using ComposeNet;

namespace ComposeNet.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var count       = Remember(() => new MutableNumberState<int>(0));
            var name        = Remember(() => new MutableState<string>(""));
            var liked       = Remember(() => new MutableState<bool>(false));
            var tab         = Remember(() => new MutableNumberState<int>(0));
            var sub         = Remember(() => new MutableNumberState<int>(0));
            var showAlert   = Remember(() => new MutableState<bool>(false));
            var showSheet   = Remember(() => new MutableState<bool>(false));
            var showSnack   = Remember(() => new MutableState<bool>(false));
            var showDate    = Remember(() => new MutableState<bool>(false));
            var showTime    = Remember(() => new MutableState<bool>(false));
            var drawerKind  = Remember(() => new MutableNumberState<int>(0));
            var pickedDate  = Remember(() => new MutableState<string>("(none)"));
            var pickedTime  = Remember(() => new MutableState<string>("(none)"));
            var dateState   = Remember(() => new DatePickerState());
            var timeState   = Remember(() => new TimePickerState(initialHour: 9, initialMinute: 30));
            var segIdx      = Remember(() => new MutableNumberState<int>(0));
            var multiBold   = Remember(() => new MutableState<bool>(false));
            var multiItalic = Remember(() => new MutableState<bool>(false));
            var railIdx     = Remember(() => new MutableNumberState<int>(0));
            var showModalRail = Remember(() => new MutableState<bool>(false));
            var modalRailIdx  = Remember(() => new MutableNumberState<int>(0));

            var checkbox    = Remember(() => new MutableState<bool>(true));
            var switchOn    = Remember(() => new MutableState<bool>(false));
            var radioPick   = Remember(() => new MutableNumberState<int>(0));
            var sliderVal   = Remember(() => new MutableState<float>(0.5f));
            var rangeStart  = Remember(() => new MutableState<float>(0.25f));
            var rangeEnd    = Remember(() => new MutableState<float>(0.75f));
            var iconToggle1 = Remember(() => new MutableState<bool>(false));
            var iconToggle2 = Remember(() => new MutableState<bool>(true));
            var iconToggle3 = Remember(() => new MutableState<bool>(false));
            var iconToggle4 = Remember(() => new MutableState<bool>(true));

            var menuOpen      = Remember(() => new MutableState<bool>(false));
            var menuSelection = Remember(() => new MutableState<string>("(none)"));
            var searchState   = Remember(() => new SearchBarState());
            var searchInput   = Remember(() => new SearchBarTextFieldState());
            // Holds the committed query that drives the filter. The
            // bound TextFieldState.text getter doesn't subscribe to
            // Compose's snapshot read-tracking when read from C# build
            // code, so we can't drive the filter from it directly.
            // MutableState<string> IS snapshot-tracked, so updating it
            // from the SearchBarInputField.OnSearch callback (fired when
            // the user taps the IME Search action) gives a reactive
            // filter without binding InputTransformation.
            var searchQuery   = Remember(() => new MutableState<string>(""));

            string[] tabNames = { "Basics", "Buttons", "Cards", "Drawer", "Selection", "Pickers", "Misc", "App bars", "Lazy" };

            // Per-tab content. Only the current tab's column is added to
            // the screen — keeps the sample short enough to fit on one
            // phone-sized scroll area.
            ComposableNode tabContent = tab.Value switch
            {
                0 => new Column
                {
                    // PrimaryScrollableTabRow lets the row scroll horizontally
                    // when tabs don't fit; CustomTab is the ColumnScope-content
                    // overload (multi-line label inside the tab).
                    new PrimaryScrollableTabRow(selectedTabIndex: sub.Value)
                    {
                        new Tab(selected: sub.Value == 0, onClick: () => sub.Value = 0)
                        {
                            Text = new Text("Greeting"),
                        },
                        new Tab(selected: sub.Value == 1, onClick: () => sub.Value = 1)
                        {
                            Text = new Text("Counter"),
                        },
                        new LeadingIconTab(selected: sub.Value == 2, onClick: () => sub.Value = 2)
                        {
                            Text = new Text("List"),
                            Icon = new Text("📋"),
                        },
                        new CustomTab(selected: sub.Value == 3, onClick: () => sub.Value = 3)
                        {
                            new Text("Custom"),
                            new Text($"count={count}"),
                        },
                    },
                    sub.Value switch
                    {
                        0 => (ComposableNode)new Column
                        {
                            new Text("Hello from .NET"),
                            new OutlinedTextField(name),
                            new Text($"Hi {(string.IsNullOrEmpty(name.Value) ? "stranger" : name.Value)}"),
                            // Phase 2 modifier demo — clickable rounded chip painted with
                            // Background + Border + Clip; tapping it increments the counter.
                            new Text($"Phase 2 modifiers (tap me): {count}")
                            {
                                Modifier = Modifier.Companion
                                    .Clip(12)
                                    .Background(ColorKt.Color(red: 0x19, green: 0x76, blue: 0xD2, alpha: 0xFF))
                                    .Border(2, ColorKt.Color(red: 0x0D, green: 0x47, blue: 0xA1, alpha: 0xFF), cornerRadiusDp: 12)
                                    .Clickable(() => count++)
                                    .Padding(horizontalDp: 16, verticalDp: 8),
                            },
                        },
                        1 => new Column
                        {
                            new Text($"Count: {count}"),
                            new Row
                            {
                                new Image(Resource.Drawable.ic_star, "Star icon"),
                                new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.05f) },
                                new Column
                                {
                                    new Button(onClick: () => count++) { new Text("Tap to increment") },
                                    new IconButton(onClick: () => count--) { new Text("−") },
                                },
                            },
                        },
                        2 => (ComposableNode)new Column
                        {
                            new ListItem
                            {
                                Headline   = new Text("Inbox"),
                                Supporting = new Text("12 unread messages"),
                                Leading    = new Text("📥"),
                                Trailing   = new Badge { new Text("12") },
                            },
                            new ListItem
                            {
                                Headline   = new Text("Sent"),
                                Supporting = new Text("Last sent yesterday"),
                                Leading    = new Text("📤"),
                            },
                            new ListItem
                            {
                                Headline = new Text("Drafts"),
                                Leading  = new Text("📝"),
                                Trailing = new BadgedBox
                                {
                                    Badge   = new Badge { new Text("3") },
                                    Content = new Text("✉"),
                                },
                            },
                        },
                        _ => new Column
                        {
                            new Text("CustomTab uses the Tab-bogVsAg overload"),
                            new Text("(ColumnScope-receiver content lambda)."),
                            new Text($"Current count: {count}"),
                            new Button(onClick: () => count++) { new Text("+1") },
                        },
                    },
                },
                1 => new Column
                {
                    new Text("Button fill styles"),
                    new Button(onClick: () => count++) { new Text("Filled") },
                    new ElevatedButton(onClick: () => count++) { new Text("Elevated") },
                    new FilledTonalButton(onClick: () => count++) { new Text("Filled tonal") },
                    new OutlinedButton(onClick: () => count++) { new Text("Outlined") },
                    new TextButton(onClick: () => count++) { new Text("Text") },

                    new Text("Icon button fill styles"),
                    new Row
                    {
                        new IconButton(onClick: () => count++) { new Text("☆") },
                        new FilledIconButton(onClick: () => count++) { new Text("★") },
                        new FilledTonalIconButton(onClick: () => count++) { new Text("◆") },
                        new OutlinedIconButton(onClick: () => count++) { new Text("◇") },
                    },

                    new Text("Icon toggle buttons"),
                    new Row
                    {
                        new IconToggleButton(@checked: iconToggle1.Value,
                            onCheckedChange: v => iconToggle1.Value = v)
                        { new Text(iconToggle1.Value ? "★" : "☆") },
                        new FilledIconToggleButton(@checked: iconToggle2.Value,
                            onCheckedChange: v => iconToggle2.Value = v)
                        { new Text(iconToggle2.Value ? "★" : "☆") },
                        new FilledTonalIconToggleButton(@checked: iconToggle3.Value,
                            onCheckedChange: v => iconToggle3.Value = v)
                        { new Text(iconToggle3.Value ? "◆" : "◇") },
                        new OutlinedIconToggleButton(@checked: iconToggle4.Value,
                            onCheckedChange: v => iconToggle4.Value = v)
                        { new Text(iconToggle4.Value ? "◆" : "◇") },
                    },

                    new Text("Chips, FAB, tooltip"),
                    new AssistChip(onClick: () => count++)
                    {
                        Label = new Text("Assist (+1)"),
                    },
                    new ElevatedAssistChip(onClick: () => count++)
                    {
                        Label = new Text("Elevated assist (+1)"),
                    },
                    new FilterChip(selected: liked.Value, onClick: () => liked.Value = !liked.Value)
                    {
                        Label = new Text(liked.Value ? "Liked" : "Like"),
                    },
                    new ElevatedFilterChip(selected: liked.Value, onClick: () => liked.Value = !liked.Value)
                    {
                        Label = new Text(liked.Value ? "Elevated liked" : "Elevated like"),
                    },
                    new SuggestionChip(onClick: () => count.Value = 0)
                    {
                        Label = new Text("Reset"),
                    },
                    new ElevatedSuggestionChip(onClick: () => count.Value = 0)
                    {
                        Label = new Text("Elevated reset"),
                    },
                    new Tooltip
                    {
                        Tip    = new Surface { new Text("Helpful hint") },
                        Anchor = new Button(onClick: () => count++) { new Text("Long-press me") },
                    },
                    new FloatingActionButton(onClick: () => showAlert.Value = true)
                    {
                        new Text("✕"),
                    },
                    new Button(onClick: () => showSnack.Value = true)
                    {
                        new Text("Show snackbar"),
                    },
                },
                2 => new Column
                {
                    new Text("Card variants"),
                    new Card
                    {
                        new Text("Card (tonal)"),
                        new Text($"Counter snapshot: {count}"),
                    },
                    new ElevatedCard
                    {
                        new Text("ElevatedCard (shadow)"),
                        new Text($"Counter snapshot: {count}"),
                    },
                    new OutlinedCard
                    {
                        new Text("OutlinedCard (border)"),
                        new Text($"Counter snapshot: {count}"),
                    },
                },
                3 => new Column
                {
                    new Text("Navigation drawers"),
                    new Text("Tap a button to switch demo. Swipe from the"),
                    new Text("left edge of the demo area to open Modal /"),
                    new Text("Dismissible drawers (no coroutine plumbing)."),
                    new Row
                    {
                        new Button(onClick: () => drawerKind.Value = 0) { new Text("Modal") },
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.03f) },
                        new Button(onClick: () => drawerKind.Value = 1) { new Text("Dismissible") },
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.03f) },
                        new Button(onClick: () => drawerKind.Value = 2) { new Text("Permanent") },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    drawerKind.Value switch
                    {
                        0 => (ComposableNode)new ModalNavigationDrawer
                        {
                            Drawer = new ModalDrawerSheet
                            {
                                new Text("Modal drawer"),
                                new Text("• Inbox"),
                                new Text("• Sent"),
                                new Text("• Drafts"),
                            },
                            Content = new Column
                            {
                                new Text("Main content"),
                                new Text("Swipe right from edge →"),
                                new Text($"Count: {count}"),
                                new Button(onClick: () => count++) { new Text("+1") },
                            },
                        },
                        1 => new DismissibleNavigationDrawer
                        {
                            Drawer = new DismissibleDrawerSheet
                            {
                                new Text("Dismissible drawer"),
                                new Text("• Inbox"),
                                new Text("• Sent"),
                                new Text("• Drafts"),
                            },
                            Content = new Column
                            {
                                new Text("Main content"),
                                new Text("Swipe right to open →"),
                                new Text($"Count: {count}"),
                                new Button(onClick: () => count++) { new Text("+1") },
                            },
                        },
                        _ => new PermanentNavigationDrawer
                        {
                            Drawer = new PermanentDrawerSheet
                            {
                                new Text("Permanent drawer"),
                                new Text("• Inbox"),
                                new Text("• Sent"),
                                new Text("• Drafts"),
                            },
                            Content = new Column
                            {
                                new Text("Main content"),
                                new Text($"Count: {count}"),
                                new Button(onClick: () => count++) { new Text("+1") },
                            },
                        },
                    },
                },
                4 => (ComposableNode)new Column
                {
                    new Text("Selection controls"),
                    new Row
                    {
                        new Checkbox(@checked: checkbox.Value, onCheckedChange: v => checkbox.Value = v),
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.05f) },
                        new Switch(@checked: switchOn.Value, onCheckedChange: v => switchOn.Value = v),
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.05f) },
                        new RadioButton(selected: radioPick.Value == 0, onClick: () => radioPick.Value = 0),
                        new Text("A"),
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.03f) },
                        new RadioButton(selected: radioPick.Value == 1, onClick: () => radioPick.Value = 1),
                        new Text("B"),
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.03f) },
                        new RadioButton(selected: radioPick.Value == 2, onClick: () => radioPick.Value = 2),
                        new Text("C"),
                    },
                    new Text($"Checkbox: {checkbox.Value}, Switch: {switchOn.Value}, Radio: {(char)('A' + radioPick.Value)}"),
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Slider(value: sliderVal.Value, onValueChange: v => sliderVal.Value = v),
                    new Text($"Slider: {sliderVal.Value:F2}"),
                    new RangeSlider(
                        value: (rangeStart.Value, rangeEnd.Value),
                        onValueChange: r =>
                        {
                            rangeStart.Value = r.Start;
                            rangeEnd.Value   = r.End;
                        }),
                    new Text($"Range: {rangeStart.Value:F2} – {rangeEnd.Value:F2}"),
                },
                5 => new Column
                {
                    new Text("Dialogs and sheets"),
                    new Row
                    {
                        new Button(onClick: () => showSheet.Value = true) { new Text("Sheet") },
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.03f) },
                        new Button(onClick: () => showDate.Value  = true) { new Text("Date") },
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.03f) },
                        new Button(onClick: () => showTime.Value  = true) { new Text("Time") },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text($"Picked date: {pickedDate}"),
                    new Text($"Picked time: {pickedTime}"),
                },
                6 => (ComposableNode)new Column
                {
                    new Text("Misc Material 3"),
                    new Text("Progress indicators (indeterminate):"),
                    new Row
                    {
                        new CircularProgressIndicator(),
                        new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.05f) },
                        new Column
                        {
                            new Text("Linear ↓"),
                            new LinearProgressIndicator { Modifier = Modifier.Companion.FillMaxWidth() },
                        },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text($"Single-choice segmented (selected: {segIdx})"),
                    new SingleChoiceSegmentedButtonRow
                    {
                        new SegmentedButton(selected: segIdx.Value == 0, onClick: () => segIdx.Value = 0) { new Text("Day") },
                        new SegmentedButton(selected: segIdx.Value == 1, onClick: () => segIdx.Value = 1) { new Text("Week") },
                        new SegmentedButton(selected: segIdx.Value == 2, onClick: () => segIdx.Value = 2) { new Text("Month") },
                    },
                    new Text($"Multi-choice segmented (bold={multiBold.Value}, italic={multiItalic.Value})"),
                    new MultiChoiceSegmentedButtonRow
                    {
                        new SegmentedButton(@checked: multiBold.Value,   onCheckedChange: v => multiBold.Value   = v) { new Text("Bold") },
                        new SegmentedButton(@checked: multiItalic.Value, onCheckedChange: v => multiItalic.Value = v) { new Text("Italic") },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text($"WideNavigationRail (selected: {railIdx})"),
                    new Row
                    {
                        new WideNavigationRail
                        {
                            new WideNavigationRailItem(selected: railIdx.Value == 0, onClick: () => railIdx.Value = 0)
                            {
                                Icon  = new Text("🏠"),
                                Label = new Text("Home"),
                            },
                            new WideNavigationRailItem(selected: railIdx.Value == 1, onClick: () => railIdx.Value = 1)
                            {
                                Icon  = new Text("🔍"),
                                Label = new Text("Search"),
                            },
                            new WideNavigationRailItem(selected: railIdx.Value == 2, onClick: () => railIdx.Value = 2)
                            {
                                Icon  = new Text("⚙"),
                                Label = new Text("Settings"),
                            },
                        },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text($"ModalWideNavigationRail (selected: {modalRailIdx})"),
                    new Row
                    {
                        new Button(onClick: () => showModalRail.Value = !showModalRail.Value)
                        {
                            new Text(showModalRail.Value ? "Hide modal rail" : "Open modal rail"),
                        },
                    },
                    // Visibility-toggle pattern (see ModalWideNavigationRail
                    // XML doc): mounting the rail opens it; the "Close"
                    // item dismisses it. Scrim taps visually hide the rail
                    // but can't notify C# (no onDismissRequest / coroutine
                    // support), so the toggle button above is the escape
                    // hatch for re-mounting after a scrim dismiss.
                    showModalRail.Value
                        ? new ModalWideNavigationRail
                        {
                            new WideNavigationRailItem(
                                selected: modalRailIdx.Value == 0,
                                onClick:  () => { modalRailIdx.Value = 0; showModalRail.Value = false; })
                            {
                                Icon  = new Text("🏠"),
                                Label = new Text("Home"),
                            },
                            new WideNavigationRailItem(
                                selected: modalRailIdx.Value == 1,
                                onClick:  () => { modalRailIdx.Value = 1; showModalRail.Value = false; })
                            {
                                Icon  = new Text("🔍"),
                                Label = new Text("Search"),
                            },
                            new WideNavigationRailItem(
                                selected: modalRailIdx.Value == 2,
                                onClick:  () => { modalRailIdx.Value = 2; showModalRail.Value = false; })
                            {
                                Icon  = new Text("⚙"),
                                Label = new Text("Settings"),
                            },
                            new WideNavigationRailItem(selected: false, onClick: () => showModalRail.Value = false)
                            {
                                Icon  = new Text("✕"),
                                Label = new Text("Close"),
                            },
                        }
                        : (ComposableNode?)null,
                },
                7 => new Column
                {
                    // Inline samples of the M3 app-bar family bound by
                    // ComposeNet — normally these live in Scaffold.TopBar /
                    // Scaffold.BottomBar, but we drop them inline here just
                    // so all variants are visible side-by-side.
                    new Text("TopAppBar variants"),
                    new MediumFlexibleTopAppBar
                    {
                        Title    = new Text("MediumFlexibleTopAppBar"),
                        Subtitle = new Text($"count={count}"),
                    },
                    new LargeFlexibleTopAppBar
                    {
                        Title    = new Text("LargeFlexibleTopAppBar"),
                        Subtitle = new Text($"count={count}"),
                        Actions  = new Row
                        {
                            new IconButton(onClick: () => count++) { new Text("+") },
                        },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("SecondaryScrollableTabRow"),
                    new SecondaryScrollableTabRow(selectedTabIndex: sub.Value)
                    {
                        new Tab(selected: sub.Value == 0, onClick: () => sub.Value = 0) { Text = new Text("One") },
                        new Tab(selected: sub.Value == 1, onClick: () => sub.Value = 1) { Text = new Text("Two") },
                        new Tab(selected: sub.Value == 2, onClick: () => sub.Value = 2) { Text = new Text("Three") },
                        new Tab(selected: sub.Value == 3, onClick: () => sub.Value = 3) { Text = new Text("Four") },
                        new Tab(selected: sub.Value == 4, onClick: () => sub.Value = 4) { Text = new Text("Five") },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text($"BottomAppBar (actions only) — count is {count}"),
                    new BottomAppBar
                    {
                        new IconButton(onClick: () => count--) { new Text("−") },
                        new IconButton(onClick: () => count.Value = 0) { new Text("↺") },
                        new IconButton(onClick: () => count++) { new Text("+") },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("BottomAppBar (with FAB slot)"),
                    new BottomAppBar
                    {
                        FloatingActionButton = new FloatingActionButton(onClick: () => count++)
                        {
                            new Text("+"),
                        },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("FlexibleBottomAppBar"),
                    new FlexibleBottomAppBar
                    {
                        new IconButton(onClick: () => count++) { new Text("+") },
                        new IconButton(onClick: () => count--) { new Text("−") },
                    },
                },
                _ => new Column
                {
                    // Lazy lists — bound through LazyDslKt / LazyGridDslKt.
                    // Each LazyColumn / LazyVerticalGrid takes the items
                    // list + a per-item callback. Compose lazily composes
                    // only the visible window, so 1000 rows costs about
                    // the same as 20.
                    new Text("LazyColumn (1000 rows)"),
                    new LazyColumn<int>(
                        items:       System.Linq.Enumerable.Range(0, 1000).ToList(),
                        itemContent: i => new Text($"Row {i:D4}"))
                    {
                        Modifier = Modifier.Companion.FillMaxWidth().Height(220),
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("LazyRow (carousel)"),
                    new LazyRow<int>(
                        items:       System.Linq.Enumerable.Range(0, 50).ToList(),
                        itemContent: i => new Card
                        {
                            Modifier.Companion.Padding(4).Size(80),
                            new Text($"#{i}"),
                        })
                    {
                        Modifier = Modifier.Companion.FillMaxWidth(),
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("LazyVerticalGrid (Fixed 3)"),
                    new LazyVerticalGrid<int>(
                        columns:     GridCells.Fixed(3),
                        items:       System.Linq.Enumerable.Range(0, 60).ToList(),
                        itemContent: i => new Card
                        {
                            Modifier.Companion.Padding(4),
                            new Text($"Cell {i}"),
                        })
                    {
                        Modifier = Modifier.Companion.FillMaxWidth().Height(220),
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("LazyVerticalGrid (Adaptive 96dp)"),
                    new LazyVerticalGrid<int>(
                        columns:     GridCells.Adaptive(96f),
                        items:       System.Linq.Enumerable.Range(0, 40).ToList(),
                        itemContent: i => new Card
                        {
                            Modifier.Companion.Padding(4),
                            new Text($"A {i}"),
                        })
                    {
                        Modifier = Modifier.Companion.FillMaxWidth().Height(220),
                    },
                },
            };

            // Tab 5 (Pickers) appends DropdownMenu + SearchBar sections after
            // the dialog/sheet/date/time content from the switch. The SearchBar
            // result list is built dynamically with foreach, which can't live
            // inside a switch-expression collection-initializer.
            if (tab.Value == 5 && tabContent is Column pickers)
            {
                var fruits = new[]
                {
                    "Apple", "Banana", "Cherry", "Date", "Elderberry",
                    "Fig", "Grape", "Kiwi", "Lemon", "Mango",
                };
                // Read the live query from the MutableState that the
                // OnSearch callback pumps the typed text into. We can't
                // read SearchBarTextFieldState.Text here — the bound
                // JNI getter doesn't subscribe to Compose's snapshot
                // tracking when read from C# build code, so changes
                // wouldn't recompose this lambda. MutableState<string>
                // does subscribe, so the result list reacts when the
                // user commits a query via the keyboard Search action.
                var query   = searchQuery.Value;
                var matches = System.Array.FindAll(
                    fruits,
                    f => string.IsNullOrEmpty(query)
                         || f.Contains(query, System.StringComparison.OrdinalIgnoreCase));

                var expanded = new ExpandedFullScreenSearchBar(state: searchState)
                {
                    InputField = new SearchBarInputField(searchInput, searchState)
                    {
                        Placeholder = new Text("Search fruits"),
                        LeadingIcon = new Text("🔍"),
                        OnSearch    = q => searchQuery.Value = q,
                    },
                };
                foreach (var f in matches)
                    expanded.Add(new Text(f) { Modifier = Modifier.Companion.Padding(16, 12) });
                if (matches.Length == 0)
                    expanded.Add(new Text("(no matches)") { Modifier = Modifier.Companion.Padding(16, 12) });

                pickers.Add(new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 16) });
                pickers.Add(new Text("DropdownMenu"));
                pickers.Add(new Row
                {
                    new Text("Tap ⋮ for actions:"),
                    new Spacer { Modifier = Modifier.Companion.FillMaxWidth(0.03f) },
                    // The Box anchors the popup to the IconButton — both
                    // children share the Box's coordinate space, which is
                    // what DropdownMenu needs for positioning.
                    new Box
                    {
                        new IconButton(onClick: () => menuOpen.Value = true)
                        {
                            new Text("⋮"),
                        },
                        new DropdownMenu(
                            expanded:         menuOpen.Value,
                            onDismissRequest: () => menuOpen.Value = false)
                        {
                            new DropdownMenuItem(
                                text:    new Text("Refresh"),
                                onClick: () => { menuSelection.Value = "Refresh";  menuOpen.Value = false; }),
                            new DropdownMenuItem(
                                text:    new Text("Settings"),
                                onClick: () => { menuSelection.Value = "Settings"; menuOpen.Value = false; }),
                            new DropdownMenuItem(
                                text:    new Text("About"),
                                onClick: () => { menuSelection.Value = "About";    menuOpen.Value = false; }),
                        },
                    },
                });
                pickers.Add(new Text($"Last menu choice: {menuSelection}"));
                pickers.Add(new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 16) });
                pickers.Add(new Text("SearchBar"));
                pickers.Add(new Text("Tap the bar, type a query, then press the keyboard's 🔍 Search key to filter the fruit list."));
                pickers.Add(new Text($"Filter: \"{query}\" — {matches.Length} match{(matches.Length == 1 ? "" : "es")}"));
                // Render BOTH halves of the SearchBar pair sharing the
                // same SearchBarState + SearchBarTextFieldState — Compose
                // toggles the popup's visibility internally based on the
                // state, and the typed text is shared between halves.
                pickers.Add(new Box
                {
                    new SearchBar(state: searchState)
                    {
                        InputField = new SearchBarInputField(searchInput, searchState)
                        {
                            Placeholder = new Text("Search fruits"),
                            LeadingIcon = new Text("🔍"),
                            OnSearch    = q => searchQuery.Value = q,
                        },
                    },
                    expanded,
                });
            }

            return new MaterialTheme
            {
                new Surface
                {
                    new Scaffold
                    {
                        // The main TopAppBar uses the new Subtitle slot
                        // (routes to the M3 two-line TopAppBar-cJHQLPU overload).
                        TopBar = new TopAppBar
                        {
                            Title    = new Text(tabNames[tab.Value]),
                            Subtitle = new Text($"count={count}  ·  sub={sub}"),
                        },
                        SnackbarHost = showSnack.Value
                            ? new Snackbar
                            {
                                Action = new Button(onClick: () => showSnack.Value = false)
                                {
                                    new Text("Hide"),
                                },
                                Body = new Text($"Hello from {tabNames[tab.Value]}"),
                            }
                            : null,
                        Body = new Column
                        {
                            Modifier.Companion.Padding(16),
                            // ScrollableTabRow handles many tabs better than NavigationBar
                            // (which is spec'd for 3-5 items) — the row scrolls horizontally
                            // so every tab stays reachable as we add more demos.
                            new ScrollableTabRow(selectedTabIndex: tab.Value)
                            {
                                new Tab(selected: tab.Value == 0, onClick: () => tab.Value = 0)
                                {
                                    Text = new Text("Basics"),
                                    Icon = new Icon(Resource.Drawable.ic_settings, "Basics"),
                                },
                                new Tab(selected: tab.Value == 1, onClick: () => tab.Value = 1)
                                {
                                    Text = new Text("Buttons"),
                                    Icon = new Text("👍"),
                                },
                                new Tab(selected: tab.Value == 2, onClick: () => tab.Value = 2)
                                {
                                    Text = new Text("Cards"),
                                    Icon = new Text("🃏"),
                                },
                                new Tab(selected: tab.Value == 3, onClick: () => tab.Value = 3)
                                {
                                    Text = new Text("Drawer"),
                                    Icon = new Text("📂"),
                                },
                                new Tab(selected: tab.Value == 4, onClick: () => tab.Value = 4)
                                {
                                    Text = new Text("Selection"),
                                    Icon = new Text("☑"),
                                },
                                new Tab(selected: tab.Value == 5, onClick: () => tab.Value = 5)
                                {
                                    Text = new Text("Pickers"),
                                    Icon = new Text("📅"),
                                },
                                new Tab(selected: tab.Value == 6, onClick: () => tab.Value = 6)
                                {
                                    Text = new Text("Misc"),
                                    Icon = new Text("✨"),
                                },
                                new Tab(selected: tab.Value == 7, onClick: () => tab.Value = 7)
                                {
                                    Text = new Text("Bars"),
                                    Icon = new Text("📐"),
                                },
                                new Tab(selected: tab.Value == 8, onClick: () => tab.Value = 8)
                                {
                                    Text = new Text("Lazy"),
                                    Icon = new Text("🪟"),
                                },
                            },
                            tabContent,

                            // Overlays: rendered in the body so they participate in the
                            // same composition as the tab content. A dialog opened on
                            // Tab 1 still works after switching tabs.
                            showAlert.Value
                                ? new AlertDialog(onDismissRequest: () => showAlert.Value = false)
                                {
                                    Title         = new Text("Reset counter?"),
                                    Text          = new Text("This will set the counter back to zero."),
                                    ConfirmButton = new Button(onClick: () => { count.Value = 0; showAlert.Value = false; })
                                    {
                                        new Text("Reset"),
                                    },
                                    DismissButton = new Button(onClick: () => showAlert.Value = false)
                                    {
                                        new Text("Cancel"),
                                    },
                                }
                                : null,

                            // ModalBottomSheet — SheetState comes from the bound C#
                            // RememberModalBottomSheetState (NOT stripped, no JNI).
                            showSheet.Value
                                ? new ModalBottomSheet(onDismissRequest: () => showSheet.Value = false)
                                  {
                                      new Column
                                      {
                                          new Text("Modal bottom sheet"),
                                          new Text("Drag down or tap outside to dismiss."),
                                          new Button(onClick: () => showSheet.Value = false) { new Text("Hide") },
                                      },
                                  }
                                : null,

                            showDate.Value
                                ? new DatePickerDialog(onDismissRequest: () => showDate.Value = false)
                                {
                                    ConfirmButton = new Button(onClick: () =>
                                    {
                                        pickedDate.Value = dateState.SelectedDateMillis is long ms
                                            ? System.DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.ToString("yyyy-MM-dd")
                                            : "(none)";
                                        showDate.Value = false;
                                    })
                                    { new Text("OK") },
                                    DismissButton = new Button(onClick: () => showDate.Value = false) { new Text("Cancel") },
                                    Body          = new DatePicker(dateState),
                                }
                                : null,

                            showTime.Value
                                ? new TimePickerDialog(onDismissRequest: () => showTime.Value = false)
                                {
                                    Title         = new Text("Pick a time"),
                                    ConfirmButton = new Button(onClick: () =>
                                    {
                                        pickedTime.Value = $"{timeState.Hour:D2}:{timeState.Minute:D2}";
                                        showTime.Value = false;
                                    })
                                    { new Text("OK") },
                                    DismissButton = new Button(onClick: () => showTime.Value = false) { new Text("Cancel") },
                                    Body          = new TimePicker(timeState),
                                }
                                : null,
                        },
                    },
                },
            };
        });
    }
}