// Run from the repository root:
// dotnet run scripts/check-shared-state-dex.cs -- [--smoke-harness] app.apk [other.apk ...]
// dotnet run scripts/check-shared-state-dex.cs -- --self-test

using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FieldMap = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, (string Type, uint Access)>>;
using MethodMap = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(string Name, string Signature, uint Access, bool Direct, uint CodeOffset)>>;

if (args is ["--self-test"])
{
    RunSelfTests();
    return 0;
}

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("Usage: dotnet run scripts/check-shared-state-dex.cs -- [--smoke-harness] <apk> [<apk> ...]");
    Console.WriteLine("       dotnet run scripts/check-shared-state-dex.cs -- --self-test");
    return args.Length == 0 ? 2 : 0;
}

bool smokeHarness = args.Contains("--smoke-harness");
string[] paths = args.Where(arg => arg != "--smoke-harness").ToArray();
if (paths.Length == 0 || paths.Any(path => path.StartsWith("--", StringComparison.Ordinal)))
{
    Console.Error.WriteLine("Specify at least one APK; supported options are --smoke-harness, --self-test, and --help.");
    return 2;
}

var contract = ReflectionContract(File.ReadAllText(ContractSource()));
using var output = new MemoryStream();
bool passed = true;
using (var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true }))
{
    writer.WriteStartArray();
    foreach (string path in paths)
        passed &= Inspect(path, contract, smokeHarness, writer);
    writer.WriteEndArray();
}
Console.WriteLine(Encoding.UTF8.GetString(output.ToArray()));
return passed ? 0 : 1;

static string ContractSource([CallerFilePath] string script = "") =>
    Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(script) ?? throw new InvalidOperationException("Script directory is unavailable."),
        "..", "src", "Microsoft.AndroidX.Compose", "Java", "SharedStateLifetime.java"));

static List<(string Owner, string Field)> ReflectionContract(string source)
{
    var imports = Regex.Matches(source, @"^import ([\w.]+);", RegexOptions.Multiline)
        .Select(match => match.Groups[1].Value)
        .ToDictionary(name => name[(name.LastIndexOf('.') + 1)..], StringComparer.Ordinal);
    List<(string Owner, string Field)> contract = [];
    foreach (Match match in Regex.Matches(source, """\bfield\(\s*([\w.]+)\.class,\s*"(\w+)"\s*\)"""))
    {
        string owner = match.Groups[1].Value;
        owner = imports.GetValueOrDefault(owner, owner);
        if (!owner.Contains('.'))
            throw new InvalidDataException($"Unresolved reflection owner: {owner}");
        contract.Add(("L" + owner.Replace('.', '/') + ";", match.Groups[2].Value));
    }
    if (contract.Count == 0)
        throw new InvalidDataException("No reflection contract found in SharedStateLifetime.java.");
    return contract;
}

static FieldMap DeclaredFields(byte[] data, MethodMap? methods = null)
{
    if (data.Length < 112 || !data.AsSpan(0, 4).SequenceEqual("dex\n"u8) ||
        Encoding.ASCII.GetString(data, 4, 4) is not ("035\0" or "037\0" or "038\0" or "039\0" or "040\0"))
        throw new InvalidDataException("Unsupported DEX format.");
    if (U32(40) != 0x12345678)
        throw new InvalidDataException("Unsupported DEX endianness.");

    var strings = new string[Index(56)];
    for (int i = 0; i < strings.Length; i++)
    {
        int start = Index(checked(Index(60) + i * 4));
        _ = Uleb(ref start);
        int end = Array.IndexOf(data, (byte)0, start);
        if (end < 0)
            throw new InvalidDataException("Unterminated DEX string.");
        // Contract identifiers are ASCII; other strings may contain DEX MUTF-8.
        strings[i] = Encoding.UTF8.GetString(data, start, end - start);
    }
    var types = new string[Index(64)];
    for (int i = 0; i < types.Length; i++)
        types[i] = strings[Index(checked(Index(68) + i * 4))];

    FieldMap classes = new(StringComparer.Ordinal);
    for (int i = 0; i < Index(96); i++)
    {
        int start = checked(Index(100) + i * 32);
        string owner = types[Index(start)];
        Dictionary<string, (string Type, uint Access)> fields = new(StringComparer.Ordinal);
        if (!classes.TryAdd(owner, fields))
            throw new InvalidDataException($"Duplicate DEX class: {owner}");
        int position = Index(start + 24);
        if (position == 0)
            continue;
        var sizes = new int[4];
        for (int j = 0; j < sizes.Length; j++)
            sizes[j] = checked((int)Uleb(ref position));
        for (int group = 0; group < 2; group++)
        {
            int fieldIndex = 0;
            for (int j = 0; j < sizes[group]; j++)
            {
                fieldIndex = checked(fieldIndex + (int)Uleb(ref position));
                uint access = Uleb(ref position);
                int field = checked(Index(84) + fieldIndex * 8);
                if (types[U16(field)] != owner)
                    throw new InvalidDataException("DEX field is declared on a different class.");
                fields.Add(strings[Index(field + 4)], (types[U16(field + 2)], access));
            }
        }
        if (methods is null)
            continue;
        var declared = methods[owner] = [];
        for (int group = 2; group < 4; group++)
        {
            int methodIndex = 0;
            for (int j = 0; j < sizes[group]; j++)
            {
                methodIndex = checked(methodIndex + (int)Uleb(ref position));
                uint access = Uleb(ref position);
                uint codeOffset = Uleb(ref position);
                int method = checked(Index(92) + methodIndex * 8);
                if (types[U16(method)] != owner)
                    throw new InvalidDataException("DEX method is declared on a different class.");
                int proto = checked(Index(76) + U16(method + 2) * 12);
                int parametersOffset = Index(proto + 8);
                var signature = new StringBuilder("(");
                if (parametersOffset != 0)
                {
                    for (int parameter = 0; parameter < Index(parametersOffset); parameter++)
                        signature.Append(types[U16(checked(parametersOffset + 4 + parameter * 2))]);
                }
                signature.Append(')').Append(types[Index(proto + 4)]);
                declared.Add((strings[Index(method + 4)], signature.ToString(), access, group == 2, codeOffset));
            }
        }
    }
    return classes;

    uint U32(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
    ushort U16(int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
    int Index(int offset) => checked((int)U32(offset));
    uint Uleb(ref int offset)
    {
        uint result = 0;
        for (int shift = 0; shift < 35; shift += 7)
        {
            if ((uint)offset >= data.Length)
                throw new InvalidDataException("Truncated DEX ULEB128.");
            byte value = data[offset++];
            if (shift == 28 && value > 15)
                throw new InvalidDataException("Invalid DEX ULEB128.");
            result |= (uint)(value & 127) << shift;
            if (value < 128)
                return result;
        }
        throw new InvalidDataException("Invalid DEX ULEB128.");
    }
}

static bool BackendSelectorIsPreserved(FieldMap classes) =>
    classes.TryGetValue("Landroidx/compose/runtime/ComposeRuntimeFlags;", out var fields) &&
    fields.TryGetValue("isLinkBufferComposerEnabled", out var field) &&
    field.Type == "Z" && (field.Access & 9) == 9;

static bool ConfirmGetterIsPreserved(MethodMap methods) =>
    methods.TryGetValue("Landroidx/compose/material3/DrawerState;", out var declared) &&
    declared.Any(method => method.Name == "getConfirmStateChange$material3" &&
        method.Signature == "()Lkotlin/jvm/functions/Function1;" && (method.Access & 8) == 0);

static (string Owner, string Name, string Signature, uint Access, bool Direct)[] JniHelperContract() =>
[
    ("Lmono/android/GCUserPeer;", "<init>", "()V", 0x10000, true),
    ("Lmono/android/GCUserPeer;", "monodroidAddReference", "(Ljava/lang/Object;)V", 1, false),
    ("Lmono/android/GCUserPeer;", "monodroidClearReferences", "()V", 1, false),
    ("Lnet/compose/PointerInputEventHandlerImpl;", "<init>", "(Lkotlin/jvm/functions/Function2;)V", 0x10001, true),
    ("Lnet/compose/PointerInputEventHandlerImpl;", "invoke",
        "(Landroidx/compose/ui/input/pointer/PointerInputScope;Lkotlin/coroutines/Continuation;)Ljava/lang/Object;", 1, false),
    ("Lcomposenet/compose/MeasurePolicyFactory;", "create",
        "(Lkotlin/jvm/functions/Function3;)Landroidx/compose/ui/layout/MeasurePolicy;", 8, true)
];

static bool JniHelperIsPreserved(MethodMap methods,
    (string Owner, string Name, string Signature, uint Access, bool Direct) entry) =>
    methods.TryGetValue(entry.Owner, out var declared) &&
    declared.Any(method => method.Name == entry.Name && method.Signature == entry.Signature &&
        (method.Access & entry.Access) == entry.Access &&
        // Static/constructor shape must match; native/abstract methods have no implementation here.
        (method.Access & 0x10508) == (entry.Access & 0x10508) &&
        method.Direct == entry.Direct && method.CodeOffset != 0);

static bool Inspect(string apkPath, IReadOnlyList<(string Owner, string Field)> contract,
    bool smokeHarness, Utf8JsonWriter writer)
{
    FieldMap classes = new(StringComparer.Ordinal);
    MethodMap methods = new(StringComparer.Ordinal);
    List<string> dexNames = [];
    using var file = File.OpenRead(apkPath);
    string hash = Convert.ToHexStringLower(SHA256.HashData(file));
    file.Position = 0;
    using (var apk = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: true))
    {
        foreach (var entry in apk.Entries.Where(entry => Regex.IsMatch(entry.FullName, @"^classes\d*\.dex$")))
        {
            dexNames.Add(entry.FullName);
            using var input = entry.Open();
            using var dex = new MemoryStream();
            input.CopyTo(dex);
            foreach (var (owner, fields) in DeclaredFields(dex.ToArray(), methods))
            {
                if (!classes.TryAdd(owner, fields))
                    throw new InvalidDataException($"Duplicate DEX class: {owner}");
            }
        }
    }
    if (dexNames.Count == 0)
        throw new InvalidDataException($"No DEX files in {apkPath}");

    bool helperPresent = classes.ContainsKey("Lcomposenet/compose/SharedStateLifetime;");
    bool selectorPresent = BackendSelectorIsPreserved(classes);
    bool getterPresent = smokeHarness && ConfirmGetterIsPreserved(methods);
    bool passed = helperPresent && (!smokeHarness || (selectorPresent && getterPresent));
    writer.WriteStartObject();
    writer.WriteString("apk", Path.GetFullPath(apkPath));
    writer.WriteString("sha256", hash);
    writer.WriteStartArray("dex");
    foreach (string name in dexNames)
        writer.WriteStringValue(name);
    writer.WriteEndArray();
    writer.WriteBoolean("helper_present", helperPresent);
    writer.WriteStartArray("fields");
    foreach (var (owner, name) in contract)
    {
        (string Type, uint Access) field = default;
        bool present = classes.TryGetValue(owner, out var fields) && fields.TryGetValue(name, out field);
        passed &= present;
        writer.WriteStartObject();
        writer.WriteString("class", owner);
        writer.WriteString("field", name);
        writer.WriteBoolean("present", present);
        if (present)
        {
            writer.WriteStartObject("declaration");
            writer.WriteString("type", field.Type);
            writer.WriteNumber("access", field.Access);
            writer.WriteEndObject();
        }
        else
            writer.WriteNull("declaration");
        writer.WriteEndObject();
    }
    writer.WriteEndArray();
    writer.WriteStartArray("jni_helpers");
    foreach (var entry in JniHelperContract())
    {
        bool present = JniHelperIsPreserved(methods, entry);
        passed &= present;
        writer.WriteStartObject();
        writer.WriteString("class", entry.Owner);
        writer.WriteString("method", entry.Name);
        writer.WriteString("signature", entry.Signature);
        writer.WriteBoolean("present", present);
        writer.WriteEndObject();
    }
    writer.WriteEndArray();
    writer.WriteBoolean("smoke_harness_required", smokeHarness);
    writer.WriteBoolean("backend_selector_present", selectorPresent);
    if (smokeHarness)
        writer.WriteBoolean("confirm_getter_present", getterPresent);
    else
        writer.WriteNull("confirm_getter_present");
    writer.WriteBoolean("passed", passed);
    writer.WriteEndObject();
    return passed;
}

static void RunSelfTests()
{
    const string owner = "Landroidx/compose/runtime/LinkComposer;";
    const string drawer = "Landroidx/compose/material3/DrawerState;";
    int passed = 0;
    Test("Field references are not declarations", () =>
        Check(DeclaredFields(DexFixture(owner, false))[owner].Count == 0));
    Test("Instance field declaration and flags", () =>
        Check(DeclaredFields(DexFixture(owner, true))[owner]["changeListWriter"] == ("Ljava/lang/Object;", 18u)));
    Test("Unsupported DEX fails explicitly", () =>
        ExpectInvalid(() => DeclaredFields("not dex"u8.ToArray()), "Unsupported DEX format"));
    Test("Backend selector requires a public static boolean", () =>
    {
        Check(!BackendSelectorIsPreserved([]));
        (string Type, uint Access, bool Expected)[] cases =
            [("Z", 9, true), ("Z", 1, false), ("I", 9, false), ("Z", 8, false)];
        foreach (var (type, access, expected) in cases)
        {
            FieldMap classes = new()
            {
                ["Landroidx/compose/runtime/ComposeRuntimeFlags;"] = new()
                {
                    ["isLinkBufferComposerEnabled"] = (type, access)
                }
            };
            Check(BackendSelectorIsPreserved(classes) == expected);
        }
    });
    Test("Reflection contract includes both backend paths", () =>
    {
        var contract = ReflectionContract(File.ReadAllText(ContractSource()));
        Check(contract.Count == 11);
        string[] backends = ["Gap", "Link"];
        (string Type, string Field)[] fields = [("ComposerChangeListWriter", "changeList"), ("ChangeList", "operations")];
        foreach (string backend in backends)
        {
            Check(contract.Contains(($"Landroidx/compose/runtime/{backend}Composer;", "changeListWriter")));
            foreach (var (type, field) in fields)
                Check(contract.Contains(($"Landroidx/compose/runtime/composer/{backend.ToLowerInvariant()}buffer/changelist/{type};", field)));
        }
    });
    Test("Veto getter requires the exact instance signature", () =>
    {
        Check(!ConfirmGetterIsPreserved([]));
        (string Signature, uint Access, bool Expected)[] cases =
            [("()Lkotlin/jvm/functions/Function1;", 1, true), ("()Lkotlin/jvm/functions/Function1;", 9, false),
             ("()Ljava/lang/Object;", 1, false)];
        foreach (var (signature, access, expected) in cases)
        {
            MethodMap methods = new() { [drawer] = [("getConfirmStateChange$material3", signature, access, false, 1)] };
            Check(ConfirmGetterIsPreserved(methods) == expected);
        }
    });
    Test("Method class-data and prototype decoding", () =>
    {
        bool[] declarations = [false, true];
        foreach (bool declared in declarations)
        {
            MethodMap methods = [];
            DeclaredFields(DexFixture(drawer, false, declared), methods);
            Check(ConfirmGetterIsPreserved(methods) == declared);
        }
    });
    Test("JNI helpers require exact concrete direct/virtual method declarations", () =>
    {
        foreach (var entry in JniHelperContract())
        {
            Check(!JniHelperIsPreserved([], entry));
            MethodMap methods = [];
            DeclaredFields(HelperDexFixture(entry.Owner, declareMethods: false), methods);
            Check(!JniHelperIsPreserved(methods, entry));
            DeclaredFields(HelperDexFixture(entry.Owner), methods);
            Check(JniHelperIsPreserved(methods, entry));
            var original = methods[entry.Owner].Single(method => method.Name == entry.Name);
            (string Name, string Signature, uint Access, bool Direct, uint CodeOffset)[] invalid =
            [
                original with { Name = "renamed" },
                original with { Signature = original.Signature == "()V" ? "(I)V" : "()V" },
                original with { Access = original.Access ^ 8 },
                original with { Access = original.Access | 0x100 },
                original with { Access = original.Access | 0x400 },
                original with { Direct = !original.Direct },
                original with { CodeOffset = 0 }
            ];
            foreach (var method in invalid)
            {
                methods[entry.Owner] = [method];
                Check(!JniHelperIsPreserved(methods, entry));
            }
        }
    });
    Test("Multidex APK results preserve the JSON contract", () =>
    {
        string apkPath = Path.GetTempFileName();
        try
        {
            bool[] declarations = [false, true];
            foreach (bool declared in declarations)
            {
                using (var file = File.Create(apkPath))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    using (var entry = zip.CreateEntry("classes.dex").Open())
                        entry.Write(DexFixture("Lcomposenet/compose/SharedStateLifetime;", false));
                    using (var entry = zip.CreateEntry("classes2.dex").Open())
                        entry.Write(DexFixture(owner, declared));
                    int index = 3;
                    foreach (string helper in JniHelperContract().Select(entry => entry.Owner).Distinct())
                    {
                        using var entry = zip.CreateEntry($"classes{index++}.dex").Open();
                        entry.Write(HelperDexFixture(helper));
                    }
                }
                using var output = new MemoryStream();
                using (var writer = new Utf8JsonWriter(output))
                    Check(Inspect(apkPath, [(owner, "changeListWriter")], false, writer) == declared);
                using var json = JsonDocument.Parse(output.ToArray());
                var result = json.RootElement;
                Check(result.GetProperty("passed").GetBoolean() == declared);
                Check(result.GetProperty("dex").GetArrayLength() == 5);
                Check(result.GetProperty("jni_helpers").GetArrayLength() == 6);
                Check(result.GetProperty("jni_helpers").EnumerateArray().All(helper => helper.GetProperty("present").GetBoolean()));
                Check(result.GetProperty("sha256").GetString() ==
                    Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(apkPath))));
                Check(result.GetProperty("helper_present").GetBoolean());
                Check(!result.GetProperty("smoke_harness_required").GetBoolean());
                Check(result.GetProperty("confirm_getter_present").ValueKind == JsonValueKind.Null);
                var field = result.GetProperty("fields")[0];
                Check(field.GetProperty("class").GetString() == owner);
                Check(field.GetProperty("present").GetBoolean() == declared);
                if (declared)
                    Check(field.GetProperty("declaration").GetProperty("access").GetUInt32() == 18);
                else
                    Check(field.GetProperty("declaration").ValueKind == JsonValueKind.Null);
            }
        }
        finally
        {
            File.Delete(apkPath);
        }
    });
    Test("Final APK fails if any JNI helper method is only a reference", () =>
    {
        string apkPath = Path.GetTempFileName();
        try
        {
            foreach (var missing in JniHelperContract())
            {
                using (var file = File.Create(apkPath))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    using (var entry = zip.CreateEntry("classes.dex").Open())
                        entry.Write(DexFixture("Lcomposenet/compose/SharedStateLifetime;", false));
                    int index = 2;
                    foreach (string helper in JniHelperContract().Select(entry => entry.Owner).Distinct())
                    {
                        using var entry = zip.CreateEntry($"classes{index++}.dex").Open();
                        entry.Write(HelperDexFixture(helper, omitMethod: helper == missing.Owner ? missing.Name : null));
                    }
                }
                using var output = new MemoryStream();
                using (var writer = new Utf8JsonWriter(output))
                    Check(!Inspect(apkPath, [], false, writer));
                using var json = JsonDocument.Parse(output.ToArray());
                Check(json.RootElement.GetProperty("jni_helpers").EnumerateArray()
                    .Count(helper => !helper.GetProperty("present").GetBoolean()) == 1);
            }
        }
        finally
        {
            File.Delete(apkPath);
        }
    });
    Console.WriteLine($"{passed} self-tests passed.");

    void Test(string name, Action test)
    {
        test();
        passed++;
        Console.WriteLine($"PASS: {name}");
    }
    static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("DEX checker self-test assertion failed.");
    }
    static void ExpectInvalid(Action test, string message)
    {
        try
        {
            test();
        }
        catch (InvalidDataException error) when (error.Message.Contains(message, StringComparison.Ordinal))
        {
            return;
        }
        throw new InvalidOperationException($"Expected InvalidDataException containing '{message}'.");
    }
}

static byte[] HelperDexFixture(string owner, bool declareMethods = true, string? omitMethod = null)
{
    var methods = JniHelperContract().Where(entry => entry.Owner == owner).ToArray();
    var parameters = methods.Select(entry => Regex.Matches(
        entry.Signature[..entry.Signature.IndexOf(')')], @"L[^;]+;").Select(match => match.Value).ToArray()).ToArray();
    var returns = methods.Select(entry => entry.Signature[(entry.Signature.IndexOf(')') + 1)..]).ToArray();
    string[] types = [.. parameters.SelectMany(value => value).Concat(returns).Prepend(owner).Distinct()];
    string[] strings = [.. types.Concat(methods.Select(entry => entry.Name)).Distinct()];
    const int stringIds = 112;
    int typeIds = stringIds + strings.Length * 4;
    int protoIds = typeIds + types.Length * 4;
    int methodIds = protoIds + methods.Length * 12;
    int classDefs = methodIds + methods.Length * 8;
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
    stream.SetLength(classDefs + 32);
    writer.Write("dex\n039\0"u8);
    U32(40, 0x12345678);
    U32(56, strings.Length);
    U32(60, stringIds);
    U32(64, types.Length);
    U32(68, typeIds);
    U32(72, methods.Length);
    U32(76, protoIds);
    U32(88, methods.Length);
    U32(92, methodIds);
    U32(96, 1);
    U32(100, classDefs);
    for (int i = 0; i < strings.Length; i++)
    {
        int offset = checked((int)stream.Length);
        U32(stringIds + i * 4, offset);
        stream.Position = offset;
        Uleb((uint)strings[i].Length);
        writer.Write(Encoding.ASCII.GetBytes(strings[i]));
        writer.Write((byte)0);
    }
    for (int i = 0; i < types.Length; i++)
        U32(typeIds + i * 4, Array.IndexOf(strings, types[i]));
    for (int i = 0; i < methods.Length; i++)
    {
        int offset = checked((int)stream.Length);
        U32(protoIds + i * 12 + 4, Array.IndexOf(types, returns[i]));
        U32(protoIds + i * 12 + 8, offset);
        stream.Position = offset;
        writer.Write(parameters[i].Length);
        foreach (string parameter in parameters[i])
            writer.Write((ushort)Array.IndexOf(types, parameter));
        stream.Position = methodIds + i * 8;
        writer.Write((ushort)0);
        writer.Write((ushort)i);
        writer.Write(Array.IndexOf(strings, methods[i].Name));
    }
    if (declareMethods)
    {
        int offset = checked((int)stream.Length);
        U32(classDefs + 24, offset);
        stream.Position = offset;
        Uleb(0);
        Uleb(0);
        Uleb((uint)methods.Count(entry => entry.Direct && entry.Name != omitMethod));
        Uleb((uint)methods.Count(entry => !entry.Direct && entry.Name != omitMethod));
        bool[] groups = [true, false];
        foreach (bool direct in groups)
        {
            int previous = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Direct != direct || methods[i].Name == omitMethod)
                    continue;
                Uleb((uint)(i - previous));
                Uleb(methods[i].Access);
                Uleb(1); // Synthetic nonzero code marker; this fixture tests metadata decoding, not execution.
                previous = i;
            }
        }
    }
    return stream.ToArray();

    void U32(int offset, int value)
    {
        stream.Position = offset;
        writer.Write(value);
    }
    void Uleb(uint value)
    {
        do
        {
            byte part = (byte)(value & 127);
            value >>= 7;
            writer.Write((byte)(part | (value != 0 ? 128 : 0)));
        } while (value != 0);
    }
}

static byte[] DexFixture(string owner, bool declareField, bool declareMethod = false)
{
    string[] strings = [owner, "Ljava/lang/Object;", "changeListWriter",
        "getConfirmStateChange$material3", "Lkotlin/jvm/functions/Function1;", "L"];
    const int stringIds = 112;
    int typeIds = stringIds + 4 * strings.Length;
    int protoIds = typeIds + 12;
    int fieldIds = protoIds + 12;
    int methodIds = fieldIds + 8;
    int classDefs = methodIds + 8;
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
    stream.SetLength(classDefs + 32);
    writer.Write("dex\n039\0"u8);
    U32(40, 0x12345678);
    U32(56, strings.Length);
    U32(60, stringIds);
    U32(64, 3);
    U32(68, typeIds);
    U32(72, 1);
    U32(76, protoIds);
    U32(80, 1);
    U32(84, fieldIds);
    U32(88, 1);
    U32(92, methodIds);
    U32(96, 1);
    U32(100, classDefs);
    U32(typeIds, 0);
    U32(typeIds + 4, 1);
    U32(typeIds + 8, 4);
    U32(protoIds, 5);
    U32(protoIds + 4, 2);
    stream.Position = fieldIds;
    writer.Write((ushort)0);
    writer.Write((ushort)1);
    writer.Write(2);
    stream.Position = methodIds;
    writer.Write((ushort)0);
    writer.Write((ushort)0);
    writer.Write(3);
    for (int i = 0; i < strings.Length; i++)
    {
        int offset = checked((int)stream.Length);
        U32(stringIds + 4 * i, offset);
        stream.Position = offset;
        writer.Write(checked((byte)strings[i].Length));
        writer.Write(Encoding.ASCII.GetBytes(strings[i]));
        writer.Write((byte)0);
    }
    if (declareField || declareMethod)
    {
        int offset = checked((int)stream.Length);
        U32(classDefs + 24, offset);
        stream.Position = offset;
        writer.Write((byte[])[0, (byte)(declareField ? 1 : 0), 0, (byte)(declareMethod ? 1 : 0)]);
        if (declareField)
            writer.Write((byte[])[0, 18]);
        if (declareMethod)
            writer.Write((byte[])[0, 1, 0]);
    }
    return stream.ToArray();

    void U32(int offset, int value)
    {
        stream.Position = offset;
        writer.Write(value);
    }
}
