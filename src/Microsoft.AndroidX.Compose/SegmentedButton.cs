using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>SegmentedButton</c> — a radio-button-shaped selector
/// segment. Must be a child of a
/// <see cref="SingleChoiceSegmentedButtonRow"/> or
/// <see cref="MultiChoiceSegmentedButtonRow"/>; the parent publishes its
/// scope receiver via <see cref="RenderContext"/>.
///
/// <para>Use the <c>onClick</c> ctor for the single-choice (radio) flavor
/// and the <c>onCheckedChange</c> ctor for the multi-choice (toggle)
/// flavor — the matching scope must already be on the stack.</para>
///
/// <para>Children are rendered as the <c>label</c> slot (required). Set
/// <see cref="Icon"/> via object-initializer for an optional leading
/// indicator.</para>
/// <code>
/// new SingleChoiceSegmentedButtonRow
/// {
///     new SegmentedButton(selected: tab == 0, onClick: () =&gt; tab.Value = 0) { new Text("Day") },
///     new SegmentedButton(selected: tab == 1, onClick: () =&gt; tab.Value = 1) { new Text("Week") },
/// }
/// </code>
/// </summary>
public sealed class SegmentedButton : ComposableContainer
{
    readonly bool _selected;
    readonly Action? _onClick;
    readonly Action<bool>? _onCheckedChange;

    /// <summary>Single-choice (radio) ctor for <see cref="SingleChoiceSegmentedButtonRow"/>.</summary>
    public SegmentedButton(bool selected, Action onClick)
    {
        _selected = selected;
        _onClick  = onClick;
    }

    /// <summary>Multi-choice (toggle) ctor for <see cref="MultiChoiceSegmentedButtonRow"/>.</summary>
    public SegmentedButton(bool @checked, Action<bool> onCheckedChange)
    {
        _selected        = @checked;
        _onCheckedChange = onCheckedChange;
    }

    /// <summary>Optional leading icon slot.</summary>
    public ComposableNode? Icon { get; set; }

    public override void Render(IComposer composer)
    {
        if (Children.Count == 0)
            throw new InvalidOperationException(
                "SegmentedButton requires at least one child (the label slot has no Kotlin default).");

        var label = ComposableLambdas.Wrap2(composer, c =>
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                c.StartReplaceableGroup(HashCode.Combine(i, child.GetType()));
                try { child.Render(c); }
                finally { c.EndReplaceableGroup(); }
            }
        });
        var iconNode = Icon;
        var icon = iconNode is null ? null : ComposableLambdas.Wrap2(composer, c => iconNode.Render(c));
        // Capture modifier structural key BEFORE BuildModifier consumes
        // _prepended/_appended/_contentPadding.
        var __modifierKey = BuildModifierStructuralKey();
        var modifier = BuildModifier();

        var scope = RenderContext.CurrentScope;
        var count = RenderContext.CurrentRowChildCount;
        var index = RenderContext.CurrentRowChildIndex;
        if (scope == IntPtr.Zero || count == 0)
            throw new InvalidOperationException(
                "SegmentedButton must be rendered inside a SingleChoiceSegmentedButtonRow " +
                "or MultiChoiceSegmentedButtonRow — no row scope/position is on the stack.");

        // Kotlin signature has no default for `shape`, so we must supply
        // one. SegmentedButtonDefaults.itemShape returns the correct
        // start/middle/end rounded-corner shape based on (index, count).
        var shape = ComposeBridges.ItemShape(index, count, composer);

        // Both bridges share a common bit layout for their first 6
        // user params (excluding the scope receiver, which is consumed
        // by Kotlin and contributes Static):
        //   bit 1  = selected/checked (primitive DiffSlot)
        //   bit 4  = onClick/onCheckedChange (RememberAction → Static)
        //   bit 7  = shape (IntPtr DiffSlot — itemShape returns the
        //            same handle for the same (index, count))
        //   bit 10 = label (composableLambda → Static)
        //   bit 13 = modifier (DiffSlot on structural key)
        //   bit 16 = icon (Function2? identity diff)
        if (_onCheckedChange is not null)
        {
            int defaults = (int)MultiChoiceSegmentedButtonDefault.All;
            if (modifier is not null) defaults &= ~(int)MultiChoiceSegmentedButtonDefault.Modifier;
            if (icon     is not null) defaults &= ~(int)MultiChoiceSegmentedButtonDefault.Icon;

            var cb = composer.RememberAction((Java.Lang.Object? arg) =>
            {
                if (arg is not Java.Lang.Boolean jb)
                    throw new InvalidOperationException(
                        $"MultiChoiceSegmentedButton.onCheckedChange expected a Java.Lang.Boolean, got '{arg?.GetType().FullName ?? "null"}'.");
                _onCheckedChange(jb.BooleanValue());
            });

            int __changed = 0;
            __changed |= composer.DiffSlot(_selected, 1);
            __changed |= (int)ChangedBits.Static << 4;
            __changed |= composer.DiffSlot(shape, 7);
            __changed |= (int)ChangedBits.Static << 10;
            __changed |= composer.DiffSlot(__modifierKey, 13);
            __changed |= composer.DiffSlot<object?>(icon, 16);

            ComposeBridges.MultiChoiceSegmentedButton(
                multiChoiceScope: scope,
                @checked:         _selected,
                onCheckedChange:  cb,
                shape:            shape,
                label:            label,
                modifier:         modifier,
                icon:             icon,
                defaults:         defaults,
                composer:         composer,
                _changed:         __changed);
        }
        else
        {
            int defaults = (int)SingleChoiceSegmentedButtonDefault.All;
            if (modifier is not null) defaults &= ~(int)SingleChoiceSegmentedButtonDefault.Modifier;
            if (icon     is not null) defaults &= ~(int)SingleChoiceSegmentedButtonDefault.Icon;

            var click = composer.RememberAction(_onClick
                ?? throw new InvalidOperationException("SegmentedButton single-choice ctor requires non-null onClick."));

            int __changed = 0;
            __changed |= composer.DiffSlot(_selected, 1);
            __changed |= (int)ChangedBits.Static << 4;
            __changed |= composer.DiffSlot(shape, 7);
            __changed |= (int)ChangedBits.Static << 10;
            __changed |= composer.DiffSlot(__modifierKey, 13);
            __changed |= composer.DiffSlot<object?>(icon, 16);

            ComposeBridges.SingleChoiceSegmentedButton(
                singleChoiceScope: scope,
                selected:          _selected,
                onClick:           click,
                shape:             shape,
                label:             label,
                modifier:          modifier,
                icon:              icon,
                defaults:          defaults,
                composer:          composer,
                _changed:          __changed);
        }
    }
}
