# Resource-backed fonts

Place a licensed TTF, OTF, or supported font XML resource under your Android
app's `Resources/font` directory, then describe each face using its actual
weight and style:

```csharp
var family = FontFamily.FromFonts(
    Font.Resource(Resource.Font.karla_regular),
    Font.Resource(Resource.Font.karla_bold, FontWeight.Bold));

var text = new Text("Hello Karla")
{
    FontFamily = family,
    FontWeight = FontWeight.Bold,
};

var typography = MaterialTheme.BuildTypography(
    bodyLarge: new TextStyle { FontFamily = family, FontSize = 16 });
```

`Font.Resource` returns the official bound `AndroidX.Compose.UI.Text.Font.IFont`,
not another wrapper for an already-bound type. `FontFamily.FromFonts` accepts
those descriptors (including descriptors created directly through the binding)
and returns the existing facade type accepted by `Text`, `AnnotatedText`,
`TextStyle`, and theme typography. Existing built-in families are unchanged.
The runtime companion `Xamarin.AndroidX.Compose.UI.Text.Android` 1.11.3.1
exposes both `FontKt.Font` and `FontFamilyKt.FontFamily`; neither needs new JNI.

The default loading strategy is `Blocking`, appropriate for bundled fonts.
`OptionalLocal` allows fallback if a local font is unavailable; `Async` uses a
fallback while loading and may reflow text. These are Compose's supported
strategies, not a new font downloader. Resource IDs must be positive and
strategies must be defined. Resource existence, XML support and decoding errors
are resolved by Android at load time. Weight/style metadata selects a face; it
does not turn a regular font file into a bold or italic file.

## Lifetime

Create families once (for example with `composer.Remember`) rather than every
render. `FromFonts` snapshots its array and never disposes inputs. The Kotlin
family retains the Java font objects, so callers may dispose their descriptor
peers after construction. The family has an independent JNI reference; bound
conversions can be shared and are not disposed internally. Keep the family
alive while passing it to facade consumers. Dispose custom families only when
those consumers are finished; do not dispose cached built-in family/weight/style
singletons. Bound text styles retain the underlying Java family independently.

The Gallery's `text-resource-fonts` demo exercises regular, bold, genuine
italic, optional-local and async loading, both palettes, and repeated managed/
Java GC. Device tests compare resolved glyph rasters with the exact packaged
resources (with synthesis disabled), verify snapshot/disposal behavior, and
check stable Compose output through repeated collection and recomposition.
Font sources, copyright, weights and hashes ship in the apps' `Assets` folders
alongside their OFL licenses.
