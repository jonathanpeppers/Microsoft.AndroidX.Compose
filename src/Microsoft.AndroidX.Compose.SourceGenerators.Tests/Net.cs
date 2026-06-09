using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AndroidX.Compose.SourceGenerators.Tests;

/// <summary>Minimal reference set so the synthetic source compiles.</summary>
internal static class Net
{
    public static class Sdk
    {
        public static readonly ImmutableArray<MetadataReference> References = BuildReferences();

        static ImmutableArray<MetadataReference> BuildReferences()
        {
            var trustedAssemblies = ((string?)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")) ?? string.Empty;
            return trustedAssemblies
                .Split(System.IO.Path.PathSeparator)
                .Where(p => p.Length > 0 && System.IO.File.Exists(p))
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .ToImmutableArray();
        }
    }
}
