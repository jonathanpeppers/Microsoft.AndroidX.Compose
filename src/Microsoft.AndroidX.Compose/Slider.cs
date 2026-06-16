namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>Slider</c>:
/// <code>
/// new Slider(value: pos.Value, onValueChange: v => pos.Value = v)
/// </code>
/// Calls the rich <c>(Float, (Float) -&gt; Unit, …, thumb)</c> overload —
/// custom <c>track</c> slot and the <c>SliderState</c>-first overload
/// aren't exposed.
///
/// Optional <c>ValueRange</c> property surfaces Kotlin's
/// <c>ClosedFloatingPointRange&lt;Float&gt;</c> — build with
/// <c>Kotlin.Ranges.RangesKt.RangeTo(min, max)</c>. Optional
/// <c>Colors</c> property accepts a
/// <see cref="AndroidX.Compose.Material3.SliderColors"/>; build via
/// <c>composer.SliderColors(...)</c> to override individual color slots
/// without restating the full theme.
///
/// Optional <c>Thumb</c> property replaces the stock 20-dp filled
/// circle with any <see cref="ComposableNode"/>:
/// <code>
/// new Slider(value: pos.Value, onValueChange: v => pos.Value = v)
/// {
///     Thumb = new Image(Resource.Drawable.dotnet_bot) { Modifier = Modifier.Size(40) },
/// }
/// </code>
/// The slot's Kotlin signature is <c>@Composable (SliderState) -&gt; Unit</c>;
/// the C# property ignores the <c>SliderState</c> arg, so a thumb that
/// reacts to drag / focus state isn't expressible from this facade — drop
/// to the binding directly with a hand-written
/// <see cref="ComposableNode"/> when needed.
/// </summary>
public sealed partial class Slider;
