using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class FabFacadeTests
{
    [Theory]
    [InlineData("FloatingActionButton", false, 126)]
    [InlineData("SmallFloatingActionButton", false, 126)]
    [InlineData("LargeFloatingActionButton", false, 126)]
    [InlineData("ExtendedFloatingActionButton", true, 1000)]
    public void ActualWrappersPreserveEveryDefaultAndCompatibilitySlot(string name, bool extended, int all)
    {
        var (output, diags, emitted) = FacadeGeneratorTests.Run(Source(name, extended), name);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("Color? ContainerColor { get; set; }", emitted);
        Assert.Contains("Color? ContentColor { get; set; }", emitted);
        Assert.Contains("FloatingActionButtonElevation? Elevation { get; set; }", emitted);
        Assert.Contains("IMutableInteractionSource? InteractionSource { get; set; }", emitted);
        Assert.Contains("ComposableLambdas.Wrap2", emitted);
        Assert.DoesNotContain("new global::AndroidX.Compose.ComposableLambda2", emitted);
        string[] properties = ["ContainerColor", "ContentColor", "Elevation", "InteractionSource"];
        foreach (string property in properties)
            Assert.Contains($"if ({property} is not null) __defaults &= ~(int)global::AndroidX.Compose.{name}Default.{property};", emitted);

        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(name, isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            var catalog = assembly.GetType("AndroidX.Compose.Composables")
                ?? throw new InvalidOperationException("Generated catalog missing.");
            var helper = catalog.GetMethod(name + "_PrimaryResource_Implicit_WithAddedSlots")
                ?? throw new InvalidOperationException("Added-slot helper missing.");
            var capture = assembly.GetType("AndroidX.Compose.Material3.FloatingActionButtonKt")
                ?? throw new InvalidOperationException("Binding stand-in missing.");
            var defaults = capture.GetField("LastDefaults")
                ?? throw new InvalidOperationException("Default capture missing.");
            object?[] arguments = helper.GetParameters().Select(p => p.Name switch
            {
                "onClick" or "content" or "text" or "icon" => (object)(Action)(() => { }),
                "expanded" => true,
                "__omittedArguments" => extended ? 0x3F0UL : 0xFCUL,
                "__directChanged" => 0,
                _ => null,
            }).ToArray();
            helper.Invoke(null, arguments);
            Assert.Equal(all, defaults.GetValue(null));

            string[] styling = ["containerColor", "contentColor", "elevation", "interactionSource"];
            for (int i = 0; i < styling.Length; i++)
            {
                var parameters = helper.GetParameters();
                int position = Array.FindIndex(parameters, p => p.Name == styling[i]);
                int omitted = Array.FindIndex(parameters, p => p.Name == "__omittedArguments");
                ulong fullOmission = extended ? 0x3F0UL : 0xFCUL;
                // Explicit null and explicit zero both clear only the corresponding physical Kotlin bit.
                arguments[omitted] = fullOmission & ~(1UL << (position - 1));
                helper.Invoke(null, arguments);
                Assert.Equal(all & ~(1 << ((extended ? 6 : 3) + i)), defaults.GetValue(null));
                if (i < 2)
                {
                    var type = Nullable.GetUnderlyingType(parameters[position].ParameterType)
                        ?? throw new InvalidOperationException("Expected nullable Color.");
                    arguments[position] = Activator.CreateInstance(type);
                    helper.Invoke(null, arguments);
                    Assert.Equal(all & ~(1 << ((extended ? 6 : 3) + i)), defaults.GetValue(null));
                    arguments[position] = null;
                }
                arguments[omitted] = fullOmission;
            }

            var facade = assembly.GetType("AndroidX.Compose." + name)
                ?? throw new InvalidOperationException("Facade missing.");
            var ctor = Assert.Single(facade.GetConstructors());
            Assert.Equal(extended ? 2 : 1, ctor.GetParameters().Length);
            Assert.Contains(catalog.GetMethods(), m => m.Name == name && m.GetParameters().Length == (extended ? 6 : 4));
            Assert.Contains(catalog.GetMethods(), m => m.Name == name && m.GetParameters().Length == (extended ? 10 : 8));
            Assert.NotNull(catalog.GetMethod(name + "_PrimaryResource_Implicit"));
        }
        finally
        {
            context.Unload();
        }
    }

    static string Source(string name, bool extended, [CallerFilePath] string file = "")
    {
        string runtime = System.IO.Path.GetFullPath(System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(file) ?? throw new InvalidOperationException("Test source path missing."),
            "..", "Microsoft.AndroidX.Compose"));
        var methods = CSharpSyntaxTree.ParseText(File.ReadAllText(System.IO.Path.Combine(runtime, "ComposeBridges.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == name);
        var defaults = CSharpSyntaxTree.ParseText(File.ReadAllText(System.IO.Path.Combine(runtime, "ComposeDefaults.cs")))
            .GetCompilationUnitRoot().AttributeLists.Single(a => a.ToString().Contains("\"" + name + "Default\""));
        string parameters = extended
            ? "IFunction2 text, IFunction2 icon, IFunction0 onClick, IModifier? modifier, bool expanded, IShape? shape, long containerColor, long contentColor, FloatingActionButtonElevation? elevation, IMutableInteractionSource? interactionSource, IComposer? composer, int p11, int _changed"
            : "IFunction0 onClick, IModifier? modifier, IShape? shape, long containerColor, long contentColor, FloatingActionButtonElevation? elevation, IMutableInteractionSource? interactionSource, IFunction2 content, IComposer? composer, int p9, int _changed";
        return $$"""
            using Android.Runtime;
            using AndroidX.Compose;
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using AndroidX.Compose.UI.Graphics;
            using AndroidX.Compose.Material3;
            using AndroidX.Compose.Foundation.Interaction;
            using Kotlin.Jvm.Functions;
            {{defaults}}
            namespace Android.Runtime
            {
                public static class Casts
                {
                    public static T JavaCast<T>(this Java.Lang.Object value) => throw new System.NotSupportedException();
                }
            }
            namespace AndroidX.Compose.UI.Graphics { public interface IShape { } }
            namespace AndroidX.Compose.Foundation.Interaction { public interface IMutableInteractionSource { } }
            namespace AndroidX.Compose.Material3
            {
                public class FloatingActionButtonElevation : Java.Lang.Object { }
                public static class FloatingActionButtonKt
                {
                    public static int LastDefaults;
                    public static void {{name}}({{parameters}}) { LastDefaults = _changed; }
                }
            }
            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    {{string.Join(Environment.NewLine, methods)}}
                }
            }
            """;
    }
}
