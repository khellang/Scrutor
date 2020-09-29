using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Runtime.Loader;
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
            return PopulateExtensions.Populate(services, RegistrationStrategy.Append, AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.GetLoadContext(typeof(StaticScrutorExtensions).Assembly) ?? AssemblyLoadContext.Default, filePath, memberName, lineNumber);
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
            return PopulateExtensions.Populate(services, strategy, AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.GetLoadContext(typeof(StaticScrutorExtensions).Assembly) ?? AssemblyLoadContext.Default, filePath, memberName, lineNumber);
        }

        public static IServiceCollection ScanStatic(
            this IServiceCollection services,
            Action<ICompiledAssemblySelector> action,
            AssemblyLoadContext context,
	        [CallerFilePathAttribute] string filePath = """",
	        [CallerMemberName] string memberName = """",
	        [CallerLineNumberAttribute] int lineNumber = 0
        )
        {
            return PopulateExtensions.Populate(services, RegistrationStrategy.Append, context, filePath, memberName, lineNumber);
        }

        public static IServiceCollection ScanStatic(
            this IServiceCollection services,
            Action<ICompiledAssemblySelector> action,
            RegistrationStrategy strategy,
            AssemblyLoadContext context,
	        [CallerFilePathAttribute] string filePath = """",
	        [CallerMemberName] string memberName = """",
	        [CallerLineNumberAttribute] int lineNumber = 0
        )
        {
            return PopulateExtensions.Populate(services, strategy, context, filePath, memberName, lineNumber);
        }
    }
}
",
            Encoding.UTF8
        );

        static SourceText populateSourceText = SourceText.From(
            @"
using System;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Scrutor.Static
{
    internal static class PopulateExtensions
    {
        public static IServiceCollection Populate(IServiceCollection services, RegistrationStrategy strategy, AssemblyLoadContext context, string filePath, string memberName, int lineNumber)
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
                    context,
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
            var privateAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

            var switchStatement = SwitchStatement(lineNumberIdentifier);
            foreach (var lineGrouping in groups.GroupBy(z => z.lineNumber))
            {
                var innerBlock = Block();
                var blocks = new List<(string filePath, string memberName, BlockSyntax block)>();
                foreach (var (expression, filePath, memberName, lineNumber, assemblies, typeFilters, serviceTypes, classFilter, lifetime) in lineGrouping)
                {
                    var types = NarrowListOfTypes(assemblies, allNamedTypes, compilationWithMethod, classFilter, typeFilters);

                    var localBlock = GenerateDescriptors(
                        compilationWithMethod,
                        types,
                        serviceTypes,
                        innerBlock,
                        strategyName,
                        serviceCollectionName,
                        lifetime,
                        privateAssemblies
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
                        section = section.AddSections(next(item).AddLabels(CaseSwitchLabel(literalFactory(item.Key))));
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

                static SwitchSectionSyntax GenerateFilePathSwitchStatement(IGrouping<string, (string filePath, string memberName, BlockSyntax block)> innerGroup) =>
                    CreateNestedSwitchSections(
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

                static SwitchSectionSyntax GenerateMemberNameSwitchStatement(IGrouping<string, (string filePath, string memberName, BlockSyntax block)> innerGroup) =>
                    SwitchSection()
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

                var assemblyContext = IdentifierName("context");

                var newMethod = method
                    .WithBody(block.AddStatements(switchStatement).AddStatements(method.Body!.Statements.ToArray()));

                root = root.ReplaceNode(method, newMethod);

                if (privateAssemblies.Any())
                {
                    var @class = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
                    var privateAssemblyNodes = privateAssemblies
                        .SelectMany(StatementGeneration.AssemblyDeclaration);
                    root = root.ReplaceNode(@class, @class.AddMembers(privateAssemblyNodes.ToArray()));
                }

                context.AddSource("Scrutor.Static.Populate.cs", root.NormalizeWhitespace().GetText());
            }
        }

        private static BlockSyntax GenerateDescriptors(
            CSharpCompilation compilation,
            ImmutableArray<INamedTypeSymbol> types,
            List<IServiceTypeDescriptor> serviceTypes,
            BlockSyntax innerBlock,
            IdentifierNameSyntax strategyName,
            IdentifierNameSyntax serviceCollectionName,
            ExpressionSyntax lifetime,
            HashSet<IAssemblySymbol> privateAssemblies
        )
        {
            var asSelf = serviceTypes.OfType<SelfServiceTypeDescriptor>().Any() || !serviceTypes.Any();
            var asImplementedInterfaces = serviceTypes.OfType<ImplementedInterfacesServiceTypeDescriptor>().Any();
            var asMatchingInterface = serviceTypes.OfType<MatchingInterfaceServiceTypeDescriptor>().Any();
            var asSpecificTypes = serviceTypes.OfType<CompiledServiceTypeDescriptor>().Select(z => z.Type).ToArray();

            foreach (var type in types)
            {
                var emittedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var typeIsOpenGeneric = type.IsOpenGenericType();
                if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
                {
                    privateAssemblies.Add(type.ContainingAssembly);
                }

                if (asSelf && !emittedTypes.Contains(type))
                {
                    innerBlock = innerBlock.AddStatements(
                        ExpressionStatement(
                            StatementGeneration.GenerateServiceType(
                                compilation,
                                strategyName,
                                serviceCollectionName,
                                type,
                                type,
                                lifetime
                            )
                        )
                    );
                    emittedTypes.Add(type);
                }

                if (asMatchingInterface)
                {
                    var name = $"I{type.Name}";
                    var @interface = type.AllInterfaces.FirstOrDefault(z => z.Name == name);
                    if (@interface is not null && !emittedTypes.Contains(@interface))
                    {
                        innerBlock = innerBlock.AddStatements(
                            ExpressionStatement(
                                typeIsOpenGeneric || !asSelf
                                    ? StatementGeneration.GenerateServiceType(
                                        compilation,
                                        strategyName,
                                        serviceCollectionName,
                                        @interface,
                                        type,
                                        lifetime
                                    )
                                    : StatementGeneration.GenerateServiceFactory(
                                        compilation,
                                        strategyName,
                                        serviceCollectionName,
                                        @interface,
                                        type,
                                        lifetime
                                    )
                            )
                        );
                        if (!compilation.IsSymbolAccessibleWithin(@interface, compilation.Assembly))
                        {
                            privateAssemblies.Add(type.ContainingAssembly);
                        }

                        emittedTypes.Add(@interface);
                    }
                }

                if (asImplementedInterfaces)
                {
                    foreach (var @interface in type.AllInterfaces)
                    {
                        if (!emittedTypes.Contains(@interface))
                        {
                            innerBlock = innerBlock.AddStatements(
                                ExpressionStatement(
                                    typeIsOpenGeneric || !asSelf
                                        ? StatementGeneration.GenerateServiceType(
                                            compilation,
                                            strategyName,
                                            serviceCollectionName,
                                            @interface,
                                            type,
                                            lifetime
                                        )
                                        : StatementGeneration.GenerateServiceFactory(
                                            compilation,
                                            strategyName,
                                            serviceCollectionName,
                                            @interface,
                                            type,
                                            lifetime
                                        )
                                )
                            );
                            if (!compilation.IsSymbolAccessibleWithin(@interface, compilation.Assembly))
                            {
                                privateAssemblies.Add(type.ContainingAssembly);
                            }

                            emittedTypes.Add(@interface);
                        }
                    }
                }

                foreach (var asType in asSpecificTypes)
                {
                    if (!emittedTypes.Contains(asType))
                    {
                        innerBlock = innerBlock.AddStatements(
                            ExpressionStatement(
                                !asSelf
                                    ? StatementGeneration.GenerateServiceType(
                                        compilation,
                                        strategyName,
                                        serviceCollectionName,
                                        asType,
                                        type,
                                        lifetime
                                    )
                                    : StatementGeneration.GenerateServiceFactory(
                                        compilation,
                                        strategyName,
                                        serviceCollectionName,
                                        asType,
                                        type,
                                        lifetime
                                    )
                            )
                        );
                        emittedTypes.Add(asType);
                    }
                }
            }

            return innerBlock;
        }

        static ImmutableArray<INamedTypeSymbol> NarrowListOfTypes(
            List<IAssemblyDescriptor> assemblies,
            ImmutableArray<INamedTypeSymbol> iNamedTypeSymbols,
            CSharpCompilation compilation,
            ClassFilter classFilter,
            List<ITypeFilterDescriptor> typeFilters
        )
        {
            var types = assemblies.OfType<AllAssemblyDescriptor>().Any()
                ? iNamedTypeSymbols
                : TypeSymbolVisitor.GetTypes(
                    compilation,
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
                types = types.RemoveAll(toSymbol => StatementGeneration.RemoveImplicitGenericConversion(compilation, filter.Type, toSymbol));
            }

            var anyFilters = typeFilters.OfType<CompiledAssignableToAnyTypeFilterDescriptor>().ToArray();
            if (anyFilters.Length > 0)
            {
                types = types.RemoveAll(toSymbol => anyFilters.All(filter => StatementGeneration.RemoveImplicitGenericConversion(compilation, filter.Type, toSymbol)));
            }

            foreach (var filter in typeFilters.OfType<NamespaceFilterDescriptor>())
            {
                types = filter.Filter switch
                {
                    NamespaceFilter.Exact => types.RemoveAll(toSymbol => toSymbol.ContainingNamespace.ToDisplayString() != filter.Namespace),
                    NamespaceFilter.In => types.RemoveAll(toSymbol => !toSymbol.ContainingNamespace.ToDisplayString().StartsWith(filter.Namespace, StringComparison.Ordinal)),
                    NamespaceFilter.NotIn => types.RemoveAll(toSymbol => toSymbol.ContainingNamespace.ToDisplayString().StartsWith(filter.Namespace, StringComparison.Ordinal)),
                    _ => types
                };
            }

            // foreach (var filter in typeFilters.OfType<CompiledAttributeFilterDescriptor>())
            // {
            // }
            return types;
        }
    }
}
