// scripts/manual-jni-report.cs
//
// Manual JNI surface report generator for Microsoft.AndroidX.Compose.
//
// Run from repo root:
//
//     dotnet run scripts/manual-jni-report.cs
//
// What it does:
//   1. Walks every C# source file under src/Microsoft.AndroidX.Compose/.
//   2. Detects raw JNI usage (any `JNIEnv.*` call, optionally
//      qualified `Android.Runtime.JNIEnv.*`) and Java Callable
//      Wrappers (`[Register("net/compose/...")]` classes).
//   3. Classifies each JNI call site by nearest enclosing C# member
//      (method or constructor), skipping members decorated with
//      `[ComposeBridge]` / `[ComposeFacade]` because those bodies are
//      emitted by the source generators.
//   4. Pulls a "Purpose" paragraph and a "Why not generated?" rationale
//      from existing XML doc <remarks>/<summary> blocks and adjacent
//      `// ...` comments, falling back to "TODO: document" when the
//      source doesn't already explain itself.
//   5. Writes docs/manual-jni.md — checked in deliberately so PR diffs
//      surface migrations to the bridge generator.
//
// Strategy: lightweight regex/text scan, no Roslyn — same approach as
// scripts/api-comparison.cs. The repo's "one type per file" discipline
// makes regex sufficient.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

const string SourceRoot = "src/Microsoft.AndroidX.Compose";
const string ReportPath = "docs/manual-jni.md";

// --- Regex catalog ----------------------------------------------------------

// Generic JNI call detector — `JNIEnv.X` or `Android.Runtime.JNIEnv.X`.
// We capture the call name for evidence; counts in the report use this.
var rxJni = new Regex(
    @"(?:Android\.Runtime\.)?JNIEnv\.(\w+)\s*\(",
    RegexOptions.Compiled);

// `[Register("net/compose/...")]` — Microsoft.AndroidX.Compose's own JCWs, distinguished
// from binding-generator-emitted [Register] attributes by the
// "net/compose/" prefix the project uses for its own JNI namespace.
var rxRegister = new Regex(
    @"\[Register\(""net/compose/[^""]+""\)\]",
    RegexOptions.Compiled);

// Class declaration — must follow a `[Register("net/compose/...")]`
// line for us to count it as a JCW.
var rxClassDecl = new Regex(
    @"^\s*(?:public|internal|private|sealed|abstract|static|partial|\s)*\bclass\s+(\w+)\s*(?::\s*(.+?))?(?:\s*\{|$)",
    RegexOptions.Compiled);

// Member declarations we anchor JNI sites to. Order matters — try
// constructor first, then methods (covers `static unsafe`, no-modifier,
// instance, etc.). The `(` may close on the same line OR on a later
// line; multi-line parameter lists are common in this codebase
// (`ModifierPointerInput`, `ExposedDropdownMenu`, `DetectTapGestures`,
// …) and we must not lose them.
var rxCtor = new Regex(
    @"^\s*(?:public|internal|private|protected|static|unsafe|partial|new|\s)*\b(?<name>[A-Z]\w+)\s*\(",
    RegexOptions.Compiled);
var rxMethod = new Regex(
    @"^\s*(?:public|internal|private|protected|static|unsafe|partial|virtual|override|sealed|abstract|extern|async|new|readonly|\s)*" +
    @"(?<ret>[\w<>?\[\]\.]+\??)\s+(?<name>\w+)\s*(?:<[^>]+>)?\s*\(",
    RegexOptions.Compiled);

// Generator-emitted markers — anything decorated with these has a
// generator-emitted body and must be skipped.
var rxGeneratorAttr = new Regex(
    @"\[(?:ComposeBridge|ComposeFacade|ComposeDefaults)\b",
    RegexOptions.Compiled);

// Stable cross-references inside leading comments — e.g. issue links,
// `dotnet/java-interop#1440`, `dotnet/android-libraries#NNN`, `#NN`.
// Used to derive a "Migration path" hint when the source doesn't have
// an explicit `// Migration:` line.
var rxIssueRef = new Regex(
    @"(?<repo>(?:dotnet/[\w\-]+))#(?<num>\d+)|(?<bare>#\d+)",
    RegexOptions.Compiled);

// --- Walk the source tree ---------------------------------------------------

if (!Directory.Exists(SourceRoot))
{
    Console.Error.WriteLine($"ERROR: cannot find {SourceRoot} (run from repo root)");
    return 1;
}

var files = Directory
    .EnumerateFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
    .OrderBy(f => f, StringComparer.Ordinal)
    .ToList();

var perFile        = new List<FileReport>();
var jcwClasses     = new List<JcwClass>();
int totalComposeBridge = 0;
int totalComposeFacade = 0;
int totalJniSites      = 0;

foreach (var path in files)
{
    var rel = path.Replace('\\', '/');
    // Trim the cwd prefix if the script was launched with absolute paths.
    var cwd = Directory.GetCurrentDirectory().Replace('\\', '/');
    if (rel.StartsWith(cwd + "/", StringComparison.Ordinal))
        rel = rel[(cwd.Length + 1)..];

    var text = File.ReadAllText(path);
    var lines = text.Replace("\r\n", "\n").Split('\n');
    // Block-comment-masked view used for code-shape analysis (JNI call
    // sites, member spans, JCW class lookup). Comment-prose extraction
    // (ExtractPurpose / ExtractWhy / ExtractMigration) keeps reading
    // `lines` so `///` and `// Why raw JNI` markers stay intact.
    var codeLines = MaskBlockComments(lines);

    // Tally generator-emitted partials regardless of whether the file
    // also contains hand-written JNI.
    int composeBridgeHere = CountAttribute(text, "ComposeBridge");
    int composeFacadeHere = CountAttribute(text, "ComposeFacade");
    totalComposeBridge += composeBridgeHere;
    totalComposeFacade += composeFacadeHere;

    var jniSites = FindJniSites(codeLines, rxJni);
    var jcws     = FindJcwClasses(codeLines, rxRegister, rxClassDecl);
    foreach (var j in jcws)
    {
        // Determine whether this JCW class itself contains JNI (used
        // to pick which table column it lights up in the report).
        bool hasJni = jniSites.Any(s => s.Line >= j.StartLine && s.Line <= j.EndLine);
        jcwClasses.Add(j with { HasJni = hasJni, RelativePath = rel });
    }

    if (jniSites.Count == 0 && jcws.Count == 0)
        continue; // skip files with neither manual JNI nor a JCW

    totalJniSites += jniSites.Count;

    // Anchor each JNI site to its enclosing member. Members decorated
    // with [ComposeBridge]/[ComposeFacade] are excluded — generator
    // emits their bodies, and any "JNI" in source there would be a
    // contract violation we want surfaced separately, not folded into
    // "manual" tallies.
    var members = ScanMembers(codeLines, rxCtor, rxMethod, rxGeneratorAttr);

    var entries = new List<MemberEntry>();
    foreach (var m in members)
    {
        var sitesInMember = jniSites
            .Where(s => s.Line >= m.StartLine && s.Line <= m.EndLine)
            .ToList();
        if (sitesInMember.Count == 0)
            continue;
        if (m.IsGenerated)
            continue;
        entries.Add(new MemberEntry(
            Name: m.Name,
            Kind: m.Kind,
            StartLine: m.StartLine,
            EndLine: m.EndLine,
            JniCallCount: sitesInMember.Count,
            Why: ExtractWhy(lines, m.StartLine),
            Migration: ExtractMigration(lines, m.StartLine, rxIssueRef)));
    }

    // Any JNI sites NOT covered by an enumerated member (rare —
    // class-static initializers, lambdas inside fields). Bucket those
    // under one or more synthetic "(file-level)" entries so no call
    // disappears. Group adjacent orphans (gap ≤ 50 lines) into a single
    // entry; otherwise emit separate entries so the line ranges stay
    // tight and the report doesn't visually merge unrelated regions.
    var enumeratedRanges = entries.Select(e => (e.StartLine, e.EndLine)).ToList();
    var orphaned = jniSites
        .Where(s => !enumeratedRanges.Any(r => s.Line >= r.StartLine && s.Line <= r.EndLine))
        .OrderBy(s => s.Line)
        .ToList();
    if (orphaned.Count > 0)
    {
        // Confirm whether any orphans land inside a generator-emitted
        // member — if so, that's a generator/source mismatch worth
        // flagging in the entry name.
        bool anyInGenerated = members.Any(m => m.IsGenerated &&
            orphaned.Any(s => s.Line >= m.StartLine && s.Line <= m.EndLine));
        var label = anyInGenerated
            ? "(file-level / partly inside generator-decorated member!)"
            : "(file-level)";

        const int maxGap = 50;
        int groupStart = 0;
        for (int gi = 1; gi <= orphaned.Count; gi++)
        {
            bool flush = gi == orphaned.Count
                || orphaned[gi].Line - orphaned[gi - 1].Line > maxGap;
            if (flush)
            {
                var slice = orphaned.GetRange(groupStart, gi - groupStart);
                entries.Add(new MemberEntry(
                    Name:         label,
                    Kind:         "init",
                    StartLine:    slice[0].Line,
                    EndLine:      slice[^1].Line,
                    JniCallCount: slice.Count,
                    Why:          "TODO: document",
                    Migration:    "TODO"));
                groupStart = gi;
            }
        }
    }

    perFile.Add(new FileReport(
        RelativePath: rel,
        Purpose:      ExtractPurpose(lines),
        Entries:      entries,
        JniSites:     jniSites,
        Jcws:         jcws,
        ComposeBridgeCount: composeBridgeHere,
        ComposeFacadeCount: composeFacadeHere));
}

// --- Aggregate metrics ------------------------------------------------------

int filesScanned   = files.Count;
int filesFlagged   = perFile.Count;
int totalJcw       = jcwClasses.Count;
int jcwsWithJni    = jcwClasses.Count(j => j.HasJni);
int jcwsManagedOnly = totalJcw - jcwsWithJni;

int manualLineSpan = perFile.SelectMany(f => f.Entries)
    .Sum(e => e.EndLine - e.StartLine + 1);

// --- Write report -----------------------------------------------------------

Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);

var sb = new StringBuilder();
sb.AppendLine("# Manual JNI surface — Microsoft.AndroidX.Compose");
sb.AppendLine();
sb.AppendLine($"Generated by `scripts/manual-jni-report.cs` on " +
    $"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC.");
sb.AppendLine();
sb.AppendLine("Source of truth: `src/Microsoft.AndroidX.Compose/*.cs`. The script flags ");
sb.AppendLine("every C# member that calls `JNIEnv.*` directly, plus every Java ");
sb.AppendLine("Callable Wrapper the project registers under `net/compose/...`. ");
sb.AppendLine("Members whose bodies are emitted by the source generators ");
sb.AppendLine("(`[ComposeBridge]`, `[ComposeFacade]`) are deliberately excluded — ");
sb.AppendLine("the report measures **what's left for the generator to absorb**.");
sb.AppendLine();
sb.AppendLine("## Summary");
sb.AppendLine();
sb.AppendLine($"- C# files scanned: **{filesScanned}**");
sb.AppendLine($"- Files containing manual JNI or a JCW: **{filesFlagged}**");
sb.AppendLine($"- Manual `JNIEnv.*` call sites: **{totalJniSites}**");
sb.AppendLine($"- Lines inside flagged members: **{manualLineSpan}**");
sb.AppendLine($"- Java Callable Wrappers (`[Register(\"net/compose/…\")]`): **{totalJcw}** ({jcwsWithJni} with raw JNI inside, {jcwsManagedOnly} pure-managed delegate adapters)");
sb.AppendLine($"- Generator-emitted `[ComposeBridge]` partials: **{totalComposeBridge}**");
sb.AppendLine($"- Generator-emitted `[ComposeFacade]` partials: **{totalComposeFacade}**");
if (totalComposeBridge + totalComposeFacade > 0)
{
    double generatorTotal = totalComposeBridge + totalComposeFacade;
    double ratio = generatorTotal == 0 ? 0 : (double)totalJniSites / generatorTotal;
    sb.AppendLine($"- Manual JNI : generator-emitted ratio: **{ratio:F2}** (`{totalJniSites}` raw-JNI calls vs `{(int)generatorTotal}` generator-emitted partials)");
}
sb.AppendLine();

sb.AppendLine("## Per-file detail");
sb.AppendLine();
foreach (var f in perFile)
{
    sb.AppendLine($"### `{f.RelativePath}`");
    sb.AppendLine();
    sb.AppendLine($"**Purpose.** {f.Purpose}");
    sb.AppendLine();

    if (f.ComposeBridgeCount > 0 || f.ComposeFacadeCount > 0)
    {
        var parts = new List<string>();
        if (f.ComposeBridgeCount > 0) parts.Add($"{f.ComposeBridgeCount} `[ComposeBridge]`");
        if (f.ComposeFacadeCount > 0) parts.Add($"{f.ComposeFacadeCount} `[ComposeFacade]`");
        sb.AppendLine($"_File also contributes {string.Join(" + ", parts)} generator-emitted partials, excluded from this section._");
        sb.AppendLine();
    }

    if (f.Entries.Count == 0)
    {
        sb.AppendLine("_No hand-written JNI members; JCW(s) only — see table below._");
        sb.AppendLine();
    }
    else
    {
        sb.AppendLine($"**Manual entry points** ({f.Entries.Count}):");
        sb.AppendLine();
        sb.AppendLine("| Member | Kind | Lines | JNI calls | Why not generated? | Migration |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var e in f.Entries.OrderBy(x => x.StartLine))
        {
            sb.AppendLine(
                $"| `{e.Name}` | {e.Kind} | {e.StartLine}–{e.EndLine} | " +
                $"{e.JniCallCount} | {Escape(e.Why)} | {Escape(e.Migration)} |");
        }
        sb.AppendLine();
    }
}

sb.AppendLine("## JCW classes");
sb.AppendLine();
sb.AppendLine("Every Microsoft.AndroidX.Compose Java Callable Wrapper — classes registered under ");
sb.AppendLine("the project's own `net/compose/` JNI namespace. They lower C# delegates ");
sb.AppendLine("to Kotlin `IFunctionN` shapes the binding generator can't synthesize.");
sb.AppendLine();
sb.AppendLine("| Class | Implements | File:line | Has raw JNI? |");
sb.AppendLine("| --- | --- | --- | --- |");
foreach (var j in jcwClasses.OrderBy(j => j.ClassName, StringComparer.Ordinal))
{
    var implements = string.IsNullOrWhiteSpace(j.Implements) ? "—" : Escape(j.Implements);
    sb.AppendLine($"| `{j.ClassName}` | `{implements}` | `{j.RelativePath}:{j.StartLine}` | {(j.HasJni ? "yes" : "no")} |");
}
sb.AppendLine();

sb.AppendLine("## Caveats");
sb.AppendLine();
sb.AppendLine("- **Heuristic, not semantic.** The classifier is regex-based. Member ");
sb.AppendLine("  detection covers methods, constructors, and property accessors but ");
sb.AppendLine("  may misattribute a JNI call inside an unusual member shape (lambda ");
sb.AppendLine("  bodies stored in fields, expression-bodied members spanning many ");
sb.AppendLine("  lines). Such calls land under a synthetic `(file-level)` row so no ");
sb.AppendLine("  call disappears.");
sb.AppendLine("- **`[ComposeBridge]` / `[ComposeFacade]` exclusion** is by attribute ");
sb.AppendLine("  presence on the immediately preceding lines. If a hand-written ");
sb.AppendLine("  member sits next to a generator-decorated one in the same file, both ");
sb.AppendLine("  classifications are emitted independently.");
sb.AppendLine("- **\"Why not generated?\"** is extracted from existing source comments ");
sb.AppendLine("  — XML doc `<remarks>` paragraphs containing _Why raw JNI_, ");
sb.AppendLine("  `// Why raw JNI:` lines, or adjacent `// ...` blocks. Entries that ");
sb.AppendLine("  show **TODO** are docs gaps in the source itself.");
sb.AppendLine("- **Call sites, not unique JVM symbols.** A cached `static IntPtr s_X` ");
sb.AppendLine("  bound once and used three times counts as three call sites.");
sb.AppendLine("- **Microsoft.AndroidX.Compose JCWs only.** The JCW table tracks `[Register(\"net/compose/…\")]`, ");
sb.AppendLine("  the namespace this project owns. JCW shapes the Mono.Android binding ");
sb.AppendLine("  generator emits for upstream interfaces are not flagged.");

File.WriteAllText(ReportPath, sb.ToString());

Console.WriteLine();
Console.WriteLine($"Files scanned:           {filesScanned}");
Console.WriteLine($"Files with manual JNI:   {filesFlagged}");
Console.WriteLine($"JNIEnv.* call sites:     {totalJniSites}");
Console.WriteLine($"JCW classes:             {totalJcw} ({jcwsWithJni} with JNI, {jcwsManagedOnly} pure-managed)");
Console.WriteLine($"[ComposeBridge] partials: {totalComposeBridge}");
Console.WriteLine($"[ComposeFacade] partials: {totalComposeFacade}");
Console.WriteLine();
Console.WriteLine($"Report written: {ReportPath}");
return 0;

// ===========================================================================
// Helpers
// ===========================================================================

static int CountAttribute(string text, string name)
{
    var rx = new Regex($@"\[{Regex.Escape(name)}\b", RegexOptions.Compiled);
    return rx.Matches(text).Count;
}

static List<JniSite> FindJniSites(string[] lines, Regex rx)
{
    var output = new List<JniSite>();
    for (int i = 0; i < lines.Length; i++)
    {
        var line = StripLineComment(lines[i]);
        foreach (Match m in rx.Matches(line))
        {
            output.Add(new JniSite(Line: i + 1, CallName: m.Groups[1].Value));
        }
    }
    return output;
}

static List<JcwClass> FindJcwClasses(string[] lines, Regex rxRegister, Regex rxClassDecl)
{
    var output = new List<JcwClass>();
    for (int i = 0; i < lines.Length; i++)
    {
        if (!rxRegister.IsMatch(lines[i])) continue;
        // Scan forward to the next class declaration, skipping blank
        // lines, doc comments (`///`), and additional attributes. Stop
        // at the first non-skippable line — if it's not the class
        // declaration, we've lost the JCW and bail rather than guess.
        for (int j = i + 1; j < lines.Length; j++)
        {
            var t = lines[j].TrimStart();
            if (t.Length == 0) continue;
            if (t.StartsWith("//")) continue;             // covers `///`
            if (t.StartsWith("/*") || t.StartsWith("*"))  continue;
            if (t.StartsWith("["))  continue;             // another attribute
            var m = rxClassDecl.Match(lines[j]);
            if (!m.Success) break;                        // unexpected: lost the JCW
            var name = m.Groups[1].Value;
            var bases = m.Groups[2].Success ? m.Groups[2].Value.Trim().TrimEnd('{').Trim() : "";
            // Implements = trim "Java.Lang.Object," prefix and pick the
            // remainder; it's almost always a single IFunctionN.
            var implements = ExtractImplements(bases);
            // Locate the matching closing brace to derive line range.
            int endLine = FindClassEnd(lines, j);
            output.Add(new JcwClass(
                ClassName:    name,
                Implements:   implements,
                StartLine:    j + 1,
                EndLine:      endLine,
                HasJni:       false,            // filled by caller
                RelativePath: ""));              // filled by caller
            break;
        }
    }
    return output;
}

static string ExtractImplements(string bases)
{
    if (string.IsNullOrWhiteSpace(bases)) return "";
    var parts = bases.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    // Drop the base class (first entry) if it's an Object derivative, keep interfaces.
    var keep = parts.Where(p => !p.Equals("Java.Lang.Object", StringComparison.Ordinal))
                    .ToList();
    return string.Join(", ", keep);
}

static int FindClassEnd(string[] lines, int classDeclLine)
{
    int depth = 0;
    bool started = false;
    for (int i = classDeclLine; i < lines.Length; i++)
    {
        foreach (var ch in MaskStringsAndChars(StripLineComment(lines[i])))
        {
            if (ch == '{') { depth++; started = true; }
            else if (ch == '}') { depth--; if (started && depth == 0) return i + 1; }
        }
    }
    return lines.Length;
}

static List<MemberSpan> ScanMembers(string[] lines, Regex rxCtor, Regex rxMethod, Regex rxGen)
{
    var output = new List<MemberSpan>();
    var classStack = new Stack<(string Name, int Depth)>();
    int depth = 0;

    int i = 0;
    while (i < lines.Length)
    {
        var raw = lines[i];
        var line = MaskStringsAndChars(StripLineComment(raw));

        // Capture the brace depth at the start of this line. We use it
        // to gate member detection so only declarations sitting
        // directly inside a class body (depth == enclosing class
        // depth + 1) match — guards against method-shaped statements
        // inside method bodies (local functions, nested lambdas
        // formatted onto one line, etc.).
        int depthAtLineStart = depth;

        // Track class scope depth so we can detect ctor names by class.
        var classMatch = Regex.Match(line, @"^\s*(?:public|internal|private|protected|sealed|abstract|static|partial|\s)*\bclass\s+(\w+)\b");
        if (classMatch.Success)
        {
            classStack.Push((classMatch.Groups[1].Value, depth));
        }

        // Open/close brace counting for current line — done after class
        // detection so the `class X { … }` line opens its own scope.
        foreach (var ch in line)
        {
            if (ch == '{') depth++;
            else if (ch == '}')
            {
                depth--;
                while (classStack.Count > 0 && classStack.Peek().Depth >= depth)
                    classStack.Pop();
            }
        }

        // Only consider member declarations directly inside a class
        // body. Inside method bodies depthAtLineStart will be deeper
        // than classStack.Peek().Depth + 1, so we skip cheaply.
        bool atMemberDepth = classStack.Count > 0
            && depthAtLineStart == classStack.Peek().Depth + 1;
        if (!atMemberDepth)
        {
            i++;
            continue;
        }

        if (HasGeneratorAttrAbove(lines, i, rxGen))
        {
            // Skip the entire generator-decorated declaration. A
            // partial without a body is a one-liner; if there's a
            // body we still want to record the span as "generated"
            // so orphan-detection knows to treat any JNI calls in it
            // as anomalies.
            int memberStart = i;
            int memberEnd = FindMemberEnd(lines, i);
            string name = TryGetMemberName(line, classStack);
            output.Add(new MemberSpan(
                Name:        name,
                Kind:        "generator",
                StartLine:   memberStart + 1,
                EndLine:     memberEnd + 1,
                IsGenerated: true));
            i = memberEnd + 1;
            continue;
        }

        var matchCtor   = TryMatchCtor(line, classStack, rxCtor);
        var matchMethod = matchCtor is null ? TryMatchMethod(line, rxMethod) : null;

        if (matchCtor.HasValue)
        {
            int memberEnd = FindMemberEnd(lines, i);
            output.Add(new MemberSpan(
                Name:        matchCtor.Value.Name,
                Kind:        "ctor",
                StartLine:   i + 1,
                EndLine:     memberEnd + 1,
                IsGenerated: false));
            i = memberEnd + 1;
            continue;
        }
        else if (matchMethod.HasValue)
        {
            int memberEnd = FindMemberEnd(lines, i);
            output.Add(new MemberSpan(
                Name:        matchMethod.Value.Name,
                Kind:        matchMethod.Value.Kind,
                StartLine:   i + 1,
                EndLine:     memberEnd + 1,
                IsGenerated: false));
            i = memberEnd + 1;
            continue;
        }

        i++;
    }

    return output;
}

static (string Name, string Kind)? TryMatchCtor(string line, Stack<(string Name, int Depth)> classStack, Regex rxCtor)
{
    if (classStack.Count == 0) return null;
    var topClass = classStack.Peek().Name;
    var m = rxCtor.Match(line);
    if (!m.Success) return null;
    var name = m.Groups["name"].Value;
    if (!name.Equals(topClass, StringComparison.Ordinal)) return null;
    return (name, "ctor");
}

static (string Name, string Kind)? TryMatchMethod(string line, Regex rxMethod)
{
    var m = rxMethod.Match(line);
    if (!m.Success) return null;
    var name = m.Groups["name"].Value;
    var ret = m.Groups["ret"].Value;
    // Filter out keywords that confuse the regex (e.g. `if (...)`).
    if (IsCSharpKeyword(name) || IsCSharpKeyword(ret)) return null;
    // `class`, `interface` etc. would have matched up the chain — but
    // the class/interface keywords in the modifier list pre-filter
    // already. Still, drop obvious false positives.
    if (ret == "class" || ret == "interface" || ret == "enum" || ret == "struct" ||
        ret == "namespace" || ret == "record")
        return null;
    return (name, "method");
}

static bool IsCSharpKeyword(string s)
{
    switch (s)
    {
        case "if": case "else": case "for": case "foreach": case "while": case "do":
        case "switch": case "case": case "return": case "throw": case "using":
        case "lock": case "fixed": case "unsafe": case "checked": case "unchecked":
        case "try": case "catch": case "finally": case "yield":
        case "await": case "in": case "out": case "ref": case "is": case "as":
        case "new": case "var":
            return true;
        default:
            return false;
    }
}

static bool HasGeneratorAttrAbove(string[] lines, int idx, Regex rxGen)
{
    // Walk upward through attribute lines and blank lines.
    for (int i = idx - 1; i >= 0; i--)
    {
        var t = lines[i].TrimStart();
        if (t.Length == 0) continue;
        if (t.StartsWith("//")) continue;
        if (t.StartsWith("/*") || t.StartsWith("*")) continue;
        if (t.StartsWith("[") && rxGen.IsMatch(t)) return true;
        if (t.StartsWith("[")) continue; // unrelated attribute, keep walking
        return false;
    }
    return false;
}

static string TryGetMemberName(string line, Stack<(string Name, int Depth)> classStack)
{
    // Best-effort name capture for generator-decorated members; not
    // critical to the report but useful for diagnostics if a JNI call
    // appears in a generator-decorated body.
    var m = Regex.Match(line, @"\b(\w+)\s*(?:<[^>]+>)?\s*\(");
    return m.Success ? m.Groups[1].Value : "<unknown>";
}

static int FindMemberEnd(string[] lines, int start)
{
    // Expression-bodied member ends at first `;`. Block-bodied member
    // ends when its `{` opens and the matching `}` closes. JNI type
    // signatures embedded in strings (`"Landroidx/foo/Bar;"`) or
    // verbatim strings can contain `;`, `{`, `}` we MUST not count —
    // mask string/char literals first.
    int depth = 0;
    bool seenBrace = false;
    for (int i = start; i < lines.Length; i++)
    {
        var line = MaskStringsAndChars(StripLineComment(lines[i]));
        foreach (var ch in line)
        {
            if (ch == '{') { depth++; seenBrace = true; }
            else if (ch == '}')
            {
                depth--;
                if (seenBrace && depth == 0) return i;
            }
            else if (ch == ';' && !seenBrace && depth == 0)
            {
                return i;
            }
        }
    }
    return Math.Min(start + 200, lines.Length - 1);
}

static string MaskStringsAndChars(string line)
{
    // Replace contents of `"..."`, `@"..."`, `'...'` literals with
    // spaces so embedded JNI-signature semicolons / braces don't
    // confuse the brace-tracker. Verbatim strings (`@"..."`) escape
    // a `"` as `""`; regular strings escape with `\"`.
    var sb = new StringBuilder(line.Length);
    int i = 0;
    while (i < line.Length)
    {
        var ch = line[i];
        // Verbatim string?
        if (ch == '@' && i + 1 < line.Length && line[i + 1] == '"')
        {
            sb.Append('@'); sb.Append('"'); i += 2;
            while (i < line.Length)
            {
                if (line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append(' '); sb.Append(' '); i += 2;
                    continue;
                }
                if (line[i] == '"')
                {
                    sb.Append('"'); i++; break;
                }
                sb.Append(' '); i++;
            }
            continue;
        }
        // Regular string
        if (ch == '"')
        {
            sb.Append('"'); i++;
            while (i < line.Length)
            {
                if (line[i] == '\\' && i + 1 < line.Length)
                {
                    sb.Append(' '); sb.Append(' '); i += 2; continue;
                }
                if (line[i] == '"')
                {
                    sb.Append('"'); i++; break;
                }
                sb.Append(' '); i++;
            }
            continue;
        }
        // Char literal — mask interior.
        if (ch == '\'')
        {
            sb.Append('\''); i++;
            while (i < line.Length)
            {
                if (line[i] == '\\' && i + 1 < line.Length)
                {
                    sb.Append(' '); sb.Append(' '); i += 2; continue;
                }
                if (line[i] == '\'')
                {
                    sb.Append('\''); i++; break;
                }
                sb.Append(' '); i++;
            }
            continue;
        }
        sb.Append(ch); i++;
    }
    return sb.ToString();
}

static string ExtractPurpose(string[] lines)
{
    // Try XML <summary> on first type, then leading `// ...` block.
    var xml = ExtractFirstXmlBlock(lines, "summary");
    if (!string.IsNullOrWhiteSpace(xml)) return Compact(xml);

    // Leading `// ...` block at file top. Check `///` before `//`.
    var sb = new StringBuilder();
    foreach (var raw in lines)
    {
        var t = raw.TrimStart();
        if (t.StartsWith("///"))
        {
            sb.Append(t.Substring(3).Trim()).Append(' ');
        }
        else if (t.StartsWith("//"))
        {
            sb.Append(t.Substring(2).Trim()).Append(' ');
        }
        else if (t.StartsWith("using ") || t.StartsWith("namespace ") || t.Length == 0)
        {
            // skip blank / using lines, they don't end the block
            if (sb.Length > 0 && t.Length > 0 && !t.StartsWith("//")) break;
        }
        else
        {
            break;
        }
    }
    if (sb.Length > 0) return Compact(sb.ToString());

    return "TODO: document";
}

static string ExtractFirstXmlBlock(string[] lines, string tag)
{
    // Find the first `/// <tag>` … `/// </tag>` group.
    var open = $"<{tag}>";
    var close = $"</{tag}>";
    var sb = new StringBuilder();
    bool inside = false;
    foreach (var raw in lines)
    {
        var t = raw.TrimStart();
        if (!t.StartsWith("///")) continue;
        var content = t.Substring(3).Trim();
        if (!inside)
        {
            int idx = content.IndexOf(open, StringComparison.Ordinal);
            if (idx >= 0)
            {
                inside = true;
                content = content[(idx + open.Length)..];
            }
        }
        if (inside)
        {
            int idx = content.IndexOf(close, StringComparison.Ordinal);
            if (idx >= 0)
            {
                sb.Append(content.Substring(0, idx));
                return sb.ToString();
            }
            sb.Append(content).Append(' ');
        }
    }
    return sb.ToString();
}

static string ExtractWhy(string[] lines, int memberLine)
{
    // 1) `// Why raw JNI:` / `// Why manual:` immediately above the member.
    for (int i = memberLine - 2; i >= 0 && i >= memberLine - 30; i--)
    {
        var t = lines[i].TrimStart();
        if (t.Length == 0) continue;
        if (t.StartsWith("[")) continue;
        if (t.StartsWith("///"))
        {
            var content = t.Substring(3).Trim();
            if (content.Contains("Why raw JNI", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("Why manual",  StringComparison.OrdinalIgnoreCase))
            {
                return Compact(GatherXmlNeighbors(lines, i));
            }
            continue;
        }
        if (t.StartsWith("// Why raw JNI", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("// Why manual",  StringComparison.OrdinalIgnoreCase))
        {
            return Compact(GatherCommentNeighbors(lines, i));
        }
        if (!t.StartsWith("//")) break;
    }

    // 2) Generic adjacent `// ...` / `/// ...` comment block above the member.
    // Note: check `///` before `//` because `t.StartsWith("//")` matches both.
    var sb = new StringBuilder();
    for (int i = memberLine - 2; i >= 0; i--)
    {
        var t = lines[i].TrimStart();
        if (t.StartsWith("[")) continue;
        if (t.StartsWith("///"))
        {
            sb.Insert(0, t.Substring(3).Trim() + " ");
            continue;
        }
        if (t.StartsWith("//"))
        {
            sb.Insert(0, t.Substring(2).Trim() + " ");
            continue;
        }
        if (t.Length == 0)
        {
            if (sb.Length > 0) break;
            continue;
        }
        break;
    }
    var s = Compact(sb.ToString());
    if (s.Length > 0) return s;

    return "TODO: document";
}

static string ExtractMigration(string[] lines, int memberLine, Regex rxIssueRef)
{
    // 1) `// Migration:` line. Check `///` before `//` so the `///`
    // branch is reachable.
    for (int i = memberLine - 2; i >= 0 && i >= memberLine - 30; i--)
    {
        var t = lines[i].TrimStart();
        if (t.Length == 0) continue;
        if (t.StartsWith("///"))
        {
            if (t.Substring(3).Trim().StartsWith("Migration:", StringComparison.OrdinalIgnoreCase))
                return Compact(t.Substring(3).Trim().Substring("Migration:".Length).Trim());
            continue;
        }
        if (t.StartsWith("//"))
        {
            if (t.StartsWith("// Migration:", StringComparison.OrdinalIgnoreCase))
                return Compact(t.Substring("// Migration:".Length).Trim());
            continue;
        }
        if (t.StartsWith("[")) continue;
        break;
    }

    // 2) Issue references found anywhere in the leading comment block.
    var sb = new StringBuilder();
    for (int i = memberLine - 2; i >= 0; i--)
    {
        var t = lines[i].TrimStart();
        if (t.StartsWith("[")) continue;
        if (t.StartsWith("///") || t.StartsWith("//"))
        {
            sb.Insert(0, t + "\n");
            continue;
        }
        if (t.Length == 0) { if (sb.Length > 0) break; continue; }
        break;
    }
    var refs = new List<string>();
    foreach (Match m in rxIssueRef.Matches(sb.ToString()))
    {
        if (m.Groups["repo"].Success)
            refs.Add($"{m.Groups["repo"].Value}#{m.Groups["num"].Value}");
        else if (m.Groups["bare"].Success)
            refs.Add(m.Groups["bare"].Value);
    }
    refs = refs.Distinct(StringComparer.Ordinal).ToList();
    if (refs.Count > 0)
        return $"see {string.Join(", ", refs)}";

    return "TODO";
}

static string GatherXmlNeighbors(string[] lines, int hitLine)
{
    var sb = new StringBuilder();
    for (int i = hitLine; i < lines.Length; i++)
    {
        var t = lines[i].TrimStart();
        if (!t.StartsWith("///")) break;
        sb.Append(t.Substring(3).Trim()).Append(' ');
    }
    for (int i = hitLine - 1; i >= 0; i--)
    {
        var t = lines[i].TrimStart();
        if (!t.StartsWith("///")) break;
        sb.Insert(0, t.Substring(3).Trim() + " ");
    }
    return sb.ToString();
}

static string GatherCommentNeighbors(string[] lines, int hitLine)
{
    // Walks `// ...` lines around `hitLine`. Skips `///` because XML
    // docs go through GatherXmlNeighbors.
    var sb = new StringBuilder();
    for (int i = hitLine; i < lines.Length; i++)
    {
        var t = lines[i].TrimStart();
        if (t.StartsWith("///")) break;
        if (!t.StartsWith("//")) break;
        sb.Append(t.Substring(2).Trim()).Append(' ');
    }
    for (int i = hitLine - 1; i >= 0; i--)
    {
        var t = lines[i].TrimStart();
        if (t.StartsWith("///")) break;
        if (!t.StartsWith("//")) break;
        sb.Insert(0, t.Substring(2).Trim() + " ");
    }
    return sb.ToString();
}

static string Compact(string s)
{
    s = Regex.Replace(s ?? "", @"\s+", " ").Trim();
    // Strip XML cref/href fluff for prose readability — we want a one-line summary.
    s = Regex.Replace(s, @"<see\s+cref=""([^""]+)""\s*/>",
        m => "`" + m.Groups[1].Value.Split('.').Last().TrimEnd('(', ')') + "`");
    s = Regex.Replace(s, @"<see\s+href=""([^""]+)""\s*>([^<]*)</see>",
        m => string.IsNullOrEmpty(m.Groups[2].Value) ? m.Groups[1].Value : m.Groups[2].Value);
    s = Regex.Replace(s, @"<paramref\s+name=""([^""]+)""\s*/>", m => "`" + m.Groups[1].Value + "`");
    s = Regex.Replace(s, @"</?(?:para|list|item|description|c|para|remarks|summary)[^>]*>", "");
    s = Regex.Replace(s, @"<c>([^<]*)</c>", m => "`" + m.Groups[1].Value + "`");
    s = Regex.Replace(s, @"\s+", " ").Trim();
    // Trim very long entries so the markdown table stays readable.
    if (s.Length > 240) s = s.Substring(0, 237) + "...";
    return s;
}

static string Escape(string s) =>
    (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

static string StripLineComment(string line)
{
    // Find `//` outside of string/char literals. Reuse MaskStringsAndChars
    // so verbatim (`@"..."`) and raw strings with `//` in the body
    // (e.g. URLs in resource paths) aren't mistaken for comments.
    var masked = MaskStringsAndChars(line);
    int idx = masked.IndexOf("//", StringComparison.Ordinal);
    return idx < 0 ? line : line.Substring(0, idx);
}

static string[] MaskBlockComments(string[] lines)
{
    // Mask `/* ... */` contents with spaces, preserving line count and
    // newlines so all downstream line-indexed analysis stays aligned.
    // Doesn't track string literals — `/*` / `*/` inside a string are
    // exceedingly rare and the C# tokenizer treats `*/` inside an open
    // block comment as the close anyway, so this matches spec behavior.
    var output = new string[lines.Length];
    bool inBlock = false;
    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i];
        var sb = new StringBuilder(line.Length);
        int j = 0;
        while (j < line.Length)
        {
            if (inBlock)
            {
                if (j + 1 < line.Length && line[j] == '*' && line[j + 1] == '/')
                {
                    sb.Append("  "); j += 2; inBlock = false; continue;
                }
                sb.Append(' '); j++;
                continue;
            }
            if (j + 1 < line.Length && line[j] == '/' && line[j + 1] == '*')
            {
                sb.Append("  "); j += 2; inBlock = true; continue;
            }
            sb.Append(line[j]); j++;
        }
        output[i] = sb.ToString();
    }
    return output;
}

// --- Records ----------------------------------------------------------------

record JniSite(int Line, string CallName);
record MemberSpan(string Name, string Kind, int StartLine, int EndLine, bool IsGenerated);
record MemberEntry(string Name, string Kind, int StartLine, int EndLine, int JniCallCount, string Why, string Migration);
record JcwClass(string ClassName, string Implements, int StartLine, int EndLine, bool HasJni, string RelativePath);
record FileReport(
    string RelativePath,
    string Purpose,
    List<MemberEntry> Entries,
    List<JniSite> JniSites,
    List<JcwClass> Jcws,
    int ComposeBridgeCount,
    int ComposeFacadeCount);
