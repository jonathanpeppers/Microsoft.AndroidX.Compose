---
applyTo: "src/Microsoft.AndroidX.Compose/**/*.cs,src/Microsoft.AndroidX.Compose.Templates/templates/**/*.cs,samples/**/*.cs"
---

# Canonical .NET Android APIs

Use `OperatingSystem.IsAndroidVersionAtLeast(apiLevel)` for Android API-level
checks. Prefer it over comparing `Android.OS.Build.VERSION.SdkInt` with a
`BuildVersionCodes` value so .NET platform compatibility analysis can reason
about the guarded code.

Add a brief comment when the API level alone does not explain the feature
being detected, such as API 31 enabling Material You dynamic colors.
