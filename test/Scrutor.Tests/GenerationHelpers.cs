using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Scrutor.Tests
{
    public class GeneratorTester : IDisposable
    {
        private readonly List<MetadataReference> _references;
        private readonly List<Assembly> _assemblies;
        private readonly List<SourceText> _sources;
        private readonly CollectibleTestAssemblyLoadContext _context;
        private readonly string _testProjectName;
        private readonly string _filePathPrefix;
        private readonly string _fileExt;
        private readonly IDictionary<Type, CSharpCompilation> _lastCompilations = new Dictionary<Type, CSharpCompilation>();
#if NETCOREAPP3_1
        [AllowNull, MaybeNull]
#endif
        private Project _project = null;
#if NETCOREAPP3_1
        [AllowNull, MaybeNull]
#endif
        private ImmutableArray<SyntaxTree> _lastSyntax = ImmutableArray<SyntaxTree>.Empty;

        private ImmutableArray<Diagnostic> _generatorDiagnostics = ImmutableArray<Diagnostic>.Empty;
        private ITestOutputHelper? _testOutputHelper;

        public GeneratorTester(
            CollectibleTestAssemblyLoadContext context,
            string testProjectName = "TestProject",
            string filePathPrefix = "Test",
            string fileExt = "cs")
        {
            _context = context;
            _testProjectName = testProjectName;
            _filePathPrefix = filePathPrefix;
            _fileExt = fileExt;
            _assemblies = new List<Assembly>();
            _sources = new List<SourceText>();
            _references = new List<MetadataReference>(GenerationHelpers.MetadataReferences);
            AddReferences(typeof(IServiceCollection).Assembly, typeof(ServiceCollection).Assembly, typeof(IFluentInterface).Assembly);
        }

        public GeneratorTester AddCompilationReference(IEnumerable<CSharpCompilation> additionalCompilations)
        {
            _references.AddRange(additionalCompilations.Select(z => z.CreateMetadataReference()));
            return this;
        }

        public GeneratorTester AddCompilationReference(CSharpCompilation compilation, params CSharpCompilation[] additionalCompilations)
        {
            return AddReferences(compilation.CreateMetadataReference(), additionalCompilations.Select(z => z.CreateMetadataReference()).ToArray());
        }

        public GeneratorTester Output(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            return this;
        }

        public GeneratorTester AddReferences(IEnumerable<MetadataReference> additionalSources)
        {
            _references.AddRange(additionalSources);
            _project = null;
            _lastCompilations.Clear();
            return this;
        }

        public GeneratorTester AddReferences(MetadataReference reference, params MetadataReference[] additionalReferences)
        {
            _references.Add(reference);
            _references.AddRange(additionalReferences);
            _project = null;
            _lastCompilations.Clear();
            return this;
        }

        public GeneratorTester AddReferences(IEnumerable<Assembly> additionalSources)
        {
            _assemblies.AddRange(additionalSources);
            _project = null;
            _lastCompilations.Clear();
            return this;
        }

        public GeneratorTester AddReferences(Assembly assembly, params Assembly[] additionalAssemblies)
        {
            _assemblies.Add(assembly);
            _assemblies.AddRange(additionalAssemblies);
            _project = null;
            _lastCompilations.Clear();
            return this;
        }

        public GeneratorTester AddSources(SourceText source, params SourceText[] additionalSources)
        {
            _sources.Add(source);
            _sources.AddRange(additionalSources);
            _project = null;
            _lastCompilations.Clear();
            return this;
        }

        public GeneratorTester AddSources(IEnumerable<SourceText> additionalSources)
        {
            _sources.AddRange(additionalSources);
            _project = null;
            _lastCompilations.Clear();
            return this;
        }

        public GeneratorTester AddSources(string source, params string[] additionalSources)
        {
            _sources.Add(SourceText.From(source, Encoding.UTF8));
            _sources.AddRange(additionalSources.Select(s => SourceText.From(s, Encoding.UTF8)));
            _project = null;
            _lastCompilations.Clear();
            return this;
        }

        public GeneratorTester AddSources(IEnumerable<string> additionalSources)
        {
            _sources.AddRange(additionalSources.Select(s => SourceText.From(s, Encoding.UTF8)));
            _project = null;
            _lastCompilations.Clear();
            return this;
        }

        public CSharpCompilation Compile()
        {
            var project = CreateProject();

            if (_lastCompilations.TryGetValue(typeof(CSharpCompilation), out var outCompilation))
            {
                return outCompilation;
            }

            var compilation = (CSharpCompilation) project.GetCompilationAsync().ConfigureAwait(false).GetAwaiter().GetResult()!;
            if (compilation is null)
            {
                throw new InvalidOperationException("Could not compile the sources");
            }

            return _lastCompilations[typeof(CSharpCompilation)] = compilation;
        }

        public IEnumerable<SyntaxTree> Generate<T>()
            where T : ISourceGenerator, new()
        {
            var project = CreateProject();

            var compilation = (CSharpCompilation) project.GetCompilationAsync().ConfigureAwait(false).GetAwaiter().GetResult()!;
            if (compilation is null)
            {
                throw new InvalidOperationException("Could not compile the sources");
            }


            if (_lastCompilations.TryGetValue(typeof(CSharpCompilation), out var outCompilation))
            {
                return _lastSyntax = outCompilation.SyntaxTrees.TakeLast(outCompilation.SyntaxTrees.Count() - compilation.SyntaxTrees.Length).ToImmutableArray();
            }

            var startingSyntaxTress = compilation.SyntaxTrees.Length;

            // var diagnostics = compilation.GetDiagnostics();
            // Assert.Empty(diagnostics.Where(x => x.Severity > DiagnosticSeverity.Warning));

            ISourceGenerator generator = new T();

            var driver = new CSharpGeneratorDriver(compilation.SyntaxTrees[0].Options, ImmutableArray.Create(generator), default, ImmutableArray<AdditionalText>.Empty);

            driver.RunFullGeneration(compilation, out var outputCompilation, out _generatorDiagnostics);
            _lastCompilations[typeof(T)] = outputCompilation as CSharpCompilation;

            // the syntax tree added by the generator will be the last one in the compilation
            return _lastSyntax = outputCompilation.SyntaxTrees.TakeLast(outputCompilation.SyntaxTrees.Count() - startingSyntaxTress).Select(z =>
            {
                _testOutputHelper?.WriteLine(z.GetText().ToString());
                return z;
            }).ToImmutableArray();
        }

        public void AssertCompilationWasSuccessful(Type? type = null)
        {
            if (type == null && _lastCompilations.Count > 1) return;
            var compilation = _lastCompilations.Single(z => type == null || z.Key == type).Value;
            Assert.NotNull(compilation);
            var diagnostics = CompilationDiagnostics;
            Assert.Empty(diagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning));
        }

        public void AssertGenerationWasSuccessful(Type? type = null)
        {
            if (type == null && _lastCompilations.Count > 1) return;
            var compilation = _lastCompilations.Single(z => type == null || z.Key == type).Value;
            Assert.NotNull(compilation);
            Assert.Empty(GeneratorDiagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning));
        }

        public Assembly Emit(string? outputName = null)
        {
            using var stream = new MemoryStream();
            var emitResult = _lastCompilations.Values.Single().Emit(stream, options: new EmitOptions(outputNameOverride: outputName));
            if (!emitResult.Success)
            {
                Assert.Empty(emitResult.Diagnostics);
            }

            var data = stream.ToArray();

            using var assemblyStream = new MemoryStream(data);
            return _context.LoadFromStream(assemblyStream);
        }

        public ImmutableArray<Diagnostic> CompilationDiagnostics => _lastCompilations.Values.SelectMany(z => z.GetDiagnostics()).ToImmutableArray();
        public ImmutableArray<SyntaxTree> GeneratorSyntaxTrees => _lastSyntax!;
        public ImmutableArray<Diagnostic> GeneratorDiagnostics => _generatorDiagnostics;

        public void Dispose()
        {
        }

        private Project CreateProject()
        {
            if (_project != null) return _project;
            var projectId = ProjectId.CreateNewId(_testProjectName);
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, _testProjectName, _testProjectName, LanguageNames.CSharp)
                .WithProjectCompilationOptions(
                    projectId,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                )
                .WithProjectParseOptions(
                    projectId,
                    new CSharpParseOptions(preprocessorSymbols: new[] {"SOMETHING_ACTIVE"})
                )
                .AddMetadataReferences(projectId, _references.Concat(_assemblies.Distinct().Select(z => MetadataReference.CreateFromFile(z.Location))));

            var count = 0;
            foreach (var source in _sources)
            {
                var newFileName = _filePathPrefix + count + "." + _fileExt;
                var documentId = DocumentId.CreateNewId(projectId, newFileName);
                solution = solution.AddDocument(documentId, newFileName, source);
                count++;
            }

            var project = solution.GetProject(projectId);
            if (project is null)
            {
                throw new InvalidOperationException($"The ad hoc workspace does not contain a project with the id {projectId.Id}");
            }

            return _project = project;
        }
    }

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

        // internal static readonly string NormalizedPreamble = NormalizeToLf(Preamble.GeneratedByATool + Lf);

        internal static readonly ImmutableArray<PortableExecutableReference> MetadataReferences;

        public static string NormalizeToLf(string input) => input.Replace(CrLf, Lf);

        public static Assembly EmitInto(this CSharpCompilation compilation, AssemblyLoadContext context, string? outputName = null)
        {
            using var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream, options: new EmitOptions(outputNameOverride: outputName));
            if (!emitResult.Success)
            {
                Assert.Empty(emitResult.Diagnostics);
            }

            var data = stream.ToArray();

            using var assemblyStream = new MemoryStream(data);
            return context.LoadFromStream(assemblyStream);
        }

        public static MetadataReference CreateMetadataReference(this CSharpCompilation compilation)
        {
            using var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream, options: new EmitOptions(outputNameOverride: compilation.AssemblyName));
            if (!emitResult.Success)
            {
                Assert.Empty(emitResult.Diagnostics);
            }

            var data = stream.ToArray();

            using var assemblyStream = new MemoryStream(data);
            return MetadataReference.CreateFromStream(assemblyStream, MetadataReferenceProperties.Assembly);
        }

        private static Func<SyntaxTree, bool> ApplyPathHint(string? pathHint) =>
            z => pathHint == null || z.FilePath.Contains(pathHint, StringComparison.OrdinalIgnoreCase);

        public static void AssertGeneratedAsExpected<T>(this GeneratorTester generator, string source, string expected, string? pathHint = null)
            where T : ISourceGenerator, new()
        {
            var generatedTree = generator.Generate<T>(new[] {source});
            // normalize line endings to just LF
            var generatedText = generatedTree
                .Where(ApplyPathHint(pathHint))
                .Select(z => NormalizeToLf(z.GetText().ToString()));
            // and append preamble to the expected
            var expectedText = NormalizeToLf(expected).Trim();
            Assert.Equal(generatedText.LastOrDefault()!, expectedText);
        }

        public static void AssertGeneratedAsExpected<T>(this GeneratorTester generator, IEnumerable<string> sources, IEnumerable<string> expected, string? pathHint = null)
            where T : ISourceGenerator, new()
        {
            var generatedTree = generator.Generate<T>(sources);
            // normalize line endings to just LF
            var generatedText = generatedTree
                .Where(ApplyPathHint(pathHint))
                .Select(z => NormalizeToLf(z.GetText().ToString()))
                .ToArray();
            // and append preamble to the expected
            var expectedText = expected.Select(z => NormalizeToLf(z).Trim()).ToArray();

            Assert.Equal(generatedText.Length, expectedText.Length);
            foreach (var (generated, expectedTxt) in generatedText.Zip(expectedText, (generated, expected) => (generated, expected)))
            {
                Assert.Equal(generated, expectedTxt);
            }
        }

        public static IEnumerable<SyntaxTree> Generate<T>(this GeneratorTester generator, string source, string? pathHint = null)
            where T : ISourceGenerator, new()
        {
            var generatedTree = generator.AddSources(source).Generate<T>();
            // normalize line endings to just LF
            var generatedText = generatedTree.Where(ApplyPathHint(pathHint));
            // and append preamble to the expected
            return generatedText;
        }

        public static IEnumerable<SyntaxTree> Generate<T>(this GeneratorTester generator, IEnumerable<string> sources, string? pathHint = null)
            where T : ISourceGenerator, new()
        {
            var generatedTree = generator.AddSources(sources).Generate<T>();
            // normalize line endings to just LF
            var generatedText = generatedTree.Where(ApplyPathHint(pathHint));
            // and append preamble to the expected
            return generatedText.ToArray();
        }

        public static IEnumerable<string> Normalize(this IEnumerable<SyntaxTree> syntaxTrees) =>
            syntaxTrees.Select(z => NormalizeToLf(z.GetText().ToString()));
    }
}
