using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class LambdaAdapterLoweringTests
{
    const string Preamble = """
        namespace Kotlin.Jvm.Functions
        {
            public interface IFunction0 { }
            public interface IFunction1 { }
            public interface IFunction2 { }
            public interface IFunction3 { }
            public interface IFunction4 { }
        }

        namespace AndroidX.Compose
        {
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class CallbackAttribute : System.Attribute
            {
                public CallbackAttribute(System.Type valueType) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class ComposableContentAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class DeferredComposableContentAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class RawCallbackAttribute : System.Attribute { }
        }
        """;

    [Theory]
    [InlineData("Kotlin.Jvm.Functions.IFunction2 content", nameof(LambdaExecutionMode.SynchronousComposable), 2)]
    [InlineData("Kotlin.Jvm.Functions.IFunction3 content", nameof(LambdaExecutionMode.SynchronousComposable), 3)]
    [InlineData("Kotlin.Jvm.Functions.IFunction0 onClick", nameof(LambdaExecutionMode.Event), 0)]
    [InlineData("[AndroidX.Compose.Callback(typeof(string))] Kotlin.Jvm.Functions.IFunction1 onValueChange", nameof(LambdaExecutionMode.Event), 1)]
    [InlineData("[AndroidX.Compose.RawCallback] Kotlin.Jvm.Functions.IFunction1 builder", nameof(LambdaExecutionMode.Raw), 1)]
    [InlineData("[AndroidX.Compose.ComposableContent] Kotlin.Jvm.Functions.IFunction4 content", nameof(LambdaExecutionMode.SynchronousComposable), 4)]
    [InlineData("[AndroidX.Compose.DeferredComposableContent] Kotlin.Jvm.Functions.IFunction4 itemContent", nameof(LambdaExecutionMode.DeferredComposable), 4)]
    public void Classify_RecognizesExplicitExecutionModes(
        string parameter,
        string expectedMode,
        int expectedArity)
    {
        var result = LambdaAdapterLowering.Classify(GetParameter(parameter));

        Assert.True(result.Success, result.Error);
        Assert.Equal(expectedMode, result.Classification.Mode.ToString());
        Assert.Equal(expectedArity, result.Classification.Arity);
    }

    [Theory]
    [InlineData(
        nameof(LambdaExecutionMode.SynchronousComposable),
        2,
        "global::AndroidX.Compose.ComposableLambdas.Wrap2(composer, body)")]
    [InlineData(
        nameof(LambdaExecutionMode.SynchronousComposable),
        3,
        "global::AndroidX.Compose.ComposableLambdas.Wrap3(composer, body)")]
    [InlineData(
        nameof(LambdaExecutionMode.SynchronousComposable),
        4,
        "global::AndroidX.Compose.ComposableLambdas.Wrap4(composer, body)")]
    [InlineData(
        nameof(LambdaExecutionMode.DeferredComposable),
        4,
        "global::AndroidX.Compose.ComposableLambdas.Instantiate4(body)")]
    [InlineData(
        nameof(LambdaExecutionMode.Event),
        0,
        "composer.RememberAction(body)")]
    [InlineData(
        nameof(LambdaExecutionMode.Event),
        1,
        "composer.RememberAction(body)")]
    [InlineData(
        nameof(LambdaExecutionMode.Raw),
        1,
        "new global::AndroidX.Compose.ComposableLambda1(body)")]
    public void EmitExpression_UsesIdentitySafeFactory(
        string mode,
        int arity,
        string expected)
    {
        var actual = LambdaAdapterLowering.EmitExpression(
            new LambdaAdapterClassification(
                Enum.Parse<LambdaExecutionMode>(mode),
                arity),
            "composer",
            "body");

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(
        "Kotlin.Jvm.Functions.IFunction1 callback",
        "IFunction1 parameter 'callback' is ambiguous")]
    [InlineData(
        "Kotlin.Jvm.Functions.IFunction4 content",
        "IFunction4 parameter 'content' is ambiguous")]
    [InlineData(
        "[AndroidX.Compose.RawCallback, AndroidX.Compose.Callback(typeof(string))] Kotlin.Jvm.Functions.IFunction1 callback",
        "conflicting lambda execution-mode attributes")]
    [InlineData(
        "[AndroidX.Compose.DeferredComposableContent] Kotlin.Jvm.Functions.IFunction3 content",
        "must be IFunction4 lazy item content")]
    public void Classify_RejectsAmbiguousOrInvalidModes(
        string parameter,
        string expectedError)
    {
        var result = LambdaAdapterLowering.Classify(GetParameter(parameter));

        Assert.False(result.Success);
        Assert.Contains(expectedError, result.Error);
    }

    static IParameterSymbol GetParameter(string parameter)
    {
        var tree = CSharpSyntaxTree.ParseText(
            Preamble
            + "\npublic static class C { public static void M("
            + parameter
            + ") { } }");
        var compilation = CSharpCompilation.Create(
            "LambdaAdapterTest",
            [tree],
            Net.Sdk.References,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        var method = compilation.GetTypeByMetadataName("C")
            ?.GetMembers("M")
            .OfType<IMethodSymbol>()
            .Single()
            ?? throw new InvalidOperationException(
                "Synthetic lambda adapter test method was not compiled.");
        return method.Parameters.Single();
    }
}
