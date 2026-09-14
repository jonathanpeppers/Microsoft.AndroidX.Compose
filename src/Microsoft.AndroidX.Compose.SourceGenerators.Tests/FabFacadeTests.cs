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
            var requests = capture.GetField("FactoryRequests")
                ?? throw new InvalidOperationException("Factory capture missing.");
            Assert.Equal(all, Convert.ToInt32(Enum.Parse(
                assembly.GetType("AndroidX.Compose." + name + "Default")
                    ?? throw new InvalidOperationException("FAB default enum missing."), "All")));
            object?[] arguments = helper.GetParameters().Select(p => p.Name switch
            {
                "onClick" or "content" or "text" or "icon" => (object)(Action)(() => { }),
                "expanded" => true,
                "__omittedArguments" => extended ? 0x3F0UL : 0xFCUL,
                "__directChanged" => 0,
                _ => null,
            }).ToArray();
            helper.Invoke(null, arguments);
            Assert.Equal(0, defaults.GetValue(null));
            Assert.Equal(15, requests.GetValue(null));
            Assert.Equal(15, capture.GetField("ElevationDefaults")?.GetValue(null));
            Assert.Equal(0, capture.GetField("LastChanged")?.GetValue(null));
            Assert.Equal(11L, capture.GetField("LastContainer")?.GetValue(null));
            Assert.Equal(111L, capture.GetField("LastContent")?.GetValue(null));
            Assert.NotNull(capture.GetField("LastModifier")?.GetValue(null));
            Assert.Equal(name switch
            {
                "SmallFloatingActionButton" => "GetSmallShape",
                "LargeFloatingActionButton" => "GetLargeShape",
                "ExtendedFloatingActionButton" => "GetExtendedFabShape",
                _ => "GetShape",
            }, capture.GetField("ShapeFactory")?.GetValue(null));

            string[] styling = ["containerColor", "contentColor", "elevation", "interactionSource"];
            for (int i = 0; i < styling.Length; i++)
            {
                var parameters = helper.GetParameters();
                int position = Array.FindIndex(parameters, p => p.Name == styling[i]);
                int omitted = Array.FindIndex(parameters, p => p.Name == "__omittedArguments");
                ulong fullOmission = extended ? 0x3F0UL : 0xFCUL;
                // Logical omission selects the native factories; the native comparer footprint stays fixed.
                arguments[omitted] = fullOmission & ~(1UL << (position - 1));
                requests.SetValue(null, 0);
                helper.Invoke(null, arguments);
                Assert.Equal(0, defaults.GetValue(null));
                Assert.Equal(i < 3 ? 15 & ~(1 << i) : 15, requests.GetValue(null));
                Assert.Equal(0, capture.GetField("LastChanged")?.GetValue(null));
                if (i < 2)
                {
                    var type = Nullable.GetUnderlyingType(parameters[position].ParameterType)
                        ?? throw new InvalidOperationException("Expected nullable Color.");
                    arguments[position] = Activator.CreateInstance(type);
                    requests.SetValue(null, 0);
                    helper.Invoke(null, arguments);
                    Assert.Equal(0, defaults.GetValue(null));
                    Assert.Equal(15 & ~(1 << i), requests.GetValue(null));
                    Assert.Equal(0L, capture.GetField(i == 0 ? "LastContainer" : "LastContent")?.GetValue(null));
                }
                else
                {
                    Assert.Null(capture.GetField(i == 2 ? "LastElevation" : "LastSource")?.GetValue(null));
                    var type = assembly.GetType(i == 2
                        ? "AndroidX.Compose.Material3.FloatingActionButtonElevation"
                        : "AndroidX.Compose.Foundation.Interaction.MutableSource")
                        ?? throw new InvalidOperationException("Native reference stand-in missing.");
                    arguments[position] = Activator.CreateInstance(type);
                    helper.Invoke(null, arguments);
                    Assert.Same(arguments[position], capture.GetField(i == 2 ? "LastElevation" : "LastSource")?.GetValue(null));
                }
                arguments[position] = null;
                arguments[omitted] = fullOmission;
            }
            int shapePosition = Array.FindIndex(helper.GetParameters(), p => p.Name == "shape");
            int omissionPosition = Array.FindIndex(helper.GetParameters(), p => p.Name == "__omittedArguments");
            arguments[omissionPosition] = (extended ? 0x3F0UL : 0xFCUL) & ~(1UL << (shapePosition - 1));
            requests.SetValue(null, 0);
            helper.Invoke(null, arguments);
            Assert.Equal(7, requests.GetValue(null));
            Assert.Null(capture.GetField("LastShape")?.GetValue(null));
            arguments[shapePosition] = Activator.CreateInstance(assembly.GetType("AndroidX.Compose.UI.Graphics.NativeShape")
                ?? throw new InvalidOperationException("Shape stand-in missing."));
            helper.Invoke(null, arguments);
            Assert.Same(arguments[shapePosition], capture.GetField("LastShape")?.GetValue(null));
            Assert.Equal(0, defaults.GetValue(null));
            arguments[shapePosition] = null;
            int modifierPosition = Array.FindIndex(helper.GetParameters(), p => p.Name == "modifier");
            arguments[omissionPosition] = (extended ? 0x3F0UL : 0xFCUL) & ~(1UL << (modifierPosition - 1));
            helper.Invoke(null, arguments);
            Assert.Null(capture.GetField("LastModifier")?.GetValue(null));
            Assert.Equal(0, assembly.GetType("AndroidX.Compose.GroupCapture")?.GetField("Depth")?.GetValue(null));

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
        var elevationDefaults = CSharpSyntaxTree.ParseText(File.ReadAllText(System.IO.Path.Combine(runtime, "ComposeDefaults.cs")))
            .GetCompilationUnitRoot().AttributeLists.Single(a => a.ToString().Contains("\"FloatingActionButtonElevationDefault\""));
        var resolver = CSharpSyntaxTree.ParseText(File.ReadAllText(System.IO.Path.Combine(runtime, "FabStyleDefaults.cs")))
            .GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
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
            {{elevationDefaults}}
            namespace Android.Runtime
            {
                public static class Casts
                {
                    public static T JavaCast<T>(this Java.Lang.Object value) =>
                        value is T result ? result : throw new System.InvalidCastException();
                }
            }
            namespace AndroidX.Compose.UI.Graphics
            {
                public interface IShape { }
                public class NativeShape : AndroidX.Compose.Shape, IShape { }
            }
            namespace AndroidX.Compose.Foundation.Interaction
            {
                public interface IMutableInteractionSource { }
                public class MutableSource : IMutableInteractionSource { }
            }
            namespace AndroidX.Compose.Material3
            {
                public class FloatingActionButtonElevation : Java.Lang.Object { }
                public class FloatingActionButtonDefaults
                {
                    public static FloatingActionButtonDefaults Instance { get; } = new();
                    IShape Shape(string factory)
                    {
                        FloatingActionButtonKt.FactoryRequests |= 8;
                        FloatingActionButtonKt.ShapeFactory = factory;
                        return new NativeShape();
                    }
                    public IShape GetShape(IComposer c, int changed) => Shape(nameof(GetShape));
                    public IShape GetSmallShape(IComposer c, int changed) => Shape(nameof(GetSmallShape));
                    public IShape GetLargeShape(IComposer c, int changed) => Shape(nameof(GetLargeShape));
                    public IShape GetExtendedFabShape(IComposer c, int changed) => Shape(nameof(GetExtendedFabShape));
                    public long GetContainerColor(IComposer c, int changed)
                    {
                        FloatingActionButtonKt.FactoryRequests |= 1;
                        return 11;
                    }
                    public FloatingActionButtonElevation Elevation(float a, float b, float c, float d,
                        IComposer composer, int p5, int _changed)
                    {
                        FloatingActionButtonKt.FactoryRequests |= 4;
                        FloatingActionButtonKt.ElevationDefaults = _changed;
                        return new();
                    }
                }
                public static class ColorSchemeKt
                {
                    public static long ContentColorFor(long color, IComposer composer, int changed)
                    {
                        FloatingActionButtonKt.FactoryRequests |= 2;
                        return color + 100;
                    }
                }
                public static class FloatingActionButtonKt
                {
                    public static int LastDefaults, LastChanged, FactoryRequests, ElevationDefaults;
                    public static long LastContainer, LastContent;
                    public static FloatingActionButtonElevation? LastElevation;
                    public static IMutableInteractionSource? LastSource;
                    public static IShape? LastShape;
                    public static IModifier? LastModifier;
                    public static string ShapeFactory = "";
                    public static void {{name}}({{parameters}})
                    {
                        LastDefaults = _changed;
                        LastChanged = {{(extended ? "p11" : "p9")}};
                        LastContainer = containerColor; LastContent = contentColor;
                        LastElevation = elevation; LastSource = interactionSource;
                        LastShape = shape;
                        LastModifier = modifier;
                    }
                }
            }
            namespace AndroidX.Compose
            {
                public static class CompositionGroupKey
                {
                    public static int Compute(int position, System.Type type) => position;
                }
                public static class GroupCapture
                {
                    public static int Depth;
                    public static void StartReplaceableGroup(this IComposer composer, int key) => Depth++;
                    public static void EndReplaceableGroup(this IComposer composer) => Depth--;
                }
                {{resolver}}
                public static partial class ComposeBridges
                {
                    {{string.Join(Environment.NewLine, methods)}}
                }
            }
            """;
    }
}
