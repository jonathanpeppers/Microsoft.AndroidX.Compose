// scripts/api-comparison.cs
//
// Jetpack Compose ⇄ Microsoft.AndroidX.Compose API coverage report generator.
//
// Run from repo root:
//
//     dotnet run scripts/api-comparison.cs
//
// What it does:
//   1. Downloads `-sources.jar` for each AndroidX Compose artifact this repo
//      references (matches Directory.Build.targets), straight from
//      dl.google.com/dl/android/maven2. Cached under `obj/api-coverage/`.
//   2. Regex-parses the extracted Kotlin sources to enumerate top-level
//      public declarations (fun, class, interface, object, enum, value, ...).
//   3. Parses `src/Microsoft.AndroidX.Compose/PublicAPI.Unshipped.txt` to enumerate
//      the C# facade's public surface.
//   4. Cross-references Kotlin ⇄ C# by short name with a few heuristics
//      (Modifier extensions, *Defaults objects, `remember*` ↔ state-holder
//      ctor, etc.) and writes `docs/api-coverage.md`.
//
// Re-run after bumping a Compose package version in Directory.Build.targets
// and the report auto-refreshes.

using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

const string CacheRoot       = "obj/api-coverage";
const string SourcesDir      = CacheRoot + "/sources";
const string MavenBase       = "https://dl.google.com/dl/android/maven2";
const string PublicApiPath   = "src/Microsoft.AndroidX.Compose/PublicAPI.Unshipped.txt";
const string ReportPath      = "docs/api-coverage.md";

// (Group, Artifact, Version, DisplayModule). Mirrors Directory.Build.targets;
// strips the trailing Xamarin wrapper revision (1.11.2.1 -> 1.11.2).
var artifacts = new (string Group, string Artifact, string Version, string Module)[]
{
    ("androidx.compose.animation",  "animation",                  "1.11.2", "animation"),
    ("androidx.compose.animation",  "animation-android",          "1.11.2", "animation"),
    ("androidx.compose.animation",  "animation-core",             "1.11.2", "animation-core"),
    ("androidx.compose.animation",  "animation-core-android",     "1.11.2", "animation-core"),
    ("androidx.compose.foundation", "foundation",                 "1.11.2", "foundation"),
    ("androidx.compose.foundation", "foundation-android",         "1.11.2", "foundation"),
    ("androidx.compose.foundation", "foundation-layout",          "1.11.2", "foundation-layout"),
    ("androidx.compose.foundation", "foundation-layout-android",  "1.11.2", "foundation-layout"),
    ("androidx.compose.material3",  "material3",                  "1.4.0",  "material3"),
    ("androidx.compose.material3",  "material3-android",          "1.4.0",  "material3"),
    ("androidx.compose.runtime",    "runtime",                    "1.11.2", "runtime"),
    ("androidx.compose.runtime",    "runtime-android",            "1.11.2", "runtime"),
    ("androidx.compose.runtime",    "runtime-saveable",           "1.11.2", "runtime-saveable"),
    ("androidx.compose.runtime",    "runtime-saveable-android",   "1.11.2", "runtime-saveable"),
    ("androidx.compose.ui",         "ui",                         "1.11.2", "ui"),
    ("androidx.compose.ui",         "ui-android",                 "1.11.2", "ui"),
    ("androidx.compose.ui",         "ui-graphics",                "1.11.2", "ui-graphics"),
    ("androidx.compose.ui",         "ui-graphics-android",        "1.11.2", "ui-graphics"),
    ("androidx.compose.ui",         "ui-text",                    "1.11.2", "ui-text"),
    ("androidx.compose.ui",         "ui-text-android",            "1.11.2", "ui-text"),
    ("androidx.compose.ui",         "ui-unit",                    "1.11.2", "ui-unit"),
    ("androidx.compose.ui",         "ui-unit-android",            "1.11.2", "ui-unit"),
    ("androidx.activity",           "activity-compose",           "1.13.0", "activity-compose"),
    ("androidx.navigation",         "navigation-compose",         "2.9.8",  "navigation-compose"),
};

// ------------------------------------------------------------
// 1. Download + extract -sources.jar
// ------------------------------------------------------------

Directory.CreateDirectory(SourcesDir);
using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

foreach (var a in artifacts)
{
    var extractDir = Path.Combine(SourcesDir, $"{a.Artifact}-{a.Version}");
    bool hasKt = Directory.Exists(extractDir)
        && Directory.EnumerateFiles(extractDir, "*.kt", SearchOption.AllDirectories).Any();
    if (hasKt)
    {
        Console.WriteLine($"[cached] {a.Artifact}-{a.Version}");
        continue;
    }

    var jarPath = Path.Combine(SourcesDir, $"{a.Artifact}-{a.Version}-sources.jar");
    if (!File.Exists(jarPath))
    {
        var url = $"{MavenBase}/{a.Group.Replace('.', '/')}/{a.Artifact}/{a.Version}/{a.Artifact}-{a.Version}-sources.jar";
        Console.WriteLine($"[fetch ] {url}");
        try
        {
            var bytes = await http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(jarPath, bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  WARN: could not download {a.Artifact}-{a.Version}: {ex.Message}");
            continue;
        }
    }

    Directory.CreateDirectory(extractDir);
    try
    {
        using var zip = ZipFile.OpenRead(jarPath);
        var extractRoot = Path.GetFullPath(extractDir) + Path.DirectorySeparatorChar;
        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.EndsWith(".kt", StringComparison.Ordinal)) continue;
            var dest = Path.GetFullPath(Path.Combine(extractDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            if (!dest.StartsWith(extractRoot, StringComparison.Ordinal))
            {
                Console.WriteLine($"  WARN: skipping zip entry outside extract dir: {entry.FullName}");
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            entry.ExtractToFile(dest, overwrite: true);
        }
        Console.WriteLine($"[done  ] {a.Artifact}-{a.Version}");
    }
    catch (InvalidDataException ex)
    {
        Console.WriteLine($"  WARN: {jarPath} not a valid zip: {ex.Message}");
    }
}

// ------------------------------------------------------------
// 2. Scan Kotlin sources
// ------------------------------------------------------------

var kotlinSymbols = new List<KotlinSymbol>();
foreach (var a in artifacts)
{
    var extractDir = Path.Combine(SourcesDir, $"{a.Artifact}-{a.Version}");
    if (!Directory.Exists(extractDir)) continue;
    foreach (var file in Directory.EnumerateFiles(extractDir, "*.kt", SearchOption.AllDirectories))
    {
        ScanKotlinFile(file, a.Module, kotlinSymbols);
    }
}

// Dedupe across the platform-common + -android source jars in the same module.
kotlinSymbols = kotlinSymbols
    .GroupBy(s => (s.Module, s.Package, s.Kind, s.Name, s.Receiver))
    .Select(g => g.OrderByDescending(s => s.ParamCount ?? 0).First())
    .ToList();

Console.WriteLine();
Console.WriteLine($"Kotlin symbols scanned: {kotlinSymbols.Count}");

// ------------------------------------------------------------
// 3. Scan PublicAPI.Unshipped.txt
// ------------------------------------------------------------

var csharpSymbols = ScanPublicApi(PublicApiPath);
Console.WriteLine($"C# public symbols:      {csharpSymbols.Count}");

// Indexes for fast lookup.
var csharpByShort = csharpSymbols
    .GroupBy(s => s.ShortName, StringComparer.OrdinalIgnoreCase)
    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

var csharpTypes = csharpSymbols
    .Where(s => s.Kind == CSharpKind.Type)
    .GroupBy(s => s.ShortName, StringComparer.OrdinalIgnoreCase)
    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

// ------------------------------------------------------------
// 4. Match Kotlin → Microsoft.AndroidX.Compose
// ------------------------------------------------------------

var matches = new List<Match>();
foreach (var k in kotlinSymbols)
{
    matches.Add(Classify(k, csharpTypes, csharpByShort));
}

// ------------------------------------------------------------
// 5. Write report
// ------------------------------------------------------------

Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
WriteReport(ReportPath, matches, csharpSymbols, artifacts.Select(a => a.Module).Distinct().ToList());

Console.WriteLine();
Console.WriteLine($"Report written: {ReportPath}");

// ==========================================================
// Helpers
// ==========================================================

static void ScanKotlinFile(string path, string module, List<KotlinSymbol> output)
{
    string src;
    try { src = File.ReadAllText(path); }
    catch { return; }

    src = StripCommentsAndStrings(src);

    var pkgMatch = Regex.Match(src, @"^\s*package\s+([\w.]+)", RegexOptions.Multiline);
    var package = pkgMatch.Success ? pkgMatch.Groups[1].Value : "";

    var lines = src.Split('\n');
    var pendingAnnotations = new List<string>();

    for (int i = 0; i < lines.Length; i++)
    {
        var raw = lines[i].TrimEnd('\r');
        // Top-level declarations live at column 0. Annotations on the line
        // immediately preceding the declaration are also at column 0.
        if (raw.Length == 0 || char.IsWhiteSpace(raw[0]))
            continue;

        // Annotation? Collect and continue.
        var annoMatch = Regex.Match(raw, @"^@(\w+)(?:\([^)]*\))?\s*$");
        if (annoMatch.Success)
        {
            pendingAnnotations.Add(annoMatch.Groups[1].Value);
            continue;
        }

        // Visibility filter — skip internal/private/protected declarations.
        if (Regex.IsMatch(raw, @"^(internal|private|protected)\b"))
        {
            pendingAnnotations.Clear();
            continue;
        }

        // Strip a leading "public " for matching simplicity.
        var afterVis = Regex.Replace(raw, @"^public\s+", "");

        // Modifier soup. Then the declaration kind keyword.
        var declRegex = @"^(?:(?:abstract|open|final|sealed|inline|infix|operator|tailrec|suspend|external|crossinline|noinline|expect|actual|annotation|data|enum|value|inner|companion|fun|override)\s+)*(fun|class|interface|object|typealias)\b";
        var declMatch = Regex.Match(afterVis, declRegex);
        if (!declMatch.Success)
        {
            // Could be a property/val/var at top level. Capture those too for completeness.
            var propMatch = Regex.Match(afterVis, @"^(?:const\s+)?(val|var)\s+(?:<[^>]+>\s+)?(?:([\w\.<>?,\s]+)\.)?(\w+)\b");
            if (propMatch.Success)
            {
                output.Add(new KotlinSymbol(
                    Module: module,
                    Package: package,
                    Kind: "property",
                    Name: propMatch.Groups[3].Value,
                    Receiver: propMatch.Groups[2].Success ? propMatch.Groups[2].Value.Trim() : null,
                    ParamCount: null,
                    Annotations: pendingAnnotations.ToList(),
                    IsExperimental: pendingAnnotations.Any(a => a.Contains("Experimental")),
                    IsDeprecated: pendingAnnotations.Contains("Deprecated"),
                    SourceFile: path));
            }
            pendingAnnotations.Clear();
            continue;
        }

        var kindKeyword = declMatch.Groups[1].Value;
        var beforeKind  = afterVis.Substring(0, declMatch.Groups[1].Index);

        string fullKind = kindKeyword;
        if (kindKeyword == "class")
        {
            if (Regex.IsMatch(beforeKind, @"\benum\b"))        fullKind = "enum class";
            else if (Regex.IsMatch(beforeKind, @"\bvalue\b"))  fullKind = "value class";
            else if (Regex.IsMatch(beforeKind, @"\bsealed\b")) fullKind = "sealed class";
            else if (Regex.IsMatch(beforeKind, @"\bdata\b"))   fullKind = "data class";
            else if (Regex.IsMatch(beforeKind, @"\bannotation\b")) fullKind = "annotation class";
            else if (Regex.IsMatch(beforeKind, @"\bcompanion\b")) { pendingAnnotations.Clear(); continue; }
        }
        else if (kindKeyword == "interface")
        {
            if (Regex.IsMatch(beforeKind, @"\bsealed\b"))    fullKind = "sealed interface";
            else if (Regex.IsMatch(beforeKind, @"\bfun\b"))  fullKind = "fun interface";
        }
        else if (kindKeyword == "object")
        {
            if (Regex.IsMatch(beforeKind, @"\bcompanion\b")) { pendingAnnotations.Clear(); continue; }
        }

        var afterKind = afterVis.Substring(declMatch.Index + declMatch.Length);

        string? receiver = null;
        string  name;
        int?    paramCount = null;

        if (kindKeyword == "fun")
        {
            afterKind = afterKind.TrimStart();
            // Drop leading type parameter clause: <T>, <reified T : Foo>, <T, U>...
            afterKind = StripBalanced(afterKind, '<', '>');
            // Receiver + name + (
            var nameMatch = Regex.Match(afterKind, @"^\s*(?:([^\s(`]+?)\.)?([\w`]+)\s*\(");
            if (!nameMatch.Success) { pendingAnnotations.Clear(); continue; }
            receiver = nameMatch.Groups[1].Success ? CleanReceiver(nameMatch.Groups[1].Value) : null;
            name     = nameMatch.Groups[2].Value.Trim('`');
            // Walk balanced ( ) across lines to count top-level commas.
            int parenAbs = lines[i].IndexOf('(', declMatch.Index);
            paramCount = parenAbs >= 0 ? CountTopLevelParams(lines, i, parenAbs) : null;
        }
        else if (kindKeyword == "typealias")
        {
            var nameMatch = Regex.Match(afterKind, @"^\s*(\w+)");
            if (!nameMatch.Success) { pendingAnnotations.Clear(); continue; }
            name = nameMatch.Groups[1].Value;
            fullKind = "typealias";
        }
        else
        {
            var nameMatch = Regex.Match(afterKind, @"^\s*(\w+)");
            if (!nameMatch.Success) { pendingAnnotations.Clear(); continue; }
            name = nameMatch.Groups[1].Value;
        }

        output.Add(new KotlinSymbol(
            Module: module,
            Package: package,
            Kind: fullKind,
            Name: name,
            Receiver: receiver,
            ParamCount: paramCount,
            Annotations: pendingAnnotations.ToList(),
            IsExperimental: pendingAnnotations.Any(a => a.Contains("Experimental")),
            IsDeprecated: pendingAnnotations.Contains("Deprecated"),
            SourceFile: path));

        pendingAnnotations.Clear();
    }
}

// Strip a leading <...> balanced clause (returns the rest unchanged if not present).
static string StripBalanced(string s, char open, char close)
{
    if (s.Length == 0 || s[0] != open) return s;
    int depth = 0;
    for (int i = 0; i < s.Length; i++)
    {
        if (s[i] == open) depth++;
        else if (s[i] == close)
        {
            depth--;
            if (depth == 0) return s.Substring(i + 1).TrimStart();
        }
    }
    return s;
}

// Trim type-args off the receiver (e.g. "List<T>" → "List"; "Modifier.Companion" stays).
static string CleanReceiver(string r)
{
    var s = r.Trim();
    int lt = s.IndexOf('<');
    if (lt >= 0) s = s.Substring(0, lt);
    return s.Trim('?');
}

// Count top-level params inside a balanced (...) starting at lines[startLine][openIdx].
static int CountTopLevelParams(string[] lines, int startLine, int openIdx)
{
    int depth = 0;
    int commas = 0;
    bool hasContent = false;
    for (int li = startLine; li < lines.Length; li++)
    {
        var line = lines[li];
        int start = (li == startLine) ? openIdx : 0;
        for (int ci = start; ci < line.Length; ci++)
        {
            char c = line[ci];
            // Skip Kotlin lambda arrow so `->` isn't counted as a closing `>`.
            if (c == '-' && ci + 1 < line.Length && line[ci + 1] == '>')
            {
                ci++;
                continue;
            }
            switch (c)
            {
                case '(': case '[': case '{': case '<':
                    depth++;
                    break;
                case ')': case ']': case '}': case '>':
                    depth--;
                    if (depth == 0 && c == ')')
                        return hasContent ? commas + 1 : 0;
                    break;
                case ',':
                    if (depth == 1) commas++;
                    break;
                default:
                    if (!char.IsWhiteSpace(c) && depth >= 1) hasContent = true;
                    break;
            }
        }
    }
    return hasContent ? commas + 1 : 0;
}

// Strip // ... and /* ... */ comments. Preserves newlines for line-number stability.
// Also masks string contents so `class` etc. inside literals don't fool the scanner.
static string StripCommentsAndStrings(string src)
{
    var sb = new StringBuilder(src.Length);
    int i = 0;
    while (i < src.Length)
    {
        char c = src[i];
        if (c == '/' && i + 1 < src.Length && src[i + 1] == '/')
        {
            while (i < src.Length && src[i] != '\n') i++;
        }
        else if (c == '/' && i + 1 < src.Length && src[i + 1] == '*')
        {
            i += 2;
            int depth = 1;
            while (i < src.Length && depth > 0)
            {
                if (i + 1 < src.Length && src[i] == '/' && src[i + 1] == '*') { depth++; i += 2; sb.Append("  "); }
                else if (i + 1 < src.Length && src[i] == '*' && src[i + 1] == '/') { depth--; i += 2; sb.Append("  "); }
                else { if (src[i] == '\n') sb.Append('\n'); else sb.Append(' '); i++; }
            }
        }
        else if (c == '"')
        {
            if (i + 2 < src.Length && src[i + 1] == '"' && src[i + 2] == '"')
            {
                sb.Append("\"\"\"");
                i += 3;
                while (i + 2 < src.Length && !(src[i] == '"' && src[i + 1] == '"' && src[i + 2] == '"'))
                {
                    sb.Append(src[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                if (i + 2 < src.Length) { sb.Append("\"\"\""); i += 3; }
            }
            else
            {
                sb.Append('"');
                i++;
                while (i < src.Length && src[i] != '"' && src[i] != '\n')
                {
                    if (src[i] == '\\' && i + 1 < src.Length) { sb.Append("  "); i += 2; continue; }
                    sb.Append(' ');
                    i++;
                }
                if (i < src.Length) { sb.Append(src[i]); i++; }
            }
        }
        else
        {
            sb.Append(c);
            i++;
        }
    }
    return sb.ToString();
}

static List<CSharpSymbol> ScanPublicApi(string path)
{
    var result = new List<CSharpSymbol>();
    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.TrimEnd();
        if (string.IsNullOrWhiteSpace(line)) continue;
        if (line.StartsWith("#"))            continue;
        // Strip nullability prefix indicator "~".
        if (line.StartsWith("~")) line = line.Substring(1);
        // Strip "static " prefix used by PublicApiAnalyzers for static members.
        if (line.StartsWith("static ")) line = line.Substring("static ".Length);
        // Strip "abstract " / "virtual " / "override " / "sealed " modifiers
        // that PublicApiAnalyzers sometimes emits on members.
        foreach (var mod in new[] { "abstract ", "virtual ", "override ", "sealed " })
        {
            if (line.StartsWith(mod)) { line = line.Substring(mod.Length); break; }
        }

        int arrowIdx = line.IndexOf(" -> ");
        var head = arrowIdx > 0 ? line.Substring(0, arrowIdx) : line;
        var tail = arrowIdx > 0 ? line.Substring(arrowIdx + 4) : "";

        // Type-only declaration: no '(', no '.get', no '.set', no ' -> '.
        if (arrowIdx < 0 && !head.Contains('(') && !head.EndsWith(".get") && !head.EndsWith(".set"))
        {
            var fqn = head.Trim();
            var shortName = StripGenerics(fqn.Split('.').Last());
            result.Add(new CSharpSymbol(fqn, shortName, fqn, CSharpKind.Type, line));
            continue;
        }

        int parenIdx = head.IndexOf('(');
        if (parenIdx > 0)
        {
            var beforeParen = head.Substring(0, parenIdx);
            int dotIdx = beforeParen.LastIndexOf('.');
            var typeFqn   = dotIdx > 0 ? beforeParen.Substring(0, dotIdx) : "";
            var memberPart = StripGenerics(dotIdx > 0 ? beforeParen.Substring(dotIdx + 1) : beforeParen);
            var typeShort = StripGenerics(typeFqn.Split('.').LastOrDefault() ?? "");
            var kind = string.Equals(typeShort, memberPart, StringComparison.Ordinal)
                ? CSharpKind.Constructor
                : CSharpKind.Method;
            result.Add(new CSharpSymbol(beforeParen, memberPart, typeFqn, kind, line));
            continue;
        }

        if (head.EndsWith(".get") || head.EndsWith(".set"))
        {
            var noGetSet = head.Substring(0, head.LastIndexOf('.'));
            int dotIdx = noGetSet.LastIndexOf('.');
            var typeFqn = dotIdx > 0 ? noGetSet.Substring(0, dotIdx) : "";
            var memberPart = StripGenerics(dotIdx > 0 ? noGetSet.Substring(dotIdx + 1) : noGetSet);
            result.Add(new CSharpSymbol(noGetSet, memberPart, typeFqn, CSharpKind.Property, line));
            continue;
        }

        // Field-like (e.g. const, static readonly) or other oddity.
        var lastDot = head.LastIndexOf('.');
        var member = StripGenerics(lastDot > 0 ? head.Substring(lastDot + 1) : head);
        var typeOnly = lastDot > 0 ? head.Substring(0, lastDot) : "";
        result.Add(new CSharpSymbol(head, member, typeOnly, CSharpKind.Field, line));
    }
    return result;
}

static string StripGenerics(string name)
{
    int lt = name.IndexOf('<');
    return lt >= 0 ? name.Substring(0, lt) : name;
}

static Match Classify(
    KotlinSymbol k,
    Dictionary<string, CSharpSymbol> csharpTypes,
    Dictionary<string, List<CSharpSymbol>> csharpByShort)
{
    // Skip annotation classes from coverage scoring (they're metadata for the
    // Kotlin compiler plugin, not a user-facing surface to reimplement in C#).
    if (k.Kind == "annotation class")
        return new Match(k, MatchStatus.NotApplicable, null, "annotation");

    // Skip typealiases — they don't correspond to first-class C# types.
    if (k.Kind == "typealias")
        return new Match(k, MatchStatus.NotApplicable, null, "typealias");

    // Composable: must have a class with the same name.
    bool isComposable = k.Annotations.Contains("Composable");

    // remember* state-holder builders → covered by a state-holder C# type.
    if (isComposable && k.Kind == "fun" && k.Name.StartsWith("remember", StringComparison.Ordinal))
    {
        var stateName = k.Name.Substring("remember".Length); // "rememberScrollState" → "ScrollState"
        if (string.IsNullOrEmpty(stateName))
            stateName = k.Name;
        if (csharpTypes.TryGetValue(stateName, out var hit))
            return new Match(k, MatchStatus.Covered, hit, $"covered by C# {hit.ShortName}");
        // Some facades expose remember* as a static method on Compose.cs.
        if (csharpByShort.TryGetValue(k.Name, out var hits))
            return new Match(k, MatchStatus.Covered, hits.First(), "covered by Compose.* static");
        return new Match(k, MatchStatus.Missing, null, $"no C# {stateName} state holder");
    }

    // Modifier.xxx extension functions → covered by a method on Microsoft.AndroidX.Compose.Modifier.
    if (k.Kind == "fun" && k.Receiver == "Modifier")
    {
        if (csharpByShort.TryGetValue(k.Name, out var hits))
        {
            var hit = hits.FirstOrDefault(h => h.TypeFqn.EndsWith("Modifier", StringComparison.Ordinal))
                   ?? hits.First();
            return new Match(k, MatchStatus.Covered, hit, "Modifier extension");
        }
        return new Match(k, MatchStatus.Missing, null, "no C# Modifier.* method");
    }

    // Other extension functions (e.g. LazyListScope.items) → covered when a
    // method of the same name exists on the receiver type.
    if (k.Kind == "fun" && k.Receiver is not null && k.Receiver != "Modifier")
    {
        if (csharpByShort.TryGetValue(k.Name, out var hits))
            return new Match(k, MatchStatus.Covered, hits.First(), $"{k.Receiver}.* extension");
        // Try the receiver as a type — coverage of the *receiver type* counts
        // the extension as covered-by-association.
        if (csharpTypes.ContainsKey(k.Receiver))
            return new Match(k, MatchStatus.PartialReceiverOnly, null, $"receiver {k.Receiver} present, ext missing");
        return new Match(k, MatchStatus.Missing, null, $"no C# {k.Receiver}.{k.Name}");
    }

    // Composable function or other top-level declaration → match by short name.
    if (csharpTypes.TryGetValue(k.Name, out var typeHit))
        return new Match(k, MatchStatus.Covered, typeHit, "type match");

    // Method/property fallback — many lowercase Kotlin functions (derivedStateOf,
    // produceState, rememberCoroutineScope, mutableStateListOf, …) map to a
    // PascalCase static method on Compose.cs rather than a dedicated facade type.
    if (k.Kind == "fun" && csharpByShort.TryGetValue(k.Name, out var methodHits))
    {
        var hit = methodHits.FirstOrDefault(h => h.Kind is CSharpKind.Method or CSharpKind.Property);
        if (hit is not null)
            return new Match(k, MatchStatus.Covered, hit, $"covered by `{hit.TypeFqn.Split('.').Last()}.{hit.ShortName}`");
    }

    // Companion-style facade: e.g. Kotlin object ButtonDefaults sometimes
    // shows up in C# as a static or as values folded into the facade.
    if (k.Kind == "object" && k.Name.EndsWith("Defaults", StringComparison.Ordinal))
    {
        // We treat *Defaults as "consumed" if its base facade (ButtonDefaults
        // → Button) exists in C#. Defaults objects rarely surface to users.
        var baseName = k.Name.Substring(0, k.Name.Length - "Defaults".Length);
        if (!string.IsNullOrEmpty(baseName) && csharpTypes.ContainsKey(baseName))
            return new Match(k, MatchStatus.ConsumedByFacade, csharpTypes[baseName], $"consumed by C# {baseName}");
        return new Match(k, MatchStatus.Missing, null, "no C# *Defaults object");
    }

    return new Match(k, MatchStatus.Missing, null, "no C# match");
}

static void WriteReport(string path, List<Match> matches, List<CSharpSymbol> csharpSymbols, List<string> moduleOrder)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Jetpack Compose ⇄ Microsoft.AndroidX.Compose API coverage");
    sb.AppendLine();
    sb.AppendLine($"Generated by `scripts/api-comparison.cs` on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.");
    sb.AppendLine();
    sb.AppendLine("Source of truth: AndroidX `-sources.jar` files for the Compose artifact ");
    sb.AppendLine("versions this repo references (see `Directory.Build.targets`). Re-run after ");
    sb.AppendLine("bumping a Compose package version. Coverage is symbol-level (short-name match).");
    sb.AppendLine();

    // Overall totals — exclude NotApplicable (annotations / typealiases).
    var scored = matches.Where(m => m.Status != MatchStatus.NotApplicable).ToList();
    var coveredAll = scored.Count(m => m.Status == MatchStatus.Covered || m.Status == MatchStatus.ConsumedByFacade);
    double pctAll = scored.Count == 0 ? 0 : 100.0 * coveredAll / scored.Count;

    sb.AppendLine("## Summary");
    sb.AppendLine();
    sb.AppendLine($"- **Total Kotlin public symbols in scope**: {scored.Count}");
    sb.AppendLine($"- **Covered by Microsoft.AndroidX.Compose (any kind)**: {coveredAll} (**{pctAll:0.0}%**)");
    sb.AppendLine();

    // Composables-only sub-summary (the most user-facing slice).
    var composables = scored.Where(m => m.Kotlin.Annotations.Contains("Composable") && m.Kotlin.Kind == "fun").ToList();
    var coveredComposables = composables.Count(m => m.Status == MatchStatus.Covered || m.Status == MatchStatus.ConsumedByFacade);
    double pctComposables = composables.Count == 0 ? 0 : 100.0 * coveredComposables / composables.Count;
    sb.AppendLine($"- **@Composable functions**: {coveredComposables} / {composables.Count} (**{pctComposables:0.0}%**)");
    sb.AppendLine();

    // Per-module table.
    sb.AppendLine("### Per-module coverage");
    sb.AppendLine();
    sb.AppendLine("| Module | Composables | All public |");
    sb.AppendLine("| --- | --- | --- |");
    foreach (var m in moduleOrder)
    {
        var moduleMatches = scored.Where(x => x.Kotlin.Module == m).ToList();
        var modAll = moduleMatches.Count;
        var modCov = moduleMatches.Count(x => x.Status == MatchStatus.Covered || x.Status == MatchStatus.ConsumedByFacade);
        var modComp = moduleMatches.Where(x => x.Kotlin.Annotations.Contains("Composable") && x.Kotlin.Kind == "fun").ToList();
        var modCompCov = modComp.Count(x => x.Status == MatchStatus.Covered || x.Status == MatchStatus.ConsumedByFacade);
        sb.AppendLine($"| `{m}` | {modCompCov}/{modComp.Count} ({(modComp.Count == 0 ? 0 : 100.0 * modCompCov / modComp.Count):0}%) | {modCov}/{modAll} ({(modAll == 0 ? 0 : 100.0 * modCov / modAll):0}%) |");
    }
    sb.AppendLine();

    // Per-module detail sections.
    foreach (var module in moduleOrder)
    {
        var moduleMatches = scored.Where(x => x.Kotlin.Module == module).ToList();
        if (moduleMatches.Count == 0) continue;

        sb.AppendLine($"## `{module}`");
        sb.AppendLine();

        WriteSubsection(sb, "@Composable functions", moduleMatches.Where(m => m.Kotlin.Annotations.Contains("Composable") && m.Kotlin.Kind == "fun"));
        WriteSubsection(sb, "Other top-level functions", moduleMatches.Where(m => !m.Kotlin.Annotations.Contains("Composable") && m.Kotlin.Kind == "fun"));
        WriteSubsection(sb, "Classes", moduleMatches.Where(m => m.Kotlin.Kind is "class" or "abstract class" or "open class" or "sealed class" or "data class"));
        WriteSubsection(sb, "Interfaces", moduleMatches.Where(m => m.Kotlin.Kind is "interface" or "sealed interface" or "fun interface"));
        WriteSubsection(sb, "Objects (incl. Defaults)", moduleMatches.Where(m => m.Kotlin.Kind == "object"));
        WriteSubsection(sb, "Enums", moduleMatches.Where(m => m.Kotlin.Kind == "enum class"));
        WriteSubsection(sb, "Value classes", moduleMatches.Where(m => m.Kotlin.Kind == "value class"));
        WriteSubsection(sb, "Top-level properties", moduleMatches.Where(m => m.Kotlin.Kind == "property"));
    }

    // C#-only symbols (in PublicAPI.Unshipped.txt but not matched to any Kotlin symbol).
    var matchedCsharpFqns = new HashSet<string>(matches
        .Where(m => m.CSharp is not null)
        .Select(m => m.CSharp!.FullyQualifiedName), StringComparer.OrdinalIgnoreCase);

    // Short names of all matched C# types — used to suppress nested types
    // whose parent already matched a Kotlin type (e.g. Microsoft.AndroidX.Compose.Alignment.Horizontal
    // is a wrapper for Kotlin's nested Alignment.Horizontal interface, which our
    // top-level-only Kotlin scanner doesn't see).
    var matchedTypeShortNames = new HashSet<string>(matches
        .Where(m => m.CSharp is { Kind: CSharpKind.Type })
        .Select(m => m.CSharp!.ShortName), StringComparer.OrdinalIgnoreCase);

    // Names to always exclude from the C#-only review list (project plumbing,
    // not a Compose wrapper).
    var alwaysExclude = new HashSet<string>(StringComparer.Ordinal)
    {
        "Microsoft.AndroidX.Compose.Resource",        // Android auto-generated resources class
        "Microsoft.AndroidX.Compose",         // host for Remember/RememberSaveable/Effect statics
        "Microsoft.AndroidX.Compose.ComposeExtensions", // extension methods on IComposer / ComponentActivity / ComposeView
        "Microsoft.AndroidX.Compose.ComposableNode",  // facade base class
        "Microsoft.AndroidX.Compose.ComposableContainer",
        "Microsoft.AndroidX.Compose.RenderContext",
        "Microsoft.AndroidX.Compose.SourceLocationKey",
    };

    var csharpOnly = csharpSymbols
        .Where(s => s.Kind == CSharpKind.Type)
        .Where(s => !matchedCsharpFqns.Contains(s.FullyQualifiedName))
        .Where(s => !alwaysExclude.Contains(s.FullyQualifiedName))
        // Skip nested types whose parent already matched a Kotlin type.
        .Where(s =>
        {
            // FQN like Microsoft.AndroidX.Compose.Alignment.Horizontal → parent = Alignment.
            // We treat it as nested if the FQN has more than 2 dot segments.
            var parts = s.FullyQualifiedName.Split('.');
            if (parts.Length <= 2) return true;
            var parentShort = parts[^2];
            return !matchedTypeShortNames.Contains(parentShort);
        })
        .OrderBy(s => s.ShortName)
        .ToList();

    sb.AppendLine();
    sb.AppendLine("## Microsoft.AndroidX.Compose-only types (no Kotlin counterpart in scope)");
    sb.AppendLine();
    sb.AppendLine("Some of these are intentional infrastructure (`ComposableLambda*`, `RenderContext`, ");
    sb.AppendLine("`MutableNumberState<T>`, etc.); others may indicate rename drift or a wrapper for ");
    sb.AppendLine("a Compose API the matcher didn't pair up. ");
    sb.AppendLine();
    sb.AppendLine("> Note: a handful of facades (`FlexibleBottomAppBar`, `MediumFlexibleTopAppBar`, ");
    sb.AppendLine("> `LargeFlexibleTopAppBar`) wrap `internal fun` Kotlin symbols. They're real on the JVM ");
    sb.AppendLine("> (the JVM `internal` modifier is a name-mangling marker, not a visibility barrier) but ");
    sb.AppendLine("> not part of Kotlin's public API surface — Kotlin developers technically can't call them. ");
    sb.AppendLine();
    foreach (var s in csharpOnly)
        sb.AppendLine($"- `{s.FullyQualifiedName}`");

    // Differences worth fixing (composables where C# is significantly thinner).
    sb.AppendLine();
    sb.AppendLine("## Differences worth investigating");
    sb.AppendLine();
    sb.AppendLine("Composables where C# wraps the Kotlin call but the public surface differs notably ");
    sb.AppendLine("(by parameter count). This is a coarse signal — most diffs are intentional ");
    sb.AppendLine("(C# folds Kotlin's `modifier`, `colors`, `elevation`, `border`, `interactionSource`, ");
    sb.AppendLine("etc. into facade defaults), but large deltas may flag missing slots.");
    sb.AppendLine();
    sb.AppendLine("| Module | Composable | Kotlin params | Notes |");
    sb.AppendLine("| --- | --- | --- | --- |");
    foreach (var m in scored
        .Where(x => x.Status == MatchStatus.Covered)
        .Where(x => x.Kotlin.Annotations.Contains("Composable") && x.Kotlin.Kind == "fun")
        .Where(x => (x.Kotlin.ParamCount ?? 0) >= 8)
        .OrderByDescending(x => x.Kotlin.ParamCount ?? 0)
        .Take(60))
    {
        sb.AppendLine($"| `{m.Kotlin.Module}` | `{m.Kotlin.Name}` | {m.Kotlin.ParamCount} | C# wrapper: `{m.CSharp?.ShortName}` — review slot coverage |");
    }

    sb.AppendLine();
    sb.AppendLine("## Caveats");
    sb.AppendLine();
    sb.AppendLine("- Matching is **short-name** based. A Kotlin `Button` matches a C# `Button` ");
    sb.AppendLine("  regardless of namespace nesting; collisions are possible but rare in Compose.");
    sb.AppendLine("- Overloads on the Kotlin side collapse to one entry (the one with the most params).");
    sb.AppendLine("- Modifier extensions only match if a method of the same name exists in PublicAPI; ");
    sb.AppendLine("  the matcher doesn't check the extension's parameter shape.");
    sb.AppendLine("- `@ExperimentalFoundationApi`, `@ExperimentalMaterial3Api`, etc. annotations are ");
    sb.AppendLine("  preserved in symbol metadata but don't currently affect scoring.");
    sb.AppendLine("- See `scripts/api-comparison.cs` for the full set of heuristics.");

    File.WriteAllText(path, sb.ToString());
}

static void WriteSubsection(StringBuilder sb, string title, IEnumerable<Match> matches)
{
    var list = matches.OrderBy(m => m.Kotlin.Name, StringComparer.OrdinalIgnoreCase).ToList();
    if (list.Count == 0) return;

    int covered = list.Count(m => m.Status == MatchStatus.Covered || m.Status == MatchStatus.ConsumedByFacade);
    sb.AppendLine($"### {title} — {covered}/{list.Count} ({(list.Count == 0 ? 0 : 100.0 * covered / list.Count):0}%)");
    sb.AppendLine();
    foreach (var m in list)
    {
        var box = (m.Status == MatchStatus.Covered || m.Status == MatchStatus.ConsumedByFacade) ? "x" : " ";
        var marker = m.Kotlin.IsExperimental ? " *(experimental)*" : "";
        var deprecated = m.Kotlin.IsDeprecated ? " *(deprecated)*" : "";
        var display = m.Kotlin.Receiver is null ? m.Kotlin.Name : $"{m.Kotlin.Receiver}.{m.Kotlin.Name}";
        var paramSuffix = m.Kotlin.ParamCount is int n ? $"({n})" : "";
        var note = m.Status switch
        {
            MatchStatus.Covered          => m.Notes is not null ? $" → {m.Notes}" : "",
            MatchStatus.ConsumedByFacade => $" → {m.Notes}",
            MatchStatus.PartialReceiverOnly => $" → {m.Notes}",
            MatchStatus.Missing          => "",
            _                            => $" → {m.Notes}"
        };
        sb.AppendLine($"- [{box}] `{display}`{paramSuffix}{marker}{deprecated}{note}");
    }
    sb.AppendLine();
}

// ==========================================================
// Records
// ==========================================================

record KotlinSymbol(
    string Module,
    string Package,
    string Kind,
    string Name,
    string? Receiver,
    int? ParamCount,
    List<string> Annotations,
    bool IsExperimental,
    bool IsDeprecated,
    string SourceFile);

enum CSharpKind { Type, Constructor, Method, Property, Field }

record CSharpSymbol(
    string FullyQualifiedName,
    string ShortName,
    string TypeFqn,
    CSharpKind Kind,
    string Raw);

enum MatchStatus
{
    Covered,
    ConsumedByFacade,
    PartialReceiverOnly,
    Missing,
    NotApplicable
}

record Match(KotlinSymbol Kotlin, MatchStatus Status, CSharpSymbol? CSharp, string? Notes);
