using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Scrutor.Tests
{
    public static class GenerationHelpers
    {
        static GenerationHelpers()
        {
            // this "core assemblies hack" is from https://stackoverflow.com/a/47196516/4418060
            var coreAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            var coreAssemblyNames = new[]
            {
                "mscorlib.dll",
                "netstandard.dll",
                "System.dll",
                "System.Core.dll",
#if NETCOREAPP
                "System.Private.CoreLib.dll",
                "System.ComponentModel.dll",
#endif
                "System.Runtime.dll",
                "System.Linq.dll",
            };
            var coreMetaReferences =
                coreAssemblyNames.Select(x => MetadataReference.CreateFromFile(Path.Combine(coreAssemblyPath, x)));
            MetadataReferences = coreMetaReferences.ToImmutableArray();
        }

        internal const string CrLf = "\r\n";
        internal const string Lf = "\n";
        internal const string DefaultFilePathPrefix = "Test";
        internal const string CSharpDefaultFileExt = "cs";
        internal const string TestProjectName = "TestProject";

        // internal static readonly string NormalizedPreamble = NormalizeToLf(Preamble.GeneratedByATool + Lf);

        internal static readonly ImmutableArray<PortableExecutableReference> MetadataReferences;

        public static async Task AssertGeneratedAsExpected<T>(IEnumerable<Assembly> metadataReferences, string source, string expected, string? pathHint = null)
            where T : ISourceGenerator, new()
        {
            var generatedTree = await GenerateAsync<T>(new[] { source }, metadataReferences).ConfigureAwait(false);
            // normalize line endings to just LF
            var generatedText = generatedTree
                .Where(z => pathHint == null || z.FilePath.Contains(pathHint, StringComparison.OrdinalIgnoreCase))
                .Select(z => NormalizeToLf(z.GetText().ToString()));
            // and append preamble to the expected
            var expectedText = NormalizeToLf(expected).Trim();
            Assert.Equal(generatedText.Last(), expectedText);
        }

        public static async Task AssertGeneratedAsExpected<T>(IEnumerable<Assembly> metadataReferences, IEnumerable<string> sources, IEnumerable<string> expected, string? pathHint = null)
            where T : ISourceGenerator, new()
        {
            var generatedTree = await GenerateAsync<T>(sources, metadataReferences).ConfigureAwait(false);
            // normalize line endings to just LF
            var generatedText = generatedTree
                .Where(z => pathHint == null || z.FilePath.Contains(pathHint, StringComparison.OrdinalIgnoreCase))
                .Select(z => NormalizeToLf(z.GetText().ToString())).ToArray();
            // and append preamble to the expected
            var expectedText = expected.Select(z => NormalizeToLf(z).Trim()).ToArray();

            Assert.Equal(generatedText.Length, expectedText.Length);
            foreach (var (generated, expectedTxt) in generatedText.Zip(expectedText, (generated, expected) => (generated, expected)))
            {
                Assert.Equal(generated, expectedTxt);
            }
        }

        public static async Task AssertGeneratedAsExpected<T>(string source, string expected, string? pathHint = null)
            where T : ISourceGenerator, new()
        {
            var generatedTree = await GenerateAsync<T>(new[] { source }, Array.Empty<Assembly>()).ConfigureAwait(false);
            // normalize line endings to just LF
            var generatedText = generatedTree
                .Where(z => pathHint == null || z.FilePath.Contains(pathHint, StringComparison.OrdinalIgnoreCase))
                .Select(z => NormalizeToLf(z.GetText().ToString()));
            // and append preamble to the expected
            var expectedText = NormalizeToLf(expected).Trim();
            Assert.Equal(generatedText.Last(), expectedText);
        }

        public static async Task<string> Generate<T>(IEnumerable<Assembly> metadataReferences, string source, string? pathHint = null)
            where T : ISourceGenerator, new()
        {
            var generatedTree = await GenerateAsync<T>(new[] { source }, metadataReferences).ConfigureAwait(false);
            // normalize line endings to just LF
            var generatedText = generatedTree
                .Where(z => pathHint == null || z.FilePath.Contains(pathHint, StringComparison.OrdinalIgnoreCase))
                .Select(z => NormalizeToLf(z.GetText().ToString()));
            // and append preamble to the expected
            return generatedText.Last();
        }

        public static async Task<string[]> Generate<T>(IEnumerable<Assembly> metadataReferences, IEnumerable<string> sources)
            where T : ISourceGenerator, new()
        {
            var generatedTree = await GenerateAsync<T>(sources, metadataReferences).ConfigureAwait(false);
            // normalize line endings to just LF
            var generatedText = generatedTree
                .Select(z => NormalizeToLf(z.GetText().ToString()));
            // and append preamble to the expected
            return generatedText.ToArray();
        }

        public static async Task<string> Generate<T>(string source)
            where T : ISourceGenerator, new()
        {
            var generatedTree = await GenerateAsync<T>(new[] { source }, Array.Empty<Assembly>()).ConfigureAwait(false);
            // normalize line endings to just LF
            var generatedText = generatedTree
                .Select(z => NormalizeToLf(z.GetText().ToString()));
            // and append preamble to the expected
            return generatedText.Last();
        }

        public static string NormalizeToLf(string input) => input.Replace(CrLf, Lf);

        public static async Task<IEnumerable<SyntaxTree>> GenerateAsync<T>(IEnumerable<string> sources, IEnumerable<Assembly> metadataReferences)
            where T : ISourceGenerator, new()
        {
            var references = metadataReferences
                .Concat(
                    new[]
                    {
                        typeof(IServiceCollection).Assembly,
                        typeof(ServiceCollection).Assembly,
                        typeof(IFluentInterface).Assembly,
                    }
                )
                .Distinct()
                .Select(x => MetadataReference.CreateFromFile(x.Location))
                .ToArray();
            var project = CreateProject(references, sources.ToArray());

            var compilation = (CSharpCompilation?)await project.GetCompilationAsync().ConfigureAwait(false);
            if (compilation is null)
            {
                throw new InvalidOperationException("Could not compile the sources");
            }

            var startingSyntaxTress = compilation.SyntaxTrees.Length;

            // var diagnostics = compilation.GetDiagnostics();
            // Assert.Empty(diagnostics.Where(x => x.Severity > DiagnosticSeverity.Warning));

            ISourceGenerator generator = new T();

            var driver = new CSharpGeneratorDriver(compilation.SyntaxTrees[0].Options, ImmutableArray.Create(generator), default, ImmutableArray<AdditionalText>.Empty);

            driver.RunFullGeneration(compilation, out var outputCompilation, out var innerDiagnostics);

            var diagnostics = outputCompilation.GetDiagnostics();
            Assert.Empty(diagnostics.Where(x => x.Severity > DiagnosticSeverity.Warning));
            // Assert.Empty(diagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning));

            // the syntax tree added by the generator will be the last one in the compilation
            return outputCompilation.SyntaxTrees.TakeLast(outputCompilation.SyntaxTrees.Count() - startingSyntaxTress);
        }

        public static async Task<CSharpCompilation> CreateProject<T>(IEnumerable<Assembly> metadataReferences, params string[] sources)
            where T : ISourceGenerator, new()
        {
            var references = metadataReferences
                .Concat(
                    new[]
                    {
                        typeof(IServiceCollection).Assembly,
                        typeof(ServiceCollection).Assembly,
                        typeof(IFluentInterface).Assembly,
                    }
                )
                .Distinct()
                .Select(x => MetadataReference.CreateFromFile(x.Location))
                .ToArray();

            var project = CreateProject(references, sources);


            var compilation = (CSharpCompilation?)await project.GetCompilationAsync().ConfigureAwait(false);
            if (compilation is null)
            {
                throw new InvalidOperationException("Could not compile the sources");
            }

            // var diagnostics = compilation.GetDiagnostics();
            // Assert.Empty(diagnostics.Where(x => x.Severity > DiagnosticSeverity.Warning));

            ISourceGenerator generator = new T();

            var driver = new CSharpGeneratorDriver(compilation.SyntaxTrees[0].Options, ImmutableArray.Create(generator), default, ImmutableArray<AdditionalText>.Empty);

            driver.RunFullGeneration(compilation, out var outputCompilation, out var innerDiagnostics);

            var diagnostics = outputCompilation.GetDiagnostics();
            // Assert.Empty(diagnostics.Where(x => x.Severity > DiagnosticSeverity.Warning));

            return (outputCompilation as CSharpCompilation)!;
        }

        public static Project CreateProject(IEnumerable<MetadataReference> metadataReferences, params string[] sources)
        {
            var projectId = ProjectId.CreateNewId(TestProjectName);
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp)
                .WithProjectCompilationOptions(
                    projectId,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                )
                .WithProjectParseOptions(
                    projectId,
                    new CSharpParseOptions(preprocessorSymbols: new[] { "SOMETHING_ACTIVE" })
                )
                .AddMetadataReferences(projectId, MetadataReferences.Concat(metadataReferences ?? Array.Empty<MetadataReference>()));

            var count = 0;
            foreach (var source in sources)
            {
                var newFileName = DefaultFilePathPrefix + count + "." + CSharpDefaultFileExt;
                var documentId = DocumentId.CreateNewId(projectId, newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }

            var project = solution.GetProject(projectId);
            if (project is null)
            {
                throw new InvalidOperationException($"The ad hoc workspace does not contain a project with the id {projectId.Id}");
            }

            return project;
        }
    }
}
