using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

/// <summary>
/// Translates MAUI <see cref="Semantics"/> (Hint / Description /
/// HeadingLevel) and <see cref="IView.AutomationId"/> into a Compose
/// <c>Modifier.Semantics { … }</c> + <c>Modifier.TestTag(…)</c> chain.
/// Every Compose-backed handler chains
/// <see cref="ApplySemantics"/> on its outermost modifier so MAUI's
/// accessibility surface reaches TalkBack and the automation harness
/// reaches the underlying composable, even when the leaf's
/// <c>ComposeView</c> platform view is detached (the common path —
/// see <c>docs/maui-backend.md</c>).
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> MAUI's default
/// <c>ViewHandler.ViewMapper["Semantics"] = MapSemantics</c> calls
/// <c>SemanticExtensions.UpdateSemantics(handler.PlatformView, view)</c>
/// on the leaf's <c>PlatformView</c>. For our Compose-folded leaves
/// the platform view is a detached <c>ComposeView</c> on the common
/// path — Compose's attached page <c>ComposeView</c> owns the
/// accessibility tree via its own <c>Modifier.Semantics { }</c>
/// system, which we never populate. Net effect: <c>SemanticProperties.*</c>
/// + <c>AutomationId</c> are silently ignored on Compose-folded leaves.
/// This bridge re-routes them through Compose's modifier system so
/// they land on the right node.</para>
///
/// <para><b>Mapping table</b> (mirrors stock MAUI's
/// <c>SemanticExtensions.UpdateSemantics</c> /
/// <c>UpdateSemanticNodeInfo</c> from MAUI 10.0.20):</para>
/// <list type="table">
///   <listheader>
///     <term>MAUI</term>
///     <description>Compose</description>
///   </listheader>
///   <item>
///     <term><see cref="Semantics.Description"/></term>
///     <description><c>contentDescription = description</c> — primary
///     read by TalkBack, equivalent to MAUI's primary
///     <c>info.ContentDescription</c>.</description>
///   </item>
///   <item>
///     <term><see cref="Semantics.Hint"/></term>
///     <description><c>stateDescription = hint</c> when Description
///     is also set — MAUI's stock pipeline routes Hint to
///     <c>info.HintText</c> on API 26+ which TalkBack reads after
///     ContentDescription. Compose's analog is
///     <c>stateDescription</c>. When only Hint is set (no
///     Description), the bridge promotes Hint to
///     <c>contentDescription</c> so the node is focusable and read by
///     TalkBack at all (a node with only stateDescription doesn't get
///     announced).</description>
///   </item>
///   <item>
///     <term><see cref="Semantics.HeadingLevel"/> ≠
///     <see cref="SemanticHeadingLevel.None"/></term>
///     <description><c>heading()</c> — Compose's Foundation
///     <c>heading()</c> takes no level. Stock MAUI also collapses
///     <c>HeadingLevel</c> to a boolean
///     (<c>ViewCompat.SetAccessibilityHeading(view, true)</c>), so
///     this is a faithful translation.</description>
///   </item>
///   <item>
///     <term><see cref="IView.AutomationId"/></term>
///     <description><c>testTag(automationId)</c> — UI automation
///     (Appium's <c>FindByAutomationId</c>) reads testTag back through
///     Compose's semantics tree.</description>
///   </item>
/// </list>
///
/// <para><b>mergeDescendants = true.</b> Always set on the emitted
/// semantics block. Compound widgets like our
/// <see cref="Microsoft.Maui.Controls.Stepper"/>
/// (<c>Row { IconButton − + IconButton + Text }</c>) or
/// <see cref="Microsoft.Maui.Controls.RadioButton"/>
/// (<c>Row { RadioButton + Text }</c>) need to be announced as a
/// single TalkBack node carrying the Description / Hint, not split
/// across the children's individual semantic nodes. <c>mergeDescendants</c>
/// is Compose's mechanism for "this subtree is one logical node from
/// a screen-reader's perspective" — equivalent to Android's
/// <c>importantForAccessibility="yes"</c> +
/// <c>focusable="true"</c> on a parent ViewGroup, with descendants set
/// to <c>importantForAccessibility="no"</c>.</para>
///
/// <para><b>Recomposition.</b>
/// Mapper writes for <c>Semantics</c> / <c>AutomationId</c> bump the
/// shared view-properties version slot via
/// <see cref="ComposeElementHandler{TVirtualView}.BumpViewPropertiesVersion"/>
/// (wired in <c>RemapForCompose</c>); BuildNode subscribes via
/// <see cref="ComposeElementHandler{TVirtualView}.SubscribeToViewProperties"/>,
/// so a runtime change to <c>SemanticProperties.Description</c>
/// re-runs the modifier chain and re-emits the semantics block with
/// fresh values. Same pattern as
/// <see cref="ModifierBridge.ApplyViewProperties"/>.</para>
///
/// <para><b>Common-case allocation skip.</b> When the view has no
/// Description, no Hint, no HeadingLevel, and no AutomationId,
/// <see cref="ApplySemantics"/> returns the input modifier unchanged
/// (no <c>Modifier.Semantics</c> allocation, no JNI calls). That's
/// the dominant path on a typical screen — most leaves don't carry
/// explicit semantics — so the bridge is cheap to chain
/// unconditionally on every handler.</para>
/// </remarks>
internal static class SemanticsBridge
{
    /// <summary>
    /// Chain <c>Modifier.Semantics { … }</c> + <c>Modifier.TestTag(…)</c>
    /// onto <paramref name="modifier"/> for any non-default MAUI
    /// accessibility / automation property on
    /// <paramref name="view"/>. Returns <paramref name="modifier"/>
    /// unchanged when the view has no Description, Hint, HeadingLevel,
    /// or AutomationId set (no allocation, no JNI calls — common
    /// case).
    /// </summary>
    /// <param name="modifier">Starting modifier; usually the result of
    /// <see cref="ModifierBridge.ApplyViewProperties"/>.</param>
    /// <param name="view">The MAUI virtual view whose
    /// <see cref="Semantics"/> + <see cref="IView.AutomationId"/>
    /// drive the emitted Compose semantics block.</param>
    /// <returns>A modifier with at most one
    /// <c>Modifier.Semantics(mergeDescendants: true) { … }</c> block
    /// and at most one <c>Modifier.TestTag(automationId)</c> appended.
    /// TestTag goes after Semantics so the test tag lives at the same
    /// merged-semantics node TalkBack reads.</returns>
    internal static Modifier ApplySemantics(this Modifier modifier, IView view)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        ArgumentNullException.ThrowIfNull(view);

        var semantics = view.Semantics;
        var description = semantics?.Description;
        var hint = semantics?.Hint;
        var heading = semantics is not null
            && semantics.HeadingLevel != SemanticHeadingLevel.None;
        var automationId = view.AutomationId;

        var hasContent = !string.IsNullOrEmpty(description)
            || !string.IsNullOrEmpty(hint)
            || heading;
        var hasAutomationId = !string.IsNullOrEmpty(automationId);

        if (!hasContent && !hasAutomationId)
            return modifier;

        if (hasContent || hasAutomationId)
        {
            modifier = modifier.Semantics(mergeDescendants: true, s =>
            {
                // Description wins for contentDescription; if absent
                // we promote Hint so the node is at all focusable
                // (a stateDescription-only node isn't announced).
                if (!string.IsNullOrEmpty(description))
                {
                    s.ContentDescription(description);
                    if (!string.IsNullOrEmpty(hint))
                        s.StateDescription(hint);
                }
                else if (!string.IsNullOrEmpty(hint))
                {
                    s.ContentDescription(hint);
                }

                if (heading)
                    s.Heading();

                // Surface Modifier.TestTag(...) through
                // AccessibilityNodeInfo.viewIdResourceName so Appium /
                // UIAutomator's `By.id(automationId)` finds the node.
                // Compose doesn't expose testTag to platform a11y by
                // default; this flag flips it on for the subtree.
                if (hasAutomationId)
                    s.TestTagsAsResourceId(true);
            });
        }

        if (hasAutomationId)
            modifier = modifier.TestTag(automationId!);

        return modifier;
    }
}
