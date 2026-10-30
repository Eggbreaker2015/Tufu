// csmembers: syntax-only Roslyn dump of the members of named classes (all partial parts), for tests that
// need source positions, bodies and attributes (tests/test_cs_signature_lock.py). Semantic checks stay in
// cscheck; this tool reads only the given files and needs no references.
//
//   csmembers --type SimulationSystem [--type ...] [--define UNITY_EDITOR ...] <file.cs>...
//
// Prints one JSON document. Offsets are UTF-16 code units into the file text (ASCII sources: bytes).
// Exit code: 0 ok, 2 tool error (missing file, syntax errors).

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tufu.CsMembers;

static class Program
{
	static int Main(string[] args)
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		var types = new HashSet<string>(StringComparer.Ordinal);
		var defines = new List<string>();
		var files = new List<string>();
		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i])
			{
				case "--type" when i + 1 < args.Length: types.Add(args[++i]); break;
				case "--define" when i + 1 < args.Length: defines.Add(args[++i]); break;
				default:
					if (args[i].StartsWith("--", StringComparison.Ordinal))
						return Fail($"unknown option {args[i]}");
					files.Add(args[i]);
					break;
			}
		}
		if (types.Count == 0 || files.Count == 0)
			return Fail("usage: csmembers --type NAME [--type ...] [--define SYM ...] FILE...");

		var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: defines);
		var typeDecls = new JsonArray();
		var members = new JsonArray();
		foreach (string file in files)
		{
			if (!File.Exists(file))
				return Fail($"file not found: {file}");
			string text = File.ReadAllText(file);
			var tree = CSharpSyntaxTree.ParseText(text, options, path: file);
			var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
			if (errors.Count > 0)
				return Fail($"{file}: syntax error {errors[0]}");
			foreach (var decl in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
			{
				if (!types.Contains(decl.Identifier.ValueText))
					continue;
				typeDecls.Add(TypeDecl(file, decl));
				foreach (var m in decl.Members)
					members.Add(Member(file, decl.Identifier.ValueText, m));
			}
		}
		var doc = new JsonObject { ["schema"] = "tufu.csmembers/1", ["types"] = typeDecls, ["members"] = members };
		Console.Out.Write(doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
		return 0;
	}

	static int Fail(string message)
	{
		Console.Error.WriteLine("csmembers: " + message);
		return 2;
	}

	static int Line(SyntaxNode n) => n.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

	static JsonArray Arr(IEnumerable<string> xs) => new(xs.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray());

	static JsonObject TypeDecl(string file, TypeDeclarationSyntax d) => new()
	{
		["file"] = file,
		["name"] = d.Identifier.ValueText,
		["keyword"] = d.Keyword.ValueText,
		["line"] = Line(d),
		["modifiers"] = Arr(d.Modifiers.Select(t => t.ValueText)),
		["base_list"] = d.BaseList?.Types.ToString(),
		["containing_type"] = (d.Parent as TypeDeclarationSyntax)?.Identifier.ValueText,
		["namespace"] = d.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString(),
	};

	static string Compact(SyntaxNode? n) => n == null ? "" : string.Concat(n.DescendantTokens().Select(t => t.ValueText is "," ? ", " : t.Text));

	static JsonArray Attributes(SyntaxList<AttributeListSyntax> lists) => new(lists.SelectMany(l => l.Attributes.Select(a => (JsonNode?)new JsonObject
	{
		["target"] = l.Target?.Identifier.ValueText,
		["name"] = a.Name.ToString(),
		["args"] = Arr(a.ArgumentList?.Arguments.Select(x => x.ToString()) ?? []),
		["line"] = Line(a),
	})).ToArray());

	// A placeholder statement of a decompiled empty method: "return;", "return <constant>;" (literal,
	// default, Enum.Member) or "<out parameter> = <constant>;".
	static bool Trivial(StatementSyntax st, BaseMethodDeclarationSyntax? method)
	{
		static bool Constant(ExpressionSyntax? e) => e switch
		{
			null => true,
			LiteralExpressionSyntax or DefaultExpressionSyntax => true,
			PrefixUnaryExpressionSyntax u when u.IsKind(SyntaxKind.UnaryMinusExpression) => Constant(u.Operand),
			MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax t, Name: IdentifierNameSyntax } => char.IsUpper(t.Identifier.ValueText[0]),
			_ => false,
		};
		return st switch
		{
			ReturnStatementSyntax r => Constant(r.Expression),
			ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax id } a } when a.IsKind(SyntaxKind.SimpleAssignmentExpression) =>
				Constant(a.Right) && method != null && method.ParameterList.Parameters.Any(p => p.Identifier.ValueText == id.Identifier.ValueText && p.Modifiers.Any(SyntaxKind.OutKeyword)),
			_ => false,
		};
	}

	static JsonObject Member(string file, string type, MemberDeclarationSyntax m)
	{
		var o = new JsonObject
		{
			["file"] = file,
			["type"] = type,
			["kind"] = m.Kind().ToString(),
			["line"] = Line(m),
			["span"] = new JsonArray(m.Span.Start, m.Span.End),
			["full_span"] = new JsonArray(m.FullSpan.Start, m.FullSpan.End),
			["modifiers"] = Arr(m.Modifiers.Select(t => t.ValueText)),
			["attributes"] = Attributes(m.AttributeLists),
		};
		BlockSyntax? body = null;
		ArrowExpressionClauseSyntax? arrow = null;
		switch (m)
		{
			case BaseMethodDeclarationSyntax bm:
				string name = bm switch
				{
					MethodDeclarationSyntax md => md.Identifier.ValueText,
					ConstructorDeclarationSyntax cd => cd.Identifier.ValueText,
					DestructorDeclarationSyntax dd => "~" + dd.Identifier.ValueText,
					OperatorDeclarationSyntax od => "op_" + od.OperatorToken.ValueText,
					ConversionOperatorDeclarationSyntax co => "conv_" + Compact(co.Type),
					_ => bm.Kind().ToString(),
				};
				o["name"] = name;
				o["key"] = $"{name}({string.Join(", ", bm.ParameterList.Parameters.Select(p => (p.Modifiers.Count > 0 ? string.Join(" ", p.Modifiers.Select(t => t.ValueText)) + " " : "") + Compact(p.Type)))})";
				o["return_type"] = bm is MethodDeclarationSyntax md2 ? Compact(md2.ReturnType) : null;
				// Declaration text without attributes and body: modifiers, return type, name, parameters, constraints.
				var first = bm.AttributeLists.Count > 0 ? bm.AttributeLists.Last().GetLastToken().GetNextToken() : bm.GetFirstToken();
				int headerEnd = bm switch
				{
					MethodDeclarationSyntax md3 when md3.ConstraintClauses.Count > 0 => md3.ConstraintClauses.Last().Span.End,
					ConstructorDeclarationSyntax cd2 when cd2.Initializer != null => cd2.Initializer.Span.End,
					_ => bm.ParameterList.Span.End,
				};
				o["header"] = m.SyntaxTree.GetText().ToString(Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(first.SpanStart, headerEnd));
				body = bm.Body;
				arrow = bm.ExpressionBody;
				break;
			case PropertyDeclarationSyntax p:
				o["name"] = p.Identifier.ValueText;
				o["key"] = p.Identifier.ValueText;
				arrow = p.ExpressionBody;
				break;
			case FieldDeclarationSyntax f:
				o["name"] = string.Join(", ", f.Declaration.Variables.Select(v => v.Identifier.ValueText));
				o["key"] = (string)o["name"]!;
				break;
			case EventFieldDeclarationSyntax ef:
				o["name"] = string.Join(", ", ef.Declaration.Variables.Select(v => v.Identifier.ValueText));
				o["key"] = (string)o["name"]!;
				break;
			case BaseTypeDeclarationSyntax bt:
				o["name"] = bt.Identifier.ValueText;
				o["key"] = bt.Identifier.ValueText;
				break;
			default:
				o["name"] = null;
				o["key"] = m.Kind().ToString();
				break;
		}
		o["has_body"] = body != null || arrow != null;
		o["trivial_body"] = body != null && body.Statements.All(st => Trivial(st, m as BaseMethodDeclarationSyntax));
		o["body"] = body?.ToFullString() ?? arrow?.ToFullString();
		o["statements"] = body == null ? new JsonArray() : new JsonArray(body.Statements.Select(s => (JsonNode?)new JsonObject { ["kind"] = s.Kind().ToString(), ["text"] = s.ToString() }).ToArray());
		var scope = (SyntaxNode?)body ?? arrow;
		o["invocations"] = Arr(scope == null ? [] : scope.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(i => i.Expression switch
		{
			IdentifierNameSyntax id => id.Identifier.ValueText,
			GenericNameSyntax g => g.Identifier.ValueText,
			MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
			_ => i.Expression.ToString(),
		}));
		// Comments in the leading trivia and anywhere inside the member (recovered bodies carry "Original ELF VA").
		o["comments"] = Arr(m.DescendantTrivia(descendIntoTrivia: true).Concat(m.GetLeadingTrivia())
			.Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
			.Distinct().OrderBy(t => t.SpanStart).Select(t => t.ToString()));
		return o;
	}
}
