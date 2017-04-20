using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using FUR10N.NullContracts;
using NUnit.Framework;
using System.Reflection;

namespace FUR10N.NullContractsTests
{
    public abstract class TestBase
    {
        private readonly string[] diagnosticIds;

        protected TestBase(params string[] diagnosticIds)
        {
            this.diagnosticIds = diagnosticIds;
        }

        protected static Document CreateDocument(string source)
        {
            var projectId = ProjectId.CreateNewId(debugName: "TestProject");

            var workingDirectory = Assembly.GetExecutingAssembly().GetName().CodeBase;
            var runtime = new Uri(workingDirectory + @"..\..\..\..\..\..\packages\System.Runtime.4.3.0\lib\net462\System.Runtime.dll").LocalPath;
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ValueTuple).Assembly.Location),
                MetadataReference.CreateFromFile(runtime)
            };
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(projectId, references);

            var documentId = DocumentId.CreateNewId(projectId, debugName: "Test.cs");
            solution = solution.AddDocument(documentId, "Test.cs", SourceText.From(source));
            return solution.GetProject(projectId).Documents.First();
        }
        
        protected static Diagnostic[] GetDiagnosticsForAnalyzer(Document document, DiagnosticAnalyzer analyzer)
        {
            var diagnostics = new List<Diagnostic>();
            var compilationWithAnalyzers = document.Project.GetCompilationAsync().Result.WithAnalyzers(ImmutableArray.Create(analyzer));
            var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
            foreach (var diag in diags)
            {
                if (diag.Location == Location.None || diag.Location.IsInMetadata)
                {
                    diagnostics.Add(diag);
                }
                else
                {
                    var tree = document.GetSyntaxTreeAsync().Result;
                    if (tree == diag.Location.SourceTree)
                    {
                        diagnostics.Add(diag);
                    }
                }
            }

            var results = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
            diagnostics.Clear();
            return results;
        }

        protected Diagnostic[] GetDiagnostics(string code, bool ignoreErrors = false)
        {
            code = @"
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
public class NotNullAttribute : System.Attribute { }
public class CheckNullAttribute : System.Attribute { }
public static class Constraint
{
    public static void NotNull(Expression<Func<object>> func)
    {
    }
}
" + code;
            var document = CreateDocument(code);
            var analyzer = new MainAnalyzer();

            var compilation = document.Project.GetCompilationAsync().Result;
            var compilerDiagnostics = compilation.GetDiagnostics();
            if (!ignoreErrors && compilerDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                Assert.Fail($"Compilation error(s): {compilerDiagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error)}");
            }

            if (diagnosticIds == null || diagnosticIds.Length == 0)
            {
                return GetDiagnosticsForAnalyzer(document, analyzer);
            }
            return GetDiagnosticsForAnalyzer(document, analyzer).Where(i => !i.Id.StartsWith("NC") || diagnosticIds.Contains(i.Id)).ToArray();
        }

        protected void AssertIssues(Diagnostic[] dx, params string[] ids)
        {
            if (dx.Length > 0 && ids.Length == 0)
            {
                Assert.Fail("Expected no issues, but found: " + dx[0].GetMessage());
            }
            if (dx.Length == 0 && ids.Length > 0)
            {
                Assert.Fail("Expected an issue but found none.");
            }
            Assert.AreEqual(ids.Length, dx.Length, $"Issue count mismatch. Actual: {string.Join("\n", dx.Select(i => i.GetMessage()))}");
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.AreEqual(ids[i], dx[i].Id);
            }
        }
    }
}
