# AI Code Generation Pitfalls

These plausible-looking patterns are frequent sources of Compose regressions.

| Pattern | What to watch for |
|---------|------------------|
| Reinventing generated infrastructure | Hand-written `$default` enums, JNI boilerplate, companion accessors, or facades that existing generators should emit. |
| Demoting generated facades | Removing `[ComposeFacade]` because a new shape is unsupported instead of extending and testing the generator. |
| Inspecting the wrong assembly | Concluding an API is unbound after looking only at the small multi-target facade DLL rather than `Xamarin.AndroidX.Compose.*.Android.dll`. |
| Fresh composable lambdas | Constructing `ComposableLambda2/3/4` in `Render` instead of using `ComposableLambdas.Wrap*` or `Instantiate4` according to execution timing. |
| Unstable keys | Using `HashCode`, `Type.GetHashCode`, or string hashes for composition identity instead of repository stable-key helpers. |
| Confident mask arithmetic | Hand-writing `$default` or `$changed` bits without accounting for physical Kotlin parameter positions, receivers, and multiple mask groups. |
| JNI local-reference leaks | Allocating a local reference without cleanup in `finally`, especially on exception paths. |
| Incorrect ownership transfer | Wrapping suspend raw handles or Kotlin singletons with the wrong `JniHandleOwnership`, causing stale or invalid JNI refs. |
| Nullability suppression | Adding postfix `!` rather than enforcing the actual parameter, framework-property, or lifecycle contract. |
| Incomplete public API work | Adding runtime surface without XML docs, `PublicAPI.Unshipped.txt`, generator coverage, gallery coverage, or all relevant call paths. |
| Hard-coded package versions | Putting `Version` on project-level `PackageReference Include` instead of central `Update` entries in `Directory.Build.targets`. |
| Broad exception handling | Returning success-shaped defaults, swallowing cancellation, or hiding JNI, binding, build, and device failures. |
| Over-engineering | New abstractions, wrappers, and compatibility layers that duplicate existing helpers or have no current caller. |
| Docs describe intent, not reality | Comments and XML docs that state desired behavior but disagree with lowering, JNI signatures, defaults, or runtime ownership. |

Before accepting a new helper, search for prior art. Before accepting a
framework or Compose claim, trace the exact target framework, pinned binding,
generated output, or runtime consumer.

