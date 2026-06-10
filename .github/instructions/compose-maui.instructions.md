---
applyTo: "src/Microsoft.AndroidX.Compose.Maui/**,src/Microsoft.AndroidX.Compose.Maui.Sample/**"
---

# Microsoft.AndroidX.Compose.Maui — agent instructions

This project re-skins .NET MAUI's Android handlers with Jetpack Compose
via `Microsoft.AndroidX.Compose`. Consumers opt in with one call:

```csharp
MauiApp.CreateBuilder()
    .UseMauiApp<App>()
    .UseAndroidXCompose();          // ← our extension, must be after UseMauiApp
```

`UseAndroidXCompose()` calls `ConfigureMauiHandlers(...)` to overwrite
MAUI's stock handlers in the DI-backed handler registry. **Last
`AddHandler<TVirtualView, THandler>()` per virtual-view type wins**, so
ordering matters.

See `docs/maui-backend.md` for the full multi-phase plan. This file is
the rule set for adding new Compose-backed handlers.

## Layout

- `Hosting/AppHostBuilderExtensions.cs` — the public
  `UseAndroidXCompose()` extension. Add new handler registrations here.
- `Handlers/` — one handler per file, named `<MauiType>Handler.cs`.
  `<MauiType>` is the **MAUI cross-platform virtual-view name** (e.g.
  `Label`, `Button`, `Slider`), not the platform-view name.

## Pinned package versions (read first if you touch the csproj)

`Microsoft.AndroidX.Compose 1.11.x` pins
`Xamarin.AndroidX.Core` to `1.18.0` via `include="All"`.

`Microsoft.Maui.Controls` is intentionally pinned to **10.0.20** in
`Directory.Build.targets`. MAUI 10.0.70 added a call to
`AndroidX.Core.View.Accessibility.AccessibilityNodeInfoCompat.set_Checked(bool)`
in `SemanticExtensions.UpdateSemanticNodeInfo`, but that setter was
removed in `Xamarin.AndroidX.Core 1.18.0` in favor of a `CheckedState`
enum. The result is a runtime `MissingMethodException` on every view
that publishes accessibility info. **Do not bump MAUI past 10.0.20**
until a MAUI release ships that's compiled against
`Xamarin.AndroidX.Core 1.18+`.

The csproj also pins **six** AndroidX parent packages to versions that
align Compose 1.11.x's chain with MAUI 10.0.20's transitive demands
(`Lifecycle.LiveData`, `Lifecycle.LiveData.Core.Ktx`,
`Navigation.Fragment`, `Navigation.UI`, `Fragment.Ktx`,
`Emoji2.ViewsHelper`). Don't trim them — they're load-bearing for
NU1107/NU1608.

## Canonical handler shape

Every handler follows the same skeleton. Concrete examples in
`Handlers/LabelHandler.cs` and `Handlers/ButtonHandler.cs`.

```csharp
public partial class FooHandler : ViewHandler<IFoo, ComposeView>
{
    public static IPropertyMapper<IFoo, FooHandler> Mapper =
        new PropertyMapper<IFoo, FooHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IText.Text)] = MapText,
            // … other properties
        };

    public static CommandMapper<IFoo, FooHandler> CommandMapper =
        new(ViewCommandMapper);

    // One MutableState<T> per Compose slot the composition reads.
    readonly MutableState<string> _text = new(string.Empty);

    public FooHandler() : base(Mapper, CommandMapper) { }

    public FooHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    protected override ComposeView CreatePlatformView()
    {
        var view = new ComposeView(Context);
        view.SetContent(_ => new Foo(/* read _text.Value etc. */));
        return view;
    }

    protected override void DisconnectHandler(ComposeView platformView)
    {
        platformView.DisposeComposition();
        base.DisconnectHandler(platformView);
    }

    public static void MapText(FooHandler handler, IFoo foo) =>
        handler._text.Value = foo.Text ?? string.Empty;
}
```

### Rules the skeleton encodes

1. **Type the mapper against the concrete handler, not the interface.**
   `PropertyMapper<TVirtualView, TViewHandler>.UpdateProperty` casts the
   handler arg of every mapper callback to `TViewHandler`. If you type
   the mapper as `IPropertyMapper<IFoo, IFooHandler>` and don't
   implement `IFooHandler`, the **first property mapping crashes with
   `InvalidCastException`** at attach time. Use
   `IPropertyMapper<IFoo, FooHandler>` and the cast lands on the
   concrete type for free. (This is the WPF backend's pattern.)

2. **One `MutableState<T>` per Compose-readable slot.** The composition
   captured by `SetContent` reads slots; mapper callbacks write them.
   Compose's snapshot system schedules a recomposition on the next
   frame — no manual invalidation needed.

3. **`ComposeView` is the platform view for every handler.**
   `ViewHandler<TVirtualView, Android.Views.View>` already implements
   `PlatformArrange(Rect)` (calls `View.Layout`) and
   `GetDesiredSize(double, double)` (calls `View.Measure`) on Android
   for any `TPlatform : Android.Views.View`. `ComposeView` IS an
   Android `View`, so no override needed.

4. **Always `DisposeComposition()` in `DisconnectHandler`.** Compose
   holds onto the composer until the host view is detached; without
   explicit disposal the composer leaks and recomposes after the MAUI
   element is gone. Call `base.DisconnectHandler(platformView)` after.

5. **Inherit from `ViewHandler.ViewMapper` / `ViewCommandMapper`** so
   our mapper picks up the standard view-level mappings
   (Background, Opacity, IsEnabled, etc.) without rewriting them.

6. **`PropertyMapper` auto-runs every entry once at attach.** Initial
   values flow through your `MapXxx` callbacks for free — you don't
   have to read `VirtualView` in `CreatePlatformView`.

7. **Constructors come in pairs.** Parameterless for DI, then a
   `(IPropertyMapper?, CommandMapper?)` overload for consumers who
   want to extend the mapper. Standard MAUI handler convention.

### Adding a new handler

1. Create `Handlers/<MauiType>Handler.cs` following the skeleton.
2. Map each MAUI interface property you care about to a
   `MutableState<T>` slot. Keep slot types primitive (string, int,
   long, packed color, bool). Convert packed values
   (`Microsoft.Maui.Graphics.Color` → packed sRGB ulong) in the
   `MapXxx` callback, not in the composition.
3. The composition reads slots and builds the Compose widget tree.
   Use `Microsoft.AndroidX.Compose`'s existing facades (`Text`,
   `Button`, `Column`, …). **Do not** spin up a fresh `IComposer` —
   `SetContent` provides one and `MutableState<T>` reads register the
   right slot-table dependencies.
4. Register the handler in `Hosting/AppHostBuilderExtensions.cs`:

   ```csharp
   handlers.AddHandler<MauiFoo, FooHandler>();
   ```

   where `MauiFoo` is the cross-platform MAUI control
   (e.g. `Microsoft.Maui.Controls.Slider`).
5. Add a Phase entry in `docs/maui-backend.md`.
6. **Add a demo in `Microsoft.AndroidX.Compose.Maui.Sample`.** Drop the
   new control on `MainPage.xaml` so manual smoke-test deploys
   exercise it.
7. Build + deploy with `dotnet build
   src/Microsoft.AndroidX.Compose.Maui.Sample -t:Install`; launch with
   `adb shell am start -n
   net.compose.maui.sample/crc645d633bf51a3beaf9.MainActivity`. Confirm
   `androidx.compose.ui.platform.ComposeView` nodes for your new
   control in `uiautomator dump`, then exercise it (tap, type, drag).

### Event forwarding

If the MAUI interface defines events the host expects in a specific
order (e.g. `IButton` fires `Pressed → Clicked → Released` on touch),
**forward all of them** from your single Compose callback. Compose
typically surfaces only the logical event; behaviors and gesture
recognizers subscribed to the others break silently if you skip them.
See `ButtonHandler.OnClicked` for the pattern.

### Color conversion convention

`Microsoft.Maui.Graphics.Color` carries four floats in `[0, 1]`.
`AndroidX.Compose.Color` wraps Compose's packed `ULong` sRGB layout.
**Always go through `ColorMapping` — never hand-pack the bytes.**
`AndroidX.Compose.Color`'s `(byte alpha, byte red, byte green, byte blue)`
ctor already owns the ARGB layout, and `ColorMapping.ToByte` does
round-to-nearest (`* 255f + 0.5f`) so we don't drop one quantization
level on every channel.

```csharp
// In your mapper:
handler._color.Value = ColorMapping.ToPackedLong(label.TextColor);
```

`ToPackedLong` returns `long?` (nullable to model "no color set" so a
mapper can clear the slot by passing `null`). Store it as a
`MutableState<long?>` field on the handler and reconstitute
`new AndroidX.Compose.Color(packed.Value)` inside the composition.
See `LabelHandler.MapTextColor` for the canonical wiring and
`ColorMapping.cs` for the helper itself. If you need the unwrapped
`AndroidX.Compose.Color` (e.g. building a `ButtonColors` slot), use
`ColorMapping.ToCompose(mauiColor)` directly.

### What we don't override

Stock MAUI handlers already work for layout/container types
(`ContentPage`, `VerticalStackLayout`, `Grid`, `Border`, `ScrollView`)
because they target `Android.Views.ViewGroup` subclasses that hosting
`ComposeView` children just fine. **Only override leaves** — the
controls a user actually sees as "this is a button / label / slider /
…". Same goes for `ApplicationHandler`, `WindowHandler`, `PageHandler`,
`LayoutHandler`. Don't add them to `UseAndroidXCompose()` unless you
have a reason that requires Compose at the container layer.

### Activity

`MauiAppCompatActivity` extends `ComponentActivity` (via
`FragmentActivity`), so it already satisfies `ComposeView`'s host
requirements. **Don't ship a custom `MauiComposeActivity`** — it
duplicates the stock behavior without adding anything.

## Anti-patterns

- ❌ `IPropertyMapper<IFoo, IFooHandler>` when `FooHandler` doesn't
  implement `IFooHandler` → `InvalidCastException` at first map.
- ❌ Constructing a new `ComposeView` inside a mapper callback →
  defeats recomposition; the view is the platform view, created once
  per handler attach.
- ❌ Reading `VirtualView` inside the `SetContent` lambda → mixes the
  composition's snapshot system with MAUI's mutable element graph.
  Push state into `MutableState<T>` slots first.
- ❌ Skipping `DisposeComposition()` in `DisconnectHandler` → leak.
- ❌ Calling `UseAndroidXCompose()` **before** `UseMauiApp<TApp>()` →
  stock handlers register after ours and win. Order is enforced by
  `Last AddHandler wins` in MAUI's `MauiServiceCollection`.

## Style

- One handler class per file, file name = class name.
- File-scoped namespaces.
- XML docs on every public type and member.
- `ArgumentNullException.ThrowIfNull(x)` for parameter null checks.
- Don't add `// ---- Section ----` banners — one file per type makes
  them noise.
