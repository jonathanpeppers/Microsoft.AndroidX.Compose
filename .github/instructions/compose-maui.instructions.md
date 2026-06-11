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
   `MutableState<T>` slot. Keep slot types **primitive**: `string`,
   `bool`, `int`, `long`, `float`, packed color (`long?`),
   `Java.Lang.Object` subclasses. **Never a user-defined .NET enum**
   like `Microsoft.Maui.TextAlignment` — `MutableState<T>`'s `ToJava`
   switch doesn't unwrap them to their underlying primitive and the
   ctor throws `NotSupportedException` at field-initializer time,
   crashing the handler before any frame paints. Store the underlying
   `int` (`MutableState<int>`) and cast back inside `SetContent`. See
   `LabelHandler._hTextAlign` for the canonical workaround. Convert
   packed values (`Microsoft.Maui.Graphics.Color` → packed `long?`
   via `ColorMapping.ToPackedLong`) in the `MapXxx` callback, not in
   the composition.
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
   src/Microsoft.AndroidX.Compose.Maui.Sample -t:Install`. Resolve the
   launchable activity (the JCW class name is a per-assembly hash that
   changes — never hardcode it):

   ```pwsh
   adb shell cmd package resolve-activity --brief net.compose.maui.sample
   # → net.compose.maui.sample/crc6XXXXXXXX.MainActivity
   adb shell am start -n net.compose.maui.sample/<resolved activity>
   ```

   Confirm `androidx.compose.ui.platform.ComposeView` nodes for your
   new control in `uiautomator dump`, then exercise it (tap, type,
   drag).

### Event forwarding

If the MAUI interface defines events the host expects in a specific
order (e.g. `IButton` fires `Pressed → Clicked → Released` on touch),
**forward all of them** from your single Compose callback. Compose
typically surfaces only the logical event; behaviors and gesture
recognizers subscribed to the others break silently if you skip them.
See `ButtonHandler.OnClicked` for the pattern.

### Mapper rules learned the hard way

These came out of getting Label / Button / Entry / Image to render
identically to the stock template. Apply on every new handler.

- **Don't replicate `MapBackground` with a no-op — map it onto the
  composable's own colour slot.** Compose Material 3 widgets paint
  their own pill / card / surface, so a default
  `ViewMapper.MapBackground` painting a `SolidPaint` on the outer
  `ComposeView` produces a wide rectangle behind the smaller M3 pill.
  The first instinct (Phase 1) was to **suppress** the entry with a
  `(h, v) => { }` no-op. That works for "match stock M3 theme" but
  loses the caller's `BackgroundColor=` entirely — a MAUI button with
  `BackgroundColor="Primary"` then renders in M3 primary
  (`#6750A4`) instead of MAUI Primary (`#512BD4`).

  The right pattern is to **route the colour into the composable's
  own slot**. For `Button`, that's `ButtonColors.containerColor` via
  `composer.ButtonColors(containerColor: ...)`. The mapper extracts
  the packed `long?` from a `SolidPaint`; gradients / images / `null`
  leave the slot unset so M3's theme default applies:

  ```csharp
  public static void MapBackground(ButtonHandler handler, IButton button) =>
      handler._containerColor.Value = button.Background is SolidPaint solid
          ? ColorMapping.ToPackedLong(solid.Color)
          : null;
  ```

  Then inside `SetContent(c => ...)`:

  ```csharp
  if (container is not null || content is not null)
      button.Colors = c.ButtonColors(
          containerColor: container,
          contentColor:   content);
  ```

  The `c => ...` signature (not `_ =>`) is important — you need the
  composer for `composer.ButtonColors(...)` to allocate inside the
  current composition. See `ButtonHandler.cs` for the canonical
  pairing. The same pattern extends to any future Compose-skinned
  widget with a `*Colors` slot (`Card`, `Surface`, `TextField`).

- **`MapBackground` and `MapTextColor` come as a pair on coloured
  surfaces.** M3's `contentColorFor(arbitraryColor)` returns
  `Color.Unspecified` when the container colour isn't one of the
  theme's tokens — so a Compose `Text` inside a button with
  `BackgroundColor="#512BD4"` reads transparent and disappears.
  Always map `TextColor` (when `IButton is ITextStyle`) into the
  matching `contentColor` slot:

  ```csharp
  public static void MapTextColor(ButtonHandler handler, IButton button)
  {
      if (button is ITextStyle textStyle)
          handler._contentColor.Value = ColorMapping.ToPackedLong(textStyle.TextColor);
  }
  ```

  Apply to any handler whose composable owns its own surface (any
  `*Colors`-bearing Material 3 widget). Leaves with no intrinsic
  surface (`Label`) don't need it.

- **Map `HorizontalLayoutAlignment` → `Modifier.fillMaxWidth()`** when
  the caller asks to `Fill` (Button, Entry) or `Fill`/`Center`
  (Label). Compose's `Text` only honours `textAlign` when its
  measured width spans the available space, so
  `HorizontalTextAlignment="Center"` on a `Headline`/`SubHeadline`
  `Label` renders left-aligned until the Compose `Text` also fills
  its slot. Same trick for Material 3 `Button` (hugs its content by
  default, would otherwise render as a small pill on the left edge
  for `HorizontalOptions="Fill"`) and `OutlinedTextField` (otherwise
  renders as a tiny pill on the left for an Entry with
  `HorizontalOptions="Fill"`). See `LabelHandler.MapHorizontalLayoutAlignment`,
  `ButtonHandler.MapHorizontalLayoutAlignment`,
  `EntryHandler.MapHorizontalLayoutAlignment`.

### Two-way input — the feedback-loop guard

`Entry`-style handlers wire MAUI → Compose **and** Compose → MAUI.
The MAUI `Text` mapper writes the Compose state from `view.Text`;
the Compose `onValueChange` writes back to `view.Text` so MAUI's
`TextChanged` event and bound `Command` fire. If you naively forward
both directions you get either a feedback loop (Compose write →
MAUI `TextChanged` → MAUI mapper → Compose write → ...) or dropped
keystrokes (Compose doesn't see your write until the next frame, so
the rendered value snaps back to the old state).

The pattern (see `EntryHandler.OnValueChanged`):

```csharp
void OnValueChanged(string newValue)
{
    // 1. Update Compose state synchronously so the rendered value
    //    stays pinned to what the user typed.
    _text.Value = newValue;
    // 2. Update VirtualView.Text. Triggers MAUI's TextChanged event
    //    + property pipeline (data binding, behaviors, validation),
    //    which re-enters MapText with the same string. That's a
    //    no-op on MutableState<string>'s equality check — no loop.
    if (VirtualView is { } entry)
        entry.Text = newValue;
}
```

No explicit `_suppressMauiWrite` flag needed — the `MutableState<T>`
equality check breaks the cycle. **Don't use `entry.SetValueFromRenderer(...)`**;
that's internal and bypasses the equality short-circuit. The same
pattern works for any property pair where MAUI and Compose both
own the read side (`Slider.Value`, `Switch.IsToggled`).

### `IImage` / image source resolution (`ImageHandler` pattern)

Hybrid pipeline. Reuse stock dotnet/maui plumbing wherever it fits;
fork only for the per-density-bucket / vector-drawable fast path.

**Fast path — `IFileImageSource` resolved to a packaged drawable.**
Use the public `Context.GetDrawableId(file)` from
`Microsoft.Maui.Platform.ContextExtensions` (it lower-cases the file
name and asks `Resources.GetIdentifier(name, "drawable", PackageName)`
— same lookup `FileImageSourceService` does). If `> 0`, push it into
a `MutableState<int?>` and render via the source-gen `Image(int)`
ctor, which calls `painterResource(id, composer)` — preserves vector
drawables and density buckets.

```csharp
if (src is IFileImageSource file &&
    handler.Context.GetDrawableId(file.File ?? string.Empty) is var id && id > 0)
{
    handler._loader?.Reset();
    handler._painter.Value = null;
    handler._drawableResourceId.Value = id;
    return;
}
```

**General path — every other source.** `UriImageSource`,
`StreamImageSource`, `FontImageSource`, plus files that aren't
packaged as drawable resources, all go through MAUI's
`ImageSourcePartLoader(IImageSourcePartSetter)`. Because the platform
view isn't an `ImageView`, MAUI's `ImageSourcePartExtensions.UpdateSourceAsync`
takes the `GetDrawableAsync(...)` branch and invokes
`setImage(drawable)` on our setter. The setter wraps the `Drawable`
as a Compose `BitmapPainter` and writes it into a second
`MutableState<Painter?>` slot. The `SetContent` lambda prefers the
painter slot over the drawable-id slot, so a freshly-loaded URI
image immediately replaces a stale fast-path render.

`Loader` is lazy — handlers that only ever see file sources never
allocate the setter at all. The setter holds a `WeakReference<>` back
to the handler so a stale continuation can't root a disconnected
handler.

**`Drawable` → Compose `Painter` conversion.** Zero-copy wrap of
`BitmapDrawable.Bitmap` when present; rasterize once at intrinsic
size (fallback 1×1) for vector / layer-list / font glyph drawables.
Pack `IntSize` as `((long)w << 32) | (uint)h` (matches Compose's
`packInts` lowering for the `@JvmInline value class`). Pass
`IntOffset.Zero` as `0L` and `FilterQuality.Low` as `1`.

```csharp
var imageBitmap = AndroidImageBitmap_androidKt.AsImageBitmap(bitmap);
var srcSize = ((long)bitmap.Width << 32) | (uint)bitmap.Height;
return BitmapPainterKt.BitmapPainter(
    image: imageBitmap, srcOffset: 0L, srcSize: srcSize, filterQuality: 1);
```

**Async-void fire-and-forget.** `Microsoft.Maui.TaskExtensions.FireAndForget`
is **internal**; replicate the same shape inline (await,
swallow + log on exception) as a local async-void function inside
`MapSource`. The inner `UpdateSourceAsync` already catches
`Exception` and routes failure to `setImage(null)`, so the catch
here is defence-in-depth — it primarily covers
`ObjectDisposedException` from a disconnected handler.

```csharp
public static void MapSource(ImageHandler handler, MauiIImage image)
{
    // ...fast-path branches above...
    handler._drawableResourceId.Value = null;
    StartPipelineLoad();

    async void StartPipelineLoad()
    {
        try { await handler.Loader.UpdateImageSourceAsync().ConfigureAwait(false); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ImageHandler] image source load failed: {ex.Message}");
        }
    }
}
```

**Cancellation.** `_loader?.Reset()` calls `BeginLoad()` +
`CompleteLoad(null)` under the hood, cancelling the prior token. Do
it on `DisconnectHandler`, on an empty source, and on a successful
fast-path resolve so a stale URI continuation can't write into the
painter slot. The prior task's catch block swallows
`OperationCanceledException` *without* calling `setImage(null)`, so
rapid `MapSource` changes don't flash blank — the old painter stays
until the new one loads.

**Don't add a 1-arg `Image(int)` overload by hand** — `Image.cs`
already declares both `Image(int)` and `Image(Painter)` stub ctors
that delegate to the source-gen `Image(int, string?)` /
`Image(Painter, string?)` ctors. Both are part of the public surface.

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
