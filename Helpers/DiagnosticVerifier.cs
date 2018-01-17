using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using RoslynSecurityGuard.Analyzers;
using RoslynSecurityGuard.Analyzers.Utils;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynSecurityGuard.Helpers
{
    /// <summary>
    /// Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        private static readonly MetadataReference SystemDiagReference = MetadataReference.CreateFromFile(typeof(Process).Assembly.Location);

        private static readonly CompilationOptions CSharpDefaultOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        private static readonly CompilationOptions VisualBasicDefaultOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        internal static string CSharpDefaultFileExt = "*.cs";
        internal static string VisualBasicDefaultExt = "*.vb";
        internal static string TestProjectName = "TestProject";
        
        #region To be implemented by Test classes
        /// <summary>
        /// Get the CSharp analyzer being tested - to be implemented in non-abstract class
        /// </summary>
        protected abstract IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers();

        protected virtual IEnumerable<MetadataReference> GetAdditionnalReferences() {
            return null;
        }
        #endregion

        #region Verifier wrappers

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        protected async Task VerifyCSharpDiagnostic(string directory)
        {
            var a = GetDiagnosticAnalyzers().ToList();
            a.Add(new DebugAnalyzer());
            await VerifyDiagnostics(directory, LanguageNames.CSharp, a);
        }

        /// <summary>
        /// Called to test a VB.NET DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        protected async Task VerifyVisualBasicDiagnostic(string directory)
        {
            var a = GetDiagnosticAnalyzers().ToList();
            a.Add(new DebugAnalyzer());
            await VerifyDiagnostics(directory, LanguageNames.VisualBasic, a);
        }

        /// <summary>
        /// General method that gets a collection of actual diagnostics found in the source after the analyzer is run, 
        /// then verifies each of them.
        /// </summary>
        /// <param name="directory">Path to source files</param>
        /// <param name="language">The language of the classes represented by the source strings</param>
        /// <param name="analyzers">The analyzers to be run on the source code</param>
        private async Task VerifyDiagnostics(string directory, string language, List<DiagnosticAnalyzer> analyzers)
        {
            var diagnostics = await GetSortedDiagnostics(directory, language, analyzers, GetAdditionnalReferences());
            VerifyDiagnosticResults(diagnostics, analyzers, language);
        }

        #endregion

        #region Actual comparisons and verifications
        /// <summary>
        /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
        /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
        /// </summary>
        /// <param name="results">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        /// <param name="analyzers">The analyzers that was being run on the sources</param>
        private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> results, List<DiagnosticAnalyzer> analyzers, string language)
        {
            string output = FormatDiagnostics(analyzers[0], results.ToArray());
            Console.WriteLine(output);
        }

        #endregion

        #region Formatting Diagnostics
        /// <summary>
        /// Helper method to format a Diagnostic into an easily readable string
        /// </summary>
        /// <param name="analyzer">The analyzer that this verifier tests</param>
        /// <param name="diagnostics">The Diagnostics to be formatted</param>
        /// <returns>The Diagnostics formatted as a string</returns>
        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine(diagnostics[i].ToString());

                var analyzerType = analyzer.GetType();
                var rules = analyzer.SupportedDiagnostics;

                foreach (var rule in rules)
                {
                    if (rule != null && rule.Id == diagnostics[i].Id)
                    {
                        var location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                        }
                        else
                        {
                            string resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs") ? "GetCSharpResultAt" : "GetBasicResultAt";
                            var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzerType.Name,
                                rule.Id);
                        }

                        if (i != diagnostics.Length - 1)
                        {
                            builder.Append(',');
                        }

                        builder.AppendLine();
                        break;
                    }
                }
            }
            return builder.ToString();
        }
        #endregion

        #region  Get Diagnostics

        /// <summary>
        /// Given classes in the form of strings, their language, and an IDiagnosticAnlayzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="directory">Path to source files</param>
        /// <param name="language">The language the source classes are in</param>
        /// <param name="analyzers">The analyzers to be run on the sources</param>
        /// <param name="references">Addional refenced modules</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        private static async Task<Diagnostic[]> GetSortedDiagnostics(string directory, string language, List<DiagnosticAnalyzer> analyzers, IEnumerable<MetadataReference> references = null)
        {
            return await GetSortedDiagnosticsFromDocuments(analyzers, GetDocuments(directory, language, references));
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected static async Task<Diagnostic[]> GetSortedDiagnosticsFromDocuments(List<DiagnosticAnalyzer> analyzers, Document[] documents)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync();
                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzers.ToArray()));
                var diags = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = await document.GetSyntaxTreeAsync();
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            var results = SortDiagnostics(diagnostics);
            return results;
        }

        /// <summary>
        /// Sort diagnostics by location in source document
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        #endregion

        #region Set up compilation and documents
        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="directory">Path to source files</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
        private static Document[] GetDocuments(string directory, string language, IEnumerable<MetadataReference> references = null)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new ArgumentException("Unsupported Language");
            }

            var project = CreateProject(directory, language, references);
            var documents = project.Documents.ToArray();

            return documents;
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="directory">Path to source files</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        private static Project CreateProject(string directory, string language = LanguageNames.CSharp, IEnumerable<MetadataReference> references = null)
        {
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;

            CompilationOptions options = language == LanguageNames.CSharp ? CSharpDefaultOptions : VisualBasicDefaultOptions;

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference)
                .AddMetadataReference(projectId, SystemDiagReference)
                .WithProjectCompilationOptions(projectId, options);

            var files = Directory.EnumerateFiles(directory, fileExt, SearchOption.AllDirectories);
            foreach (var file in files) {
                var documentId = DocumentId.CreateNewId(projectId, debugName: file);
                var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                solution = solution.AddDocument(documentId, file, SourceText.From(fileStream));
            }
            return solution.GetProject(projectId);
        }
        #endregion
    }
}
