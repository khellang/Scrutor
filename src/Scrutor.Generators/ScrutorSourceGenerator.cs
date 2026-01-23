using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scrutor.Generators.Analyzers;
using Scrutor.Generators.Emitters;
using Scrutor.Generators.Models;
using Scrutor.Generators.Utilities;

namespace Scrutor.Generators;

/// <summary>
/// Roslyn incremental source generator that analyzes Scrutor usage and generates
/// AOT-compatible service registration code using interceptors.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ScrutorSourceGenerator : IIncrementalGenerator
{
    private const string ServiceDescriptorAttributeFullName = "Scrutor.ServiceDescriptorAttribute";
    private const string GenericServiceDescriptorAttributeFullName = "Scrutor.ServiceDescriptorAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Check MSBuild property to enable/disable generation
        var enableGeneration = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.ScrutorGenerateServices", out var value);
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            });

        // Collect [ServiceDescriptor] attributes on classes
        var attributeRegistrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ServiceDescriptorAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (context, ct) => ServiceDescriptorAttributeAnalyzer.Analyze(context, ct))
            .SelectMany((registrations, _) => registrations)
            .Collect();

        // Collect [ServiceDescriptor<T>] generic attributes on classes
        var genericAttributeRegistrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenericServiceDescriptorAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (context, ct) => ServiceDescriptorAttributeAnalyzer.Analyze(context, ct))
            .SelectMany((registrations, _) => registrations)
            .Collect();

        // Combine both attribute registrations
        var allAttributeRegistrations = attributeRegistrations.Combine(genericAttributeRegistrations)
            .Select((tuple, _) => tuple.Left.AddRange(tuple.Right));

        // Collect Scan() invocations for interceptors
        var scanInterceptorInfos = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: ScanMethodChainAnalyzer.IsScanInvocation,
                transform: ScanMethodChainAnalyzer.AnalyzeForInterceptor)
            .Where(info => info != null)
            .Select((info, _) => info!)
            .Collect();

        // Combine all inputs including compilation for type discovery
        var combined = allAttributeRegistrations
            .Combine(scanInterceptorInfos)
            .Combine(enableGeneration)
            .Combine(context.CompilationProvider);

        // Register source output
        context.RegisterSourceOutput(combined, GenerateSource);
    }

    private static void GenerateSource(
        SourceProductionContext context,
        (((ImmutableArray<ServiceRegistration> Attributes, ImmutableArray<ScanInterceptorInfo> Scans) Data, bool Enabled) Combined, Compilation Compilation) input)
    {
        var (((attributeRegistrations, scanInterceptorInfos), enabled), compilation) = input;

        // Only generate if explicitly enabled
        if (!enabled)
        {
            return;
        }

        // Assign unique IDs to each interceptor
        var interceptorList = scanInterceptorInfos.ToList();
        for (int i = 0; i < interceptorList.Count; i++)
        {
            interceptorList[i].Id = i;
        }

        // Report diagnostics for unsupported patterns in Scan() chains
        foreach (var info in interceptorList)
        {
            if (info.HasUnsupportedPatterns && info.Location != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedPattern,
                    info.Location,
                    info.UnsupportedPatternDescription ?? "Unknown pattern"));
            }
        }

        // Generate interceptors if there are any Scan() calls
        if (interceptorList.Count > 0)
        {
            var interceptorSource = InterceptorEmitter.Emit(
                interceptorList,
                attributeRegistrations,
                compilation);
            context.AddSource("ScrutorInterceptors.g.cs", interceptorSource);
        }
    }
}
