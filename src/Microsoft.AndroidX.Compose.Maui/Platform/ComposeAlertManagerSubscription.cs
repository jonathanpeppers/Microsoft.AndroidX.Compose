using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Android.App;
using Android.Views;
using AndroidX.Compose;
using AndroidX.Compose.UI.Platform;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Platform;
using ComposeAlertDialog       = AndroidX.Compose.AlertDialog;
using ComposeColor             = AndroidX.Compose.Color;
using ComposeColumn            = AndroidX.Compose.Column;
using ComposeListItem          = AndroidX.Compose.ListItem;
using ComposeMaterialTheme     = AndroidX.Compose.MaterialTheme;
using ComposeModalBottomSheet  = AndroidX.Compose.ModalBottomSheet;
using ComposeOutlinedTextField = AndroidX.Compose.OutlinedTextField;
using ComposeText              = AndroidX.Compose.Text;
using ComposeTextButton        = AndroidX.Compose.TextButton;
using MauiPage                 = Microsoft.Maui.Controls.Page;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

/// <summary>
/// MAUI <c>IAlertManagerSubscription</c> implementation that renders
/// <c>Page.DisplayAlert</c> / <c>DisplayActionSheet</c> /
/// <c>DisplayPromptAsync</c> through Material 3 Compose composables
/// (<see cref="ComposeAlertDialog"/> / <see cref="ComposeModalBottomSheet"/>)
/// instead of MAUI's stock AppCompat dialogs.
/// </summary>
/// <remarks>
/// <para>
/// MAUI's per-window <c>AlertManager.Subscribe()</c> calls
/// <c>context.Services.GetService&lt;IAlertManagerSubscription&gt;()</c>
/// <em>before</em> falling back to its built-in
/// <c>AlertRequestHelper</c>. So registering this proxy in the host's
/// service collection is enough for MAUI to route every Display* call
/// here. We never replace <c>AlertManager</c> wholesale via reflection.
/// </para>
/// <para>
/// <c>IAlertManagerSubscription</c> is <em>internal</em> to
/// <c>Microsoft.Maui.Controls</c>, so we mirror the pattern used by
/// <c>maui-labs</c>'s <c>WPFAlertManagerSubscription</c>: build a
/// <see cref="DispatchProxy"/> that implements it via reflection, then
/// register the proxy under the resolved <see cref="System.Type"/>.
/// </para>
/// <para>
/// MAUI version pinning hazard: the interface lives at
/// <c>Microsoft.Maui.Controls.Platform.AlertManager+IAlertManagerSubscription</c>
/// in MAUI 10.0.20 (and is nested-private in 10.0.x). If a future MAUI
/// version moves or renames it, <see cref="ResolveSubscriptionInterface"/>
/// needs to grow another fallback. We log + no-op when resolution
/// fails so the host app keeps booting.
/// </para>
/// <para>
/// Theme alignment: each dialog is rendered into its own transient
/// <see cref="ComposeView"/> attached to the activity's content frame,
/// wrapped in a default <see cref="ComposeMaterialTheme"/>. Until the
/// future <c>ThemeManager</c> ships, the overlay's M3 palette is
/// independent of any per-page theme override the consumer may
/// install — the visual identity matches a stock M3 dialog. Dynamic
/// color (Material You) on API 31+ is enabled by default.
/// </para>
/// <para>
/// Lifecycle: each call attaches a fresh <see cref="ComposeView"/> to
/// <c>android.R.id.content</c>, sets its content, and detaches +
/// disposes the composition once the user resolves the dialog. We
/// hold no strong references to the activity or the originating
/// <see cref="MauiPage"/> — both are reachable only through
/// <c>sender.Handler.MauiContext</c>, so a navigation-away while a
/// dialog is showing simply cancels the awaiting <see cref="Task"/>
/// (the parent activity is destroyed and its content frame torn down,
/// taking the overlay with it).
/// </para>
/// </remarks>
public class ComposeAlertManagerSubscription : DispatchProxy
{
    /// <summary>
    /// Register this subscription as the host's
    /// <c>IAlertManagerSubscription</c>. Called from
    /// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
    /// </summary>
    /// <remarks>
    /// Resolves MAUI's internal interface via reflection, builds a
    /// <see cref="DispatchProxy"/> implementing it, and adds the proxy
    /// as a singleton under the interface type. If interface
    /// resolution fails (unsupported MAUI version), the registration
    /// is silently skipped — MAUI then falls back to its stock
    /// AppCompat dialog.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "MAUI's IAlertManagerSubscription type is preserved by the host (it's the only consumer of this DI registration).")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
        Justification = "We resolve a single MAUI interface by name; trimming may rename it but we no-op on miss.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "DispatchProxy.Create<T,TProxy>() emits IL at runtime; supported on Android (Mono) but not on full NativeAOT — sample uses Mono.")]
    public static void Register(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var ifaceType = ResolveSubscriptionInterface();
        if (ifaceType is null)
        {
            // Logging: cheap System.Diagnostics. Avoid pulling
            // ILogger here — DI resolution happens before the host
            // is built when called from UseAndroidXCompose.
            System.Diagnostics.Debug.WriteLine(
                "[ComposeAlertManagerSubscription] " +
                "Could not resolve IAlertManagerSubscription on " +
                "Microsoft.Maui.Controls; stock AppCompat dialogs " +
                "will be used. (MAUI version drift?)");
            return;
        }

        // DispatchProxy.Create<TInterface, TProxy>() requires both
        // type args at compile time, but TInterface is internal to
        // MAUI. Find the open-generic via reflection and close it.
        // We register a *factory* so the proxy is built lazily — this
        // keeps a DispatchProxy initialization failure (e.g. a stripped
        // System.Reflection.Emit on a trimmed build) from blowing up
        // application startup; instead the missing service falls back
        // to MAUI's stock AppCompat dialog.
        var createGeneric = typeof(DispatchProxy)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DispatchProxy.Create)
                && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 2);
        var create = createGeneric.MakeGenericMethod(
            ifaceType, typeof(ComposeAlertManagerSubscription));

        services.AddSingleton(ifaceType, _ =>
        {
            try
            {
                return create.Invoke(null, null)
                    ?? throw new InvalidOperationException(
                        $"DispatchProxy.Create returned null for " +
                        $"{ifaceType.FullName}.");
            }
            catch (Exception ex)
            {
                var inner = ex is TargetInvocationException tie ? (tie.InnerException ?? tie) : ex;
                global::Android.Util.Log.Error(
                    "ComposeAlertManager",
                    "DispatchProxy.Create failed for " + ifaceType.FullName +
                    "; falling back to stock AppCompat dialogs. Inner: " +
                    inner.GetType().FullName + ": " + inner.Message + "\n" +
                    inner.StackTrace);
                throw;
            }
        });
    }

    /// <summary>
    /// Locate MAUI's <c>IAlertManagerSubscription</c> interface
    /// reflectively. Tries the modern shape first
    /// (nested under <c>AlertManager</c>) then a hypothetical
    /// top-level fallback for forward-compat with future MAUI
    /// reorganizations.
    /// </summary>
    static Type? ResolveSubscriptionInterface()
    {
        var asm = typeof(MauiPage).Assembly;

        // MAUI 10.0.x — interface is nested under AlertManager.
        var alertManager = asm.GetType("Microsoft.Maui.Controls.Platform.AlertManager");
        if (alertManager is not null)
        {
            var nested = alertManager.GetNestedType(
                "IAlertManagerSubscription",
                BindingFlags.Public | BindingFlags.NonPublic);
            if (nested is not null)
                return nested;
        }

        // Forward-compat: the type might surface as top-level if MAUI
        // ever externalises it (mirrors WPFAlertManagerSubscription's
        // dual lookup).
        return asm.GetType("Microsoft.Maui.Controls.Platform.IAlertManagerSubscription");
    }

    /// <summary>
    /// <see cref="DispatchProxy"/> entry point — fan out by method
    /// name to the right handler. <c>OnPageBusy</c> is deprecated in
    /// .NET 10 and intentionally a no-op (the stock alert manager
    /// only used it for the legacy <c>IsBusy</c> flag).
    /// </summary>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null || args is null)
            return null;

        switch (targetMethod.Name)
        {
            case "OnAlertRequested":
                if (args[0] is MauiPage alertPage && args[1] is AlertArguments alertArgs)
                    OnAlertRequested(alertPage, alertArgs);
                return null;

            case "OnPromptRequested":
                if (args[0] is MauiPage promptPage && args[1] is PromptArguments promptArgs)
                    OnPromptRequested(promptPage, promptArgs);
                return null;

            case "OnActionSheetRequested":
                if (args[0] is MauiPage sheetPage && args[1] is ActionSheetArguments sheetArgs)
                    OnActionSheetRequested(sheetPage, sheetArgs);
                return null;

            case "OnPageBusy":
                // Deprecated in .NET 10 — no-op intentionally.
                return null;

            default:
                System.Diagnostics.Debug.WriteLine(
                    $"[ComposeAlertManagerSubscription] " +
                    $"Unhandled IAlertManagerSubscription method " +
                    $"'{targetMethod.Name}'.");
                return null;
        }
    }

    static void OnAlertRequested(MauiPage sender, AlertArguments args)
    {
        var activity = ResolveActivity(sender);
        if (activity is null)
        {
            // Couldn't surface the dialog — complete the awaiter
            // with the cancel value so the caller doesn't hang.
            args.Result.TrySetResult(false);
            return;
        }

        ShowOnUiThread(activity, (overlay, dismiss) =>
        {
            void Confirm() { args.Result.TrySetResult(true);  dismiss(); }
            void Cancel()  { args.Result.TrySetResult(false); dismiss(); }

            // MAUI's `DisplayAlert(title, message, cancel)` overload (no
            // accept text) marks the alert as single-button by passing
            // Accept == null. Render the Cancel label as the only button
            // and route it through the confirm path — the result is
            // ignored by the awaiter (the public API returns Task, not
            // Task<bool>), but `true` is the conventional "user
            // acknowledged" value.
            var singleButton = string.IsNullOrEmpty(args.Accept);

            return new ComposeAlertDialog(onDismissRequest: Cancel)
            {
                Title         = string.IsNullOrEmpty(args.Title)   ? null : new ComposeText(args.Title),
                Text          = string.IsNullOrEmpty(args.Message) ? null : new ComposeText(args.Message),
                ConfirmButton = new ComposeTextButton(onClick: Confirm)
                {
                    new ComposeText(singleButton ? (args.Cancel ?? "OK") : args.Accept!),
                },
                DismissButton = singleButton || string.IsNullOrEmpty(args.Cancel) ? null
                    : new ComposeTextButton(onClick: Cancel)
                    {
                        new ComposeText(args.Cancel),
                    },
            };
        }, onUnattached: () => args.Result.TrySetResult(false));
    }

    static void OnPromptRequested(MauiPage sender, PromptArguments args)
    {
        var activity = ResolveActivity(sender);
        if (activity is null)
        {
            args.Result.TrySetResult(null!);
            return;
        }

        // Hold the user-edited text outside the composition so the
        // confirm callback can read the latest value. MutableState
        // ensures recomposition fires on each keystroke.
        var text = new MutableState<string>(args.InitialValue ?? string.Empty);

        ShowOnUiThread(activity, (overlay, dismiss) =>
        {
            void Confirm() { args.Result.TrySetResult(text.Value); dismiss(); }
            void Cancel()  { args.Result.TrySetResult(null!);      dismiss(); }

            // Compose AlertDialog's "text" slot is a single
            // composable — pack the message + the field in a
            // Column so both render between Title and ConfirmButton.
            var body = new ComposeColumn();
            if (!string.IsNullOrEmpty(args.Message))
                body.Add(new ComposeText(args.Message));

            var field = new ComposeOutlinedTextField(text)
            {
                SingleLine = true,
            };
            if (!string.IsNullOrEmpty(args.Placeholder))
                field.Placeholder = new ComposeText(args.Placeholder);
            body.Add(field);

            return new ComposeAlertDialog(onDismissRequest: Cancel)
            {
                Title         = string.IsNullOrEmpty(args.Title) ? null : new ComposeText(args.Title),
                Text          = body,
                ConfirmButton = new ComposeTextButton(onClick: Confirm)
                {
                    new ComposeText(args.Accept ?? "OK"),
                },
                DismissButton = string.IsNullOrEmpty(args.Cancel) ? null
                    : new ComposeTextButton(onClick: Cancel)
                    {
                        new ComposeText(args.Cancel),
                    },
            };
        }, onUnattached: () => args.Result.TrySetResult(null!));
    }

    static void OnActionSheetRequested(MauiPage sender, ActionSheetArguments args)
    {
        var activity = ResolveActivity(sender);
        if (activity is null)
        {
            args.Result.TrySetResult(args.Cancel ?? string.Empty);
            return;
        }

        // Snapshot the buttons collection — it's `IEnumerable<string>`
        // and we read it from the Compose composition (potentially
        // multiple recompositions).
        var buttons = (args.Buttons ?? Array.Empty<string>()).ToArray();

        ShowOnUiThread(activity, (overlay, dismiss) =>
        {
            void Pick(string label) { args.Result.TrySetResult(label); dismiss(); }

            var sheet = new ComposeModalBottomSheet(onDismissRequest: () =>
            {
                args.Result.TrySetResult(args.Cancel ?? string.Empty);
                dismiss();
            });

            var column = new ComposeColumn();
            if (!string.IsNullOrEmpty(args.Title))
            {
                column.Add(new ComposeListItem
                {
                    Headline = new ComposeText(args.Title),
                });
            }

            foreach (var button in buttons)
            {
                if (string.IsNullOrEmpty(button))
                    continue;
                var labelLocal = button;
                column.Add(new ComposeListItem
                {
                    Modifier = Modifier.Clickable(() => Pick(labelLocal)),
                    Headline = new ComposeText(labelLocal),
                });
            }

            // Destruction button styled red — convention from
            // iOS UIActionSheet but a familiar M3 affordance.
            if (!string.IsNullOrEmpty(args.Destruction))
            {
                var destructLocal = args.Destruction;
                column.Add(new ComposeListItem
                {
                    Modifier = Modifier.Clickable(() => Pick(destructLocal)),
                    Headline = new ComposeText(destructLocal) { Color = ComposeColor.Red },
                });
            }

            if (!string.IsNullOrEmpty(args.Cancel))
            {
                var cancelLocal = args.Cancel;
                column.Add(new ComposeListItem
                {
                    Modifier = Modifier.Clickable(() => Pick(cancelLocal)),
                    Headline = new ComposeText(cancelLocal),
                });
            }

            sheet.Add(column);
            return sheet;
        }, onUnattached: () => args.Result.TrySetResult(args.Cancel ?? string.Empty));
    }

    /// <summary>
    /// Resolve the host <see cref="Activity"/> for a MAUI page.
    /// Returns <see langword="null"/> if the page hasn't been
    /// attached to a handler yet — caller short-circuits with the
    /// cancel value rather than hanging the awaiting Task.
    /// </summary>
    static Activity? ResolveActivity(MauiPage sender)
    {
        var context = sender.Handler?.MauiContext?.Context;
        return context?.GetActivity();
    }

    /// <summary>
    /// Marshal to UI thread, attach a fresh <see cref="ComposeView"/>
    /// to the activity's content frame, and install
    /// <paramref name="contentFactory"/> as the composition.
    /// <paramref name="contentFactory"/> receives the overlay view
    /// and a <c>dismiss</c> action; calling <c>dismiss()</c> detaches
    /// + disposes the composition.
    /// </summary>
    /// <param name="onUnattached">
    /// Called if we couldn't find a content view to attach the
    /// overlay to (e.g. activity in a teardown state). Caller uses
    /// this to fault the awaiting Task with a sensible default.
    /// </param>
    static void ShowOnUiThread(
        Activity activity,
        Func<ComposeView, Action, ComposableNode> contentFactory,
        Action onUnattached)
    {
        activity.RunOnUiThread(() =>
        {
            // android.R.id.content is the FrameLayout the activity's
            // content is hosted under — adding our overlay there
            // keeps it above MAUI's page content but below the
            // status / nav bars (Compose Dialog/BottomSheet manage
            // their own Window so visual stacking is correct
            // regardless).
            var content = activity.FindViewById<ViewGroup>(global::Android.Resource.Id.Content);
            if (content is null)
            {
                onUnattached();
                return;
            }

            var overlay = new ComposeView(activity)
            {
                LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent),
            };
            content.AddView(overlay);

            // Latch so dismiss is idempotent — Compose may invoke
            // the dismiss callback on touch-outside *and* the user
            // can still tap a button before recomposition removes
            // the dialog.
            bool detached = false;
            void Dismiss()
            {
                if (detached) return;
                detached = true;
                // Post the detach so we don't tear down the view
                // while Compose is still inside its event handler.
                activity.RunOnUiThread(() =>
                {
                    try
                    {
                        overlay.DisposeComposition();
                    }
                    catch (Exception ex)
                    {
                        // Composition disposal can throw if the
                        // host window was already destroyed (e.g.
                        // user backed out of the activity). Log
                        // and continue with the view detach.
                        System.Diagnostics.Debug.WriteLine(
                            $"[ComposeAlertManagerSubscription] " +
                            $"DisposeComposition threw: {ex.Message}");
                    }
                    (overlay.Parent as ViewGroup)?.RemoveView(overlay);
                });
            }

            overlay.SetContent(c => new ComposeMaterialTheme
            {
                contentFactory(overlay, Dismiss),
            });
        });
    }
}
