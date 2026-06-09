using System.Collections.Generic;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Seed posts for the sample. Heavily condensed from the upstream
/// <c>PostsData.kt</c> (six topical articles — about ComposeNet itself
/// rather than upstream's Android-development articles, since those
/// quote real Android team members).
/// </summary>
public static class PostsRepo
{
    static readonly Post _highlighted = new(
        Id:       "compose-net-intro",
        Title:    "Hello, ComposeNet",
        Subtitle: "Jetpack Compose UI, hosted from C# on .NET-for-Android.",
        Metadata: new PostMetadata("Jordan Lee", "Apr 14", 4),
        HeroId:   Resource.Drawable.post_1,
        ThumbId:  Resource.Drawable.post_1_thumb,
        Paragraphs: new Paragraph[]
        {
            new(ParagraphType.Title,   "Hello, ComposeNet"),
            new(ParagraphType.Caption, "An idiomatic-C# facade over the real Jetpack Compose runtime."),
            // "ComposeNet" → Bold, "Kotlin" → Italic, "compiler interceptors" → Code.
            new(ParagraphType.Text,
                "ComposeNet hosts Jetpack Compose UI from a .NET-for-Android app. " +
                "There is no Kotlin in the project, no custom bindings, and no " +
                "compiler interceptors — every C# composable either calls a " +
                "generated AndroidX binding method or a tiny JNI bridge.",
                new Markup[]
                {
                    new(MarkupType.Bold,   0,  10),  // ComposeNet
                    new(MarkupType.Italic, 77, 83),  // Kotlin
                    new(MarkupType.Code,   127, 148),  // compiler interceptors
                }),
            new(ParagraphType.Subhead, "How it works"),
            // "Column", "Text", "Card" all surfaced as Code spans, plus
            // "[ComposeBridge]" and "@JvmInline".
            new(ParagraphType.Text,
                "Public facades like Column, Text, and Card derive from " +
                "ComposableNode and translate their Render(IComposer) body " +
                "into a chain of bound binding calls. Where Kotlin's @JvmInline " +
                "value classes confuse the binder, source-generated " +
                "[ComposeBridge] partial methods drop down to raw JNI for one " +
                "stripped overload at a time.",
                new Markup[]
                {
                    new(MarkupType.Code, 20, 26),    // Column
                    new(MarkupType.Code, 28, 32),    // Text
                    new(MarkupType.Code, 38, 42),    // Card
                    new(MarkupType.Code, 55, 69),    // ComposableNode
                    new(MarkupType.Code, 90, 112),   // Render(IComposer) body
                    new(MarkupType.Code, 165, 175),  // @JvmInline
                    new(MarkupType.Code, 227, 242),  // [ComposeBridge]
                }),
            new(ParagraphType.Quote,
                "Compose, but with C# 12 records, nullable refs, and the " +
                "rest of the .NET tooling you already use."),
            // "JetNews" → Link (decorative underline), "Jetchat" → Link too.
            new(ParagraphType.Text,
                "JetNews is the second sample in the repo — see Jetchat for the " +
                "navigation-drawer baseline and the per-author message bubbles.",
                new Markup[]
                {
                    new(MarkupType.Link, 0,  7,  Href: "https://github.com/android/compose-samples/tree/main/JetNews"),
                    new(MarkupType.Link, 47, 54, Href: "https://github.com/android/compose-samples/tree/main/Jetchat"),
                }),
        });

    static readonly Post _kotlinFreeBindings = new(
        Id:       "kotlin-free-bindings",
        Title:    "Why the bindings stay Kotlin-free",
        Subtitle: "One binding generator, zero Kotlin sources in the repo.",
        Metadata: new PostMetadata("Jordan Lee", "Apr 12", 5),
        HeroId:   Resource.Drawable.post_2,
        ThumbId:  Resource.Drawable.post_2_thumb,
        Paragraphs: new Paragraph[]
        {
            new(ParagraphType.Title,   "Why the bindings stay Kotlin-free"),
            new(ParagraphType.Text,
                "ComposeNet uses the official Xamarin.AndroidX.Compose.* NuGets. " +
                "When an overload is stripped by the binder, the project files an " +
                "issue against dotnet/android-libraries and works around the gap " +
                "with a [ComposeBridge]-decorated partial method until the upstream " +
                "binder fix ships.",
                new Markup[]
                {
                    new(MarkupType.Bold, 0,  10),                                              // ComposeNet
                    new(MarkupType.Code, 29, 55),                                              // Xamarin.AndroidX.Compose.*
                    new(MarkupType.Link, 143, 167, Href: "https://github.com/dotnet/android-libraries"),
                    new(MarkupType.Code, 200, 215),                                            // [ComposeBridge]
                }),
            new(ParagraphType.Subhead, "The cost of forking bindings"),
            new(ParagraphType.Text,
                "Forking the bindings would mean carrying a parallel set of " +
                "Compose definitions that has to be reconciled with every release. " +
                "Generating one JNI shim per stripped overload turned out to be " +
                "cheaper than maintaining a fork — and the shims naturally retire " +
                "as the binder catches up."),
        });

    static readonly Post _facadeGenerator = new(
        Id:       "facade-generator",
        Title:    "Generating the facade",
        Subtitle: "Phase 1-10 of ComposeFacadeGenerator, the recipe for one-line wrappers.",
        Metadata: new PostMetadata("Jordan Lee", "Apr 10", 6),
        HeroId:   Resource.Drawable.post_3,
        ThumbId:  Resource.Drawable.post_3_thumb,
        Paragraphs: new Paragraph[]
        {
            new(ParagraphType.Title,   "Generating the facade"),
            new(ParagraphType.Text,
                "ComposeFacadeGenerator turns a one-line [ComposeFacade] attribute " +
                "into a full ComposableNode subclass. The shape of the bridge " +
                "method tells the generator what kind of facade to emit."),
            new(ParagraphType.Subhead, "Phases in chronological order"),
            new(ParagraphType.Bullet, "Phase 1 - Containers with content lambdas and ctor primitives."),
            new(ParagraphType.Bullet, "Phase 2 - [Callback] for typed Action<T> ctors."),
            new(ParagraphType.Bullet, "Phase 3 - Multi-slot leafs with named ComposableNode? properties."),
            new(ParagraphType.Bullet, "Phase 4 - [StateHolder] for Remember-based state."),
            new(ParagraphType.Bullet, "Phase 7 - [PainterResource] for drawable-id ctors."),
            new(ParagraphType.Bullet, "Phase 10 - [ConfirmStateChange] for veto adapters."),
            new(ParagraphType.Text,
                "Each phase covers a small additional bridge shape. The generator " +
                "diagnostics (CN3001 through CN3011) tell you exactly which shape " +
                "you tripped over and how to fix it."),
        });

    static readonly Post _materialThree = new(
        Id:       "material-three",
        Title:    "Material 3 in C#",
        Subtitle: "Color, typography, and shape reads land alongside the dynamic palette.",
        Metadata: new PostMetadata("Jordan Lee", "Apr 8", 3),
        HeroId:   Resource.Drawable.post_4,
        ThumbId:  Resource.Drawable.post_4_thumb,
        Paragraphs: new Paragraph[]
        {
            new(ParagraphType.Title,   "Material 3 in C#"),
            new(ParagraphType.Text,
                "MaterialTheme.CurrentColorScheme(composer) and CurrentTypography " +
                "(composer) read the active theme inside any Render body. On " +
                "Android 12 and later the default scheme is dynamic — derived from " +
                "the device wallpaper — so your app picks up Material You without " +
                "extra plumbing.",
                new Markup[]
                {
                    new(MarkupType.Code,   0,   42),  // MaterialTheme.CurrentColorScheme(composer)
                    new(MarkupType.Code,   47,  64),  // CurrentTypography
                    new(MarkupType.Italic, 235, 247), // Material You
                }),
            new(ParagraphType.Header,  "Custom palettes"),
            new(ParagraphType.Text,
                "Pass MaterialTheme.LightColorScheme() / DarkColorScheme() with " +
                "an overlay of slot overrides, or call DynamicLightColorScheme " +
                "(context) directly. UseDynamicColor = false opts out of Material " +
                "You and falls back to the M3 baseline palette."),
        });

    static readonly Post _navigation = new(
        Id:       "navigation",
        Title:    "Navigation with NavHost",
        Subtitle: "Compose Navigation routes mapped onto a C# collection-initializer.",
        Metadata: new PostMetadata("Jordan Lee", "Apr 5", 3),
        HeroId:   Resource.Drawable.post_5,
        ThumbId:  Resource.Drawable.post_5_thumb,
        Paragraphs: new Paragraph[]
        {
            new(ParagraphType.Title,   "Navigation with NavHost"),
            new(ParagraphType.Text,
                "Allocate a NavController inside Remember, hand it to a NavHost, " +
                "and add destinations as collection-initializer children. " +
                "Static destinations are Composable(\"home\") { children }; dynamic " +
                "destinations take a Func<NavBackStackEntry, ComposableNode> " +
                "factory so they can read route placeholders."),
            new(ParagraphType.CodeBlock,
                "new NavHost(startDestination: \"home\", navController: nav)"),
            new(ParagraphType.CodeBlock,
                "    { new Composable(\"home\") { ... } }"),
        });

    static readonly Post _stateHolders = new(
        Id:       "state-holders",
        Title:    "State holders without coroutines",
        Subtitle: "MutableState, MutableStateList, and RememberSaveable in C#.",
        Metadata: new PostMetadata("Jordan Lee", "Apr 2", 4),
        HeroId:   Resource.Drawable.post_6,
        ThumbId:  Resource.Drawable.post_6_thumb,
        Paragraphs: new Paragraph[]
        {
            new(ParagraphType.Title,   "State holders without coroutines"),
            new(ParagraphType.Text,
                "Compose's snapshot system needs notification points for reads and " +
                "writes. MutableState<T> wraps a single value; MutableStateList<T> " +
                "is a managed observable list backed by a single tick counter. " +
                "RememberSaveable hands either through the SaveableStateRegistry " +
                "so they survive process death."),
            new(ParagraphType.Subhead, "Async paths"),
            new(ParagraphType.Text,
                "Where Kotlin Compose APIs surface suspend functions, ComposeNet " +
                "exposes Task / Task<T> via SuspendBridge — see ScrollState." +
                "ScrollToAsync for the pattern. The Continuation JCW self-roots " +
                "with a GCHandle and dispatches on the Main UI thread."),
        });

    /// <summary>The full feed of posts assembled into <see cref="PostsFeed"/>'s sections.</summary>
    public static PostsFeed Feed { get; } = new PostsFeed(
        Highlighted: _highlighted,
        Recommended: new[] { _kotlinFreeBindings, _facadeGenerator, _materialThree },
        Popular:     new[] { _navigation, _stateHolders, _kotlinFreeBindings },
        Recent:      new[] { _materialThree, _navigation, _stateHolders });

    /// <summary>Look up a post by id; throws if not found.</summary>
    public static Post Find(string id)
    {
        foreach (var p in All)
            if (p.Id == id) return p;
        throw new KeyNotFoundException(id);
    }

    /// <summary>Every post the seed data exposes, in chronological order.</summary>
    public static IReadOnlyList<Post> All { get; } = new[]
    {
        _highlighted,
        _kotlinFreeBindings,
        _facadeGenerator,
        _materialThree,
        _navigation,
        _stateHolders,
    };
}
