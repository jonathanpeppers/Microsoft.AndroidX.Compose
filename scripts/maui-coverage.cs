// scripts/maui-coverage.cs
//
// .NET MAUI ⇄ Microsoft.AndroidX.Compose.Maui backend coverage report generator.
//
// Run from repo root:
//
//     dotnet run scripts/maui-coverage.cs
//
// What it does:
//   1. Locates Microsoft.Maui.dll (Core, net10.0-android36.0) in the NuGet
//      cache for the MAUI version pinned by Directory.Build.targets.
//   2. Shells out to ilspycmd to decompile every public *Handler type
//      declared by MAUI's stock Android backend. Cached under
//      obj/maui-coverage/maui/.
//   3. Regex-parses each handler for its `Mapper` field (plus any base
//      mappers passed via the PropertyMapper ctor — ViewHandler.ViewMapper,
//      ElementHandler.ElementMapper, TextButtonMapper, etc.) and walks the
//      chain to collect the full set of property-mapper keys per handler.
//   4. Parses our handler sources in src/Microsoft.AndroidX.Compose.Maui/Handlers/*.cs
//      the same way, plus Hosting/AppHostBuilderExtensions.cs for the list of
//      MAUI virtual-view types we override.
//   5. Cross-references and writes docs/maui-coverage.md with a handler-level
//      summary table and per-handler checkbox lists of covered / missing
//      property keys.
//
// Re-run after bumping the MAUI version in Directory.Build.targets, or after
// adding / changing a handler in src/Microsoft.AndroidX.Compose.Maui.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// ----------------------------------------------------------------------------
// Config
// ----------------------------------------------------------------------------

const string CacheRoot   = "obj/maui-coverage";
const string MauiCache   = CacheRoot + "/maui";
const string OurHandlers = "src/Microsoft.AndroidX.Compose.Maui/Handlers";
const string OurHosting  = "src/Microsoft.AndroidX.Compose.Maui/Hosting/AppHostBuilderExtensions.cs";
const string TargetsPath = "Directory.Build.targets";
const string ReportPath  = "docs/maui-coverage.md";

// ----------------------------------------------------------------------------
// 1. Resolve Microsoft.Maui.dll for the pinned MAUI version
// ----------------------------------------------------------------------------

var mauiVersion = ReadMauiVersion(TargetsPath);
Console.WriteLine($"MAUI version: {mauiVersion}");

var mauiDll = ResolveMauiCoreDll(mauiVersion);
Console.WriteLine($"MAUI core DLL: {mauiDll}");

// ----------------------------------------------------------------------------
// 2. Enumerate stock handler types + decompile (cached)
// ----------------------------------------------------------------------------

Directory.CreateDirectory(MauiCache);
var stockHandlerTypes = ListMauiHandlerTypes(mauiDll);
Console.WriteLine($"Stock MAUI handler types: {stockHandlerTypes.Count}");

// We also need the cross-cutting mapper bases (ViewHandler / ElementHandler).
var allTypesToDecompile = stockHandlerTypes
    .Concat(new[]
    {
        "Microsoft.Maui.Handlers.ViewHandler",
        "Microsoft.Maui.Handlers.ElementHandler",
    })
    .Distinct()
    .ToList();

foreach (var t in allTypesToDecompile)
    DecompileIfNeeded(mauiDll, t, MauiCache);

// ----------------------------------------------------------------------------
// 3. Parse stock mappers
// ----------------------------------------------------------------------------

// short-name → Handler (e.g. "ButtonHandler" → handler info).
var stockHandlers = new Dictionary<string, HandlerInfo>(StringComparer.Ordinal);
foreach (var fqn in allTypesToDecompile)
{
    var path = Path.Combine(MauiCache, fqn + ".cs");
    if (!File.Exists(path)) continue;
    var src = File.ReadAllText(path);
    var info = ParseHandler(src, ShortName(fqn), isStock: true);
    if (info is not null)
        stockHandlers[info.ShortName] = info;
}

// Walk base mappers transitively for each stock handler's primary "Mapper".
foreach (var h in stockHandlers.Values)
    h.AllKeys = ResolveAllKeys(h, "Mapper", stockHandlers);

// ----------------------------------------------------------------------------
// 4. Parse our handlers + hosting registration
// ----------------------------------------------------------------------------

var ourHandlers = new Dictionary<string, HandlerInfo>(StringComparer.Ordinal);
foreach (var file in Directory.EnumerateFiles(OurHandlers, "*Handler.cs", SearchOption.TopDirectoryOnly))
{
    var src = File.ReadAllText(file);
    var shortName = Path.GetFileNameWithoutExtension(file);
    var info = ParseHandler(src, shortName, isStock: false);
    if (info is not null)
        ourHandlers[info.ShortName] = info;
}

// Our handlers reuse MAUI's ViewHandler.ViewMapper / ElementHandler.ElementMapper
// for cross-cutting view properties; pull those in from the stock cache so the
// chain resolves.
foreach (var h in ourHandlers.Values)
    h.AllKeys = ResolveAllKeys(h, "Mapper", ourHandlers, stockHandlers);

// MAUI virtual-view → handler-short-name pairs we register.
var ourRegistrations = ParseRegistrations(File.ReadAllText(OurHosting));

// Stock virtual-view → handler-short-name pairs from MAUI's
// AddDefaultMauiHandlers. Decompile that too.
DecompileIfNeeded(mauiDll, "Microsoft.Maui.Hosting.AppHostBuilderExtensions", MauiCache, optional: true);
DecompileFromControlsIfNeeded("Microsoft.Maui.Controls.Hosting.AppHostBuilderExtensions", mauiVersion, MauiCache);
var stockRegistrations = ParseStockRegistrations(MauiCache);
stockRegistrations = SupplementWithImpliedRegistrations(stockRegistrations, stockHandlers);
Console.WriteLine($"Stock virtual-view registrations: {stockRegistrations.Count}");

// ----------------------------------------------------------------------------
// 5. Write the report
// ----------------------------------------------------------------------------

Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
WriteReport(ReportPath, mauiVersion, stockRegistrations, ourRegistrations, stockHandlers, ourHandlers);

Console.WriteLine();
Console.WriteLine($"Report written: {ReportPath}");

// ============================================================================
// Helpers
// ============================================================================

static string ReadMauiVersion(string targetsPath)
{
    var src = File.ReadAllText(targetsPath);
    var m = Regex.Match(src, @"<PackageReference\s+Update=""Microsoft\.Maui\.Controls""\s+Version=""([^""]+)""");
    if (!m.Success)
        throw new InvalidOperationException("Could not find Microsoft.Maui.Controls version in Directory.Build.targets.");
    return m.Groups[1].Value;
}

static string ResolveMauiCoreDll(string version)
{
    var nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
    if (string.IsNullOrEmpty(nugetRoot))
        nugetRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

    var pkgDir = Path.Combine(nugetRoot, "microsoft.maui.core", version);
    if (!Directory.Exists(pkgDir))
        throw new DirectoryNotFoundException($"microsoft.maui.core {version} not found under {nugetRoot}. Run a build to restore it.");

    // Prefer the highest net10.0-androidNN.N TFM available.
    var libDir = Path.Combine(pkgDir, "lib");
    var androidTfms = Directory.GetDirectories(libDir, "net*-android*").OrderDescending().ToList();
    if (androidTfms.Count == 0)
        throw new DirectoryNotFoundException($"No net*-android TFM under {libDir}.");

    var dll = Path.Combine(androidTfms[0], "Microsoft.Maui.dll");
    if (!File.Exists(dll))
        throw new FileNotFoundException($"Microsoft.Maui.dll missing under {androidTfms[0]}.");

    return dll;
}

static List<string> ListMauiHandlerTypes(string dll)
{
    // `ilspycmd -l c <dll>` lists every class. Filter for Microsoft.Maui.Handlers.*Handler.
    var psi = new ProcessStartInfo("ilspycmd")
    {
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
        UseShellExecute        = false,
    };
    psi.ArgumentList.Add(dll);
    psi.ArgumentList.Add("-l");
    psi.ArgumentList.Add("c");

    using var p = Process.Start(psi)!;
    var output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();

    var handlers = new List<string>();
    foreach (var line in output.Split('\n'))
    {
        var m = Regex.Match(line.Trim(), @"^Class (Microsoft\.Maui\.Handlers\.[A-Za-z0-9]+Handler)$");
        if (m.Success) handlers.Add(m.Groups[1].Value);
    }
    return handlers.Distinct().OrderBy(x => x).ToList();
}

static void DecompileIfNeeded(string dll, string fqn, string cacheDir, bool optional = false)
{
    var dest = Path.Combine(cacheDir, fqn + ".cs");
    if (File.Exists(dest) && new FileInfo(dest).Length > 0)
    {
        Console.WriteLine($"[cached ] {fqn}");
        return;
    }

    Console.WriteLine($"[decomp ] {fqn}");
    var psi = new ProcessStartInfo("ilspycmd")
    {
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
        UseShellExecute        = false,
    };
    psi.ArgumentList.Add(dll);
    psi.ArgumentList.Add("-t");
    psi.ArgumentList.Add(fqn);

    using var p = Process.Start(psi)!;
    var output = p.StandardOutput.ReadToEnd();
    var err    = p.StandardError.ReadToEnd();
    p.WaitForExit();

    if (p.ExitCode != 0 || output.Contains("does not contain a type", StringComparison.Ordinal))
    {
        if (optional) return;
        throw new InvalidOperationException($"ilspycmd failed for {fqn}: {err}");
    }

    File.WriteAllText(dest, output);
}

static void DecompileFromControlsIfNeeded(string fqn, string version, string cacheDir)
{
    var dest = Path.Combine(cacheDir, fqn + ".cs");
    if (File.Exists(dest) && new FileInfo(dest).Length > 0)
    {
        Console.WriteLine($"[cached ] {fqn}");
        return;
    }

    var nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
    var pkgDir = Path.Combine(nugetRoot, "microsoft.maui.controls.core", version);
    if (!Directory.Exists(pkgDir))
    {
        Console.WriteLine($"  WARN: microsoft.maui.controls.core {version} missing; stock registration list will be partial.");
        return;
    }
    var libDir = Path.Combine(pkgDir, "lib");
    var androidTfms = Directory.GetDirectories(libDir, "net*-android*").OrderDescending().ToList();
    if (androidTfms.Count == 0)
    {
        Console.WriteLine($"  WARN: no net*-android TFM under {libDir}.");
        return;
    }
    var dll = Path.Combine(androidTfms[0], "Microsoft.Maui.Controls.dll");
    if (!File.Exists(dll))
    {
        Console.WriteLine($"  WARN: Microsoft.Maui.Controls.dll missing under {androidTfms[0]}.");
        return;
    }
    DecompileIfNeeded(dll, fqn, cacheDir);
}

static string ShortName(string fqn)
{
    var i = fqn.LastIndexOf('.');
    return i < 0 ? fqn : fqn[(i + 1)..];
}

// ----------------------------------------------------------------------------
// Handler parsing
// ----------------------------------------------------------------------------

static HandlerInfo? ParseHandler(string src, string shortName, bool isStock)
{
    var info = new HandlerInfo { ShortName = shortName, IsStock = isStock };

    // Match `public static IPropertyMapper<TVirtual, THandler> XxxMapper = new PropertyMapper<...>(...) { ... };`
    // The body is a `{ ... }` block of `["Key"] = MapXxx,` entries. Body is
    // optional — empty mappers (HybridWebView, NavigationView) still expose
    // base-mapper refs we need. Also handle `public new static IPropertyMapper`
    // (PageHandler hides ContentViewHandler.Mapper with `new`).
    var fieldRx = new Regex(
        @"public\s+(?:new\s+)?static\s+IPropertyMapper\s*<\s*(?<virtual>[^,>]+?)\s*,\s*(?<handler>[^>]+?)\s*>\s+(?<field>\w+)\s*=\s*new\s+PropertyMapper\s*<[^>]+>\s*(?:\((?<ctorArgs>[^)]*)\))?\s*(?:\{(?<body>[^}]*)\})?",
        RegexOptions.Singleline);

    foreach (Match m in fieldRx.Matches(src))
    {
        var fieldName = m.Groups["field"].Value;
        var virtualView = m.Groups["virtual"].Value.Trim();
        var ctorArgs    = m.Groups["ctorArgs"].Value;
        var body        = m.Groups["body"].Value;

        if (fieldName == "Mapper")
            info.VirtualView = virtualView;

        var block = new MapperBlock();

        // Base mapper refs (anything that isn't whitespace, comma, or empty).
        foreach (var arg in ctorArgs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            block.BaseRefs.Add(arg);

        // Keys: `["Foo"] = MapFoo,`
        foreach (Match k in Regex.Matches(body, @"\[\s*(?:nameof\([^)]+\.(?<n1>\w+)\)|""(?<n2>[^""]+)"")\s*\]\s*="))
        {
            var name = !string.IsNullOrEmpty(k.Groups["n1"].Value) ? k.Groups["n1"].Value : k.Groups["n2"].Value;
            if (!string.IsNullOrWhiteSpace(name)) block.Keys.Add(name);
        }

        info.Mappers[fieldName] = block;
    }

    return info.Mappers.Count == 0 ? null : info;
}

static HashSet<string> ResolveAllKeys(
    HandlerInfo h,
    string mapperName,
    params Dictionary<string, HandlerInfo>[] handlerSets)
{
    var visited = new HashSet<string>(StringComparer.Ordinal);
    var keys = new HashSet<string>(StringComparer.Ordinal);
    Walk(h.ShortName, mapperName);
    return keys;

    void Walk(string handlerName, string mapper)
    {
        var marker = handlerName + "::" + mapper;
        if (!visited.Add(marker)) return;

        if (!TryGetHandler(handlerName, out var handler)) return;
        if (!handler.Mappers.TryGetValue(mapper, out var block)) return;

        foreach (var k in block.Keys)
        {
            // Skip MAUI's internal batched-property bookkeeping key — it's not
            // a real user-facing property and just pollutes "missing" lists.
            if (k.StartsWith("_", StringComparison.Ordinal)) continue;
            keys.Add(k);
        }

        foreach (var baseRef in block.BaseRefs)
        {
            // Examples: ViewHandler.ViewMapper, ElementHandler.ElementMapper,
            // TextButtonMapper (local), ImageButtonMapper (local).
            string? targetHandler;
            string targetMapper;
            var dot = baseRef.LastIndexOf('.');
            if (dot > 0)
            {
                targetHandler = baseRef[..dot];
                targetMapper  = baseRef[(dot + 1)..];
            }
            else
            {
                targetHandler = handlerName;
                targetMapper  = baseRef;
            }

            Walk(targetHandler!, targetMapper);
        }
    }

    bool TryGetHandler(string name, out HandlerInfo handler)
    {
        foreach (var set in handlerSets)
            if (set.TryGetValue(name, out handler!)) return true;
        handler = null!;
        return false;
    }
}

// ----------------------------------------------------------------------------
// Registration parsing
// ----------------------------------------------------------------------------

static List<Registration> ParseRegistrations(string src)
{
    var list = new List<Registration>();
    // handlers.AddHandler<MauiX, YHandler>()
    // or .AddHandler<X, YHandler>()
    foreach (Match m in Regex.Matches(src, @"AddHandler\s*<\s*(?<v>[A-Za-z0-9_.]+)\s*,\s*(?<h>[A-Za-z0-9_.]+Handler)\s*>"))
    {
        var v = ShortName(m.Groups["v"].Value);
        // Strip our "Maui" alias prefix on virtual-view types
        // (using MauiButton = Microsoft.Maui.Controls.Button;).
        if (v.StartsWith("Maui", StringComparison.Ordinal) && v.Length > 4 && char.IsUpper(v[4]))
            v = v[4..];
        var h = ShortName(m.Groups["h"].Value);
        list.Add(new Registration(v, h));
    }
    // Also catch the non-generic form used for renderer-backed legacy types in
    // MAUI Controls (typeof(X), typeof(Y)) — we drop those since they're not
    // handlers we'd ever replace.
    return list;
}

static List<Registration> ParseStockRegistrations(string cacheDir)
{
    var list = new List<Registration>();
    foreach (var f in Directory.GetFiles(cacheDir, "Microsoft.Maui.*.Hosting.AppHostBuilderExtensions.cs"))
    {
        list.AddRange(ParseRegistrations(File.ReadAllText(f)));
    }
    // Dedupe by (virtual, handler).
    return list
        .GroupBy(r => (r.VirtualView, r.HandlerShortName))
        .Select(g => g.First())
        .OrderBy(r => r.VirtualView, StringComparer.Ordinal)
        .ToList();
}

// Some stock handlers (LabelHandler, CheckBoxHandler) aren't registered via the
// generic `AddHandler<T,H>()` pattern we regex-scan — MAUI wires them up via a
// reflection/dispatch path we don't see in IL. Fall back to deriving the virtual
// view name from the handler class name (XxxHandler → Xxx). Skip handlers that
// already have an explicit registration.
static List<Registration> SupplementWithImpliedRegistrations(
    List<Registration> stockRegs,
    Dictionary<string, HandlerInfo> stockHandlers)
{
    var registered = new HashSet<string>(stockRegs.Select(r => r.HandlerShortName), StringComparer.Ordinal);
    var supplemented = new List<Registration>(stockRegs);
    foreach (var h in stockHandlers.Values)
    {
        if (registered.Contains(h.ShortName)) continue;
        if (h.ShortName is "ViewHandler" or "ElementHandler") continue;
        if (!h.ShortName.EndsWith("Handler", StringComparison.Ordinal)) continue;
        var virt = h.ShortName[..^"Handler".Length];
        if (string.IsNullOrEmpty(virt)) continue;
        supplemented.Add(new Registration(virt, h.ShortName));
    }
    return supplemented;
}

// Bucket handlers into broad categories for the per-category summary. Mirrors
// the layering in docs/maui-backend.md so the report tells the same story.
static string CategoryOf(string handlerShortName) => handlerShortName switch
{
    "ApplicationHandler" or "WindowHandler"             => "App / Window",
    "PageHandler"
        or "FlyoutViewHandler" or "TabbedViewHandler"
        or "NavigationViewHandler"                      => "Pages / Navigation",
    "LayoutHandler" or "ScrollViewHandler"
        or "RefreshViewHandler" or "BorderHandler"
        or "ContentViewHandler"                         => "Containers",
    "LabelHandler" or "ButtonHandler"
        or "ImageButtonHandler" or "ImageHandler"
        or "CheckBoxHandler" or "SwitchHandler"
        or "RadioButtonHandler" or "EntryHandler"
        or "EditorHandler" or "SearchBarHandler"
        or "PickerHandler" or "DatePickerHandler"
        or "TimePickerHandler" or "SliderHandler"
        or "StepperHandler" or "ProgressBarHandler"
        or "ActivityIndicatorHandler"
        or "IndicatorViewHandler"                       => "Leaves",
    "ShapeViewHandler"                                  => "Shapes",
    "ToolbarHandler" or "MenuBarHandler"
        or "MenuBarItemHandler" or "MenuFlyoutHandler"
        or "MenuFlyoutItemHandler"
        or "MenuFlyoutSeparatorHandler"
        or "MenuFlyoutSubItemHandler"                   => "Menus / Toolbar",
    _                                                   => "Other / advanced",
};

// ----------------------------------------------------------------------------
// Report
// ----------------------------------------------------------------------------

static void WriteReport(
    string path,
    string mauiVersion,
    List<Registration> stockRegs,
    List<Registration> ourRegs,
    Dictionary<string, HandlerInfo> stockHandlers,
    Dictionary<string, HandlerInfo> ourHandlers)
{
    var sb = new StringBuilder();
    sb.AppendLine("# .NET MAUI ⇄ Microsoft.AndroidX.Compose.Maui backend coverage");
    sb.AppendLine();
    sb.AppendLine($"Generated by `scripts/maui-coverage.cs` on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.");
    sb.AppendLine();
    sb.AppendLine($"Pinned MAUI version: **{mauiVersion}** (from `Directory.Build.targets`).");
    sb.AppendLine();
    sb.AppendLine("Source of truth: ilspycmd-decompiled `Microsoft.Maui.Handlers.*Handler` types ");
    sb.AppendLine("from the MAUI Core assembly + our own handler sources under ");
    sb.AppendLine("`src/Microsoft.AndroidX.Compose.Maui/Handlers/`. Property-mapper keys are ");
    sb.AppendLine("collected transitively across base mappers (`ViewHandler.ViewMapper`, ");
    sb.AppendLine("`ElementHandler.ElementMapper`, and per-handler sub-mappers like ");
    sb.AppendLine("`TextButtonMapper` / `ImageButtonMapper`).");
    sb.AppendLine();

    // Build the canonical "stock virtual-view ↔ stock handler" list. Some
    // virtual views (Layout) are abstract — every concrete subclass uses the
    // same handler.
    var stockByVirtualView = stockRegs
        .GroupBy(r => r.HandlerShortName)
        .ToDictionary(g => g.Key, g => g.Select(r => r.VirtualView).Distinct().OrderBy(v => v, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);

    // Map "stock handler short name" → "our handler short name(s)" that
    // override one of its virtual views.
    var ourHandlerByStock = new Dictionary<string, List<(string OurHandler, string VirtualView)>>(StringComparer.Ordinal);
    foreach (var ourReg in ourRegs)
    {
        var stockMatch = stockRegs.FirstOrDefault(r =>
            string.Equals(r.VirtualView, ourReg.VirtualView, StringComparison.Ordinal));
        var stockHandler = stockMatch?.HandlerShortName
            ?? stockHandlers.Keys.FirstOrDefault(k => string.Equals(k, ourReg.HandlerShortName, StringComparison.Ordinal))
            ?? stockRegs.FirstOrDefault(r =>
                r.HandlerShortName == "LayoutHandler" && ourReg.HandlerShortName == "LayoutHandler")?.HandlerShortName;

        if (stockHandler is null) continue;

        if (!ourHandlerByStock.TryGetValue(stockHandler, out var list))
            ourHandlerByStock[stockHandler] = list = new();
        list.Add((ourReg.HandlerShortName, ourReg.VirtualView));
    }

    // The canonical list of stock handlers to score against.
    var allStockHandlers = stockHandlers.Values
        .Where(h => h.ShortName is not "ViewHandler" and not "ElementHandler"
                 && stockByVirtualView.ContainsKey(h.ShortName))
        .OrderBy(h => h.ShortName, StringComparer.Ordinal)
        .ToList();

    // Per-handler stats — compute once, used by both summary and detail.
    HashSet<string> OurKeysFor(string stockHandler) =>
        ourHandlerByStock.TryGetValue(stockHandler, out var ours)
            ? ours.SelectMany(o => ourHandlers.TryGetValue(o.OurHandler, out var oi) ? oi.AllKeys : Enumerable.Empty<string>())
                  .ToHashSet(StringComparer.Ordinal)
            : new(StringComparer.Ordinal);

    var stats = allStockHandlers
        .Select(h =>
        {
            var ours = ourHandlerByStock.GetValueOrDefault(h.ShortName) ?? new();
            var ourKeys = OurKeysFor(h.ShortName);
            var hit = h.AllKeys.Count(k => ourKeys.Contains(k));
            return new
            {
                Handler   = h,
                Category  = CategoryOf(h.ShortName),
                Ours      = ours,
                OurKeys   = ourKeys,
                Hit       = hit,
                Total     = h.AllKeys.Count,
                Covered   = ours.Count > 0,
            };
        })
        .ToList();

    int handlerTotal     = stats.Count;
    int handlerCovered   = stats.Count(s => s.Covered);
    double handlerPct    = handlerTotal == 0 ? 0 : 100.0 * handlerCovered / handlerTotal;
    int keyTotal         = stats.Sum(s => s.Total);
    int keyCovered       = stats.Sum(s => s.Hit);
    double keyPct        = keyTotal == 0 ? 0 : 100.0 * keyCovered / keyTotal;

    sb.AppendLine("## Summary");
    sb.AppendLine();
    sb.AppendLine($"- **Stock MAUI handlers in scope**: {handlerTotal}");
    sb.AppendLine($"- **Handlers we override**: {handlerCovered} (**{handlerPct:F1}%**)");
    sb.AppendLine($"- **Property-mapper keys covered**: {keyCovered} / {keyTotal} (**{keyPct:F1}%**)");
    sb.AppendLine();

    // -- Per-category roll-up -------------------------------------------------
    sb.AppendLine("### Per-category coverage");
    sb.AppendLine();
    sb.AppendLine("| Category | Handlers | Keys |");
    sb.AppendLine("| --- | --- | --- |");

    var categoryOrder = new[]
    {
        "Pages / Navigation",
        "Containers",
        "Leaves",
        "Menus / Toolbar",
        "Shapes",
        "App / Window",
        "Other / advanced",
    };

    foreach (var cat in categoryOrder)
    {
        var inCat = stats.Where(s => s.Category == cat).ToList();
        if (inCat.Count == 0) continue;
        var hCov = inCat.Count(s => s.Covered);
        var hTot = inCat.Count;
        var kCov = inCat.Sum(s => s.Hit);
        var kTot = inCat.Sum(s => s.Total);
        var hPct = hTot == 0 ? 0 : 100.0 * hCov / hTot;
        var kPct = kTot == 0 ? 0 : 100.0 * kCov / kTot;
        sb.AppendLine($"| **{cat}** | {hCov}/{hTot} ({hPct:F0}%) | {kCov}/{kTot} ({kPct:F0}%) |");
    }
    sb.AppendLine();

    // -- Per-handler summary --------------------------------------------------
    sb.AppendLine("### Per-handler coverage");
    sb.AppendLine();
    sb.AppendLine("Sorted by category. ✅ = fully covered (100%), 🟡 = partial, ❌ = not implemented.");
    sb.AppendLine();
    sb.AppendLine("| Status | Stock handler | Category | Virtual view(s) | Our handler | Keys |");
    sb.AppendLine("| --- | --- | --- | --- | --- | --- |");

    var sortedStats = stats
        .OrderBy(s => Array.IndexOf(categoryOrder, s.Category) is int i and >= 0 ? i : int.MaxValue)
        .ThenBy(s => s.Handler.ShortName, StringComparer.Ordinal)
        .ToList();

    foreach (var s in sortedStats)
    {
        var virtuals = string.Join(", ", stockByVirtualView.GetValueOrDefault(s.Handler.ShortName, new()).Select(v => $"`{v}`"));
        var pct = s.Total == 0 ? 0 : 100.0 * s.Hit / s.Total;
        var status = !s.Covered
            ? "❌"
            : (s.Hit == s.Total && s.Total > 0) ? "✅" : "🟡";
        var ourNames = s.Covered
            ? string.Join(", ", s.Ours.Select(o => $"`{o.OurHandler}`").Distinct())
            : "—";
        var keys = s.Total == 0 && !s.Covered
            ? "n/a"
            : $"{s.Hit} / {s.Total} ({pct:F0}%)";
        sb.AppendLine($"| {status} | `{s.Handler.ShortName}` | {s.Category} | {virtuals} | {ourNames} | {keys} |");
    }
    sb.AppendLine();

    // -- Highlights -----------------------------------------------------------
    var notCovered = sortedStats.Where(s => !s.Covered).ToList();
    var partial    = sortedStats.Where(s => s.Covered && s.Hit < s.Total).ToList();

    if (notCovered.Count > 0)
    {
        sb.AppendLine("### Missing handlers worth investigating");
        sb.AppendLine();
        sb.AppendLine("Stock MAUI handlers with no Compose backend. The AppCompat handler ");
        sb.AppendLine("keeps running, so the view still functions — it just isn't themed by ");
        sb.AppendLine("the same Material 3 / Compose pipeline as the rest of the page.");
        sb.AppendLine();
        foreach (var s in notCovered.OrderByDescending(x => x.Total))
        {
            var virtuals = string.Join(", ", stockByVirtualView.GetValueOrDefault(s.Handler.ShortName, new()));
            sb.AppendLine($"- **`{s.Handler.ShortName}`** ({virtuals}) — {s.Total} stock keys, {s.Category}");
        }
        sb.AppendLine();
    }

    if (partial.Count > 0)
    {
        sb.AppendLine("### Partial handlers (gap analysis)");
        sb.AppendLine();
        sb.AppendLine("Compose-backed handlers that miss at least one stock-MAUI property key. ");
        sb.AppendLine("Most often this is a non-trivial property we haven't wired up yet ");
        sb.AppendLine("(`CharacterSpacing`, `Font`, `Padding` on `Button`; `CornerRadius`, ");
        sb.AppendLine("dashed stroke patterns on `Border`).");
        sb.AppendLine();
        foreach (var s in partial.OrderBy(x => 100.0 * x.Hit / Math.Max(1, x.Total)))
        {
            var missing = s.Handler.AllKeys
                .Except(s.OurKeys, StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            var pct = 100.0 * s.Hit / s.Total;
            sb.AppendLine($"- **`{s.Handler.ShortName}`** ({pct:F0}%) — missing: {string.Join(", ", missing.Select(m => $"`{m}`"))}");
        }
        sb.AppendLine();
    }

    // ------------------------------------------------------------------------
    // Per-handler detail
    // ------------------------------------------------------------------------

    sb.AppendLine("## Per-handler property detail");
    sb.AppendLine();
    sb.AppendLine("Each section lists every property-mapper key MAUI's stock Android handler ");
    sb.AppendLine("wires up (transitively, including `ViewHandler.ViewMapper` cross-cutting ");
    sb.AppendLine("properties), with a check mark when our handler maps the same key — either ");
    sb.AppendLine("directly or via the shared `ViewHandler.ViewMapper` chain. Fully-covered ");
    sb.AppendLine("handlers (✅) are collapsed; expand to see the wired keys.");
    sb.AppendLine();

    foreach (var s in sortedStats)
    {
        var sh = s.Handler;
        var virtuals = string.Join(", ", stockByVirtualView.GetValueOrDefault(sh.ShortName, new()).Select(v => $"`{v}`"));
        var stockKeys = sh.AllKeys.OrderBy(x => x, StringComparer.Ordinal).ToList();

        if (!s.Covered)
        {
            sb.AppendLine($"### ❌ `{sh.ShortName}` — {virtuals}");
            sb.AppendLine();
            sb.AppendLine("_No Compose backend handler. Stock MAUI handler keeps the AppCompat backend._");
            sb.AppendLine();
            if (stockKeys.Count > 0)
            {
                sb.AppendLine("<details><summary>Stock keys (not covered)</summary>");
                sb.AppendLine();
                foreach (var k in stockKeys)
                    sb.AppendLine($"- [ ] `{k}`");
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }
            continue;
        }

        var icon = (s.Hit == s.Total && s.Total > 0) ? "✅" : "🟡";
        var ourNames = string.Join(", ", s.Ours.Select(o => $"`{o.OurHandler}`").Distinct());
        var extras = s.OurKeys.Except(sh.AllKeys, StringComparer.Ordinal)
                              .OrderBy(x => x, StringComparer.Ordinal)
                              .ToList();
        var pct = s.Total == 0 ? 0 : 100.0 * s.Hit / s.Total;

        sb.AppendLine($"### {icon} `{sh.ShortName}` — {virtuals}");
        sb.AppendLine();
        sb.AppendLine($"Backed by {ourNames}. **{s.Hit} / {s.Total} keys ({pct:F0}%)**.");
        sb.AppendLine();

        // Always show missing keys outside the <details>; they're the action items.
        var missing = stockKeys.Where(k => !s.OurKeys.Contains(k)).ToList();
        if (missing.Count > 0)
        {
            sb.AppendLine("Missing keys:");
            sb.AppendLine();
            foreach (var k in missing)
                sb.AppendLine($"- [ ] `{k}`");
            sb.AppendLine();
        }

        if (extras.Count > 0)
        {
            sb.AppendLine("Extra keys we map (no stock counterpart):");
            sb.AppendLine();
            foreach (var k in extras)
                sb.AppendLine($"- `{k}`");
            sb.AppendLine();
        }

        // Full stock-key list collapsed.
        if (stockKeys.Count > 0)
        {
            sb.AppendLine("<details><summary>All stock keys</summary>");
            sb.AppendLine();
            foreach (var k in stockKeys)
            {
                var mark = s.OurKeys.Contains(k) ? "x" : " ";
                sb.AppendLine($"- [{mark}] `{k}`");
            }
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }
    }

    // ------------------------------------------------------------------------
    // Microsoft.AndroidX.Compose.Maui-only handlers
    // ------------------------------------------------------------------------

    var ourOnly = ourHandlers.Values
        .Where(h => !stockHandlers.ContainsKey(h.ShortName)
                 && !ourHandlerByStock.Values.Any(list => list.Any(o => o.OurHandler == h.ShortName)))
        .OrderBy(h => h.ShortName, StringComparer.Ordinal)
        .ToList();
    if (ourOnly.Count > 0)
    {
        sb.AppendLine("## Microsoft.AndroidX.Compose.Maui-only handlers");
        sb.AppendLine();
        sb.AppendLine("Handlers with no direct stock-MAUI counterpart (typically internal ");
        sb.AppendLine("plumbing — see `docs/maui-backend.md`).");
        sb.AppendLine();
        foreach (var h in ourOnly)
            sb.AppendLine($"- `{h.ShortName}`");
        sb.AppendLine();
    }

    // ------------------------------------------------------------------------
    // Caveats
    // ------------------------------------------------------------------------

    sb.AppendLine("## Caveats");
    sb.AppendLine();
    sb.AppendLine("- Stock-handler key sets are read by **decompiling** MAUI Core via ");
    sb.AppendLine("  `ilspycmd` and regex-parsing the property-mapper field initializers. ");
    sb.AppendLine("  Mapper bases passed via `new PropertyMapper<...>(BaseA, BaseB, ...)` ");
    sb.AppendLine("  are followed transitively, so `ButtonHandler` picks up keys from ");
    sb.AppendLine("  `TextButtonMapper`, `ImageButtonMapper`, and `ViewHandler.ViewMapper`.");
    sb.AppendLine("- Coverage is **key-level**, not behavioural. A `[x]` only means we wire ");
    sb.AppendLine("  the same property name through Compose; it doesn't guarantee identical ");
    sb.AppendLine("  visual / interaction parity (gradient backgrounds, brush strokes, ");
    sb.AppendLine("  per-platform alignment quirks, …).");
    sb.AppendLine("- `ViewHandler.ViewMapper` is shared: every Compose-backed handler in ");
    sb.AppendLine("  this repo gets cross-cutting view properties (`Opacity`, `Translation*`, ");
    sb.AppendLine("  `Scale*`, `Rotation*`, `Clip`, `Shadow`, `Visibility`, ...) via ");
    sb.AppendLine("  `RemapForCompose()` in `Hosting/AppHostBuilderExtensions.cs`, so those ");
    sb.AppendLine("  show as `[x]` for every Compose handler.");
    sb.AppendLine("- `CommandMapper` keys (focus, scroll-to, etc.) are **not** included in ");
    sb.AppendLine("  this report — coverage is `PropertyMapper`-only.");
    sb.AppendLine("- Some stock handlers (`LabelHandler`, `CheckBoxHandler`) aren't registered ");
    sb.AppendLine("  via the regex-scannable `AddHandler<T,H>()` pattern — MAUI wires them up ");
    sb.AppendLine("  via a reflection path. The script falls back to deriving the virtual-view ");
    sb.AppendLine("  name from the handler class name (`XxxHandler` → `Xxx`) for those.");
    sb.AppendLine("- The cache lives under `obj/maui-coverage/`. Delete it to force a clean ");
    sb.AppendLine("  re-decompilation after a MAUI version bump.");
    sb.AppendLine();

    File.WriteAllText(path, sb.ToString());
}

// ----------------------------------------------------------------------------
// Types (must come after all top-level statements)
// ----------------------------------------------------------------------------

sealed class HandlerInfo
{
    public string ShortName { get; init; } = "";
    public bool IsStock { get; init; }
    public Dictionary<string, MapperBlock> Mappers { get; init; } = new(StringComparer.Ordinal);
    public string? VirtualView { get; set; }
    public HashSet<string> AllKeys { get; set; } = new(StringComparer.Ordinal);
}

sealed class MapperBlock
{
    public List<string> BaseRefs { get; init; } = new();
    public HashSet<string> Keys { get; init; } = new(StringComparer.Ordinal);
}

sealed record Registration(string VirtualView, string HandlerShortName);
