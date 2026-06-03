using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ComposeNet.SourceGenerators;

internal sealed class GenerationResult
{
    public GenerationResult(string? source, string? hintName, IReadOnlyList<Diagnostic> diagnostics)
    {
        Source = source;
        HintName = hintName;
        Diagnostics = diagnostics;
    }

    public string? Source { get; }
    public string? HintName { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
