using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Text.Input;

namespace AndroidX.Compose.Samples.DirectConsumer;

internal static class DirectConsumerApp
{
    sealed record SampleItem(int Id, string Title);

    static readonly IReadOnlyList<SampleItem> OrderedItems =
        Enumerable.Range(1, 60).Select(i => new SampleItem(i, $"Item {i:D2}")).ToArray();
    static readonly IReadOnlyList<SampleItem> ReversedItems = OrderedItems.Reverse().ToArray();

    internal static ComposableNode Build(IComposer composer)
    {
        var nav = composer.Remember(() => new NavController());
        var count = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        var input = composer.RememberSaveable(() =>
            new MutableState<TextFieldValue>(ComposeExtensions.NewTextFieldValue()));
        var submitted = composer.RememberSaveable(() => new MutableState<string>(""));
        var isOn = composer.RememberSaveable(() => new MutableState<bool>(false));
        var level = composer.RememberSaveable(() => new MutableNumberState<float>(0.5f));
        var selected = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        var showDialog = composer.MutableStateOf(false);
        var dialogResult = composer.RememberSaveable(() => new MutableState<string>("Not opened"));
        var reversed = composer.RememberSaveable(() => new MutableState<bool>(false));

        var theme = new MaterialTheme
        {
            Dark = false,
            UseDynamicColor = false,
            ColorScheme = MaterialTheme.LightColorScheme(
                primary: Color.FromRgb(0x00, 0x62, 0xA0),
                onPrimary: Color.White),
            Typography = MaterialTheme.BuildTypography(
                bodyLarge: new TextStyle { FontSize = new Sp(16) }),
            Shapes = MaterialTheme.BuildShapes(
                medium: Shape.RoundedCorners(12)),
        };
        theme.Add(new NavHost("controls", nav)
        {
            Modifier.FillMaxSize().SafeDrawingPadding(),
            new NavDestination("controls")
            {
                Controls(composer, nav, count, input, submitted, isOn, level,
                    selected, showDialog, dialogResult),
            },
            new NavDestination("items")
            {
                Items(nav, reversed),
            },
        });
        return theme;
    }

    static ComposableNode Controls(
        IComposer composer,
        NavController nav,
        MutableNumberState<int> count,
        MutableState<TextFieldValue> input,
        MutableState<string> submitted,
        MutableState<bool> isOn,
        MutableNumberState<float> level,
        MutableNumberState<int> selected,
        MutableState<bool> showDialog,
        MutableState<string> dialogResult)
    {
        var defaults = KeyboardOptionsCompanion.Default;
        var keyboardOptions = composer.Remember(() => defaults.Copy(
            defaults.Capitalization, defaults.AutoCorrectEnabled,
            KeyboardType.Text, ImeAction.Done,
            defaults.PlatformImeOptions, defaults.ShowKeyboardOnFocus, defaults.HintLocales));
        var keyboardActions = composer.Remember(() => KeyboardActionsHelper.Create(
            onDone: () => submitted.Value = input.Value.Text));

        return new Column(verticalArrangement: Arrangement.SpacedBy(12.Dp()))
        {
            Modifier.FillMaxSize().VerticalScroll(composer.Remember(() => new ScrollState()))
                .Padding(16.Dp()).Semantics(s => s.TestTagsAsResourceId(true)),
            new Text("Direct Compose foundation"),
            new Text($"Count: {count.Value}")
            {
                Modifier = Modifier.TestTag("count-value"),
            },
            new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
            {
                new Button(() => count.Value++)
                {
                    Modifier.TestTag("increment"),
                    new Text("Increment"),
                },
                new Button(() => nav.Navigate("items"))
                {
                    Modifier.TestTag("open-items"),
                    new Text("Open items"),
                },
            },
            new OutlinedTextField(input, singleLine: true)
            {
                Modifier = Modifier.FillMaxWidth().TestTag("text-input"),
                Label = new Text("Text input"),
                KeyboardOptions = keyboardOptions,
                KeyboardActions = keyboardActions,
            },
            new Text($"Selection: {(int)(input.Value.Selection >> 32)}..{(int)input.Value.Selection}")
            {
                Modifier = Modifier.TestTag("text-selection"),
            },
            new Text($"IME submitted: {submitted.Value}")
            {
                Modifier = Modifier.TestTag("ime-result"),
            },
            new Image(Android.Resource.Drawable.IcMenuGallery, "Sample image")
            {
                Modifier = Modifier.Size(48.Dp())
                    .Border(1.Dp(), Brush.SolidColor(Color.FromRgb(0x00, 0x62, 0xA0)))
                    .TestTag("sample-image"),
            },
            new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()),
                verticalAlignment: Alignment.Vertical.CenterVertically)
            {
                Modifier.Toggleable(isOn.Value, value => isOn.Value = value)
                    .TestTag("toggle")
                    .ClearAndSetSemantics(s => s.ContentDescription("Example switch")
                        .Role(SemanticsRole.Switch)
                        .ToggleableState(isOn.Value)
                        .OnClick(() => isOn.Value = !isOn.Value)),
                new Switch(@checked: isOn.Value, onCheckedChange: value => isOn.Value = value)
                {
                    Modifier = Modifier.ClearAndSetSemantics(_ => { }),
                },
                new Text("Example switch"),
            },
            new Text($"Switch: {(isOn.Value ? "On" : "Off")}"),
            new Slider(level.Value, value => level.Value = value)
            {
                Modifier = Modifier.FillMaxWidth().TestTag("level-slider")
                    .ClearAndSetSemantics(s => s.ContentDescription("Example level")
                        .Progress(level.Value, new FloatRange(0f, 1f), value =>
                        {
                            if (level.Value == value)
                                return false;
                            level.Value = value;
                            return true;
                        })),
            },
            new Text($"Slider: {level.Value:F2}"),
            new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()),
                verticalAlignment: Alignment.Vertical.CenterVertically)
            {
                Modifier.TestTag("option-group")
                    .Semantics(s => s.SelectableGroup()),
                RadioOption(0, "Option A", selected),
                RadioOption(1, "Option B", selected),
            },
            new Text($"Selected: {(selected.Value == 0 ? "A" : "B")}"),
            new Button(() => showDialog.Value = true)
            {
                Modifier.TestTag("open-dialog"),
                new Text("Open dialog"),
            },
            new Text($"Dialog: {dialogResult.Value}"),
            showDialog.Value
                ? new AlertDialog(() => showDialog.Value = false)
                {
                    Title = new Text("Example dialog"),
                    Text = new Text("This dialog uses Material 3."),
                    ConfirmButton = new Button(() =>
                    {
                        dialogResult.Value = "Confirmed";
                        showDialog.Value = false;
                    })
                    {
                        new Text("Confirm"),
                    },
                    DismissButton = new Button(() => showDialog.Value = false)
                    {
                        new Text("Cancel"),
                    },
                }
                : (ComposableNode?)null,
        };
    }

    static ComposableNode RadioOption(int index, string label, MutableNumberState<int> selected) =>
        new Row(horizontalArrangement: Arrangement.SpacedBy(4.Dp()),
            verticalAlignment: Alignment.Vertical.CenterVertically)
        {
            Modifier.Selectable(selected.Value == index, () => selected.Value = index)
                .TestTag(index == 0 ? "option-a" : "option-b")
                .ClearAndSetSemantics(s => s.ContentDescription(label)
                    .Role(SemanticsRole.RadioButton)
                    .Selected(selected.Value == index)
                    .OnClick(() => selected.Value = index)),
            new RadioButton(selected.Value == index, () => selected.Value = index)
            {
                Modifier = Modifier.ClearAndSetSemantics(_ => { }),
            },
            new Text(label),
        };

    static ComposableNode Items(
        NavController nav,
        MutableState<bool> reversed)
    {
        var items = reversed.Value ? ReversedItems : OrderedItems;
        return new Column(verticalArrangement: Arrangement.SpacedBy(8.Dp()))
        {
            Modifier.FillMaxSize().Padding(16.Dp())
                .Semantics(s => s.TestTagsAsResourceId(true)),
            new Button(() => nav.PopBackStack())
            {
                Modifier.TestTag("back-to-controls"),
                new Text("Back"),
            },
            new Text($"First item: {items[0].Title}")
            {
                Modifier = Modifier.TestTag("first-item"),
            },
            new Button(() => reversed.Value = !reversed.Value)
            {
                Modifier.TestTag("reverse-items"),
                new Text("Reverse items"),
            },
            new LazyColumn<SampleItem>(items, item => new Text(item.Title)
            {
                Modifier = Modifier.TestTag($"item-{item.Id}"),
            })
            {
                Modifier = Modifier.FillMaxWidth().Weight(1f).TestTag("items-list"),
                Key = item => item.Id,
            },
        };
    }
}
