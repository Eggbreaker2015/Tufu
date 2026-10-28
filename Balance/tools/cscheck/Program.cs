// cscheck: offline Roslyn checks for the Unity project (design 1.6).
//   check      compile Assembly-CSharp(-Editor) with Unity's .rsp, compile without UNITY_EDITOR,
//              signature lock against signature_6af011f.json, banned APIs in rebuild code
//   signature  write the signature lock for a worktree or a git revision
//   sources    print the predefined-assembly membership of a source tree
// Output is one JSON document (stdout or --json). Exit code: 0 ok, 1 violations, 2 tool error.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Tufu.CsCheck;

sealed class ToolError(string message) : Exception(message);

static class Program
{
	public const string ProjectRel = "Recovery/StudyProject";
	public const string RuntimeAsm = "Assembly-CSharp";
	public const string EditorAsm = "Assembly-CSharp-Editor";

	static int Main(string[] args)
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		Options? opts = null;
		try
		{
			opts = Options.Parse(args);
			return opts.Command switch
			{
				"check" => Checker.Run(opts),
				"signature" => SignatureCommand.Run(opts),
				"sources" => SourcesCommand.Run(opts),
				_ => throw new ToolError($"unknown command '{opts.Command}' (check | signature | sources)"),
			};
		}
		catch (ToolError e)
		{
			Console.Error.WriteLine("cscheck: " + e.Message);
			var doc = new JsonObject { ["schema"] = "tufu.cscheck.result/1", ["ok"] = false, ["tool_error"] = e.Message };
			if (opts?.JsonOut != null)
				Json.Write(opts.JsonOut, doc);
			return 2;
		}
	}
}

sealed class Options
{
	public string Command = "";
	public string? Worktree;
	public string? GitRev;
	public string? Repo;
	public string? RunWorktree;
	public string? JsonOut;
	public string? Out;
	public string? SignatureFile;
	public string? BaselineFile;
	public bool Compile, NoEditor, Signature, Bans;
	public bool UpdateBaseline;
	public bool ListWarnings;

	public static Options Parse(string[] args)
	{
		if (args.Length == 0)
			throw new ToolError("usage: cscheck check|signature|sources [options]");
		var o = new Options { Command = args[0] };
		for (int i = 1; i < args.Length; i++)
		{
			string a = args[i];
			string Next() => i + 1 < args.Length ? args[++i] : throw new ToolError($"{a} needs a value");
			switch (a)
			{
				case "--worktree": o.Worktree = Path.GetFullPath(Next()); break;
				case "--git-rev": o.GitRev = Next(); break;
				case "--repo": o.Repo = Path.GetFullPath(Next()); break;
				case "--run-worktree": o.RunWorktree = Path.GetFullPath(Next()); break;
				case "--json": o.JsonOut = Path.GetFullPath(Next()); break;
				case "--out": o.Out = Path.GetFullPath(Next()); break;
				case "--signature-file": o.SignatureFile = Path.GetFullPath(Next()); break;
				case "--baseline-file": o.BaselineFile = Path.GetFullPath(Next()); break;
				case "--compile": o.Compile = true; break;
				case "--no-editor-define": o.NoEditor = true; break;
				case "--signature": o.Signature = true; break;
				case "--bans": o.Bans = true; break;
				case "--update-baseline": o.UpdateBaseline = true; break;
				case "--list-warnings": o.ListWarnings = true; break;
				default: throw new ToolError($"unknown option {a}");
			}
		}
		if (!(o.Compile || o.NoEditor || o.Signature || o.Bans))
			o.Compile = o.NoEditor = o.Signature = o.Bans = true;
		if (o.RunWorktree == null)
			throw new ToolError("--run-worktree is required (the worktree whose Library holds Unity's .rsp)");
		return o;
	}

	public ISourceTree OpenSourceTree()
	{
		if (GitRev != null)
			return new GitTree(Repo ?? Worktree ?? throw new ToolError("--git-rev needs --repo"), GitRev);
		if (Worktree == null)
			throw new ToolError("--worktree or --git-rev is required");
		return new FileTree(Worktree);
	}
}

static class Json
{
	public static readonly JsonSerializerOptions Opts = new()
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static string Serialize(JsonNode node) => node.ToJsonString(Opts).Replace("\r\n", "\n") + "\n";

	public static void Write(string path, JsonNode node)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, Serialize(node), new UTF8Encoding(false));
	}

	public static JsonArray Arr(IEnumerable<string> items) => new(items.Select(s => (JsonNode?)JsonValue.Create(s)).ToArray());
}

// ---------------------------------------------------------------- sources

sealed record SourceFile(string Path, byte[] Bytes);

interface ISourceTree
{
	string Describe { get; }
	// .cs/.asmdef/.asmref files under Assets, paths relative to the Unity project ("Assets/...").
	IReadOnlyList<SourceFile> LoadAssetSources();
	byte[]? ReadProjectFile(string rel);
}

static class UnityPaths
{
	static readonly string[] Kept = [".cs", ".asmdef", ".asmref"];

	// Unity skips hidden assets: names starting with '.', ending with '~', "cvs" and "*.tmp".
	public static bool IsHidden(string name) =>
		name.StartsWith('.') || name.EndsWith('~') || name.Equals("cvs", StringComparison.OrdinalIgnoreCase)
		|| name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);

	public static bool Keep(string rel)
	{
		if (!Kept.Any(e => rel.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
			return false;
		return !rel.Split('/').Any(IsHidden);
	}
}

sealed class FileTree(string worktree) : ISourceTree
{
	readonly string project = Path.Combine(worktree, Program.ProjectRel);

	public string Describe => worktree;

	public IReadOnlyList<SourceFile> LoadAssetSources()
	{
		string assets = Path.Combine(project, "Assets");
		if (!Directory.Exists(assets))
			throw new ToolError($"no Unity Assets folder at {assets}");
		var paths = new List<string>();
		var stack = new Stack<string>();
		stack.Push(assets);
		while (stack.Count > 0)
		{
			string dir = stack.Pop();
			foreach (string d in Directory.EnumerateDirectories(dir))
				if (!UnityPaths.IsHidden(Path.GetFileName(d)))
					stack.Push(d);
			foreach (string f in Directory.EnumerateFiles(dir))
			{
				string rel = Path.GetRelativePath(project, f).Replace('\\', '/');
				if (UnityPaths.Keep(rel))
					paths.Add(rel);
			}
		}
		paths.Sort(StringComparer.Ordinal);
		var result = new SourceFile[paths.Count];
		Parallel.For(0, paths.Count, i => result[i] = new SourceFile(paths[i], File.ReadAllBytes(Path.Combine(project, paths[i]))));
		return result;
	}

	public byte[]? ReadProjectFile(string rel)
	{
		string p = Path.Combine(project, rel);
		return File.Exists(p) ? File.ReadAllBytes(p) : null;
	}
}

sealed class GitTree(string repo, string rev) : ISourceTree
{
	public string Describe => $"git:{rev}";
	public string Commit => Encoding.UTF8.GetString(Git("rev-parse", "--verify", rev + "^{commit}")).Trim();

	byte[] Git(params string[] args)
	{
		var psi = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true };
		psi.ArgumentList.Add("-C");
		psi.ArgumentList.Add(repo);
		foreach (string a in args)
			psi.ArgumentList.Add(a);
		using var p = Process.Start(psi)!;
		var ms = new MemoryStream();
		var errTask = p.StandardError.ReadToEndAsync();
		p.StandardOutput.BaseStream.CopyTo(ms);
		p.WaitForExit();
		if (p.ExitCode != 0)
			throw new ToolError($"git {string.Join(' ', args)}: {errTask.Result.Trim()}");
		return ms.ToArray();
	}

	public IReadOnlyList<SourceFile> LoadAssetSources()
	{
		string prefix = Program.ProjectRel + "/";
		var entries = new List<(string Rel, string Sha)>();
		string listing = Encoding.UTF8.GetString(Git("ls-tree", "-r", "-z", "--full-tree", rev, "--", prefix + "Assets"));
		foreach (string line in listing.Split('\0', StringSplitOptions.RemoveEmptyEntries))
		{
			int tab = line.IndexOf('\t');
			string[] meta = line[..tab].Split(' ');
			string rel = line[(tab + 1)..][prefix.Length..];
			if (meta[1] == "blob" && UnityPaths.Keep(rel))
				entries.Add((rel, meta[2]));
		}
		entries.Sort((a, b) => string.CompareOrdinal(a.Rel, b.Rel));
		return CatFiles(entries);
	}

	List<SourceFile> CatFiles(List<(string Rel, string Sha)> entries)
	{
		var psi = new ProcessStartInfo("git") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (string a in new[] { "-C", repo, "cat-file", "--batch" })
			psi.ArgumentList.Add(a);
		using var p = Process.Start(psi)!;
		var writer = Task.Run(() =>
		{
			foreach (var e in entries)
				p.StandardInput.Write(e.Sha + "\n");
			p.StandardInput.Close();
		});
		var errTask = p.StandardError.ReadToEndAsync();
		var output = p.StandardOutput.BaseStream;
		var result = new List<SourceFile>(entries.Count);
		foreach (var e in entries)
		{
			string header = ReadLine(output);
			string[] parts = header.Split(' ');
			if (parts.Length < 3 || parts[1] != "blob")
				throw new ToolError($"git cat-file: unexpected header '{header}' for {e.Rel}");
			int size = int.Parse(parts[2], CultureInfo.InvariantCulture);
			var buf = new byte[size];
			output.ReadExactly(buf);
			output.ReadByte();
			result.Add(new SourceFile(e.Rel, buf));
		}
		writer.Wait();
		p.WaitForExit();
		_ = errTask.Result;
		return result;
	}

	static string ReadLine(Stream s)
	{
		var sb = new StringBuilder();
		int b;
		while ((b = s.ReadByte()) != -1 && b != '\n')
			sb.Append((char)b);
		return sb.ToString();
	}

	public byte[]? ReadProjectFile(string rel)
	{
		try
		{
			return Git("show", $"{rev}:{Program.ProjectRel}/{rel}");
		}
		catch (ToolError)
		{
			return null;
		}
	}
}

// Unity's predefined-assembly rules: scripts under a folder with an .asmdef/.asmref belong to
// that assembly; top-level Plugins/Standard Assets/Pro Standard Assets go to *-firstpass;
// any folder named Editor sends a script to the Editor variant; everything else is Assembly-CSharp.
static class Membership
{
	static readonly string[] FirstPassRoots = ["Plugins", "Standard Assets", "Pro Standard Assets"];

	public static Dictionary<string, List<SourceFile>> Assign(IReadOnlyList<SourceFile> files)
	{
		var asmDirs = files.Where(f => f.Path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) || f.Path.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase))
			.Select(f => f.Path[..f.Path.LastIndexOf('/')]).ToHashSet(StringComparer.Ordinal);
		var result = new Dictionary<string, List<SourceFile>>(StringComparer.Ordinal);
		foreach (var f in files.Where(f => f.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
		{
			string asm = AssemblyFor(f.Path, asmDirs);
			if (!result.TryGetValue(asm, out var list))
				result[asm] = list = [];
			list.Add(f);
		}
		return result;
	}

	static string AssemblyFor(string rel, HashSet<string> asmDirs)
	{
		string[] parts = rel.Split('/');
		for (int i = parts.Length - 1; i >= 1; i--)
		{
			string dir = string.Join('/', parts, 0, i);
			if (asmDirs.Contains(dir))
				return "asmdef:" + dir;
		}
		bool firstpass = parts.Length > 2 && FirstPassRoots.Any(r => r.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
		bool editor = parts.Skip(1).Take(parts.Length - 2).Any(p => p.Equals("Editor", StringComparison.OrdinalIgnoreCase));
		return Program.RuntimeAsm + (editor ? "-Editor" : "") + (firstpass ? "-firstpass" : "");
	}

	public static JsonObject Describe(Dictionary<string, List<SourceFile>> groups, UnityProject unity, bool listFiles, List<string>? failures)
	{
		var doc = new JsonObject();
		foreach (var (name, files) in groups.OrderBy(g => g.Key, StringComparer.Ordinal))
		{
			var entry = new JsonObject { ["count"] = files.Count };
			if (unity.Rsps.TryGetValue(name, out var rsp))
			{
				var (added, removed) = rsp.DiffSources(files.Select(f => f.Path));
				entry["rsp_count"] = rsp.Sources.Count;
				entry["added"] = Json.Arr(added);
				entry["removed"] = Json.Arr(removed);
			}
			else
			{
				failures?.Add($"sources in {name} cannot be checked (no .rsp for it): {files.Count} file(s), e.g. {files[0].Path}");
			}
			if (listFiles)
				entry["files"] = Json.Arr(files.Select(f => f.Path));
			doc[name] = entry;
		}
		return doc;
	}
}

static class SourcesCommand
{
	public static int Run(Options o)
	{
		var unity = UnityProject.Load(o.RunWorktree!);
		var tree = o.OpenSourceTree();
		var groups = Membership.Assign(tree.LoadAssetSources());
		var doc = new JsonObject
		{
			["schema"] = "tufu.cscheck.sources/1",
			["tree"] = tree.Describe,
			["assemblies"] = Membership.Describe(groups, unity, listFiles: true, failures: null),
		};
		string text = Json.Serialize(doc);
		if (o.JsonOut != null)
			File.WriteAllText(o.JsonOut, text);
		else
			Console.Write(text);
		return 0;
	}
}

// ---------------------------------------------------------------- Unity response files

sealed class Rsp
{
	public string File = "";
	public List<string> Defines = [];
	public List<string> References = [];   // absolute paths
	public List<string> Sources = [];      // "Assets/..." as Unity wrote them
	public LanguageVersion LangVersion = LanguageVersion.Default;
	public bool Unsafe;
	public NullableContextOptions Nullable = NullableContextOptions.Disable;
	public int WarningLevel = 4;
	public List<string> NoWarn = [];
	public bool WarnAsError;
	public bool CheckOverflow;
	public List<string> Analyzers = [];
	public List<string> Ignored = [];

	static readonly char[] ListSeparators = [';', ','];

	static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
	{ "target", "out", "refout", "optimize+", "optimize-", "optimize", "debug", "nologo", "runtimemetadataversion", "utf8output", "preferreduilang", "additionalfile", "deterministic", "deterministic+", "noconfig", "nostdlib", "nostdlib+" };

	public static Rsp Parse(string path, string projectDir)
	{
		var rsp = new Rsp { File = path };
		foreach (string raw in System.IO.File.ReadAllLines(path))
		{
			string line = raw.Trim();
			if (line.Length == 0)
				continue;
			if (line[0] != '-' && line[0] != '/')
			{
				rsp.Sources.Add(Unquote(line).Replace('\\', '/'));
				continue;
			}
			int colon = line.IndexOf(':');
			string name = (colon < 0 ? line[1..] : line[1..colon]).ToLowerInvariant();
			string value = colon < 0 ? "" : Unquote(line[(colon + 1)..]);
			switch (name)
			{
				case "define":
				case "d":
					rsp.Defines.AddRange(value.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries));
					break;
				case "r":
				case "reference":
					rsp.References.Add(Path.IsPathRooted(value) ? value : Path.GetFullPath(Path.Combine(projectDir, value)));
					break;
				case "langversion":
					if (!LanguageVersionFacts.TryParse(value, out rsp.LangVersion))
						throw new ToolError($"{path}: unknown -langversion:{value}");
					break;
				case "unsafe":
				case "unsafe+":
					rsp.Unsafe = true;
					break;
				case "unsafe-":
					rsp.Unsafe = false;
					break;
				case "nullable":
				case "nullable+":
					rsp.Nullable = value.Length == 0 ? NullableContextOptions.Enable : Enum.Parse<NullableContextOptions>(value, true);
					break;
				case "nullable-":
					rsp.Nullable = NullableContextOptions.Disable;
					break;
				case "warn":
					rsp.WarningLevel = int.Parse(value, CultureInfo.InvariantCulture);
					break;
				case "nowarn":
					rsp.NoWarn.AddRange(value.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries).Select(NormalizeId));
					break;
				case "warnaserror":
				case "warnaserror+":
					if (value.Length == 0)
						rsp.WarnAsError = true;
					else
						rsp.Ignored.Add(line);
					break;
				case "checked":
				case "checked+":
					rsp.CheckOverflow = true;
					break;
				case "analyzer":
				case "a":
					rsp.Analyzers.Add(value);
					break;
				default:
					if (!Known.Contains(name))
						rsp.Ignored.Add(line);
					break;
			}
		}
		return rsp;
	}

	static string NormalizeId(string id) => id.All(char.IsDigit) ? "CS" + id.PadLeft(4, '0') : id;

	static string Unquote(string s) => s.Length >= 2 && s[0] == '"' && s[^1] == '"' ? s[1..^1] : s;

	public (List<string> Added, List<string> Removed) DiffSources(IEnumerable<string> current)
	{
		var cur = current.ToHashSet(StringComparer.Ordinal);
		var old = Sources.ToHashSet(StringComparer.Ordinal);
		return (cur.Where(p => !old.Contains(p)).OrderBy(p => p, StringComparer.Ordinal).ToList(),
			old.Where(p => !cur.Contains(p)).OrderBy(p => p, StringComparer.Ordinal).ToList());
	}
}

sealed class UnityProject
{
	public string Worktree = "";
	public string ProjectDir = "";
	public string ArtifactsDir = "";
	public Dictionary<string, Rsp> Rsps = new(StringComparer.Ordinal);

	public static UnityProject Load(string runWorktree)
	{
		string project = Path.Combine(runWorktree, Program.ProjectRel);
		string bee = Path.Combine(project, "Library", "Bee", "artifacts");
		var candidates = Directory.Exists(bee)
			? Directory.GetDirectories(bee).Where(d => System.IO.File.Exists(Path.Combine(d, Program.RuntimeAsm + ".rsp"))).ToList()
			: [];
		if (candidates.Count == 0)
			throw new ToolError($"no Library/Bee/artifacts/*/{Program.RuntimeAsm}.rsp under {project}; open the run worktree once in Unity (batchmode) so it compiles");
		string dag = candidates.OrderByDescending(d => System.IO.File.GetLastWriteTimeUtc(Path.Combine(d, Program.RuntimeAsm + ".rsp"))).First();
		var u = new UnityProject { Worktree = runWorktree, ProjectDir = project, ArtifactsDir = dag };
		foreach (string asm in new[] { Program.RuntimeAsm, Program.EditorAsm })
		{
			string path = Path.Combine(dag, asm + ".rsp");
			if (!System.IO.File.Exists(path))
				throw new ToolError($"missing {path}; open the run worktree once in Unity so it compiles");
			u.Rsps[asm] = Rsp.Parse(path, project);
		}
		string ownRef = Path.Combine(dag, Program.RuntimeAsm + ".ref.dll");
		var missing = u.Rsps.Values.SelectMany(r => r.References).Distinct().Where(p => p != ownRef && !System.IO.File.Exists(p)).ToList();
		if (missing.Count > 0)
			throw new ToolError($"{missing.Count} DLL(s) referenced by the .rsp are missing (first: {missing[0]}); let Unity compile the run worktree once (-batchmode -quit) and retry");
		return u;
	}

	// The .rsp references describe the run worktree's packages; a target with different
	// Packages/ (or a run worktree whose Packages/ changed after the last compile) cannot be checked.
	public List<string> PackageProblems(ISourceTree target)
	{
		var problems = new List<string>();
		// Bee rewrites tundra.log.json on every script compile, the .rsp only when its content changes.
		string tundraLog = Path.Combine(ProjectDir, "Library", "Bee", "tundra.log.json");
		DateTime compiled = Rsps.Values.Select(r => System.IO.File.GetLastWriteTimeUtc(r.File))
			.Append(System.IO.File.Exists(tundraLog) ? System.IO.File.GetLastWriteTimeUtc(tundraLog) : DateTime.MinValue).Max();
		foreach (string rel in new[] { "Packages/manifest.json", "Packages/packages-lock.json" })
		{
			string runFile = Path.Combine(ProjectDir, rel);
			byte[]? run = System.IO.File.Exists(runFile) ? System.IO.File.ReadAllBytes(runFile) : null;
			byte[]? tgt = target.ReadProjectFile(rel);
			if (run == null || tgt == null || !run.AsSpan().SequenceEqual(tgt))
				problems.Add($"{rel} differs between {target.Describe} and the run worktree; check out that commit in the run worktree and let Unity compile it once");
			if (run != null && System.IO.File.GetLastWriteTimeUtc(runFile) > compiled)
				problems.Add($"run worktree {rel} changed after Unity last compiled it; let Unity compile the run worktree once");
		}
		return problems;
	}
}

// ---------------------------------------------------------------- compilation

sealed class Build
{
	public required string Name;
	public required CSharpCompilation Compilation;
	public required IReadOnlyList<SyntaxTree> Trees;
}

static class Compiler
{
	static readonly ConcurrentDictionary<string, MetadataReference> RefCache = new(StringComparer.Ordinal);

	static MetadataReference Ref(string path) => RefCache.GetOrAdd(path, p => MetadataReference.CreateFromFile(p));

	public static IReadOnlyList<SyntaxTree> Parse(IReadOnlyList<SourceFile> files, CSharpParseOptions options)
	{
		var trees = new SyntaxTree[files.Count];
		Parallel.For(0, files.Count, i =>
		{
			var text = SourceText.From(files[i].Bytes, files[i].Bytes.Length, encoding: null, checksumAlgorithm: SourceHashAlgorithm.Sha256, throwIfBinaryDetected: false, canBeEmbedded: false);
			trees[i] = CSharpSyntaxTree.ParseText(text, options, files[i].Path);
		});
		return trees;
	}

	public static CSharpParseOptions ParseOptions(Rsp rsp, IEnumerable<string> defines) =>
		new(rsp.LangVersion, DocumentationMode.None, SourceCodeKind.Regular, defines.Distinct(StringComparer.Ordinal).ToArray());

	public static Build Create(string name, Rsp rsp, IReadOnlyList<SyntaxTree> trees, IEnumerable<MetadataReference> refs)
	{
		var options = new CSharpCompilationOptions(
			OutputKind.DynamicallyLinkedLibrary,
			optimizationLevel: OptimizationLevel.Release,
			checkOverflow: rsp.CheckOverflow,
			allowUnsafe: rsp.Unsafe,
			nullableContextOptions: rsp.Nullable,
			warningLevel: rsp.WarningLevel,
			generalDiagnosticOption: rsp.WarnAsError ? ReportDiagnostic.Error : ReportDiagnostic.Default,
			specificDiagnosticOptions: rsp.NoWarn.Select(id => KeyValuePair.Create(id, ReportDiagnostic.Suppress)),
			concurrentBuild: true,
			deterministic: true,
			assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
		var compilation = CSharpCompilation.Create(name, trees, refs, options);
		return new Build { Name = name, Compilation = compilation, Trees = trees };
	}

	public static IEnumerable<MetadataReference> References(IEnumerable<string> paths) => paths.Select(Ref).ToList();

	// Order the target's sources like Unity's .rsp (diagnostic order), new files last.
	public static List<SourceFile> InRspOrder(Rsp rsp, IReadOnlyList<SourceFile> files)
	{
		var index = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < rsp.Sources.Count; i++)
			index.TryAdd(rsp.Sources[i], i);
		return files.OrderBy(f => index.TryGetValue(f.Path, out int i) ? i : int.MaxValue).ThenBy(f => f.Path, StringComparer.Ordinal).ToList();
	}
}

static class PlayerApprox
{
	// Approximates a player build: no UNITY_EDITOR* defines, no UnityEditor* / *.Editor
	// assemblies and no editor-only plugins (PluginImporter enables only Editor).
	public static bool IsEditorDefine(string d) => d.StartsWith("UNITY_EDITOR", StringComparison.Ordinal);

	public static bool IsEditorReference(string path)
	{
		string name = Path.GetFileName(path);
		if (name.StartsWith("UnityEditor", StringComparison.Ordinal))
			return true;
		if (name.EndsWith(".Editor.dll", StringComparison.Ordinal) || name.EndsWith(".Editor.ref.dll", StringComparison.Ordinal))
			return true;
		return IsEditorOnlyPlugin(path + ".meta");
	}

	static bool IsEditorOnlyPlugin(string meta)
	{
		if (!File.Exists(meta))
			return false;
		var enabled = new Dictionary<string, bool>(StringComparer.Ordinal);
		bool inPlatforms = false;
		string? platform = null;
		foreach (string line in File.ReadLines(meta))
		{
			if (line.StartsWith("  platformData:", StringComparison.Ordinal))
			{
				inPlatforms = true;
				continue;
			}
			if (!inPlatforms)
				continue;
			if (!line.StartsWith("   ", StringComparison.Ordinal))
				break;
			if (line.StartsWith("    ", StringComparison.Ordinal) && !line.StartsWith("     ", StringComparison.Ordinal) && line.TrimEnd().EndsWith(':'))
				platform = line.Trim().TrimEnd(':');
			else if (platform != null && line.Trim().StartsWith("enabled:", StringComparison.Ordinal))
				enabled[platform] = line.Trim().EndsWith('1');
		}
		return enabled.GetValueOrDefault("Editor") && enabled.Where(kv => kv.Key != "Editor").All(kv => !kv.Value);
	}
}

static class Diags
{
	public sealed record Item(string File, int Line, int Col, string Id, string Severity, string Message)
	{
		public string Key => $"{File}|{Id}|{Message}";
		public JsonObject ToJson() => new() { ["file"] = File, ["line"] = Line, ["col"] = Col, ["id"] = Id, ["message"] = Message };
	}

	public static List<Item> Collect(Compilation c)
	{
		var list = new List<Item>();
		foreach (var d in c.GetDiagnostics())
		{
			if (d.IsSuppressed || d.Severity is DiagnosticSeverity.Hidden or DiagnosticSeverity.Info)
				continue;
			var span = d.Location.GetLineSpan();
			string file = d.Location.IsInSource ? span.Path : "";
			list.Add(new Item(file, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, d.Id,
				d.Severity == DiagnosticSeverity.Error ? "error" : "warning", d.GetMessage(CultureInfo.InvariantCulture)));
		}
		list.Sort((a, b) =>
		{
			int r = string.CompareOrdinal(a.File, b.File);
			if (r == 0) r = a.Line.CompareTo(b.Line);
			if (r == 0) r = a.Col.CompareTo(b.Col);
			if (r == 0) r = string.CompareOrdinal(a.Id, b.Id);
			return r == 0 ? string.CompareOrdinal(a.Message, b.Message) : r;
		});
		return list;
	}

	public static SortedDictionary<string, int> CountById(IEnumerable<Item> items)
	{
		var d = new SortedDictionary<string, int>(StringComparer.Ordinal);
		foreach (var i in items)
			d[i.Id] = d.GetValueOrDefault(i.Id) + 1;
		return d;
	}

	public static JsonObject ToJson(IDictionary<string, int> counts)
	{
		var o = new JsonObject();
		foreach (var (k, v) in counts)
			o[k] = v;
		return o;
	}

	// Multiset difference on line-independent keys.
	public static List<string> Minus(IEnumerable<string> a, IEnumerable<string> b)
	{
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (string k in b)
			counts[k] = counts.GetValueOrDefault(k) + 1;
		var result = new List<string>();
		foreach (string k in a)
		{
			if (counts.GetValueOrDefault(k) > 0)
				counts[k]--;
			else
				result.Add(k);
		}
		result.Sort(StringComparer.Ordinal);
		return result;
	}
}

// ---------------------------------------------------------------- signature lock

static class Signature
{
	public static readonly string[] LockedTypes =
	[
		"Codebase.Simulation.Systems.SimulationSystem",
		"Codebase.Simulation.Systems.SpawnerData",
		"Codebase.Simulation.SimulationConfig",
	];

	public const string RebuildPrefix = "Rebuild";
	public const string RebuildAttribute = "SimulationRebuildAttribute";

	static readonly SymbolDisplayFormat TypeFmt = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
			| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
			| SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

	static readonly SymbolDisplayFormat MemberFmt = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance,
		memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeType
			| SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeRef | SymbolDisplayMemberOptions.IncludeConstantValue
			| SymbolDisplayMemberOptions.IncludeExplicitInterface,
		kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword,
		propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
		parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue
			| SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeExtensionThis,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
			| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
			| SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

	static string T(ITypeSymbol t) => t.ToDisplayString(TypeFmt);

	public static string Acc(Accessibility a) => a switch
	{
		Accessibility.Public => "public",
		Accessibility.Private => "private",
		Accessibility.Internal => "internal",
		Accessibility.Protected => "protected",
		Accessibility.ProtectedOrInternal => "protected internal",
		Accessibility.ProtectedAndInternal => "private protected",
		_ => a.ToString().ToLowerInvariant(),
	};

	static string RefKindName(RefKind k) => k switch
	{
		RefKind.None => "",
		RefKind.Ref => "ref",
		RefKind.Out => "out",
		RefKind.In => "in",
		_ => k.ToString().ToLowerInvariant(),
	};

	public static string MetadataFullName(INamedTypeSymbol t)
	{
		if (t.ContainingType != null)
			return MetadataFullName(t.ContainingType) + "+" + t.MetadataName;
		string ns = t.ContainingNamespace.IsGlobalNamespace ? "" : t.ContainingNamespace.ToDisplayString() + ".";
		return ns + t.MetadataName;
	}

	public static bool Included(ISymbol m)
	{
		if (m is IMethodSymbol ms)
		{
			if (ms.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise)
				return false;
			if (ms.IsImplicitlyDeclared)
				return ms.MethodKind == MethodKind.Constructor && !ms.IsStatic;
			return true;
		}
		if (m.IsImplicitlyDeclared)
			return false;
		return m is IFieldSymbol or IPropertySymbol or IEventSymbol or INamedTypeSymbol;
	}

	public static string Key(ISymbol m) => m switch
	{
		INamedTypeSymbol nt => $"type:{nt.MetadataName}",
		IMethodSymbol ms => $"method:{ms.MetadataName}({string.Join(", ", ms.Parameters.Select(ParamKey))})",
		IPropertySymbol { IsIndexer: true } p => $"indexer:this[{string.Join(", ", p.Parameters.Select(ParamKey))}]",
		IPropertySymbol p => $"property:{p.MetadataName}",
		IFieldSymbol f => $"field:{f.Name}",
		IEventSymbol e => $"event:{e.Name}",
		_ => $"other:{m.Name}",
	};

	static string ParamKey(IParameterSymbol p)
	{
		string rk = RefKindName(p.RefKind);
		return (rk.Length > 0 ? rk + " " : "") + T(p.Type);
	}

	static string Kind(ISymbol m) => m switch
	{
		INamedTypeSymbol => "nested_type",
		IMethodSymbol ms => ms.MethodKind switch
		{
			MethodKind.Constructor => "constructor",
			MethodKind.StaticConstructor => "static_constructor",
			MethodKind.Destructor => "destructor",
			MethodKind.UserDefinedOperator => "operator",
			MethodKind.Conversion => "conversion",
			_ => "method",
		},
		IPropertySymbol { IsIndexer: true } => "indexer",
		IPropertySymbol => "property",
		IFieldSymbol => "field",
		IEventSymbol => "event",
		_ => "other",
	};

	static List<string> Modifiers(ISymbol m)
	{
		var mods = new List<string>();
		if (m is IFieldSymbol { IsConst: true })
			mods.Add("const");
		else if (m.IsStatic)
			mods.Add("static");
		if (m.IsAbstract && m is not INamedTypeSymbol { TypeKind: TypeKind.Interface })
			mods.Add("abstract");
		if (m.IsVirtual)
			mods.Add("virtual");
		if (m.IsOverride)
			mods.Add("override");
		if (m.IsSealed && m is not INamedTypeSymbol { TypeKind: TypeKind.Struct or TypeKind.Enum or TypeKind.Delegate })
			mods.Add("sealed");
		if (m.IsExtern)
			mods.Add("extern");
		switch (m)
		{
			case IFieldSymbol f:
				if (f.IsReadOnly) mods.Add("readonly");
				if (f.IsVolatile) mods.Add("volatile");
				if (f.IsFixedSizeBuffer) mods.Add("fixed");
				break;
			case IMethodSymbol ms:
				if (ms.IsReadOnly) mods.Add("readonly");
				break;
			case INamedTypeSymbol nt:
				if (nt.IsReadOnly) mods.Add("readonly");
				if (nt.IsRefLikeType) mods.Add("ref");
				if (nt.IsRecord) mods.Add("record");
				break;
		}
		return mods;
	}

	static IEnumerable<string> Attributes(ImmutableArray<AttributeData> attrs) =>
		attrs.Where(a => a.AttributeClass?.Name != RebuildAttribute)
			.Select(a =>
			{
				var args = a.ConstructorArguments.Select(c => c.ToCSharpString())
					.Concat(a.NamedArguments.Select(n => $"{n.Key} = {n.Value.ToCSharpString()}"));
				return $"{(a.AttributeClass is null ? "?" : T(a.AttributeClass))}({string.Join(", ", args)})";
			})
			.OrderBy(s => s, StringComparer.Ordinal);

	static string DefaultValue(IParameterSymbol p)
	{
		object? v = p.ExplicitDefaultValue;
		if (v == null)
			return "null";
		var type = p.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } n ? n.TypeArguments[0] : p.Type;
		if (type.TypeKind == TypeKind.Enum)
		{
			var member = type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, v));
			if (member != null)
				return T(type) + "." + member.Name;
		}
		return SymbolDisplay.FormatPrimitive(v, quoteStrings: true, useHexadecimalNumbers: false) + v switch
		{
			float => "f",
			double => "d",
			decimal => "m",
			long => "L",
			uint => "u",
			ulong => "UL",
			_ => "",
		};
	}

	static JsonObject Parameter(IParameterSymbol p)
	{
		var o = new JsonObject
		{
			["name"] = p.Name,
			["type"] = T(p.Type),
			["ref_kind"] = RefKindName(p.RefKind),
			["params"] = p.IsParams,
			["optional"] = p.IsOptional,
		};
		if (p.HasExplicitDefaultValue)
			o["default"] = DefaultValue(p);
		var attrs = Attributes(p.GetAttributes()).ToList();
		if (attrs.Count > 0)
			o["attributes"] = Json.Arr(attrs);
		return o;
	}

	static JsonArray TypeParameters(ImmutableArray<ITypeParameterSymbol> tps) => new(tps.Select(tp =>
	{
		var cons = new List<string>();
		if (tp.HasReferenceTypeConstraint) cons.Add("class");
		if (tp.HasValueTypeConstraint) cons.Add("struct");
		if (tp.HasUnmanagedTypeConstraint) cons.Add("unmanaged");
		if (tp.HasNotNullConstraint) cons.Add("notnull");
		cons.AddRange(tp.ConstraintTypes.Select(T));
		if (tp.HasConstructorConstraint) cons.Add("new()");
		return (JsonNode?)new JsonObject { ["name"] = tp.Name, ["variance"] = tp.Variance.ToString(), ["constraints"] = Json.Arr(cons) };
	}).ToArray());

	static string Display(ISymbol m) => m is INamedTypeSymbol nt
		? $"{Acc(nt.DeclaredAccessibility)} {string.Join("", Modifiers(nt).Select(x => x + " "))}{nt.TypeKind.ToString().ToLowerInvariant()} {T(nt)}"
		: m.ToDisplayString(MemberFmt);

	public static JsonObject Member(ISymbol m)
	{
		var o = new JsonObject
		{
			["key"] = Key(m),
			["kind"] = Kind(m),
			["name"] = m.Name,
			["accessibility"] = Acc(m.DeclaredAccessibility),
			["modifiers"] = Json.Arr(Modifiers(m)),
		};
		switch (m)
		{
			case IFieldSymbol f:
				o["type"] = T(f.Type);
				if (f.HasConstantValue)
					o["constant"] = f.ConstantValue == null ? "null" : SymbolDisplay.FormatPrimitive(f.ConstantValue, quoteStrings: true, useHexadecimalNumbers: false);
				break;
			case IPropertySymbol p:
				o["type"] = T(p.Type);
				o["ref_kind"] = p.ReturnsByRefReadonly ? "ref readonly" : p.ReturnsByRef ? "ref" : "";
				o["parameters"] = new JsonArray(p.Parameters.Select(x => (JsonNode?)Parameter(x)).ToArray());
				o["get"] = p.GetMethod == null ? null : Acc(p.GetMethod.DeclaredAccessibility);
				o["set"] = p.SetMethod == null ? null : (p.SetMethod.IsInitOnly ? "init " : "") + Acc(p.SetMethod.DeclaredAccessibility);
				break;
			case IEventSymbol e:
				o["type"] = T(e.Type);
				break;
			case IMethodSymbol ms:
				o["return_type"] = T(ms.ReturnType);
				o["ref_kind"] = ms.ReturnsByRefReadonly ? "ref readonly" : ms.ReturnsByRef ? "ref" : "";
				o["type_parameters"] = TypeParameters(ms.TypeParameters);
				o["parameters"] = new JsonArray(ms.Parameters.Select(x => (JsonNode?)Parameter(x)).ToArray());
				var ret = Attributes(ms.GetReturnTypeAttributes()).ToList();
				if (ret.Count > 0)
					o["return_attributes"] = Json.Arr(ret);
				break;
			case INamedTypeSymbol nt:
				o["type_kind"] = nt.TypeKind.ToString();
				break;
		}
		o["attributes"] = Json.Arr(Attributes(m.GetAttributes()));
		o["display"] = Display(m);
		return o;
	}

	public static JsonObject TypeHeader(INamedTypeSymbol t) => new()
	{
		["name"] = MetadataFullName(t),
		["type_kind"] = t.TypeKind.ToString(),
		["accessibility"] = Acc(t.DeclaredAccessibility),
		["modifiers"] = Json.Arr(Modifiers(t)),
		["base"] = t.BaseType == null ? null : T(t.BaseType),
		["interfaces"] = Json.Arr(t.Interfaces.Select(T).OrderBy(s => s, StringComparer.Ordinal)),
		["type_parameters"] = TypeParameters(t.TypeParameters),
		["attributes"] = Json.Arr(Attributes(t.GetAttributes())),
	};

	static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol t)
	{
		yield return t;
		foreach (var n in t.GetTypeMembers().OrderBy(x => x.MetadataName, StringComparer.Ordinal))
			foreach (var x in WithNested(n))
				yield return x;
	}

	static IEnumerable<ISymbol> Members(INamedTypeSymbol t) =>
		t.GetMembers().Where(Included).OrderBy(Key, StringComparer.Ordinal);

	static INamedTypeSymbol Resolve(Compilation c, string metadataName) =>
		c.GetTypeByMetadataName(metadataName) ?? throw new ToolError($"type {metadataName} not found in {c.AssemblyName}");

	public static JsonObject Generate(Compilation c, string source, string? commit)
	{
		var types = new JsonArray();
		foreach (string name in LockedTypes)
			foreach (var t in WithNested(Resolve(c, name)))
			{
				var entry = TypeHeader(t);
				entry["members"] = new JsonArray(Members(t).Select(m => (JsonNode?)Member(m)).ToArray());
				types.Add(entry);
			}
		return new JsonObject
		{
			["schema"] = "tufu.cscheck.signature/1",
			["source"] = source,
			["commit"] = commit,
			["locked_types"] = Json.Arr(LockedTypes),
			["note"] = "Generated by cscheck from Assembly-CSharp semantics (Unity .rsp references and defines). The SimulationRebuild attribute is ignored; accessors, backing fields and implicit static constructors are omitted; nested types are locked recursively.",
			["types"] = types,
		};
	}

	public static JsonArray Check(Compilation c, JsonObject locked)
	{
		var violations = new List<JsonObject>();
		void Add(string rule, string type, string? member, string detail, JsonNode? expected = null, JsonNode? actual = null)
		{
			var v = new JsonObject { ["rule"] = rule, ["type"] = type, ["member"] = member, ["detail"] = detail };
			if (expected != null) v["expected"] = expected;
			if (actual != null) v["actual"] = actual;
			violations.Add(v);
		}
		foreach (var typeNode in locked["types"]!.AsArray())
		{
			var lockedType = typeNode!.AsObject();
			string name = (string)lockedType["name"]!;
			var t = c.GetTypeByMetadataName(name);
			if (t == null)
			{
				Add("type_removed", name, null, "locked type no longer exists");
				continue;
			}
			var header = TypeHeader(t);
			var expectedHeader = lockedType.DeepClone().AsObject();
			expectedHeader.Remove("members");
			if (!JsonNode.DeepEquals(header, expectedHeader))
				Add("type_changed", name, null, "type declaration changed: " + string.Join(", ", DiffFields(expectedHeader, header)), expectedHeader, header);
			var expected = lockedType["members"]!.AsArray().Select(n => n!.AsObject()).ToDictionary(n => (string)n["key"]!, StringComparer.Ordinal);
			var current = Members(t).ToDictionary(Key, StringComparer.Ordinal);
			foreach (var (key, exp) in expected)
			{
				if (!current.TryGetValue(key, out var sym))
				{
					Add("member_removed", name, key, $"locked member missing: {exp["display"]}", exp["display"]?.DeepClone());
					continue;
				}
				var act = Member(sym);
				if (!JsonNode.DeepEquals(exp, act))
					Add("member_changed", name, key, $"{string.Join(", ", DiffFields(exp, act))} changed: {exp["display"]} -> {act["display"]}", exp.DeepClone(), act);
			}
			foreach (var (key, sym) in current)
			{
				if (expected.ContainsKey(key))
					continue;
				string display = Display(sym);
				if (sym.DeclaredAccessibility is not (Accessibility.Private or Accessibility.Internal))
					Add("new_member_accessibility", name, key, $"new member must be private or internal, is {Acc(sym.DeclaredAccessibility)}: {display}", null, display);
				if (!sym.Name.StartsWith(RebuildPrefix, StringComparison.Ordinal))
					Add("new_member_name", name, key, $"new member name must start with '{RebuildPrefix}': {display}", null, display);
			}
		}
		return new JsonArray(violations.Select(v => (JsonNode?)v).ToArray());
	}

	static IEnumerable<string> DiffFields(JsonObject a, JsonObject b) =>
		a.Select(kv => kv.Key).Union(b.Select(kv => kv.Key)).Where(k => k != "display" && !JsonNode.DeepEquals(a[k], b[k]));
}

// ---------------------------------------------------------------- banned APIs

static class Bans
{
	// Rebuild code (design 1.5 C# rule 12, 1.6 item 3):
	//   whole files: everything under CodeBase/Simulation/Rebuild/, every Systems/SimulationSystem*.cs,
	//     and any other file declaring part of Codebase.Simulation.Systems.SimulationSystem;
	//   members: anywhere in Assembly-CSharp a member carrying [SimulationRebuild]; inside the locked
	//     types (SpawnerData, SimulationConfig) a member whose name starts with "Rebuild";
	//     everything nested inside such a member is in scope too.
	public const string RebuildDir = "Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Rebuild/";
	public const string SystemsDir = "Assets/Scripts/Assembly-CSharp/CodeBase/Simulation/Systems/";

	public static bool IsRebuildFile(string rel) =>
		rel.StartsWith(RebuildDir, StringComparison.Ordinal)
		|| (rel.StartsWith(SystemsDir, StringComparison.Ordinal) && rel.IndexOf('/', SystemsDir.Length) < 0
			&& Path.GetFileName(rel).StartsWith("SimulationSystem", StringComparison.Ordinal));

	static bool DeclaresSimulationSystem(SyntaxNode root, SemanticModel model) =>
		root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax)
			.OfType<ClassDeclarationSyntax>()
			.Any(c => c.Identifier.ValueText == "SimulationSystem"
				&& model.GetDeclaredSymbol(c) is { } s && Signature.MetadataFullName(s) == Signature.LockedTypes[0]);

	static bool HasRebuildAttribute(MemberDeclarationSyntax m) =>
		m.AttributeLists.SelectMany(l => l.Attributes).Any(a =>
		{
			string n = a.Name switch
			{
				QualifiedNameSyntax q => q.Right.Identifier.ValueText,
				AliasQualifiedNameSyntax aq => aq.Name.Identifier.ValueText,
				SimpleNameSyntax s => s.Identifier.ValueText,
				_ => a.Name.ToString(),
			};
			return n is "SimulationRebuild" or Signature.RebuildAttribute;
		});

	static IEnumerable<string> DeclaredNames(MemberDeclarationSyntax m) => m switch
	{
		BaseTypeDeclarationSyntax t => [t.Identifier.ValueText],
		DelegateDeclarationSyntax d => [d.Identifier.ValueText],
		MethodDeclarationSyntax md => [md.Identifier.ValueText],
		PropertyDeclarationSyntax p => [p.Identifier.ValueText],
		EventDeclarationSyntax e => [e.Identifier.ValueText],
		BaseFieldDeclarationSyntax f => f.Declaration.Variables.Select(v => v.Identifier.ValueText),
		_ => [],
	};

	// Members carrying [SimulationRebuild] anywhere, plus Rebuild* members of the locked types
	// (and their nested types); type names are resolved syntactically (namespace + nesting).
	static IEnumerable<MemberDeclarationSyntax> RebuildMembers(SyntaxNode root)
	{
		var locked = Signature.LockedTypes.ToHashSet(StringComparer.Ordinal);
		var stack = new Stack<(SyntaxNode Node, string Scope, bool InType, bool InLocked)>();
		stack.Push((root, "", false, false));
		while (stack.Count > 0)
		{
			var (node, scope, inType, inLocked) = stack.Pop();
			foreach (var child in node.ChildNodes())
			{
				if (child is not MemberDeclarationSyntax m)
					continue;
				if (HasRebuildAttribute(m) || (inLocked && DeclaredNames(m).Any(n => n.StartsWith(Signature.RebuildPrefix, StringComparison.Ordinal))))
					yield return m;
				else if (m is BaseNamespaceDeclarationSyntax ns)
					stack.Push((m, (scope.Length > 0 ? scope + "." : "") + ns.Name.ToString().Replace(" ", ""), false, false));
				else if (m is TypeDeclarationSyntax t)
				{
					string name = (scope.Length > 0 ? scope + (inType ? "+" : ".") : "") + t.Identifier.ValueText + (t.TypeParameterList is { } tp ? "`" + tp.Parameters.Count : "");
					stack.Push((m, name, true, inLocked || locked.Contains(name)));
				}
			}
		}
	}

	// Returns the banned type ("family") and the API used, or null.
	static (string Family, string Api)? Banned(ISymbol? s)
	{
		if (s is IAliasSymbol alias)
			s = alias.Target;
		if (s == null)
			return null;
		var type = s as INamedTypeSymbol ?? s.ContainingType;
		for (var t = type; t != null; t = t.ContainingType)
		{
			string n = t.OriginalDefinition.ToDisplayString();
			if (n is "UnityEngine.Random" or "UnityEngine.Time" or "System.Diagnostics.Stopwatch")
				return (n, n + (s is INamedTypeSymbol ? "" : "." + s.Name));
		}
		if (type == null || s is INamedTypeSymbol)
			return null;
		string tn = type.OriginalDefinition.ToDisplayString();
		return (tn, s.Name) switch
		{
			("System.DateTime", "Now" or "UtcNow" or "Today") => (tn, $"{tn}.{s.Name}"),
			("System.DateTimeOffset", "Now" or "UtcNow") => (tn, $"{tn}.{s.Name}"),
			("System.Environment", "TickCount" or "TickCount64") => (tn, $"{tn}.{s.Name}"),
			_ => null,
		};
	}

	public static (JsonArray Violations, JsonObject Scope) Check(Build build)
	{
		var found = new ConcurrentBag<JsonObject>();
		var wholeFiles = new ConcurrentBag<string>();
		var memberScopes = new ConcurrentBag<string>();
		Parallel.ForEach(build.Trees, tree =>
		{
			var root = tree.GetRoot();
			SemanticModel? model = null;
			SemanticModel Model() => model ??= build.Compilation.GetSemanticModel(tree);
			bool whole = IsRebuildFile(tree.FilePath)
				|| (root.DescendantTokens().Any(t => t.ValueText == "SimulationSystem") && DeclaresSimulationSystem(root, Model()));
			List<(SyntaxNode Node, string Scope)> scopes;
			if (whole)
			{
				wholeFiles.Add(tree.FilePath);
				scopes = [(root, "file")];
			}
			else
			{
				scopes = RebuildMembers(root).Select(m => ((SyntaxNode)m, "member:" + string.Join(",", DeclaredNames(m)))).ToList();
				if (scopes.Count == 0)
					return;
				foreach (var (_, sc) in scopes)
					memberScopes.Add($"{tree.FilePath}:{sc["member:".Length..]}");
			}
			// One report per line and banned type: Stopwatch.StartNew().ElapsedTicks is one use, reported as
			// its first member; a bare type reference (using alias, typeof) is reported as the type.
			var hits = new Dictionary<(int Line, string Family), (int Col, bool Member, JsonObject Doc)>();
			foreach (var (node, scope) in scopes)
				foreach (var name in node.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
				{
					var info = Model().GetSymbolInfo(name);
					var ban = Banned(info.Symbol ?? (info.CandidateSymbols.Length > 0 ? info.CandidateSymbols[0] : null));
					if (ban == null)
						continue;
					var pos = name.GetLocation().GetLineSpan().StartLinePosition;
					bool member = ban.Value.Api != ban.Value.Family;
					var key = (pos.Line + 1, ban.Value.Family);
					if (hits.TryGetValue(key, out var prev) && (prev.Member && !member || prev.Member == member && prev.Col <= pos.Character + 1))
						continue;
					hits[key] = (pos.Character + 1, member, new JsonObject
					{
						["file"] = tree.FilePath,
						["line"] = pos.Line + 1,
						["col"] = pos.Character + 1,
						["api"] = ban.Value.Api,
						["scope"] = scope,
						["text"] = (name.Parent is MemberAccessExpressionSyntax ma ? ma : (SyntaxNode)name).ToString(),
					});
				}
			foreach (var h in hits.Values)
				found.Add(h.Doc);
		});
		var ordered = found.OrderBy(v => (string)v["file"]!, StringComparer.Ordinal).ThenBy(v => (int)v["line"]!).ThenBy(v => (int)v["col"]!).ThenBy(v => (string)v["api"]!, StringComparer.Ordinal);
		var scopeDoc = new JsonObject
		{
			["rule"] = $"whole files: {RebuildDir}**, {SystemsDir}SimulationSystem*.cs, any file declaring SimulationSystem; elsewhere: members with [SimulationRebuild], and Rebuild* members of SpawnerData/SimulationConfig",
			["banned"] = Json.Arr(["UnityEngine.Random", "UnityEngine.Time", "System.Diagnostics.Stopwatch", "System.DateTime.Now/UtcNow/Today", "System.DateTimeOffset.Now/UtcNow", "System.Environment.TickCount/TickCount64"]),
			["files"] = Json.Arr(wholeFiles.OrderBy(s => s, StringComparer.Ordinal)),
			["members"] = Json.Arr(memberScopes.OrderBy(s => s, StringComparer.Ordinal)),
		};
		return (new JsonArray(ordered.Select(v => (JsonNode?)v).ToArray()), scopeDoc);
	}
}

// ---------------------------------------------------------------- commands

static class SignatureCommand
{
	public static int Run(Options o)
	{
		var sw = Stopwatch.StartNew();
		var unity = UnityProject.Load(o.RunWorktree!);
		var tree = o.OpenSourceTree();
		var groups = Membership.Assign(tree.LoadAssetSources());
		var rsp = unity.Rsps[Program.RuntimeAsm];
		var files = Compiler.InRspOrder(rsp, groups.GetValueOrDefault(Program.RuntimeAsm) ?? []);
		var trees = Compiler.Parse(files, Compiler.ParseOptions(rsp, rsp.Defines));
		var build = Compiler.Create(Program.RuntimeAsm, rsp, trees, Compiler.References(rsp.References));
		var errors = build.Compilation.GetDeclarationDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
		if (errors.Count > 0)
			throw new ToolError($"{tree.Describe} has {errors.Count} declaration error(s); first: {errors[0]}");
		string? commit = tree is GitTree g ? g.Commit : null;
		var doc = Signature.Generate(build.Compilation, tree is GitTree ? tree.Describe : "worktree", commit);
		string outPath = o.Out ?? throw new ToolError("signature needs --out");
		Json.Write(outPath, doc);
		Console.Error.WriteLine($"cscheck: wrote {outPath} ({doc["types"]!.AsArray().Count} types) in {sw.ElapsedMilliseconds} ms");
		return 0;
	}
}

static class Checker
{
	public static int Run(Options o)
	{
		var total = Stopwatch.StartNew();
		var timings = new JsonObject();
		void Mark(string name, Stopwatch s)
		{
			lock (timings)
				timings[name] = s.ElapsedMilliseconds;
		}

		var sw = Stopwatch.StartNew();
		var unity = UnityProject.Load(o.RunWorktree!);
		var tree = o.OpenSourceTree();
		var problems = unity.PackageProblems(tree);
		if (problems.Count > 0)
			throw new ToolError(string.Join("; ", problems));
		var groups = Membership.Assign(tree.LoadAssetSources());
		Mark("load_ms", sw);

		var failures = new List<string>();
		var result = new JsonObject
		{
			["schema"] = "tufu.cscheck.result/1",
			["ok"] = false,
			["worktree"] = tree.Describe,
			["run_worktree"] = unity.Worktree,
			["rsp_dir"] = unity.ArtifactsDir,
			["sources"] = Membership.Describe(groups, unity, listFiles: false, failures),
		};

		var rtRsp = unity.Rsps[Program.RuntimeAsm];
		var edRsp = unity.Rsps[Program.EditorAsm];
		result["rsp"] = new JsonObject
		{
			["langversion"] = rtRsp.LangVersion.ToDisplayString(),
			["unsafe"] = rtRsp.Unsafe,
			["nullable"] = rtRsp.Nullable.ToString(),
			["warning_level"] = rtRsp.WarningLevel,
			["nowarn"] = Json.Arr(rtRsp.NoWarn),
			["defines"] = rtRsp.Defines.Count,
			["references"] = new JsonObject { [Program.RuntimeAsm] = rtRsp.References.Count, [Program.EditorAsm] = edRsp.References.Count },
			["analyzers_not_run"] = rtRsp.Analyzers.Count,
			["ignored_options"] = Json.Arr(rtRsp.Ignored.Concat(edRsp.Ignored).Distinct()),
		};

		sw.Restart();
		var rtFiles = Compiler.InRspOrder(rtRsp, groups.GetValueOrDefault(Program.RuntimeAsm) ?? []);
		var edFiles = Compiler.InRspOrder(edRsp, groups.GetValueOrDefault(Program.EditorAsm) ?? []);
		var noEdTreesTask = o.NoEditor
			? Task.Run(() => Compiler.Parse(rtFiles, Compiler.ParseOptions(rtRsp, rtRsp.Defines.Where(d => !PlayerApprox.IsEditorDefine(d)))))
			: null;
		var edTreesTask = o.Compile ? Task.Run(() => Compiler.Parse(edFiles, Compiler.ParseOptions(edRsp, edRsp.Defines))) : null;
		var runtime = Compiler.Create(Program.RuntimeAsm, rtRsp, Compiler.Parse(rtFiles, Compiler.ParseOptions(rtRsp, rtRsp.Defines)), Compiler.References(rtRsp.References));
		Mark("parse_ms", sw);

		JsonObject? baseline = null;
		if (o.BaselineFile != null && File.Exists(o.BaselineFile) && !o.UpdateBaseline)
			baseline = JsonNode.Parse(File.ReadAllText(o.BaselineFile))!.AsObject();
		var newBaseline = new JsonObject { ["schema"] = "tufu.cscheck.baseline/1" };

		var tasks = new List<Task>();
		List<Diags.Item>? rtDiags = null, edDiags = null, noEdDiags = null;
		List<string>? droppedRefs = null;
		if (o.Compile)
		{
			tasks.Add(Task.Run(() =>
			{
				var s = Stopwatch.StartNew();
				rtDiags = Diags.Collect(runtime.Compilation);
				Mark("compile_runtime_ms", s);
			}));
			tasks.Add(Task.Run(() =>
			{
				var s = Stopwatch.StartNew();
				// The editor assembly references the Assembly-CSharp being checked, not Unity's stale build.
				var refs = Compiler.References(edRsp.References.Where(p => Path.GetFileName(p) is not (Program.RuntimeAsm + ".ref.dll") and not (Program.RuntimeAsm + ".dll")))
					.Append(runtime.Compilation.ToMetadataReference());
				var editor = Compiler.Create(Program.EditorAsm, edRsp, edTreesTask!.Result, refs);
				edDiags = Diags.Collect(editor.Compilation);
				Mark("compile_editor_ms", s);
			}));
		}
		if (o.NoEditor)
		{
			tasks.Add(Task.Run(() =>
			{
				var s = Stopwatch.StartNew();
				var refs = rtRsp.References.Where(p => !PlayerApprox.IsEditorReference(p)).ToList();
				droppedRefs = rtRsp.References.Except(refs).Select(p => Path.GetFileName(p)).OrderBy(x => x, StringComparer.Ordinal).ToList();
				var build = Compiler.Create(Program.RuntimeAsm, rtRsp, noEdTreesTask!.Result, Compiler.References(refs));
				noEdDiags = Diags.Collect(build.Compilation);
				Mark("compile_no_editor_ms", s);
			}));
		}
		JsonArray? sigViolations = null;
		if (o.Signature)
		{
			string file = o.SignatureFile ?? throw new ToolError("--signature needs --signature-file");
			if (!File.Exists(file))
				throw new ToolError($"signature file {file} not found");
			var locked = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
			tasks.Add(Task.Run(() =>
			{
				var s = Stopwatch.StartNew();
				sigViolations = Signature.Check(runtime.Compilation, locked);
				Mark("signature_ms", s);
			}));
		}
		JsonArray? banViolations = null;
		JsonObject? banScope = null;
		if (o.Bans)
		{
			tasks.Add(Task.Run(() =>
			{
				var s = Stopwatch.StartNew();
				(banViolations, banScope) = Bans.Check(runtime);
				Mark("bans_ms", s);
			}));
		}
		try
		{
			Task.WaitAll(tasks.ToArray());
		}
		catch (AggregateException e) when (e.InnerExceptions.All(x => x is ToolError))
		{
			throw e.InnerExceptions[0];
		}

		if (o.Compile)
		{
			var compile = new JsonObject();
			var warnBaseline = baseline?["warnings"]?.AsObject();
			var newWarn = new JsonObject();
			foreach (var (name, diags) in new[] { (Program.RuntimeAsm, rtDiags!), (Program.EditorAsm, edDiags!) })
			{
				var errors = diags.Where(d => d.Severity == "error").ToList();
				var warnings = diags.Where(d => d.Severity == "warning").ToList();
				var byId = Diags.CountById(warnings);
				var entry = new JsonObject
				{
					["errors"] = errors.Count,
					["warnings"] = warnings.Count,
					["warnings_by_id"] = Diags.ToJson(byId),
					["error_list"] = new JsonArray(errors.Take(200).Select(e => (JsonNode?)e.ToJson()).ToArray()),
				};
				if (o.ListWarnings)
					entry["warning_list"] = new JsonArray(warnings.Select(w => (JsonNode?)w.ToJson()).ToArray());
				if (errors.Count > 0)
					failures.Add($"{name}: {errors.Count} compile error(s), first: {errors[0].File}({errors[0].Line},{errors[0].Col}): {errors[0].Id} {errors[0].Message}");
				if (warnBaseline?[name] is JsonObject wb)
				{
					var increased = new JsonObject();
					foreach (var (id, n) in byId)
					{
						int was = (int?)wb[id] ?? 0;
						if (n > was)
							increased[id] = $"{was} -> {n}";
					}
					entry["new_warnings"] = increased;
					if (increased.Count > 0)
						failures.Add($"{name}: warning count above baseline: {increased.ToJsonString()}");
				}
				newWarn[name] = Diags.ToJson(byId);
				compile[name] = entry;
			}
			result["compile"] = compile;
			newBaseline["warnings"] = newWarn;
		}
		if (o.NoEditor)
		{
			var errors = noEdDiags!.Where(d => d.Severity == "error").ToList();
			var keys = errors.Select(e => e.Key).ToList();
			var entry = new JsonObject
			{
				["errors"] = errors.Count,
				["errors_by_id"] = Diags.ToJson(Diags.CountById(errors)),
				["error_list"] = new JsonArray(errors.Take(200).Select(e => (JsonNode?)e.ToJson()).ToArray()),
				["dropped_defines"] = Json.Arr(rtRsp.Defines.Where(PlayerApprox.IsEditorDefine)),
				["dropped_references"] = Json.Arr(droppedRefs!),
			};
			if (baseline?["no_editor_errors"] is JsonArray be)
			{
				var baseKeys = be.Select(n => (string)n!).ToList();
				var added = Diags.Minus(keys, baseKeys);
				entry["new_errors"] = Json.Arr(added);
				entry["fixed_errors"] = Json.Arr(Diags.Minus(baseKeys, keys));
				if (added.Count > 0)
					failures.Add($"no-UNITY_EDITOR compile: {added.Count} error(s) not in the baseline, first: {added[0]}");
			}
			else if (!o.UpdateBaseline && errors.Count > 0)
			{
				failures.Add($"no-UNITY_EDITOR compile: {errors.Count} error(s) and no baseline");
			}
			newBaseline["no_editor_errors"] = Json.Arr(keys.OrderBy(k => k, StringComparer.Ordinal));
			result["no_editor"] = entry;
		}
		if (sigViolations != null)
		{
			result["signature"] = new JsonObject { ["file"] = o.SignatureFile, ["violations"] = sigViolations };
			if (sigViolations.Count > 0)
				failures.Add($"signature lock: {sigViolations.Count} violation(s), first: {sigViolations[0]!["detail"]}");
		}
		if (banViolations != null)
		{
			result["bans"] = new JsonObject { ["scope"] = banScope, ["violations"] = banViolations };
			if (banViolations.Count > 0)
				failures.Add($"banned APIs: {banViolations.Count} use(s), first: {banViolations[0]!["file"]}({banViolations[0]!["line"]}): {banViolations[0]!["api"]}");
		}

		if (o.UpdateBaseline)
		{
			if (o.BaselineFile == null)
				throw new ToolError("--update-baseline needs --baseline-file");
			if (!(o.Compile && o.NoEditor))
				throw new ToolError("--update-baseline needs the compile and no-editor steps");
			Json.Write(o.BaselineFile, newBaseline);
		}

		result["ok"] = failures.Count == 0;
		result["failures"] = Json.Arr(failures);
		timings["total_ms"] = total.ElapsedMilliseconds;
		result["timings"] = timings;
		string text = Json.Serialize(result);
		if (o.JsonOut != null)
			File.WriteAllText(o.JsonOut, text);
		else
			Console.Write(text);
		foreach (string f in failures)
			Console.Error.WriteLine("cscheck: FAIL " + f);
		return failures.Count == 0 ? 0 : 1;
	}
}
