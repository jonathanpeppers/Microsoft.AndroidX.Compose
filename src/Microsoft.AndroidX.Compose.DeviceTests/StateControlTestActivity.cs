using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using SearchBarValue = AndroidX.Compose.Material3.SearchBarValue;
using WideNavigationRailValue = AndroidX.Compose.Material3.WideNavigationRailValue;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts public state-control wrappers in a real composition.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/StateControlTestActivity")]
public class StateControlTestActivity : ComponentActivity
{
    static int s_completedRenderPasses;

    internal static StateControlTestActivity? Current { get; private set; }
    internal static MutableState<bool>? Visible { get; private set; }
    internal static PagerState? Pager { get; private set; }
    internal static PullToRefreshState? PullToRefresh { get; private set; }
    internal static SearchBarState? Search { get; private set; }
    internal static SearchBarTextFieldState? SearchText { get; private set; }
    internal static SecureTextFieldState? SecureText { get; private set; }
    internal static WideNavigationRailState? Rail { get; private set; }
    internal static SnackbarHostState? Snackbar { get; private set; }
    internal static int CompletedRenderPasses => Volatile.Read(ref s_completedRenderPasses);

    internal static void MarkRenderCompleted() =>
        Interlocked.Increment(ref s_completedRenderPasses);

    internal static void Reset()
    {
        Current = null;
        Visible = null;
        Pager = new PagerState(static () => 3);
        PullToRefresh = new PullToRefreshState();
        var expanded = SearchBarValue.Expanded
            ?? throw new InvalidOperationException("SearchBarValue.Expanded was unavailable.");
        Search = new SearchBarState(expanded);
        SearchText = new SearchBarTextFieldState("initial");
        SearchText.SetTextAndSelectAll("pending search");
        SecureText = new SecureTextFieldState("initial");
        SecureText.SetTextAndSelectAll("pending secure");
        Rail = new WideNavigationRailState(WideNavigationRailValue.Collapsed);
        Snackbar = new SnackbarHostState();
        Volatile.Write(ref s_completedRenderPasses, 0);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Visible = new MutableState<bool>(true);
        this.SetContent(_ => new Composed(_ =>
        {
            var visible = Visible
                ?? throw new InvalidOperationException(
                    "Visibility state not set on StateControlTestActivity.");
            return new StateControlRenderMarker(
                visible.Value ? BuildControls() : null);
        }));
        Current = this;
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        Visible = null;
        base.OnDestroy();
    }

    static ComposableNode BuildControls()
    {
        var pager = Pager
            ?? throw new InvalidOperationException("Pager state was not initialized.");
        var pull = PullToRefresh
            ?? throw new InvalidOperationException("Pull-to-refresh state was not initialized.");
        var search = Search
            ?? throw new InvalidOperationException("Search state was not initialized.");
        var searchText = SearchText
            ?? throw new InvalidOperationException("Search text state was not initialized.");
        var secureText = SecureText
            ?? throw new InvalidOperationException("Secure text state was not initialized.");
        var rail = Rail
            ?? throw new InvalidOperationException("Rail state was not initialized.");
        var snackbar = Snackbar
            ?? throw new InvalidOperationException("Snackbar state was not initialized.");
        IReadOnlyList<int> pages = [0, 1, 2];

        return new Column
        {
            new HorizontalPager<int>(
                pages,
                static page => new Text($"Page {page}"))
            {
                State = pager,
                Modifier = Modifier.FillMaxWidth().Height(80),
            },
            new PullToRefreshBox(false, static () => { }, pull)
            {
                new Text("Pull"),
            },
            new SearchBar(search)
            {
                InputField = new SearchBarInputField(searchText, search),
            },
            new SecureTextField(secureText),
            new ModalWideNavigationRail(rail)
            {
                new Text("Rail"),
            },
            new SnackbarHost(snackbar),
        };
    }
}
