using Android.OS;
using AndroidX.Compose.Material3;
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
            var showAlert   = Remember(() => new MutableState<bool>(false));
            var showSheet   = Remember(() => new MutableState<bool>(false));
            var showDate    = Remember(() => new MutableState<bool>(false));
            var showTime    = Remember(() => new MutableState<bool>(false));
            var drawerKind  = Remember(() => new MutableNumberState<int>(0));
            var pickedDate  = Remember(() => new MutableState<string>("(none)"));
            var pickedTime  = Remember(() => new MutableState<string>("(none)"));
            var dateState   = Remember(() => new DatePickerState());
            var timeState   = Remember(() => new TimePickerState(initialHour: 9, initialMinute: 30));

            // Per-tab content. Only the current tab's column is added to
            // the screen — keeps the sample short enough to fit on one
            // phone-sized scroll area.
            ComposableNode tabContent = tab.Value switch
            {
                0 => new Column
                {
                    new Text("Hello from .NET"),
                    new Text($"Count: {count}"),
                    new Button(onClick: () => count++) { new Text("Tap to increment") },
                    new IconButton(onClick: () => count--) { new Text("−") },
                    new OutlinedTextField(name),
                    new Text($"Hi {(string.IsNullOrEmpty(name.Value) ? "stranger" : name.Value)}"),
                },
                1 => new Column
                {
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
                    new Button(onClick: () => drawerKind.Value = 0) { new Text("Modal") },
                    new Button(onClick: () => drawerKind.Value = 1) { new Text("Dismissible") },
                    new Button(onClick: () => drawerKind.Value = 2) { new Text("Permanent") },
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
                _ => new Column
                {
                    new Text("Dialogs and sheets"),
                    new Button(onClick: () => showSheet.Value = true) { new Text("Modal bottom sheet") },
                    new Button(onClick: () => showDate.Value  = true) { new Text("Date picker dialog") },
                    new Button(onClick: () => showTime.Value  = true) { new Text("Time picker dialog") },
                    new Text($"Picked date: {pickedDate}"),
                    new Text($"Picked time: {pickedTime}"),
                },
            };

            return new MaterialTheme
            {
                new Surface
                {
                    new Scaffold
                    {
                        // Bottom navigation switches between the three tabs above —
                        // pinned to the bottom edge by Scaffold instead of flowing
                        // inline at the end of a Column.
                        BottomBar = new NavigationBar
                        {
                            new NavigationBarItem(selected: tab.Value == 0, onClick: () => tab.Value = 0)
                            {
                                Icon  = new Text("🔢"),
                                Label = new Text("Basics"),
                            },
                            new NavigationBarItem(selected: tab.Value == 1, onClick: () => tab.Value = 1)
                            {
                                Icon  = new Text("👍"),
                                Label = new Text("Buttons"),
                            },
                            new NavigationBarItem(selected: tab.Value == 2, onClick: () => tab.Value = 2)
                            {
                                Icon  = new Text("🃏"),
                                Label = new Text("Cards"),
                            },
                            new NavigationBarItem(selected: tab.Value == 3, onClick: () => tab.Value = 3)
                            {
                                Icon  = new Text("📂"),
                                Label = new Text("Drawer"),
                            },
                            new NavigationBarItem(selected: tab.Value == 4, onClick: () => tab.Value = 4)
                            {
                                Icon  = new Text("📅"),
                                Label = new Text("Pickers"),
                            },
                        },
                        Body = new Column
                        {
                            Modifier.Companion.SafeDrawingPadding().Padding(16),
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
                                        pickedDate.Value = dateState.SelectedDateMillis is long ms ? ms.ToString() : "(none)";
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