using Android.OS;
using AndroidX.Compose.Material3;
using AndroidX.Compose.UI.Graphics;
using ComposeNet;

namespace ComposeNet.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    // Pastel M3-feeling palette used by the Carousels tab so the
    // item slots are visually distinct (default Card on a near-white
    // surface gives empty gray boxes).
    static readonly Color[] CarouselPalette =
    {
        Color.FromRgb(0xD0, 0xBC, 0xFF),
        Color.FromRgb(0xB3, 0xE5, 0xFC),
        Color.FromRgb(0xC8, 0xE6, 0xC9),
        Color.FromRgb(0xFF, 0xE0, 0xB2),
        Color.FromRgb(0xEF, 0xB8, 0xC8),
        Color.FromRgb(0xFF, 0xCD, 0xD2),
        Color.FromRgb(0xCC, 0xC2, 0xDC),
        Color.FromRgb(0xD7, 0xCC, 0xC8),
    };

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            // These four use RememberSaveable so their values survive
            // process death / activity recreation (e.g. rotation when
            // the activity doesn't override android:configChanges).
            var count       = RememberSaveable(() => new MutableNumberState<int>(0));
            var name        = RememberSaveable(() => new MutableState<string>(""));
            var liked       = RememberSaveable(() => new MutableState<bool>(false));
            var tab         = RememberSaveable(() => new MutableNumberState<int>(0));
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

            var pwd        = Remember(() => new SecureTextFieldState());
            var pwdConfirm = Remember(() => new SecureTextFieldState());
            // SecureTextFieldState.Text isn't snapshot-tracked when
            // read from C# build code (same constraint as
            // SearchBarTextFieldState — see Pickers tab note). Mirror
            // the lengths into a MutableState on Sign-in tap so the
            // status line updates reactively.
            var signInStatus = Remember(() => new MutableState<string>(""));

            var menuOpen      = Remember(() => new MutableState<bool>(false));
            var menuSelection = Remember(() => new MutableState<string>("(none)"));
            var searchState   = Remember(() => new SearchBarState());
            var searchInput   = Remember(() => new SearchBarTextFieldState());

            // New in this PR: range picker, exposed-dropdown box, docked
            // search bar.
            var showRange     = Remember(() => new MutableState<bool>(false));
            var pickedRange   = Remember(() => new MutableState<string>("(none)"));
            var rangeState    = Remember(() => new DateRangePickerState());
            var ddOpen        = Remember(() => new MutableState<bool>(false));
            var ddSelected    = Remember(() => new MutableState<string>("Apple"));
            var dockedOpen    = Remember(() => new MutableState<bool>(false));
            var dockedQuery   = Remember(() => new MutableState<string>(""));
            // Holds the committed query that drives the filter. The
            // bound TextFieldState.text getter doesn't subscribe to
            // Compose's snapshot read-tracking when read from C# build
            // code, so we can't drive the filter from it directly.
            // MutableState<string> IS snapshot-tracked, so updating it
            // from the SearchBarInputField.OnSearch callback (fired when
            // the user taps the IME Search action) gives a reactive
            // filter without binding InputTransformation.
            var searchQuery   = Remember(() => new MutableState<string>(""));

            // Pager tab: PagerState exposes CurrentPage to a reactive
            // indicator rendered after the pager. The same items array
            // is closed over by both the state's pageCount lambda and
            // the pager facade so the two stay in sync.
            var pagerItems = new[] { 0, 1, 2 };
            var pagerState = Remember(() => new PagerState(pageCount: () => pagerItems.Length));

            // Lazy tab: pull-to-refresh demo state. `refreshing` drives the
            // PullToRefreshBox indicator; `refreshTick` bumps once per
            // completed refresh so the rows visibly change. The reload
            // is faked with a 1.2s Handler.PostDelayed callback so the
            // spinner is observable without any real async work.
            var refreshing  = Remember(() => new MutableState<bool>(false));
            var refreshTick = Remember(() => new MutableNumberState<int>(0));

            // Scroll state for the (long) Buttons tab — drives both the
            // VerticalScroll modifier and the SuspendBridge demo buttons
            // that programmatically scroll back to the top.
            var buttonsScroll = Remember(() => new ScrollState());

            // Compose Navigation demo state (issue #60). The NavController
            // is the externally-driven entry point — Button onClicks call
            // .Navigate("...") / .PopBackStack() on it. Held in Remember
            // so it survives recompositions; Kotlin's rememberNavController
            // (via the ComposeBridges helper) stamps the underlying
            // controller into NavController.Jvm on first NavHost render.
            var navController = Remember(() => new NavController());

            string[] tabNames = { "Basics", "Buttons", "Cards", "Drawer", "Selection", "Pickers", "Misc", "App bars", "Lazy", "Carousels", "Pager", "Nav" };

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
                            // Value-type demo (issue #65): typed Sp / FontWeight /
                            // TextDecoration properties on Text + typed Dp on Modifier.
                            // Each property surfaces through the [ComposeFacade] /
                            // [ComposeBridge] generators end-to-end.
                            new Text("Styled text (issue #65):")
                            {
                                Modifier = Modifier.Companion.Padding(top: 8, bottom: 4, start: 0, end: 0),
                                FontWeight = ComposeNet.FontWeight.Bold,
                            },
                            new Text("Large + Bold")
                            {
                                FontSize = 24,
                                FontWeight = ComposeNet.FontWeight.Bold,
                            },
                            new Text("Italic-weight underline")
                            {
                                FontSize = 16,
                                FontWeight = ComposeNet.FontWeight.Medium,
                                Decoration = TextDecoration.Underline,
                            },
                            new Text("Strikethrough light")
                            {
                                FontSize = 14,
                                FontWeight = ComposeNet.FontWeight.Light,
                                Decoration = TextDecoration.LineThrough,
                            },
                            new Text("Wide letter spacing, taller lines, so the rendered glyphs visibly drift apart and rows breathe.")
                            {
                                FontSize = 14,
                                LetterSpacing = 2,
                                LineHeight = 22,
                                Modifier = Modifier.Companion.Padding(8),
                            },
                            // Issue #58: text styling additions — color, italic /
                            // family, alignment, overflow, line clamping. Each
                            // property flows through the [ComposeFacade] /
                            // [ComposeBridge] generators: Color/MaxLines/MinLines/
                            // SoftWrap use the new nullable-primitive path,
                            // FontStyle/FontFamily/TextAlign use the nullable
                            // reference-wrapper path, and TextOverflow uses the
                            // packed @JvmInline value-class path.
                            new Text("Issue #58 text styling:")
                            {
                                Modifier = Modifier.Companion.Padding(top: 8, bottom: 4, start: 0, end: 0),
                                FontWeight = ComposeNet.FontWeight.Bold,
                            },
                            new Text("Italic serif red, centered")
                            {
                                Color = Color.FromRgb(0xC6, 0x28, 0x28),
                                FontStyle = ComposeNet.FontStyle.Italic,
                                FontFamily = ComposeNet.FontFamily.Serif,
                                Align = ComposeNet.TextAlign.Center,
                                Modifier = Modifier.Companion.FillMaxWidth(),
                            },
                            new Text("Monospace, end-aligned")
                            {
                                FontFamily = ComposeNet.FontFamily.Monospace,
                                Align = ComposeNet.TextAlign.End,
                                Modifier = Modifier.Companion.FillMaxWidth(),
                            },
                            new Text("This long line should clip with an ellipsis because we cap it at MaxLines=1 and force overflow.")
                            {
                                MaxLines = 1,
                                Overflow = ComposeNet.TextOverflow.Ellipsis,
                                SoftWrap = false,
                            },
                            new Text("This paragraph wraps to at most two lines and uses a non-default minLines so the slot reserves vertical space even when the content is shorter than the maximum allowed.")
                            {
                                MaxLines = 2,
                                MinLines = 2,
                                Overflow = ComposeNet.TextOverflow.Ellipsis,
                            },
                            // TextField with new slots: leading/trailing icons,
                            // label, supporting text, prefix, suffix.
                            new TextField(name)
                            {
                                Label          = new Text("Your name"),
                                Placeholder    = new Text("Type something…"),
                                LeadingIcon    = new Text("👤"),
                                TrailingIcon   = new Text("✎"),
                                SupportingText = new Text("Powered by issue #58 slots"),
                                SingleLine     = true,
                            },
                            new OutlinedTextField(name)
                            {
                                Label          = new Text("Outlined variant"),
                                Prefix         = new Text("@"),
                                Suffix         = new Text(".dev"),
                                SupportingText = new Text($"len={name.Value.Length}"),
                                SingleLine     = true,
                            },
                            // Phase 2 modifier demo — clickable rounded chip painted with
                            // Background + Border + Clip; tapping it increments the counter.
                            new Text($"Phase 2 modifiers (tap me): {count}")
                            {
                                Modifier = Modifier.Companion
                                    .Clip(12)
                                    .Background(Color.FromRgb(0x19, 0x76, 0xD2))
                                    .Border(2, Color.FromRgb(0x0D, 0x47, 0xA1), cornerRadius: 12)
                                    .Clickable(() => count++)
                                    .Padding(horizontal: 16, vertical: 8),
                            },
                                // Secure text inputs — exercise both
                                // SecureTextField (filled) and
                                // OutlinedSecureTextField. The two state
                                // holders are independent; tapping "Sign in"
                                // snapshots both lengths into signInStatus.
                                new Text("Secure inputs:"),
                                new SecureTextField(pwd)
                                {
                                    Label          = new Text("Password"),
                                    LeadingIcon    = new Text("🔒"),
                                    SupportingText = new Text("Filled"),
                                },
                                new OutlinedSecureTextField(pwdConfirm)
                                {
                                    Label          = new Text("Confirm password"),
                                    LeadingIcon    = new Text("🔒"),
                                    SupportingText = new Text("Outlined"),
                                },
                                new Button(onClick: () =>
                                    signInStatus.Value =
                                        $"len={pwd.Text.Length}/{pwdConfirm.Text.Length}, " +
                                        $"match={(pwd.Text == pwdConfirm.Text)}")
                                {
                                    new Text("Sign in"),
                                },
                                new Text(string.IsNullOrEmpty(signInStatus.Value)
                                    ? "Tap Sign in to compare"
                                    : signInStatus.Value),
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
                    Modifier.Companion.VerticalScroll(buttonsScroll),

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
                    new Row
                    {
                        new SmallFloatingActionButton(onClick: () => count++) { new Text("+") },
                        new LargeFloatingActionButton(onClick: () => count++) { new Text("+") },
                    },
                    new ExtendedFloatingActionButton(onClick: () => count++, expanded: true)
                    {
                        Icon = new Text("✓"),
                        Text = new Text("Increment"),
                    },
                    new Button(onClick: () => showSnack.Value = true)
                    {
                        new Text("Show snackbar"),
                    },

                    new Text("Programmatic scrolling (suspend bridge)"),
                    new Row
                    {
                        new Button(onClick: () => _ = buttonsScroll.AnimateScrollToAsync(0))
                        {
                            new Text("Scroll to top (animated)"),
                        },
                        new Button(onClick: () => _ = buttonsScroll.ScrollToAsync(0))
                        {
                            new Text("Scroll to top (instant)"),
                        },
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

                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("Modifier showcase — shapes, shadow, transforms"),

                    // Background(long, Shape) + Shape.Circle / RoundedPercent /
                    // CutCorners. Each tile is a 56dp Box wrapping a label.
                    new Row
                    {
                        new Box
                        {
                            Modifier.Companion
                                .Size(56)
                                .Background(Color.FromRgb(0xD0, 0xBC, 0xFF), Shape.Circle()),
                            new Text("●") { Modifier = Modifier.Companion.Padding(16) },
                        },
                        new Spacer { Modifier = Modifier.Companion.WidthIn(8, null) },
                        new Box
                        {
                            Modifier.Companion
                                .Size(56)
                                .Background(Color.FromRgb(0xB3, 0xE5, 0xFC), Shape.RoundedPercent(25)),
                            new Text("◼") { Modifier = Modifier.Companion.Padding(16) },
                        },
                        new Spacer { Modifier = Modifier.Companion.WidthIn(8, null) },
                        new Box
                        {
                            Modifier.Companion
                                .Size(56)
                                .Background(Color.FromRgb(0xC8, 0xE6, 0xC9), Shape.CutCorners(10)),
                            new Text("◆") { Modifier = Modifier.Companion.Padding(16) },
                        },
                    },

                    // Shadow(elevation, shape) + Border(width, color, shape).
                    new Box
                    {
                        Modifier.Companion
                            .Padding(8)
                            .Shadow(8, Shape.RoundedCorners(16))
                            .Background(Color.FromRgb(0xFF, 0xE0, 0xB2), Shape.RoundedCorners(16))
                            .Border(2, Color.FromRgb(0xEF, 0x6C, 0x00), Shape.RoundedCorners(16))
                            .Padding(16),
                        new Text("Shadow + Border + Background, all on a shared Shape"),
                    },

                    // AspectRatio(16f/9f, matchHeightConstraintsFirst:true) — when
                    // both width and height are bounded, prefer the height
                    // constraint and let width follow.
                    new Box
                    {
                        Modifier.Companion
                            .FillMaxWidth()
                            .Height(80)
                            .AspectRatio(16f / 9f, matchHeightConstraintsFirst: true)
                            .Background(Color.FromRgb(0xCC, 0xC2, 0xDC)),
                        new Text("16:9 (height-first)") { Modifier = Modifier.Companion.Padding(8) },
                    },

                    // Rotate / Scale / Alpha tiles, each tagged so UI tests can
                    // match by semantic id.
                    new Row
                    {
                        new Box
                        {
                            Modifier.Companion
                                .TestTag("rotate-tile")
                                .Size(48)
                                .Rotate(15f)
                                .Background(Color.FromRgb(0xEF, 0xB8, 0xC8)),
                            new Text("⟲") { Modifier = Modifier.Companion.Padding(12) },
                        },
                        new Spacer { Modifier = Modifier.Companion.WidthIn(8, null) },
                        new Box
                        {
                            Modifier.Companion
                                .TestTag("scale-tile")
                                .Size(48)
                                .Scale(0.85f, 1.15f)
                                .Background(Color.FromRgb(0xFF, 0xCD, 0xD2)),
                            new Text("↕") { Modifier = Modifier.Companion.Padding(12) },
                        },
                        new Spacer { Modifier = Modifier.Companion.WidthIn(8, null) },
                        new Box
                        {
                            Modifier.Companion
                                .TestTag("alpha-tile")
                                .Size(48)
                                .Alpha(0.4f)
                                .Background(Color.FromRgb(0xD7, 0xCC, 0xC8)),
                            new Text("◐") { Modifier = Modifier.Companion.Padding(12) },
                        },
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
                    new Row(Arrangement.SpacedBy(12))
                    {
                        new Checkbox(@checked: checkbox.Value, onCheckedChange: v => checkbox.Value = v),
                        new Switch(@checked: switchOn.Value, onCheckedChange: v => switchOn.Value = v),
                        new RadioButton(selected: radioPick.Value == 0, onClick: () => radioPick.Value = 0),
                        new Text("A"),
                        new RadioButton(selected: radioPick.Value == 1, onClick: () => radioPick.Value = 1),
                        new Text("B"),
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
                    new Row(Arrangement.SpacedBy(8))
                    {
                        new Button(onClick: () => showSheet.Value = true) { new Text("Sheet") },
                        new Button(onClick: () => showDate.Value  = true) { new Text("Date") },
                        new Button(onClick: () => showRange.Value = true) { new Text("Range") },
                        new Button(onClick: () => showTime.Value  = true) { new Text("Time") },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text($"Picked date:  {pickedDate}"),
                    new Text($"Picked range: {pickedRange}"),
                    new Text($"Picked time:  {pickedTime}"),
                },
                6 => (ComposableNode)new Column
                {
                    new Text("Misc Material 3"),
                    new Text("Progress indicators (indeterminate):"),
                    new Row(Arrangement.SpacedBy(12))
                    {
                        new CircularProgressIndicator(),
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
                9 => new Column
                {
                    // Material 3 horizontal carousels. Each takes an items
                    // list + a per-item builder (same shape as LazyRow) and
                    // uses an auto-allocated CarouselState by default —
                    // assign State explicitly to share scroll position
                    // across recompositions. The per-item Box uses
                    // FillMaxSize so it stretches to whatever slot the
                    // carousel measures for it (the keyline strategy
                    // shrinks small/edge items independently).
                    new Text("HorizontalUncontainedCarousel (200dp items)"),
                    new HorizontalUncontainedCarousel<int>(
                        items:       System.Linq.Enumerable.Range(0, 12).ToList(),
                        itemWidth:   200f,
                        itemContent: i => new Box
                        {
                            Modifier.Companion
                                .FillMaxSize()
                                .Clip(20)
                                .Background(CarouselPalette[i % CarouselPalette.Length]),
                            new Text($"Item {i}") { Modifier = Modifier.Companion.Padding(16) },
                        })
                    {
                        Modifier    = Modifier.Companion.FillMaxWidth().Height(160),
                        ItemSpacing = 8f,
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("HorizontalMultiBrowseCarousel (240dp preferred)"),
                    new HorizontalMultiBrowseCarousel<int>(
                        items:              System.Linq.Enumerable.Range(0, 12).ToList(),
                        preferredItemWidth: 240f,
                        itemContent:        i => new Box
                        {
                            Modifier.Companion
                                .FillMaxSize()
                                .Clip(20)
                                .Background(CarouselPalette[i % CarouselPalette.Length]),
                            new Text($"#{i:D2}") { Modifier = Modifier.Companion.Padding(16) },
                        })
                    {
                        Modifier    = Modifier.Companion.FillMaxWidth().Height(180),
                        ItemSpacing = 8f,
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new Text("HorizontalCenteredHeroCarousel"),
                    new HorizontalCenteredHeroCarousel<int>(
                        items:       System.Linq.Enumerable.Range(0, 8).ToList(),
                        itemContent: i => new Box
                        {
                            Modifier.Companion
                                .FillMaxSize()
                                .Clip(24)
                                .Background(CarouselPalette[i % CarouselPalette.Length]),
                            new Text($"Hero {i}") { Modifier = Modifier.Companion.Padding(16) },
                        })
                    {
                        Modifier    = Modifier.Companion.FillMaxWidth().Height(220),
                        ItemSpacing = 8f,
                    },
                },
                10 => new Column
                {
                    // HorizontalPager swiping between 3 demo screens —
                    // the headline showcase from issue #51. Each page
                    // gets its own pastel slot so swipes feel obvious.
                    new Text("HorizontalPager (swipe between 3 screens)"),
                    new HorizontalPager<int>(
                        items:       pagerItems,
                        itemContent: i => new Box
                        {
                            Modifier.Companion
                                .FillMaxSize()
                                .Clip(20)
                                .Background(CarouselPalette[i % CarouselPalette.Length]),
                            new Text($"Screen {i + 1}")
                            {
                                Modifier = Modifier.Companion.Padding(16),
                            },
                        })
                    {
                        State    = pagerState,
                        Modifier = Modifier.Companion.FillMaxWidth().Height(200),
                    },
                    new Text($"Page {pagerState.CurrentPage + 1} of {pagerState.PageCount}"),
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },

                    // FlowRow — chip-style group that wraps when it
                    // runs out of horizontal space. Each chip is a
                    // padded Card so the wrap behaviour is visible
                    // without a Material 3 chip facade.
                    new Text("FlowRow (wraps when out of width)"),
                    new FlowRow
                    {
                        Modifier.Companion.FillMaxWidth().Padding(4),
                        new Card { Modifier.Companion.Padding(4), new Text("Music") },
                        new Card { Modifier.Companion.Padding(4), new Text("Movies") },
                        new Card { Modifier.Companion.Padding(4), new Text("Podcasts") },
                        new Card { Modifier.Companion.Padding(4), new Text("News") },
                        new Card { Modifier.Companion.Padding(4), new Text("Sports") },
                        new Card { Modifier.Companion.Padding(4), new Text("Books") },
                        new Card { Modifier.Companion.Padding(4), new Text("Games") },
                        new Card { Modifier.Companion.Padding(4), new Text("Photography") },
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },

                    // BoxWithConstraints — hands the available layout
                    // dp back as a callback so the child layout can
                    // branch on width (the idiomatic Compose alternative
                    // to runtime device-class checks).
                    new Text("BoxWithConstraints (reports its own width in dp)"),
                    new BoxWithConstraints(c => new Text(
                        $"Max width = {c.MaxWidth:0.#} dp, max height = {c.MaxHeight:0.#} dp"))
                    {
                        Modifier = Modifier.Companion.FillMaxWidth().Padding(8),
                    },
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },

                    // LazyVerticalStaggeredGrid — each cell is a Card
                    // with a deliberately varying height (cycled from a
                    // small table) so the staggered effect is obvious.
                    new Text("LazyVerticalStaggeredGrid (Adaptive 120dp)"),
                    new LazyVerticalStaggeredGrid<int>(
                        columns:     StaggeredGridCells.Adaptive(120f),
                        items:       System.Linq.Enumerable.Range(0, 30).ToList(),
                        itemContent: i => new Card
                        {
                            Modifier.Companion
                                .Padding(4)
                                .Height(60 + (i % 5) * 30)
                                .Background(CarouselPalette[i % CarouselPalette.Length]),
                            new Text($"#{i:D2}")
                            {
                                Modifier = Modifier.Companion.Padding(8),
                            },
                        })
                    {
                        Modifier = Modifier.Companion.FillMaxWidth().Height(300),
                    },
                },
                11 => new Column
                {
                    // Compose Navigation demo (issue #60). NavHost holds a
                    // graph of `Composable("route") { ... }` destinations and
                    // switches the visible one based on the bound NavController's
                    // back stack. Each route subcomposes its own children
                    // independently — unlike a normal switch-on-state UI, the
                    // destinations don't share a composition with the host.
                    //
                    // The "user/{id}" route demos the dynamic-content factory
                    // overload (Composable(route, entry => ...)) — we read
                    // the {id} placeholder out of NavBackStackEntry.Arguments.
                    new Text("Compose Navigation"),
                    new Text("Tap a button to navigate. The Up button uses navController.NavigateUp()."),
                    new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                    new NavHost(startDestination: "home", navController: navController)
                    {
                        Modifier.Companion.FillMaxWidth().Height(360),

                        new Composable("home")
                        {
                            new Column
                            {
                                Modifier.Companion.Padding(16),
                                new Text("🏠 Home"),
                                new Button(onClick: () => navController.Navigate("detail"))
                                {
                                    new Text("Go to detail"),
                                },
                                new Button(onClick: () => navController.Navigate("user/42"))
                                {
                                    new Text("Open user 42"),
                                },
                            },
                        },
                        new Composable("detail")
                        {
                            new Column
                            {
                                Modifier.Companion.Padding(16),
                                new Text("📄 Detail"),
                                new Button(onClick: () => navController.Navigate("user/7"))
                                {
                                    new Text("Drill down to user 7"),
                                },
                                new Button(onClick: () => navController.PopBackStack())
                                {
                                    new Text("Back"),
                                },
                            },
                        },
                        new Composable("user/{id}", entry => new Column
                        {
                            Modifier.Companion.Padding(16),
                            new Text($"👤 User #{entry.Arguments?.GetString("id") ?? "?"}"),
                            new Text($"Route: {entry.Route ?? "(unknown)"}"),
                            new Button(onClick: () => navController.NavigateUp())
                            {
                                new Text("Up"),
                            },
                        }),
                    },
                },
                _ => new Column
                {
                    // Lazy lists — bound through LazyDslKt / LazyGridDslKt.
                    // Each LazyColumn / LazyVerticalGrid takes the items
                    // list + a per-item callback. Compose lazily composes
                    // only the visible window, so 1000 rows costs about
                    // the same as 20.
                    new Text($"LazyColumn (1000 rows) — pull to refresh, rev {refreshTick}"),
                    // PullToRefreshBox wraps a scrollable child and surfaces
                    // the Material 3 pull gesture. The Box stretches to fill
                    // the available height so the indicator animates over
                    // the list rather than overflowing below it.
                    new PullToRefreshBox(
                        isRefreshing: refreshing.Value,
                        onRefresh:    () =>
                        {
                            refreshing.Value = true;
                            new Handler(Looper.MainLooper!).PostDelayed(() =>
                            {
                                refreshTick.Value++;
                                refreshing.Value = false;
                            }, 1200);
                        })
                    {
                        Modifier.Companion.FillMaxWidth().Height(220),

                        new LazyColumn<int>(
                            items:       System.Linq.Enumerable.Range(0, 1000).ToList(),
                            itemContent: i => new Text($"Row {i:D4} (rev {refreshTick})"))
                        {
                            Modifier = Modifier.Companion.FillMaxSize(),
                        },
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
                pickers.Add(new Text("ExposedDropdownMenuBox"));
                pickers.Add(new Text("Read-only TextField + tap the ▼ button to open the menu."));
                pickers.Add(new ExposedDropdownMenuBox(
                    expanded:         ddOpen.Value,
                    onExpandedChange: v => ddOpen.Value = v)
                {
                    new Row
                    {
                        new TextField(value: ddSelected.Value, onValueChange: _ => { }),
                        new IconButton(onClick: () => ddOpen.Value = !ddOpen.Value)
                        {
                            new Text(ddOpen.Value ? "▲" : "▼"),
                        },
                    },
                    new ExposedDropdownMenu(
                        expanded:         ddOpen.Value,
                        onDismissRequest: () => ddOpen.Value = false)
                    {
                        new DropdownMenuItem(text: new Text("Apple"),  onClick: () => { ddSelected.Value = "Apple";  ddOpen.Value = false; }),
                        new DropdownMenuItem(text: new Text("Banana"), onClick: () => { ddSelected.Value = "Banana"; ddOpen.Value = false; }),
                        new DropdownMenuItem(text: new Text("Cherry"), onClick: () => { ddSelected.Value = "Cherry"; ddOpen.Value = false; }),
                    },
                });

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

                pickers.Add(new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 16) });
                pickers.Add(new Text("DockedSearchBar (deprecated boolean-state variant)"));
                pickers.Add(new Text("Type in the field, tap the ▼/▲ button to toggle the docked results popup."));
                var dockedMatches = System.Array.FindAll(
                    fruits,
                    f => string.IsNullOrEmpty(dockedQuery.Value)
                         || f.Contains(dockedQuery.Value, System.StringComparison.OrdinalIgnoreCase));
#pragma warning disable CS0618 // DockedSearchBar is intentionally exercised here
                var docked = new DockedSearchBar(
                    expanded:         dockedOpen.Value,
                    onExpandedChange: v => dockedOpen.Value = v)
                {
                    InputField = new Row
                    {
                        new TextField(dockedQuery),
                        new IconButton(onClick: () => dockedOpen.Value = !dockedOpen.Value)
                        {
                            new Text(dockedOpen.Value ? "▲" : "▼"),
                        },
                    },
                };
#pragma warning restore CS0618
                foreach (var f in dockedMatches)
                    docked.Add(new Text(f) { Modifier = Modifier.Companion.Padding(16, 12) });
                if (dockedMatches.Length == 0)
                    docked.Add(new Text("(no matches)") { Modifier = Modifier.Companion.Padding(16, 12) });
                pickers.Add(docked);
            }

            return new MaterialTheme
            {
                new Surface
                {
                    new Scaffold
                    {
                        TopBar = new CenterAlignedTopAppBar
                        {
                            Title = new Text(tabNames[tab.Value]),
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
                                new Tab(selected: tab.Value == 9, onClick: () => tab.Value = 9)
                                {
                                    Text = new Text("Carousels"),
                                    Icon = new Text("🎠"),
                                },
                                new Tab(selected: tab.Value == 10, onClick: () => tab.Value = 10)
                                {
                                    Text = new Text("Pager"),
                                    Icon = new Text("📑"),
                                },
                                new Tab(selected: tab.Value == 11, onClick: () => tab.Value = 11)
                                {
                                    Text = new Text("Nav"),
                                    Icon = new Text("🧭"),
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

                            showRange.Value
                                ? new DateRangePickerDialog(onDismissRequest: () => showRange.Value = false)
                                {
                                    ConfirmButton = new Button(onClick: () =>
                                    {
                                        static string Fmt(long? ms) => ms is long m
                                            ? System.DateTimeOffset.FromUnixTimeMilliseconds(m).UtcDateTime.ToString("yyyy-MM-dd")
                                            : "(none)";
                                        pickedRange.Value =
                                            $"{Fmt(rangeState.SelectedStartDateMillis)} → {Fmt(rangeState.SelectedEndDateMillis)}";
                                        showRange.Value = false;
                                    })
                                    { new Text("OK") },
                                    DismissButton = new Button(onClick: () => showRange.Value = false) { new Text("Cancel") },
                                    Body          = new DateRangePicker(rangeState),
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