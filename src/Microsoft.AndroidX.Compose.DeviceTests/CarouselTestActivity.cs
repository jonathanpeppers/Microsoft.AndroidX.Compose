using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Layout;
using System.Collections.Concurrent;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts real Material 3 carousels for nullable-Dp device tests.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/CarouselTestActivity")]
public class CarouselTestActivity : ComponentActivity
{
    internal const int TreeOmitted = 1;
    internal const int TreeExplicit = 2;
    internal const int StaticOmitted = 3;
    internal const int StaticExplicit = 4;

    internal const int Uncontained = 1;
    internal const int MultiBrowse = 2;
    internal const int CenteredHero = 3;
    internal const int ExpectedMeasurements = 6;
    internal const int ExplicitSpacingDp = 32;
    internal const int ExplicitMaxItemWidthDp = 160;

    static readonly IReadOnlyList<int> Items = [0, 1, 2];
    static readonly ConcurrentDictionary<int, ItemBounds> s_measurements = new();
    static int s_scenario;

    internal static CarouselTestActivity? Current { get; private set; }

    internal static int MeasurementCount => s_measurements.Count;

    internal static void Reset(int scenario)
    {
        Current = null;
        s_measurements.Clear();
        Volatile.Write(ref s_scenario, scenario);
    }

    internal static LayoutSnapshot Snapshot() => new(
        Item(Uncontained, 0),
        Item(Uncontained, 1),
        Item(MultiBrowse, 0),
        Item(MultiBrowse, 1),
        Item(CenteredHero, 0),
        Item(CenteredHero, 1));

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        switch (Volatile.Read(ref s_scenario))
        {
            case TreeOmitted:
                this.SetContent(static _ => BuildTree(explicitDp: false));
                break;
            case TreeExplicit:
                this.SetContent(static _ => BuildTree(explicitDp: true));
                break;
            case StaticOmitted:
                this.SetContent(RenderStaticOmitted);
                break;
            case StaticExplicit:
                this.SetContent(RenderStaticExplicit);
                break;
            default:
                throw new InvalidOperationException("Carousel test scenario was not configured.");
        }

        Current = this;
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        base.OnDestroy();
    }

    static ComposableNode BuildTree(bool explicitDp)
    {
        var uncontained = new HorizontalUncontainedCarousel<int>(
            Items,
            itemWidth: 96,
            item =>
                MeasuredText(Uncontained, item, $"Uncontained {item}"))
        {
            Modifier = Viewport(),
        };
        var multiBrowse = new HorizontalMultiBrowseCarousel<int>(
            Items,
            preferredItemWidth: 120,
            item =>
                MeasuredText(MultiBrowse, item, $"Multi-browse {item}"))
        {
            Modifier = Viewport(),
        };
        var centeredHero = new HorizontalCenteredHeroCarousel<int>(
            Items,
            item =>
                MeasuredText(CenteredHero, item, $"Hero {item}"))
        {
            Modifier = Viewport(),
        };

        if (explicitDp)
        {
            uncontained.ItemSpacing = ExplicitSpacingDp;
            multiBrowse.ItemSpacing = ExplicitSpacingDp;
            centeredHero.MaxItemWidth = ExplicitMaxItemWidthDp;
            centeredHero.ItemSpacing = ExplicitSpacingDp;
        }

        return new Column
        {
            uncontained,
            multiBrowse,
            centeredHero,
        };
    }

    static void RenderStaticOmitted(IComposer composer)
    {
        Composables.Column(composer, c =>
        {
            Composables.HorizontalUncontainedCarousel(
                c,
                Items,
                itemWidth: 96,
                (item, itemComposer) =>
                    RenderMeasuredText(
                        itemComposer,
                        Uncontained,
                        item,
                        $"Uncontained {item}"),
                modifier: Viewport());
            Composables.HorizontalMultiBrowseCarousel(
                c,
                Items,
                preferredItemWidth: 120,
                (item, itemComposer) =>
                    RenderMeasuredText(
                        itemComposer,
                        MultiBrowse,
                        item,
                        $"Multi-browse {item}"),
                modifier: Viewport());
            Composables.HorizontalCenteredHeroCarousel(
                c,
                Items,
                (item, itemComposer) =>
                    RenderMeasuredText(
                        itemComposer,
                        CenteredHero,
                        item,
                        $"Hero {item}"),
                modifier: Viewport());
        });
    }

    static void RenderStaticExplicit(IComposer composer)
    {
        Composables.Column(composer, c =>
        {
            Composables.HorizontalUncontainedCarousel(
                c,
                Items,
                itemWidth: 96,
                (item, itemComposer) =>
                    RenderMeasuredText(
                        itemComposer,
                        Uncontained,
                        item,
                        $"Uncontained {item}"),
                modifier: Viewport(),
                itemSpacing: ExplicitSpacingDp);
            Composables.HorizontalMultiBrowseCarousel(
                c,
                Items,
                preferredItemWidth: 120,
                (item, itemComposer) =>
                    RenderMeasuredText(
                        itemComposer,
                        MultiBrowse,
                        item,
                        $"Multi-browse {item}"),
                modifier: Viewport(),
                itemSpacing: ExplicitSpacingDp);
            Composables.HorizontalCenteredHeroCarousel(
                c,
                Items,
                (item, itemComposer) =>
                    RenderMeasuredText(
                        itemComposer,
                        CenteredHero,
                        item,
                        $"Hero {item}"),
                modifier: Viewport(),
                maxItemWidth: ExplicitMaxItemWidthDp,
                itemSpacing: ExplicitSpacingDp);
        });
    }

    static Modifier Viewport() => Modifier.FillMaxWidth().Height(140);

    static Text MeasuredText(int carousel, int item, string text) =>
        new(text)
        {
            Modifier = MeasureItem(carousel, item),
        };

    static void RenderMeasuredText(
        IComposer composer,
        int carousel,
        int item,
        string text) =>
        Composables.Text(
            composer,
            text,
            modifier: MeasureItem(carousel, item));

    static Modifier MeasureItem(int carousel, int item)
    {
        var callback = new ComposableLambda1(arg =>
        {
            var coordinates = arg?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException(
                    "onGloballyPositioned did not provide LayoutCoordinates.");
            long position = LayoutCoordinatesKt.PositionInRoot(coordinates);
            float x = Offset.FromPacked(position).X;
            int width = (int)((ulong)coordinates.Size >> 32);
            s_measurements[Key(carousel, item)] = new ItemBounds(x, width);
        });

        return Modifier.FillMaxSize().AppendBound(
            modifier => OnGloballyPositionedModifierKt.OnGloballyPositioned(
                modifier,
                callback),
            ModifierOpKey.Opaque);
    }

    static int Key(int carousel, int item) => carousel * 10 + item;

    static ItemBounds Item(int carousel, int item) =>
        s_measurements.TryGetValue(Key(carousel, item), out var bounds)
            ? bounds
            : throw new InvalidOperationException(
                $"Carousel {carousel}, item {item} was not measured.");

    internal readonly record struct ItemBounds(float X, int Width);

    internal readonly record struct LayoutSnapshot(
        ItemBounds Uncontained0,
        ItemBounds Uncontained1,
        ItemBounds MultiBrowse0,
        ItemBounds MultiBrowse1,
        ItemBounds CenteredHero0,
        ItemBounds CenteredHero1);
}
