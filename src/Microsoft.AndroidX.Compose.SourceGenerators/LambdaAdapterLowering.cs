using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AndroidX.Compose.SourceGenerators;

internal enum LambdaExecutionMode
{
    SynchronousComposable,
    DeferredComposable,
    Event,
    Raw,
}

internal readonly struct LambdaAdapterClassification
{
    public LambdaAdapterClassification(LambdaExecutionMode mode, int arity)
    {
        Mode = mode;
        Arity = arity;
    }

    public LambdaExecutionMode Mode { get; }
    public int Arity { get; }
}

internal readonly struct LambdaClassificationResult
{
    LambdaClassificationResult(
        LambdaAdapterClassification classification,
        string? error)
    {
        Classification = classification;
        Error = error;
    }

    public LambdaAdapterClassification Classification { get; }
    public string? Error { get; }
    public bool Success => Error is null;

    public static LambdaClassificationResult Valid(
        LambdaExecutionMode mode,
        int arity) =>
        new(new LambdaAdapterClassification(mode, arity), error: null);

    public static LambdaClassificationResult Invalid(string error) =>
        new(default, error);
}

internal static class LambdaAdapterLowering
{
    const string CallbackAttributeMetadataName =
        "AndroidX.Compose.CallbackAttribute";
    const string ComposableContentAttributeMetadataName =
        "AndroidX.Compose.ComposableContentAttribute";
    const string DeferredComposableContentAttributeMetadataName =
        "AndroidX.Compose.DeferredComposableContentAttribute";
    const string RawCallbackAttributeMetadataName =
        "AndroidX.Compose.RawCallbackAttribute";

    public static LambdaClassificationResult Classify(IParameterSymbol parameter)
    {
        int arity = KotlinFunctionArity(parameter.Type);
        if (arity < 0)
            return LambdaClassificationResult.Invalid(
                $"parameter '{parameter.Name}' is not a Kotlin IFunction type");

        bool callback = HasAttribute(parameter, CallbackAttributeMetadataName);
        bool synchronous = HasAttribute(
            parameter,
            ComposableContentAttributeMetadataName);
        bool deferred = HasAttribute(
            parameter,
            DeferredComposableContentAttributeMetadataName);
        bool raw = HasAttribute(parameter, RawCallbackAttributeMetadataName);
        int markerCount = (callback ? 1 : 0)
            + (synchronous ? 1 : 0)
            + (deferred ? 1 : 0)
            + (raw ? 1 : 0);

        if (markerCount > 1)
        {
            return LambdaClassificationResult.Invalid(
                $"parameter '{parameter.Name}' has conflicting lambda execution-mode attributes");
        }

        if (callback)
        {
            return arity == 1
                ? LambdaClassificationResult.Valid(LambdaExecutionMode.Event, arity)
                : LambdaClassificationResult.Invalid(
                    $"[Callback] parameter '{parameter.Name}' must be IFunction1, not IFunction{arity}");
        }

        if (synchronous)
        {
            return arity is 2 or 3 or 4
                ? LambdaClassificationResult.Valid(
                    LambdaExecutionMode.SynchronousComposable,
                    arity)
                : LambdaClassificationResult.Invalid(
                    $"[ComposableContent] parameter '{parameter.Name}' must be IFunction2, IFunction3, or IFunction4");
        }

        if (deferred)
        {
            return arity == 4
                ? LambdaClassificationResult.Valid(
                    LambdaExecutionMode.DeferredComposable,
                    arity)
                : LambdaClassificationResult.Invalid(
                    $"[DeferredComposableContent] parameter '{parameter.Name}' must be IFunction4 lazy item content");
        }

        if (raw)
        {
            return arity is 0 or 1
                ? LambdaClassificationResult.Valid(LambdaExecutionMode.Raw, arity)
                : LambdaClassificationResult.Invalid(
                    $"[RawCallback] parameter '{parameter.Name}' must be IFunction0 or IFunction1");
        }

        return arity switch
        {
            0 => LambdaClassificationResult.Valid(LambdaExecutionMode.Event, arity),
            2 or 3 => LambdaClassificationResult.Valid(
                LambdaExecutionMode.SynchronousComposable,
                arity),
            1 => LambdaClassificationResult.Invalid(
                $"IFunction1 parameter '{parameter.Name}' is ambiguous; mark it [Callback(typeof(T))] for an identity-stable event or [RawCallback] for a non-composable DSL callback"),
            4 => LambdaClassificationResult.Invalid(
                $"IFunction4 parameter '{parameter.Name}' is ambiguous; mark it [ComposableContent] for synchronous content or [DeferredComposableContent] for lazy item content"),
            _ => LambdaClassificationResult.Invalid(
                $"IFunction{arity} parameter '{parameter.Name}' has no supported lambda adapter"),
        };
    }

    public static string EmitExpression(
        LambdaAdapterClassification classification,
        string composerExpression,
        string bodyExpression) =>
        classification.Mode switch
        {
            LambdaExecutionMode.SynchronousComposable
                when classification.Arity is 2 or 3 or 4 =>
                $"global::AndroidX.Compose.ComposableLambdas.Wrap{classification.Arity}({composerExpression}, {bodyExpression})",
            LambdaExecutionMode.DeferredComposable
                when classification.Arity == 4 =>
                $"global::AndroidX.Compose.ComposableLambdas.Instantiate{classification.Arity}({bodyExpression})",
            LambdaExecutionMode.Event
                when classification.Arity is 0 or 1 =>
                $"{composerExpression}.RememberAction({bodyExpression})",
            LambdaExecutionMode.Raw
                when classification.Arity is 0 or 1 =>
                $"new global::AndroidX.Compose.ComposableLambda{classification.Arity}({bodyExpression})",
            _ => throw new InvalidOperationException(
                $"Lambda execution mode '{classification.Mode}' does not support IFunction{classification.Arity}."),
        };

    public static int KotlinFunctionArity(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return -1;
        if (named.ContainingNamespace?.ToDisplayString()
            != "Kotlin.Jvm.Functions")
        {
            return -1;
        }

        const string prefix = "IFunction";
        if (!named.Name.StartsWith(prefix, StringComparison.Ordinal))
            return -1;

        return int.TryParse(
            named.Name.Substring(prefix.Length),
            out int arity)
            ? arity
            : -1;
    }

    static bool HasAttribute(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == metadataName);
}
