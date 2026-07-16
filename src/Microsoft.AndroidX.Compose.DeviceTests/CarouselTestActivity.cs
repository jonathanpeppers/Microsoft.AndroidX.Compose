using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;

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
    internal const int AllCarouselsRendered = 0b111;

    const int UncontainedRendered = 0b001;
    const int MultiBrowseRendered = 0b010;
    const int CenteredHeroRendered = 0b100;

    static readonly IReadOnlyList<int> Items = [0, 1, 2];
    static int s_renderedCarousels;
    static int s_scenario;

    internal static CarouselTestActivity? Current { get; private set; }

    internal static int RenderedCarousels => Volatile.Read(ref s_renderedCarousels);

    internal static void Reset(int scenario)
    {
        Current = null;
        Volatile.Write(ref s_renderedCarousels, 0);
        Volatile.Write(ref s_scenario, scenario);
    }

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
            {
                MarkRendered(UncontainedRendered);
                return new Text($"Uncontained {item}");
            })
        {
            Modifier = Viewport(),
        };
        var multiBrowse = new HorizontalMultiBrowseCarousel<int>(
            Items,
            preferredItemWidth: 120,
            item =>
            {
                MarkRendered(MultiBrowseRendered);
                return new Text($"Multi-browse {item}");
            })
        {
            Modifier = Viewport(),
        };
        var centeredHero = new HorizontalCenteredHeroCarousel<int>(
            Items,
            item =>
            {
                MarkRendered(CenteredHeroRendered);
                return new Text($"Hero {item}");
            })
        {
            Modifier = Viewport(),
        };

        if (explicitDp)
        {
            uncontained.ItemSpacing = 8;
            multiBrowse.ItemSpacing = 8;
            centeredHero.MaxItemWidth = 240;
            centeredHero.ItemSpacing = 8;
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
                {
                    MarkRendered(UncontainedRendered);
                    Composables.Text(itemComposer, $"Uncontained {item}");
                },
                modifier: Viewport());
            Composables.HorizontalMultiBrowseCarousel(
                c,
                Items,
                preferredItemWidth: 120,
                (item, itemComposer) =>
                {
                    MarkRendered(MultiBrowseRendered);
                    Composables.Text(itemComposer, $"Multi-browse {item}");
                },
                modifier: Viewport());
            Composables.HorizontalCenteredHeroCarousel(
                c,
                Items,
                (item, itemComposer) =>
                {
                    MarkRendered(CenteredHeroRendered);
                    Composables.Text(itemComposer, $"Hero {item}");
                },
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
                {
                    MarkRendered(UncontainedRendered);
                    Composables.Text(itemComposer, $"Uncontained {item}");
                },
                modifier: Viewport(),
                itemSpacing: 8);
            Composables.HorizontalMultiBrowseCarousel(
                c,
                Items,
                preferredItemWidth: 120,
                (item, itemComposer) =>
                {
                    MarkRendered(MultiBrowseRendered);
                    Composables.Text(itemComposer, $"Multi-browse {item}");
                },
                modifier: Viewport(),
                itemSpacing: 8);
            Composables.HorizontalCenteredHeroCarousel(
                c,
                Items,
                (item, itemComposer) =>
                {
                    MarkRendered(CenteredHeroRendered);
                    Composables.Text(itemComposer, $"Hero {item}");
                },
                modifier: Viewport(),
                maxItemWidth: 240,
                itemSpacing: 8);
        });
    }

    static Modifier Viewport() => Modifier.FillMaxWidth().Height(140);

    static void MarkRendered(int carousel) => Interlocked.Or(ref s_renderedCarousels, carousel);
}
