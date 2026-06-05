using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ComposeNet.SourceGenerators;

/// <summary>
/// Parsed view of a declarative <c>[assembly: ComposeDefaults("EnumName",
/// "p0", "!p1", ...)]</c> attribute. Each slot maps a Kotlin parameter
/// name to its bit position in the <c>$default</c> bitmask; <c>!</c>-
/// prefixed names are consumed by the attribute (they occupy a bit) but
/// are omitted from the generated enum. Shared by
/// <see cref="ComposeBridgeGenerator"/> and
/// <see cref="ComposeFacadeGenerator"/>.
/// </summary>
internal readonly struct DefaultsInfo
{
    public string EnumName { get; }
    public IReadOnlyList<DefaultsSlot> Slots { get; }

    public DefaultsInfo(string enumName, IReadOnlyList<DefaultsSlot> slots)
    {
        EnumName = enumName;
        Slots = slots;
    }

    public DefaultsSlot? FindByKotlinName(string kotlinName)
    {
        foreach (var s in Slots)
            if (string.Equals(s.KotlinName, kotlinName, StringComparison.Ordinal))
                return s;
        return null;
    }

    /// <summary>
    /// Look up a slot whose generated <see cref="DefaultsSlot.EnumMember"/>
    /// matches <paramref name="propertyName"/> (case-insensitive). Used
    /// when a facade has renamed a slot via <c>[Slot]</c>: the property
    /// name is the user-facing name, but the underlying enum member is
    /// still the Kotlin name PascalCased.
    /// </summary>
    public DefaultsSlot? FindByEnumMember(string propertyName)
    {
        foreach (var s in Slots)
            if (s.EnumMember is { } em &&
                string.Equals(em, propertyName, StringComparison.OrdinalIgnoreCase))
                return s;
        return null;
    }

    public static DefaultsInfo? TryRead(Compilation compilation, INamedTypeSymbol declarativeAttr, string enumName)
    {
        var match = compilation.Assembly.GetAttributes()
            .FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, declarativeAttr) &&
                a.ConstructorArguments.Length >= 1 &&
                a.ConstructorArguments[0].Value is string s &&
                s == enumName);
        if (match is null) return null;

        var arr = match.ConstructorArguments[1];
        var names = arr.Kind == TypedConstantKind.Array
            ? arr.Values.Select(v => v.Value as string ?? string.Empty).ToArray()
            : Array.Empty<string>();

        var slots = new List<DefaultsSlot>(names.Length);
        for (int i = 0; i < names.Length; i++)
        {
            var raw = names[i];
            bool suppressed = raw.StartsWith("!", StringComparison.Ordinal);
            var kotlin = suppressed ? raw.Substring(1) : raw;
            slots.Add(new DefaultsSlot(
                kotlinName: kotlin,
                bit: i,
                enumMember: suppressed ? null : Pascal(kotlin)));
        }
        return new DefaultsInfo(enumName, slots);
    }

    static string Pascal(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}

internal readonly struct DefaultsSlot
{
    public string KotlinName { get; }
    public int Bit { get; }
    /// <summary>Null when the entry was <c>!</c>-suppressed.</summary>
    public string? EnumMember { get; }

    public DefaultsSlot(string kotlinName, int bit, string? enumMember)
    {
        KotlinName = kotlinName;
        Bit = bit;
        EnumMember = enumMember;
    }
}
