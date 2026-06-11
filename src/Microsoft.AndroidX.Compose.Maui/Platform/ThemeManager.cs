using Android.Content.Res;
using AndroidX.Compose;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

/// <summary>
/// Process-wide bridge from MAUI's
/// <see cref="MauiApplication.RequestedTheme"/> /
/// <see cref="MauiApplication.UserAppTheme"/> to a Compose-observable
/// <see cref="MutableState{T}"/> of <see cref="bool"/>. Every page's
/// <see cref="Handlers.PageHandler"/> reads <see cref="IsDark"/>
/// inside its <c>SetContent</c> lambda, so flipping the MAUI theme
/// (system-driven or app-driven) recomposes every Compose-backed page
/// against the matching Material 3 colour scheme.
/// </summary>
/// <remarks>
/// <para>Registered as a DI singleton by
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>;
/// resolved by <see cref="Handlers.PageHandler"/> via
/// <c>MauiContext.Services</c>. Lifetime matches the running
/// <see cref="MauiApplication"/>.</para>
///
/// <para><b>Why a separate state, not just <c>IsSystemInDarkTheme</c>?</b>
/// Compose's <c>isSystemInDarkTheme()</c> reads
/// <c>LocalConfiguration.current.uiMode</c>, which reflects the
/// activity's resolved configuration. MAUI's
/// <see cref="MauiApplication.UserAppTheme"/> is an <em>in-app</em>
/// override that flips MAUI's <see cref="MauiApplication.RequestedTheme"/>
/// without necessarily triggering an Android configuration change —
/// so a Compose composition that only looks at
/// <c>LocalConfiguration</c> would miss it. Routing through MAUI's
/// own <see cref="MauiApplication.RequestedThemeChanged"/> event keeps
/// Compose in sync with whatever theme MAUI itself thinks is active.
/// </para>
///
/// <para><b>Resolution order</b> (matches MAUI's
/// <c>Application.RequestedTheme</c> getter):</para>
/// <list type="number">
///   <item><description>If
///   <see cref="MauiApplication.UserAppTheme"/> is
///   <see cref="AppTheme.Light"/> or
///   <see cref="AppTheme.Dark"/>, that wins.</description></item>
///   <item><description>Otherwise we read
///   <see cref="MauiApplication.RequestedTheme"/> (which falls through
///   to the platform value).</description></item>
///   <item><description>If both come back as
///   <see cref="AppTheme.Unspecified"/> (rare — usually the platform
///   resolves it), fall back to Android's
///   <c>Configuration.UiMode &amp; NightMask</c>.</description></item>
/// </list>
/// </remarks>
public sealed class ThemeManager : IDisposable
{
    /// <summary>
    /// Compose-observable dark-theme flag. Read this inside a
    /// <c>SetContent</c> lambda (or any composable scope) so the
    /// snapshot system re-runs the lambda when the theme flips.
    /// </summary>
    public MutableState<bool> IsDark { get; } = new(false);

    /// <summary>
    /// Construct the manager. Subscribes to
    /// <see cref="MauiApplication.RequestedThemeChanged"/> and seeds
    /// <see cref="IsDark"/> with the current resolved theme. Safe to
    /// call before <see cref="MauiApplication.Current"/> is set —
    /// subscription is deferred until the first
    /// <see cref="Refresh"/>.
    /// </summary>
    public ThemeManager() => Refresh();

    MauiApplication? _subscribed;

    /// <summary>
    /// Re-resolve the active theme and republish it on
    /// <see cref="IsDark"/>. Subscribes to
    /// <see cref="MauiApplication.RequestedThemeChanged"/> the first
    /// time <see cref="MauiApplication.Current"/> becomes available.
    /// Idempotent.
    /// </summary>
    public void Refresh()
    {
        var app = MauiApplication.Current;
        if (app is not null && _subscribed != app)
        {
            if (_subscribed is not null)
                _subscribed.RequestedThemeChanged -= OnRequestedThemeChanged;

            app.RequestedThemeChanged += OnRequestedThemeChanged;
            _subscribed = app;
        }

        IsDark.Value = ResolveIsDark(app);
    }

    void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        IsDark.Value = e.RequestedTheme switch
        {
            AppTheme.Dark   => true,
            AppTheme.Light  => false,
            _               => ResolvePlatformDark(),
        };

    static bool ResolveIsDark(MauiApplication? app)
    {
        // UserAppTheme takes precedence over the system value — the
        // app explicitly asked for a theme, honour it. (MAUI's own
        // RequestedTheme getter encodes this rule, but reading
        // UserAppTheme up front means we don't need to inspect both
        // properties when the user override is set.)
        if (app is not null)
        {
            switch (app.UserAppTheme)
            {
                case AppTheme.Dark:  return true;
                case AppTheme.Light: return false;
            }

            switch (app.RequestedTheme)
            {
                case AppTheme.Dark:  return true;
                case AppTheme.Light: return false;
            }
        }

        return ResolvePlatformDark();
    }

    static bool ResolvePlatformDark()
    {
        // Last-resort: ask the Android Configuration directly. Used
        // when MAUI hasn't resolved a theme yet (typical only during
        // very early startup before the first window is shown).
        var ctx = global::Android.App.Application.Context;
        var mode = ctx?.Resources?.Configuration?.UiMode ?? UiMode.NightUndefined;
        return (mode & UiMode.NightMask) == UiMode.NightYes;
    }

    /// <summary>Detach from
    /// <see cref="MauiApplication.RequestedThemeChanged"/>. Called
    /// when the DI container disposes the singleton (i.e. when the
    /// app shuts down).</summary>
    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.RequestedThemeChanged -= OnRequestedThemeChanged;
            _subscribed = null;
        }
    }
}
