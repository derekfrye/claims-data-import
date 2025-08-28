using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectStyleBuilder
{
internal static class Program
{
    private sealed record Options(
        string SolutionOrProject,
        string? PerProject,
        bool WarnAsError,
        bool IncludeUrls
    );

    private static int Main(string[] args)
    {
        if (!TryParseArgs(args, out var options))
        {
            PrintUsage();
            return 2;
        }

        var artifactsDir = Path.GetFullPath("build_artifacts");
        Directory.CreateDirectory(artifactsDir);
        var ts = DateTimeOffset.Now.ToLocalTime().ToString("yyyyMMdd_HHmmss");

        var cleanLog = Path.Combine(artifactsDir, $"clean_{ts}.log");
        var buildLog = Path.Combine(artifactsDir, $"build_{ts}.log");

        // Run dotnet clean
        WriteLine($"== dotnet clean ==\n{options.SolutionOrProject}");
        RunDotnetAndLog(cleanLog, "clean", options.SolutionOrProject);
        CopyLatest(cleanLog, Path.Combine(artifactsDir, "clean.log"));

        // Run dotnet build
        WriteLine($"== dotnet build ==\n{options.SolutionOrProject}{(options.WarnAsError ? " (warnaserror)" : string.Empty)}");
        var buildArgs = new List<string> { "build", options.SolutionOrProject };
        if (options.WarnAsError) buildArgs.Add("-warnaserror");
        RunDotnetAndLog(buildLog, buildArgs.ToArray());
        CopyLatest(buildLog, Path.Combine(artifactsDir, "build.log"));

        // Parse diagnostics from build log
        var logLines = File.ReadAllLines(buildLog);
        var diag = ParseDiagnostics(logLines);

        // Aggregate
        var rulesAll = CountByRule(diag.Entries);
        var rulesMsgMap = BuildRuleMessageMap(diag.Entries, includeUrls: options.IncludeUrls);
        var projectsAll = CountByProject(diag.Entries);

        // If a project filter is provided, compute per-project rules
        Dictionary<string, int>? rulesForProject = null;
        string? projFilterBase = null;
        if (!string.IsNullOrWhiteSpace(options.PerProject))
        {
            projFilterBase = Path.GetFileName(options.PerProject);
            var filtered = diag.Entries.Where(e => string.Equals(e.ProjectBaseName, projFilterBase, StringComparison.OrdinalIgnoreCase));
            rulesForProject = CountByRule(filtered);
        }

        // Write artifacts
        var rulesAllPath = Path.Combine(artifactsDir, $"warnings_by_id_{ts}.txt");
        var projectsAllPath = Path.Combine(artifactsDir, $"warnings_by_project_{ts}.txt");
        var rulesByProjectPath = Path.Combine(artifactsDir, $"warnings_by_id_for_project_{ts}.txt");
        var messagesWithUrlPath = Path.Combine(artifactsDir, $"warnings_messages_{ts}.tsv");
        var messagesShortPath = Path.Combine(artifactsDir, $"warnings_messages_short_{ts}.tsv");
        var samplesPath = Path.Combine(artifactsDir, $"diagnostics_samples_{ts}.txt");

        File.WriteAllLines(messagesWithUrlPath, BuildMessagesTsv(diag.Entries, includeUrls: true));
        File.WriteAllLines(messagesShortPath, BuildMessagesTsv(diag.Entries, includeUrls: false));
        CopyLatest(messagesWithUrlPath, Path.Combine(artifactsDir, "warnings_messages.tsv"));
        CopyLatest(messagesShortPath, Path.Combine(artifactsDir, "warnings_messages_short.tsv"));

        // Enriched rules (All)
        File.WriteAllLines(rulesAllPath, FormatCountsEnriched(rulesAll, rulesMsgMap));
        CopyLatest(rulesAllPath, Path.Combine(artifactsDir, "warnings_by_id.txt"));

        // Projects by diagnostics
        File.WriteAllLines(projectsAllPath, FormatCounts(projectsAll));
        CopyLatest(projectsAllPath, Path.Combine(artifactsDir, "warnings_by_project.txt"));

        // Enriched rules (Per-Project)
        if (rulesForProject is not null)
        {
            File.WriteAllLines(rulesByProjectPath, FormatCountsEnriched(rulesForProject, rulesMsgMap));
            CopyLatest(rulesByProjectPath, Path.Combine(artifactsDir, "warnings_by_id_for_project.txt"));
        }

        // Sample diagnostics
        File.WriteAllLines(samplesPath, diag.SampleLines);
        CopyLatest(samplesPath, Path.Combine(artifactsDir, "diagnostics_samples.txt"));

        // Console summary
        WriteLine("");
        WriteLine("Top rules (all projects):");
        foreach (var line in File.ReadLines(rulesAllPath).Take(10))
            Console.WriteLine(line);

        WriteLine("");
        WriteLine("Top projects by diagnostics:");
        foreach (var line in File.ReadLines(projectsAllPath).Take(10))
            Console.WriteLine(line);

        if (rulesForProject is not null)
        {
            WriteLine("");
            WriteLine($"Top rules for project: {options.PerProject}");
            foreach (var line in File.ReadLines(rulesByProjectPath).Take(15))
                Console.WriteLine(line);
        }

        WriteLine("");
        WriteLine($"Logs: {Path.GetFullPath(Path.Combine(artifactsDir, "clean.log"))} | {Path.GetFullPath(Path.Combine(artifactsDir, "build.log"))}");
        WriteLine($"Summaries: {Path.GetFullPath(Path.Combine(artifactsDir, "warnings_by_id.txt"))} | {Path.GetFullPath(Path.Combine(artifactsDir, "warnings_by_project.txt"))}{(rulesForProject is not null ? " | " + Path.GetFullPath(Path.Combine(artifactsDir, "warnings_by_id_for_project.txt")) : string.Empty)}");
        WriteLine($"Message maps: {Path.GetFullPath(Path.Combine(artifactsDir, "warnings_messages.tsv"))} | {Path.GetFullPath(Path.Combine(artifactsDir, "warnings_messages_short.tsv"))}");

        return 0;
    }

    private static void WriteLine(string s) => Console.WriteLine(s);

    private static void CopyLatest(string src, string dest)
    {
        try
        {
            File.Copy(src, dest, overwrite: true);
        }
        catch
        {
            // ignore
        }
    }

    private static void RunDotnetAndLog(string logPath, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }
        psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";

        using var proc = Process.Start(psi)!;
        using var fs = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var sw = new StreamWriter(fs, new UTF8Encoding(false));
        proc.OutputDataReceived += (_, e) => { if (e.Data is not null) sw.WriteLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) sw.WriteLine(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        sw.Flush();
    }

    private sealed class ParsedDiagnostics
    {
        public List<DiagEntry> Entries { get; set; } = new List<DiagEntry>();
        public List<string> SampleLines { get; set; } = new List<string>();
    }

    private sealed record DiagEntry(string Severity, string Id, string Message, string? ProjectPath)
    {
        public string? ProjectBaseName => ProjectPath is null ? null : Path.GetFileName(ProjectPath);
    }

    // Matches: 
    // /path/File.cs(12,34): warning IDE0065: Message text (https://...) [path/Project.csproj]
    private static readonly Regex LineRegex = new(
        @":\s*(warning|error)\s+(?<id>[A-Z]{2,}\d{3,5}):\s+(?<msg>.*?)(\s*\[(?<proj>[^\]]+\.csproj)\])?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UrlTailRegex = new("\\s*\\(https?://[^)]+\\)\\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static ParsedDiagnostics ParseDiagnostics(IEnumerable<string> lines)
    {
        var entries = new List<DiagEntry>(capacity: 1024);
        var samples = new List<string>(capacity: 64);
        foreach (var line in lines)
        {
            var m = LineRegex.Match(line);
            if (!m.Success) continue;

            var sevIdx = line.IndexOf(": ", StringComparison.Ordinal);
            var sev = sevIdx >= 0 && line.IndexOf("warning", sevIdx, StringComparison.OrdinalIgnoreCase) >= 0 ? "warning" : "error";
            var id = m.Groups["id"].Value;
            var msg = m.Groups["msg"].Value.Trim();
            var proj = m.Groups["proj"].Success ? m.Groups["proj"].Value : null;
            entries.Add(new DiagEntry(sev, id, msg, proj));
            if (samples.Count < 40) samples.Add(line);
        }

        return new ParsedDiagnostics { Entries = entries, SampleLines = samples };
    }

    private static Dictionary<string, int> CountByRule(IEnumerable<DiagEntry> entries)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            dict.TryGetValue(e.Id, out var c);
            dict[e.Id] = c + 1;
        }
        return dict
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> CountByProject(IEnumerable<DiagEntry> entries)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (e.ProjectBaseName is null) continue;
            dict.TryGetValue(e.ProjectBaseName, out var c);
            dict[e.ProjectBaseName] = c + 1;
        }
        return dict
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> BuildRuleMessageMap(IEnumerable<DiagEntry> entries, bool includeUrls)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (dict.ContainsKey(e.Id)) continue;
            var msg = includeUrls ? e.Message : UrlTailRegex.Replace(e.Message, string.Empty).TrimEnd();
            dict[e.Id] = msg;
        }
        return dict;
    }

    private static IEnumerable<string> BuildMessagesTsv(IEnumerable<DiagEntry> entries, bool includeUrls)
    {
        var dict = BuildRuleMessageMap(entries, includeUrls);
        foreach (var kv in dict.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            yield return $"{kv.Key}\t{kv.Value}";
        }
    }

    private static IEnumerable<string> FormatCounts(Dictionary<string, int> counts)
    {
        foreach (var kv in counts)
        {
            yield return $"{kv.Value,7} {kv.Key}";
        }
    }

    private static IEnumerable<string> FormatCountsEnriched(Dictionary<string, int> counts, Dictionary<string, string> msgMap)
    {
        foreach (var kv in counts)
        {
            if (msgMap.TryGetValue(kv.Key, out var msg) && !string.IsNullOrWhiteSpace(msg))
                yield return $"{kv.Value,7} {kv.Key}: {msg}";
            else
                yield return $"{kv.Value,7} {kv.Key}";
        }
    }

    private static bool TryParseArgs(string[] args, out Options opts)
    {
        string solutionOrProject = "ClaimsDataImport.sln";
        string? perProject = null;
        bool warnAsError = false;
        bool includeUrls = false;

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h":
                case "--help":
                    opts = new Options("", null, false, false);
                    return false;
                case "-s":
                case "--solution":
                    if (i + 1 >= args.Length) { opts = new Options("", null, false, false); return false; }
                    solutionOrProject = args[++i];
                    break;
                case "-p":
                case "--project":
                    if (i + 1 >= args.Length) { opts = new Options("", null, false, false); return false; }
                    perProject = args[++i];
                    break;
                case "-w":
                case "--warnaserror":
                    warnAsError = true;
                    break;
                case "-u":
                case "--include-urls":
                    includeUrls = true;
                    break;
                default:
                    // Allow passing solution/project positionally if unknown arg looks like a file
                    if (a.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || a.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        solutionOrProject = a;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Unknown argument: {a}");
                        opts = new Options("", null, false, false);
                        return false;
                    }
                    break;
            }
        }

        opts = new Options(solutionOrProject, perProject, warnAsError, includeUrls);
        return true;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"Usage: ProjectStyleBuilder [-s <solution.sln|project.csproj>] [-p <project.csproj>] [-w] [-u]

Options:
  -s, --solution <path>   Solution file or single project to build (default: ClaimsDataImport.sln)
  -p, --project <csproj>  Project filter for per-project rule summary (optional)
  -w, --warnaserror       Build with -warnaserror (optional)
  -u, --include-urls      Include rule explanation URLs in summaries (optional)
  -h, --help              Show this help

Artifacts are written to build_artifacts/ with timestamped logs and summaries,
and also copied to 'latest' filenames like build_artifacts/warnings_by_id.txt.");
    }
}
}
