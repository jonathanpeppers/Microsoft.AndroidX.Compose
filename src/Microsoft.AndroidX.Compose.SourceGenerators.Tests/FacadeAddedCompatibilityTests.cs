using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class FacadeAddedCompatibilityTests
{
    static string Contract(bool added) => $$"""
        using System;
        using AndroidX.Compose.Runtime;
        using AndroidX.Compose.UI;
        using AndroidX.Compose;
        using Kotlin.Jvm.Functions;
        [assembly: ComposeDefaults("FabDefault", "!onClick", "modifier", "shape",
            "containerColor", "contentColor", "!content")]
        namespace AndroidX.Compose
        {
            public static partial class ComposeBridges
            {
                public static int LastDefaults;
                public static int LastChanged;
                [ComposeFacade(Defaults = typeof(FabDefault))]
                public static partial void Fab(IFunction0 onClick, IModifier? modifier, Shape? shape,
                    {{(added ? "[FacadeAdded] Color? containerColor, [FacadeAdded] Color? contentColor," : "")}}
                    IFunction2 content, int defaults, IComposer composer, int _changed = 0);
                public static partial void Fab(IFunction0 onClick, IModifier? modifier, Shape? shape,
                    {{(added ? "Color? containerColor, Color? contentColor," : "")}}
                    IFunction2 content, int defaults, IComposer composer, int _changed)
                {
                    LastDefaults = defaults;
                    LastChanged = _changed;
                }
            }
        }
        """;

    [Fact]
    public void AddedSlotsKeepOldSourceAndBinaryContracts()
    {
        var (old, oldDiags, _) = FacadeGeneratorTests.Run(Contract(false), "Fab");
        Assert.Empty(oldDiags.Where(d => d.Severity == DiagnosticSeverity.Error));
        using var oldImage = Emit(old);
        const string consumerSource = """
            using System;
            using AndroidX.Compose;
            public static class Consumer
            {
                public static bool Run()
                {
                    var node = new Fab(() => {});
                    Action<Action, Action, Modifier?, Shape?> method = Composables.Fab;
                    Action<AndroidX.Compose.Runtime.IComposer, Action, Action, Modifier?, Shape?, ulong, int>
                        direct = Composables.Fab_PrimaryResource_Implicit;
                    direct(new TestComposer(), () => {}, () => {}, null, null, 0UL, int.MaxValue);
                    return ComposeBridges.LastDefaults == 24 && ComposeBridges.LastChanged == 0;
                }
                public static void SourceCalls()
                {
                    Composables.Fab(() => {}, () => {});
                    Composables.Fab(content: () => {}, onClick: () => {}, shape: null);
                    Composables.Fab(() => {}, () => {}, null, null);
                }
            }
            """;
        using var binaryConsumer = Emit(Consumer(consumerSource, oldImage.ToArray(), "OldConsumer"));
        var (current, diags, emitted) = FacadeGeneratorTests.Run(Contract(true), "Fab");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        using var currentImage = Emit(current);
        using var rebuiltConsumer = Emit(Consumer(consumerSource, currentImage.ToArray(), "RebuiltConsumer"));
        var context = new AssemblyLoadContext("FacadeAdded", isCollectible: true);
        try
        {
            context.LoadFromStream(currentImage);
            MemoryStream[] consumers = [binaryConsumer, rebuiltConsumer];
            foreach (var image in consumers)
            {
                var method = context.LoadFromStream(image).GetType("Consumer")?.GetMethod("Run")
                    ?? throw new InvalidOperationException("Compatibility consumer entry point missing.");
                Assert.Equal(true, method.Invoke(null, null));
            }
        }
        finally
        {
            context.Unload();
        }

        var methods = CSharpSyntaxTree.ParseText(emitted).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().ToArray();
        var legacy = methods.Single(m => m.Identifier.Text == "Fab_PrimaryResource_Implicit");
        Assert.DoesNotContain("FabDefault.ContainerColor", legacy.ToString());
        Assert.DoesNotContain("FabDefault.ContentColor", legacy.ToString());
        Assert.Contains("FabDefault.All", legacy.ToString());
        Assert.Contains("default, default, __content", legacy.ToString());
        var full = methods.Single(m => m.Identifier.Text == "Fab_PrimaryResource_Implicit_WithAddedSlots");
        Assert.Contains("0x10UL", full.ToString());
        Assert.Contains("0x20UL", full.ToString());
        Assert.Contains("FabDefault.ContainerColor", full.ToString());
        Assert.Contains("FabDefault.ContentColor", full.ToString());
        Assert.Contains("color", full.ToString().ToLowerInvariant());
    }

    [Fact]
    public void ExplicitAndImplicitCallsResolveWithoutAmbiguity()
    {
        var calls = """
            namespace AndroidX.Compose
            {
                public static class Calls
                {
                    public static void Check(Runtime.IComposer c)
                    {
                        System.Action<System.Action, System.Action, Modifier?, Shape?> old = Composables.Fab;
                        System.Action<Runtime.IComposer, System.Action,
                            System.Action<Runtime.IComposer>, Modifier?, Shape?> explicitOld = Composables.Fab;
                        Composables.Fab(c, () => {}, _ => {});
                        Composables.Fab(c, () => {}, _ => {}, shape: null);
                        Composables.Fab(c, () => {}, _ => {}, null, null);
                        Composables.Fab(c, () => {}, _ => {}, contentColor: default(Color));
                        Composables.Fab(() => {}, () => {}, containerColor: default(Color), contentColor: null);
                    }
                }
            }
            """;
        var (output, diags, _) = FacadeGeneratorTests.Run(Contract(true) + calls, "Fab");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void MarkerRejectsRequiredOrNonDefaultableSlots()
    {
        string[] invalidContracts =
        [
            Contract(false).Replace("IFunction0 onClick", "[FacadeAdded] IFunction0 onClick"),
            Contract(true).Replace("\"containerColor\"", "\"!containerColor\""),
        ];
        foreach (var code in invalidContracts)
        {
            var (_, diags, _) = FacadeGeneratorTests.Run(code, "Fab");
            Assert.Contains(diags, d => d.Id == "CN3014");
        }
    }

    [Fact]
    public void AddedSlotsPreserveNativeDecorationInLegacyAndRichSignatures()
    {
        const string code = """
            using AndroidX.Compose;
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI.Text.Input;
            using Kotlin.Jvm.Functions;
            [assembly: ComposeDefaults("EditorDefault", "!value", "!onValueChange", "decorationBox", "color")]
            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeFacade(Defaults = typeof(EditorDefault))]
                    public static partial void Editor(TextFieldValue value,
                        [Callback(typeof(TextFieldValue))] IFunction1 onValueChange,
                        [DecorationBox] IFunction3? decorationBox,
                        [FacadeAdded] Color? color, int defaults, IComposer composer);
                    public static partial void Editor(TextFieldValue value, IFunction1 onValueChange,
                        IFunction3? decorationBox, Color? color, int defaults, IComposer composer) { }
                }
            }
            """;
        var (output, diagnostics, emitted) = FacadeGeneratorTests.Run(code, "Editor");
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        var methods = CSharpSyntaxTree.ParseText(emitted).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "Editor").ToArray();
        Assert.Equal(4, methods.Length);
        foreach (var method in methods)
        {
            var parameters = method.ParameterList.Parameters;
            var decoration = parameters.Single(p => p.Identifier.ValueText == "decorationBox");
            Assert.Contains("System.Action<global::System.Action", decoration.Type?.ToString());
            if (parameters.Any(p => p.Identifier.ValueText == "color"))
                Assert.NotNull(decoration.Default);
            else
                Assert.Null(decoration.Default);
        }
    }

    static CSharpCompilation Consumer(string source, byte[] contract, string name) =>
        CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source)],
            Net.Sdk.References.Concat([MetadataReference.CreateFromImage(contract)]),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

    static MemoryStream Emit(Compilation compilation)
    {
        var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        stream.Position = 0;
        return stream;
    }
}
