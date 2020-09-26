using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scrutor.Analyzers.Internals;
using Scrutor.Static;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Scrutor.Analyzers
{
    [Generator]
    public class StaticScrutorGenerator : ISourceGenerator
    {
        public void Initialize(InitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new StaticScrutorSyntaxReceiver());
        }

        static SourceText staticScanSourceText = SourceText.From(
            @"
using System;
using System.Runtime.CompilerServices;
using Scrutor;
using Scrutor.Static;
namespace Microsoft.Extensions.DependencyInjection
{
    internal static class StaticScrutorExtensions
    {
        public static IServiceCollection ScanStatic(
            this IServiceCollection services,
            Action<ICompiledAssemblySelector> action,
	        [CallerFilePathAttribute] string filePath = """",
	        [CallerMemberName] string memberName = """",
	        [CallerLineNumberAttribute] int lineNumber = 0
        )
        {
            return PopulateExtensions.Populate(services, RegistrationStrategy.Append, filePath, memberName, lineNumber);
        }

        public static IServiceCollection ScanStatic(
            this IServiceCollection services,
            Action<ICompiledAssemblySelector> action,
            RegistrationStrategy strategy,
	        [CallerFilePathAttribute] string filePath = """",
	        [CallerMemberName] string memberName = """",
	        [CallerLineNumberAttribute] int lineNumber = 0
        )
        {
            return PopulateExtensions.Populate(services, strategy, filePath, memberName, lineNumber);
        }
    }
}
",
            Encoding.UTF8
        );

        static SourceText populateSourceText = SourceText.From(
            @"
using System;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
namespace Scrutor.Static
{
    internal static class PopulateExtensions
    {
        public static IServiceCollection Populate(IServiceCollection services, RegistrationStrategy strategy, string filePath, string memberName, int lineNumber)
        {
            return services;
        }
    }
}
",
            Encoding.UTF8
        );

        public void Execute(SourceGeneratorContext context)
        {
            if (!(context.SyntaxReceiver is StaticScrutorSyntaxReceiver syntaxReceiver) || syntaxReceiver.ScanStaticExpressions.Count == 0)
            {
                return;
            }

            var compilation = (context.Compilation as CSharpCompilation)!;
            var compilationWithMethod = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(staticScanSourceText), CSharpSyntaxTree.ParseText(populateSourceText));

            context.AddSource("Microsoft.Extensions.DependencyInjection.StaticScrutorExtension.cs", staticScanSourceText);

            var groups =
                new List<(
                    ExpressionSyntax expression,
                    string filePath,
                    string memberName,
                    int lineNumber,
                    List<IAssemblyDescriptor> assemblies,
                    List<ITypeFilterDescriptor> typeFilters,
                    List<IServiceTypeDescriptor> serviceTypes,
                    ClassFilter classFilter,
                    ExpressionSyntax lifetime
                    )>();

            foreach (var rootExpression in syntaxReceiver.ScanStaticExpressions)
            {
                var semanticModel = compilationWithMethod.GetSemanticModel(rootExpression.SyntaxTree);

                var diagnostics = compilationWithMethod.GetDiagnostics();
                var assemblies = new List<IAssemblyDescriptor>();
                var typeFilters = new List<ITypeFilterDescriptor>();
                var serviceTypes = new List<IServiceTypeDescriptor>();
                var classFilter = ClassFilter.All;
                var lifetimeExpressionSyntax =
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("ServiceLifetime"),
                        IdentifierName("Transient")
                    );

                DataHelpers.HandleInvocationExpressionSyntax(
                    compilationWithMethod,
                    semanticModel,
                    rootExpression,
                    assemblies,
                    typeFilters,
                    serviceTypes,
                    ref classFilter,
                    ref lifetimeExpressionSyntax
                );

                var containingMethod = rootExpression.Ancestors().OfType<MethodDeclarationSyntax>().First();

                var methodCallSyntax = rootExpression.Ancestors()
                   .OfType<InvocationExpressionSyntax>()
                   .First(
                        ies => ies.Expression is MemberAccessExpressionSyntax mae
                         && mae.Name.ToFullString().EndsWith("ScanStatic", StringComparison.Ordinal)
                    );

                groups.Add(
                    (
                        rootExpression,
                        rootExpression.SyntaxTree.FilePath,
                        containingMethod.Identifier.Text,
                        // line numbers here are 1 based
                        methodCallSyntax.SyntaxTree.GetText().Lines.First(z => z.Span.IntersectsWith(methodCallSyntax.Span)).LineNumber + 1,
                        assemblies,
                        typeFilters,
                        serviceTypes,
                        classFilter,
                        lifetimeExpressionSyntax
                    )
                );

                if (serviceTypes.Count == 0)
                {
                    serviceTypes.Add(new SelfServiceTypeDescriptor());
                }
            }

            var allNamedTypes = TypeSymbolVisitor.GetTypes(compilationWithMethod);

            var strategyName = IdentifierName("strategy");
            var serviceCollectionName = IdentifierName("services");
            var lineNumberIdentifier = IdentifierName("lineNumber");
            var filePathIdentifier = IdentifierName("filePath");
            var block = Block();

            var switchStatement = SwitchStatement(lineNumberIdentifier);
            foreach (var lineGrouping in groups.GroupBy(z => z.lineNumber))
            {
                var innerBlock = Block();
                var blocks = new List<(string filePath, string memberName, BlockSyntax block)>();
                foreach (var (expression, filePath, memberName, lineNumber, assemblies, typeFilters, serviceTypes, classFilter, lifetime) in lineGrouping)
                {
                    var types = NarrowListOfTypes(assemblies, allNamedTypes, compilationWithMethod, classFilter, typeFilters);

                    var localBlock = GenerateDescriptors(
                        types,
                        serviceTypes,
                        innerBlock,
                        strategyName,
                        serviceCollectionName,
                        lifetime
                    );

                    blocks.Add((filePath, memberName, localBlock));
                }

                static SwitchSectionSyntax CreateNestedSwitchSections<T>(
                    IReadOnlyList<(string filePath, string memberName, BlockSyntax block)> blocks,
                    NameSyntax identifier,
                    Func<(string filePath, string memberName, BlockSyntax block), T> regroup,
                    Func<IGrouping<T, (string filePath, string memberName, BlockSyntax block)>, SwitchSectionSyntax> next,
                    Func<T, LiteralExpressionSyntax> literalFactory
                )
                {
                    if (blocks.Count == 1)
                    {
                        var (_, _, localBlock) = blocks[0];
                        return SwitchSection()
                           .AddStatements(localBlock.Statements.ToArray())
                           .AddStatements(BreakStatement());
                    }

                    var section = SwitchStatement(identifier);
                    foreach (var item in blocks.GroupBy(regroup))
                    {
                        section = section
                           .AddSections(
                                next(item)
                                   .AddLabels(
                                        CaseSwitchLabel(literalFactory(item.Key))
                                    )
                            );
                    }

                    return SwitchSection().AddStatements(section, BreakStatement());
                }

                var lineSwitchSection = CreateNestedSwitchSections(
                        blocks,
                        IdentifierName("filePath"),
                        x => x.filePath,
                        GenerateFilePathSwitchStatement,
                        value =>
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(value)
                            )
                    )
                   .AddLabels(
                        CaseSwitchLabel(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(lineGrouping.Key)
                            )
                        )
                    );

                static SwitchSectionSyntax GenerateFilePathSwitchStatement(
                    IGrouping<string, (string filePath, string memberName, BlockSyntax block)> innerGroup
                ) => CreateNestedSwitchSections(
                    innerGroup.ToArray(),
                    IdentifierName("memberName"),
                    x => x.memberName,
                    GenerateMemberNameSwitchStatement,
                    value =>
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(value)
                        )
                );

                static SwitchSectionSyntax GenerateMemberNameSwitchStatement(
                    IGrouping<string, (string filePath, string memberName, BlockSyntax block)> innerGroup
                ) => SwitchSection()
                   .AddLabels(
                        CaseSwitchLabel(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(innerGroup.Key)
                            )
                        )
                    )
                   .AddStatements(innerGroup.FirstOrDefault().block?.Statements.ToArray() ?? Array.Empty<StatementSyntax>())
                   .AddStatements(BreakStatement());


                switchStatement = switchStatement.AddSections(lineSwitchSection);
            }

            {
                var root = CSharpSyntaxTree.ParseText(populateSourceText).GetCompilationUnitRoot();
                var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

                root = root.ReplaceNode(method, method.WithBody(block.AddStatements(switchStatement).AddStatements(method.Body!.Statements.ToArray())));

                context.AddSource("Scrutor.Static.Populate.cs", root.NormalizeWhitespace().GetText());
            }
        }

        private static BlockSyntax GenerateDescriptors(
            ImmutableArray<INamedTypeSymbol> types,
            List<IServiceTypeDescriptor> serviceTypes,
            BlockSyntax innerBlock,
            IdentifierNameSyntax strategyName,
            IdentifierNameSyntax serviceCollectionName,
            ExpressionSyntax lifetime
        )
        {
            var asSelf = serviceTypes.OfType<SelfServiceTypeDescriptor>().Any();
            var asImplementedInterfaces = serviceTypes.OfType<ImplementedInterfacesServiceTypeDescriptor>().Any();
            var asMatchingInterface = serviceTypes.OfType<MatchingInterfaceServiceTypeDescriptor>().Any();
            if (!asSelf && !asImplementedInterfaces && !asMatchingInterface)
                asSelf = true;
            foreach (var type in types)
            {
                if (asSelf)
                {
                    innerBlock = innerBlock.AddStatements(
                        ExpressionStatement(
                            StatementGeneration.GenerateServiceType(
                                strategyName,
                                serviceCollectionName,
                                type,
                                type,
                                lifetime
                            )
                        )
                    );

                    if (asMatchingInterface)
                    {
                        var name = $"I{type.Name}";
                        var @interface = type.AllInterfaces.FirstOrDefault(z => z.Name == name);
                        if (@interface is not null)
                        {
                            innerBlock = innerBlock.AddStatements(
                                ExpressionStatement(
                                    StatementGeneration.GenerateServiceFactory(
                                        strategyName,
                                        serviceCollectionName,
                                        @interface,
                                        type,
                                        lifetime
                                    )
                                )
                            );
                        }
                    }
                    else if (asImplementedInterfaces)
                    {
                        foreach (var @interface in type.AllInterfaces)
                        {
                            innerBlock = innerBlock.AddStatements(
                                ExpressionStatement(
                                    StatementGeneration.GenerateServiceFactory(
                                        strategyName,
                                        serviceCollectionName,
                                        @interface,
                                        type,
                                        lifetime
                                    )
                                )
                            );
                        }
                    }
                }
                else
                {
                    if (asMatchingInterface)
                    {
                        var name = $"I{type.Name}";
                        var @interface = type.AllInterfaces.FirstOrDefault(z => z.Name == name);
                        if (@interface is not null)
                        {
                            innerBlock = innerBlock.AddStatements(
                                ExpressionStatement(
                                    StatementGeneration.GenerateServiceType(
                                        strategyName,
                                        serviceCollectionName,
                                        @interface,
                                        type,
                                        lifetime
                                    )
                                )
                            );
                        }
                    }
                    else if (asImplementedInterfaces)
                    {
                        foreach (var @interface in type.AllInterfaces)
                        {
                            innerBlock = innerBlock.AddStatements(
                                ExpressionStatement(
                                    StatementGeneration.GenerateServiceType(
                                        strategyName,
                                        serviceCollectionName,
                                        @interface,
                                        type,
                                        lifetime
                                    )
                                )
                            );
                        }
                    }
                }
            }

            return innerBlock;
        }

        static ImmutableArray<INamedTypeSymbol> NarrowListOfTypes(
            List<IAssemblyDescriptor> assemblies,
            ImmutableArray<INamedTypeSymbol> iNamedTypeSymbols,
            CSharpCompilation cSharpCompilation,
            ClassFilter classFilter,
            List<ITypeFilterDescriptor> typeFilters
        )
        {
            var types = assemblies.OfType<AllAssemblyDescriptor>().Any()
                ? iNamedTypeSymbols
                : TypeSymbolVisitor.GetTypes(
                    cSharpCompilation,
                    assemblies
                       .OfType<ICompiledAssemblyDescriptor>()
                       .Select(z => z.TypeFromAssembly.ContainingAssembly)
                       .Distinct(SymbolEqualityComparer.Default)
                );

            if (classFilter == ClassFilter.PublicOnly)
            {
                types = types.RemoveAll(symbol => symbol.DeclaredAccessibility == Accessibility.Public);
            }

            foreach (var filter in typeFilters.OfType<CompiledAssignableToTypeFilterDescriptor>())
            {
                types = types.RemoveAll(
                    toSymbol => SymbolEqualityComparer.Default.Equals(filter.Type, toSymbol) || !cSharpCompilation.HasImplicitConversion(toSymbol, filter.Type)
                );
            }

            var anyFilters = typeFilters.OfType<CompiledAssignableToAnyTypeFilterDescriptor>().ToArray();
            if (anyFilters.Length > 0)
            {
                types = types.RemoveAll(
                    toSymbol => anyFilters.Any(
                        filter => SymbolEqualityComparer.Default.Equals(filter.Type, toSymbol) || cSharpCompilation.HasImplicitConversion(toSymbol, filter.Type)
                    )
                );
            }

            foreach (var filter in typeFilters.OfType<NamespaceFilterDescriptor>())
            {
                types = types.RemoveAll(
                    toSymbol =>
                        filter.Filter switch
                        {
                            NamespaceFilter.Exact => toSymbol.ContainingNamespace.ToDisplayString() != filter.Namespace,
                            NamespaceFilter.In => !toSymbol.ContainingNamespace.ToDisplayString().StartsWith(filter.Namespace, StringComparison.Ordinal),
                            NamespaceFilter.NotIn => toSymbol.ContainingNamespace.ToDisplayString().StartsWith(filter.Namespace, StringComparison.Ordinal),
                            _ => true
                        }
                );
            }

            // foreach (var filter in typeFilters.OfType<CompiledAttributeFilterDescriptor>())
            // {
            // }
            return types;
        }
    }
}
