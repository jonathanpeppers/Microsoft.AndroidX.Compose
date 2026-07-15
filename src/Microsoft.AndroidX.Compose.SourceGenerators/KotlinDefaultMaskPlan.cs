using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AndroidX.Compose.SourceGenerators;

internal sealed class KotlinDefaultMaskPlan
{
    readonly DefaultsInfo defaults;
    readonly IReadOnlyList<DefaultArgumentBinding> bindings;

    KotlinDefaultMaskPlan(
        DefaultsInfo defaults,
        IReadOnlyList<DefaultArgumentBinding> bindings)
    {
        this.defaults = defaults;
        this.bindings = bindings;
    }

    public bool IsWide => defaults.Slots.Count > 31;

    public static KotlinDefaultMaskPlan Create(
        DefaultsInfo defaults,
        IReadOnlyList<DefaultArgumentBinding> bindings)
    {
        if (defaults.Slots.Count > 64)
        {
            throw new NotSupportedException(
                $"Kotlin default mask '{defaults.EnumName}' has {defaults.Slots.Count} slots; masks wider than 64 bits are not supported.");
        }

        var kotlinNames = new HashSet<string>(StringComparer.Ordinal);
        var surfacedIndices = new HashSet<int>();
        foreach (var binding in bindings)
        {
            if ((uint)binding.SurfacedParameterIndex >= 64)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bindings),
                    binding.SurfacedParameterIndex,
                    "Surfaced optional-argument indices must fit in the 64-bit omission bitmap.");
            }
            if (defaults.FindByKotlinName(binding.KotlinName) is null)
            {
                throw new InvalidOperationException(
                    $"Kotlin default slot '{binding.KotlinName}' was not found in '{defaults.EnumName}'.");
            }
            if (!kotlinNames.Add(binding.KotlinName))
            {
                throw new InvalidOperationException(
                    $"Kotlin default slot '{binding.KotlinName}' is bound more than once.");
            }
            if (!surfacedIndices.Add(binding.SurfacedParameterIndex))
            {
                throw new InvalidOperationException(
                    $"Surfaced parameter index {binding.SurfacedParameterIndex} is bound more than once.");
            }
        }

        return new KotlinDefaultMaskPlan(defaults, bindings.ToArray());
    }

    public void EmitInitialization(
        StringBuilder sb,
        string indent,
        string omissionVariable,
        string maskVariable)
    {
        sb.Append(indent).Append("var ").Append(maskVariable)
          .Append(" = global::AndroidX.Compose.").Append(defaults.EnumName).AppendLine(".All;");

        foreach (var binding in bindings)
        {
            var slot = defaults.FindByKotlinName(binding.KotlinName);
            if (slot?.EnumMember is not { } enumMember)
                continue;

            ulong omissionBit = 1UL << binding.SurfacedParameterIndex;
            sb.Append(indent).Append("if ((").Append(omissionVariable).Append(" & 0x")
              .Append(omissionBit.ToString("X", CultureInfo.InvariantCulture))
              .Append("UL) == 0) ").Append(maskVariable)
              .Append(" &= ~global::AndroidX.Compose.").Append(defaults.EnumName)
              .Append('.').Append(enumMember).AppendLine(";");
        }

        if (IsWide)
        {
            sb.Append(indent).Append("var (").Append(maskVariable).Append("Mask0, ")
              .Append(maskVariable).Append("Mask1) = ").Append(maskVariable)
              .AppendLine(".Split();");
        }
    }

    public IReadOnlyList<string> ArgumentExpressions(string maskVariable) =>
        IsWide
            ? [maskVariable + "Mask0", maskVariable + "Mask1"]
            : ["(int)" + maskVariable];
}
