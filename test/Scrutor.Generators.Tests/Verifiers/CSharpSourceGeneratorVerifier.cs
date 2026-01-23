using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Scrutor.Generators.Tests.Verifiers;

/// <summary>
/// Helper for testing Scrutor source generators.
/// </summary>
public static class GeneratorTestHelper
{
    public static (ImmutableArray<Diagnostic> Diagnostics, string GeneratedSource) RunGenerator(
        string source,
        bool enableGeneration = true)
    {
        var (diagnostics, sources) = RunGeneratorFull(source, enableGeneration);
        var generatedSource = sources.Values.FirstOrDefault() ?? string.Empty;
        return (diagnostics, generatedSource);
    }

    public static (ImmutableArray<Diagnostic> Diagnostics, IReadOnlyDictionary<string, string> GeneratedSources) RunGeneratorFull(
        string source,
        bool enableGeneration = true)
    {
        var compilation = CreateCompilation(source);
        var generator = new ScrutorSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(enableGeneration));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        var runResult = driver.GetRunResult();

        var generatedSources = runResult.GeneratedTrees
            .ToDictionary(
                tree => Path.GetFileName(tree.FilePath),
                tree => tree.GetText().ToString());

        return (runResult.Diagnostics, generatedSources);
    }

    /// <summary>
    /// Creates a generator driver for use with Verify.
    /// </summary>
    public static GeneratorDriver CreateDriver(string source, bool enableGeneration = true)
    {
        var compilation = CreateCompilation(source);
        var generator = new ScrutorSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(enableGeneration));

        return driver.RunGenerators(compilation);
    }

    private static CSharpCompilation CreateCompilation(string source, string? filePath = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath ?? "TestSource.cs");

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ServiceDescriptorAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location)
        };

        // Add runtime references
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")));

        // Try to add netstandard if available
        var netstandardPath = Path.Combine(assemblyPath, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        return CSharpCompilation.Create(
            "TestProject",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly bool _enabled;

        public TestAnalyzerConfigOptionsProvider(bool enabled)
        {
            _enabled = enabled;
        }

        public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(_enabled);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestAnalyzerConfigOptions(_enabled);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestAnalyzerConfigOptions(_enabled);
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly bool _enabled;

        public TestAnalyzerConfigOptions(bool enabled)
        {
            _enabled = enabled;
        }

        public override bool TryGetValue(string key, out string value)
        {
            if (key == "build_property.ScrutorGenerateServices")
            {
                value = _enabled ? "true" : "false";
                return true;
            }

            value = null!;
            return false;
        }
    }
}
