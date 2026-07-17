---
applyTo: "eng/Versions.props,src/Microsoft.AndroidX.Compose.Templates/templates/android-compose/.template.config/template.json"
---

# Package version updates

When changing the package version:

1. Update `ComposePackageVersionPrefix` in `eng/Versions.props`.
2. Update the `composeVersion` default in the Android Compose template's
   `.template.config/template.json` to the matching floating beta version.

Keep the prerelease label in `eng/Versions.props` and the template default
aligned.
