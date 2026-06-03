using Android.OS;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light")]
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
            var pickedDate  = Remember(() => new MutableState<string>("(none)"));
            var pickedTime  = Remember(() => new MutableState<string>("(none)"));
            var dateState   = Remember(() => new DatePickerState());
            var timeState   = Remember(() => new TimePickerState(initialHour: 9, initialMinute: 30));

            return new MaterialTheme
            {
                new Surface
                {
                    new Column
                    {
                        new Text("Hello from .NET"),
                        new Text($"Count: {count}"),
                        new Button(onClick: () => count++)
                        {
                            new Text("Tap to increment"),
                        },
                        new IconButton(onClick: () => count--)
                        {
                            new Text("−"),
                        },
                        new OutlinedTextField(name),
                        new Text($"Hi {(string.IsNullOrEmpty(name.Value) ? "stranger" : name.Value)}"),
                        new Card
                        {
                            new Text("Inside a Card"),
                            new Text($"Counter snapshot: {count}"),
                        },
                        new AssistChip(onClick: () => count++)
                        {
                            Label = new Text("Assist (+1)"),
                        },
                        new FilterChip(selected: liked.Value, onClick: () => liked.Value = !liked.Value)
                        {
                            Label = new Text(liked.Value ? "Liked" : "Like"),
                        },
                        new SuggestionChip(onClick: () => count.Value = 0)
                        {
                            Label = new Text("Reset"),
                        },
                        new NavigationBar
                        {
                            new NavigationBarItem(selected: tab.Value == 0, onClick: () => tab.Value = 0)
                            {
                                Icon  = new Text("🏠"),
                                Label = new Text("Home"),
                            },
                            new NavigationBarItem(selected: tab.Value == 1, onClick: () => tab.Value = 1)
                            {
                                Icon  = new Text("⚙"),
                                Label = new Text("Settings"),
                            },
                        },

                        // --- Trigger row: one button per follow-up composable. ---
                        new Button(onClick: () => showSheet.Value = true) { new Text("Modal bottom sheet") },
                        new Button(onClick: () => showDate.Value  = true) { new Text("Date picker dialog") },
                        new Button(onClick: () => showTime.Value  = true) { new Text("Time picker dialog") },

                        // Tooltip wrapping a button — long-press to show the popup.
                        new Tooltip
                        {
                            Tip    = new Surface { new Text("Helpful hint") },
                            Anchor = new Button(onClick: () => count++) { new Text("Long-press me") },
                        },

                        new Text($"Picked date: {pickedDate}"),
                        new Text($"Picked time: {pickedTime}"),

                        new FloatingActionButton(onClick: () => showAlert.Value = true)
                        {
                            new Text("✕"),
                        },

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
            };
        });
    }
}