#:property PublishAot=false
#:package Microsoft.CodeAnalysis.CSharp@4.11.0

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

const string sourceRoot = "src/Microsoft.AndroidX.Compose";
const string output = "src/Microsoft.AndroidX.Compose/buildTransitive/Microsoft.AndroidX.Compose.bridges.pro";

if (args is not (["--write"] or ["--check"]))
{
    Console.Error.WriteLine("Usage: dotnet run scripts/sync-compose-bridge-r8.cs -- <--write|--check>");
    return 2;
}
if (!Directory.Exists(sourceRoot))
{
    Console.Error.WriteLine($"Run from the repository root; missing {sourceRoot}.");
    return 2;
}

var trees = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
    .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
    .OrderBy(path => path, StringComparer.Ordinal)
    .Select(path => CSharpSyntaxTree.ParseText(
        File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.Preview), path))
    .ToArray();
var compilation = CSharpCompilation.Create(
    "ComposeBridgeKeepRules",
    trees,
    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var bridges = new List<Bridge>();
foreach (var tree in trees)
{
    var model = compilation.GetSemanticModel(tree);
    foreach (var attribute in tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>())
    {
        if (attribute.Name.ToString().Split('.').Last() is not ("ComposeBridge" or "ComposeBridgeAttribute"))
            continue;
        var arguments = attribute.ArgumentList?.Arguments
            .Where(argument => argument.NameEquals is not null)
            .ToDictionary(
                argument => argument.NameEquals!.Name.Identifier.ValueText,
                argument => argument.Expression,
                StringComparer.Ordinal);
        if (arguments is null)
            throw new InvalidDataException($"ComposeBridge has no properties at {tree.FilePath}:{attribute.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");

        string Required(string name)
        {
            if (!arguments.TryGetValue(name, out var expression))
                throw new InvalidDataException($"Missing {name} at {tree.FilePath}:{attribute.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            var constant = model.GetConstantValue(expression);
            if (!constant.HasValue || constant.Value is not string value || value.Length == 0)
                throw new InvalidDataException($"Unresolved {name} at {tree.FilePath}:{expression.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            return value;
        }

        string owner = Required("Class");
        string name = Required("JvmName");
        string signature = Required("Signature");
        string? field = null;
        if (arguments.TryGetValue("InstanceField", out var fieldExpression))
        {
            var constant = model.GetConstantValue(fieldExpression);
            if (!constant.HasValue || constant.Value is not string instanceField)
                throw new InvalidDataException($"Unresolved InstanceField at {tree.FilePath}:{fieldExpression.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            field = instanceField;
        }
        bridges.Add(new Bridge(owner, name, signature, field));
    }
}
if (bridges.Count == 0)
    throw new InvalidDataException($"No [ComposeBridge] declarations found in {sourceRoot}.");

var lines = new List<string>
{
    "# Generated from all [ComposeBridge] declarations by scripts/sync-compose-bridge-r8.cs.",
    "# Do not edit by hand; each bridge comment records the exact JNI contract.",
    "",
};
foreach (var group in bridges.GroupBy(bridge => bridge.Owner).OrderBy(group => group.Key, StringComparer.Ordinal))
{
    lines.Add($"-keep,includedescriptorclasses class {group.Key.Replace('/', '.')} {{");
    foreach (string field in group.Select(bridge => bridge.InstanceField)
                 .OfType<string>().Distinct(StringComparer.Ordinal).OrderBy(field => field, StringComparer.Ordinal))
        lines.Add($"    *** {field};");
    foreach (var bridge in group.DistinctBy(bridge => (bridge.Name, bridge.Signature))
                 .OrderBy(bridge => bridge.Name, StringComparer.Ordinal)
                 .ThenBy(bridge => bridge.Signature, StringComparer.Ordinal))
    {
        var (parameters, returnType) = ParseSignature(bridge.Signature);
        lines.Add($"    # bridge {bridge.Owner}|{bridge.Name}|{bridge.Signature}");
        lines.Add(bridge.Name == "<init>"
            ? $"    <init>({string.Join(", ", parameters)});"
            : $"    {returnType} {bridge.Name}({string.Join(", ", parameters)});");
    }
    lines.Add("}");
    lines.Add("");
}

while (lines.Count > 0 && lines[^1].Length == 0)
    lines.RemoveAt(lines.Count - 1);

string generated = string.Join("\n", lines) + "\n";
if (args[0] == "--write")
{
    File.WriteAllText(output, generated);
    Console.WriteLine($"{output}: wrote {bridges.Count} declarations in {bridges.Select(bridge => bridge.Owner).Distinct().Count()} classes.");
    return 0;
}
if (!File.Exists(output) || File.ReadAllText(output) != generated)
{
    Console.Error.WriteLine($"{output} is stale. Run: dotnet run scripts/sync-compose-bridge-r8.cs -- --write");
    return 1;
}
Console.WriteLine($"{output}: {bridges.Count} source declarations match generated keep rules.");
return 0;

static (List<string> Parameters, string ReturnType) ParseSignature(string signature)
{
    if (!signature.StartsWith('('))
        throw new InvalidDataException($"Invalid JNI signature: {signature}");
    int index = 1;
    var parameters = new List<string>();
    while (index < signature.Length && signature[index] != ')')
        parameters.Add(ParseType(signature, ref index, allowVoid: false));
    if (index == signature.Length)
        throw new InvalidDataException($"Unterminated JNI parameters: {signature}");
    index++;
    string returnType = ParseType(signature, ref index, allowVoid: true);
    if (index != signature.Length)
        throw new InvalidDataException($"Unexpected JNI signature suffix: {signature}");
    return (parameters, returnType);
}

static string ParseType(string descriptor, ref int index, bool allowVoid)
{
    int dimensions = 0;
    while (index < descriptor.Length && descriptor[index] == '[')
    {
        dimensions++;
        index++;
    }
    if (index == descriptor.Length)
        throw new InvalidDataException($"Truncated JNI type: {descriptor}");
    char code = descriptor[index++];
    string type = code switch
    {
        'Z' => "boolean",
        'B' => "byte",
        'C' => "char",
        'S' => "short",
        'I' => "int",
        'J' => "long",
        'F' => "float",
        'D' => "double",
        'V' when allowVoid && dimensions == 0 => "void",
        'L' => ObjectType(descriptor, ref index),
        _ => throw new InvalidDataException($"Unsupported JNI type '{code}' in {descriptor}"),
    };
    return type + string.Concat(Enumerable.Repeat("[]", dimensions));
}

static string ObjectType(string descriptor, ref int index)
{
    int end = descriptor.IndexOf(';', index);
    if (end <= index)
        throw new InvalidDataException($"Unterminated JNI object type: {descriptor}");
    string name = descriptor[index..end].Replace('/', '.');
    index = end + 1;
    return name;
}

internal sealed record Bridge(string Owner, string Name, string Signature, string? InstanceField);
