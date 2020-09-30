using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scrutor.Analyzers.Internals;
using Scrutor.Static;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Scrutor.Analyzers
{
    static class DataHelpers
    {
        public static void HandleInvocationExpressionSyntax(
            SourceGeneratorContext context,
            CSharpCompilation compilation,
            SemanticModel semanticModel,
            ExpressionSyntax rootExpression,
            List<IAssemblyDescriptor> assemblies,
            List<ITypeFilterDescriptor> typeFilters,
            List<IServiceTypeDescriptor> serviceTypes,
            ref ClassFilter classFilter,
            ref MemberAccessExpressionSyntax lifetimeExpressionSyntax
        )
        {
            if (!(rootExpression is InvocationExpressionSyntax expression))
            {
                if (!(rootExpression is SimpleLambdaExpressionSyntax simpleLambdaExpressionSyntax))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustBeAnExpression, rootExpression.GetLocation()));
                    return;
                }

                if (simpleLambdaExpressionSyntax.ExpressionBody == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustBeAnExpression, rootExpression.GetLocation()));
                    return;
                }

                if (!(simpleLambdaExpressionSyntax.ExpressionBody is InvocationExpressionSyntax body))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustBeAnExpression, rootExpression.GetLocation()));
                    return;
                }

                expression = body;
            }

            if (expression.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax
                && memberAccessExpressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                if (memberAccessExpressionSyntax.Expression is InvocationExpressionSyntax childExpression)
                {
                    HandleInvocationExpressionSyntax(
                        context,
                        compilation,
                        semanticModel,
                        childExpression,
                        assemblies,
                        typeFilters,
                        serviceTypes,
                        ref classFilter,
                        ref lifetimeExpressionSyntax
                    );
                }

                var type = semanticModel.GetTypeInfo(memberAccessExpressionSyntax.Expression);
                if (type.Type != null)
                {
                    var typeName = type.Type.ToDisplayString();
                    if (typeName == "Scrutor.Static.ICompiledAssemblySelector")
                    {
                        var selector = HandleCompiledAssemblySelector(
                            semanticModel,
                            expression,
                            memberAccessExpressionSyntax.Name
                        );
                        if (selector != null)
                        {
                            assemblies.Add(selector);
                        }
                    }

                    if (typeName == "Scrutor.Static.ICompiledImplementationTypeSelector")
                    {
                        var selector = HandleCompiledAssemblySelector(
                            semanticModel,
                            expression,
                            memberAccessExpressionSyntax.Name
                        );
                        if (selector != null)
                        {
                            assemblies.Add(selector);
                        }
                        else
                        {
                            classFilter = HandleCompiledImplementationTypeSelector(expression, memberAccessExpressionSyntax.Name);
                        }
                    }

                    if (typeName == "Scrutor.Static.ICompiledImplementationTypeFilter")
                    {
                        typeFilters.AddRange(HandleCompiledImplementationTypeFilter(context, semanticModel, expression, memberAccessExpressionSyntax.Name));
                    }

                    if (typeName == "Scrutor.Static.ICompiledServiceTypeSelector")
                    {
                        serviceTypes.AddRange(HandleCompiledServiceTypeSelector(context, semanticModel, expression, memberAccessExpressionSyntax.Name));
                    }

                    if (typeName == "Scrutor.Static.ICompiledLifetimeSelector")
                    {
                        serviceTypes.AddRange(HandleCompiledServiceTypeSelector(context, semanticModel, expression, memberAccessExpressionSyntax.Name));
                        lifetimeExpressionSyntax = HandleCompiledLifetimeSelector(context, semanticModel, expression, memberAccessExpressionSyntax.Name) ??
                                                   lifetimeExpressionSyntax;
                    }
                }

                foreach (var argument in expression.ArgumentList.Arguments.Where(argument => argument.Expression is SimpleLambdaExpressionSyntax))
                {
                    HandleInvocationExpressionSyntax(
                        context,
                        compilation,
                        semanticModel,
                        argument.Expression,
                        assemblies,
                        typeFilters,
                        serviceTypes,
                        ref classFilter,
                        ref lifetimeExpressionSyntax
                    );
                }
            }
        }

        static MemberAccessExpressionSyntax? HandleCompiledLifetimeSelector(
            SourceGeneratorContext context,
            SemanticModel semanticModel,
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name.ToFullString() == nameof(ICompiledLifetimeSelector.WithSingletonLifetime))
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("ServiceLifetime"),
                    IdentifierName("Singleton")
                );
            }

            if (name.ToFullString() == nameof(ICompiledLifetimeSelector.WithScopedLifetime))
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("ServiceLifetime"),
                    IdentifierName("Scoped")
                );
            }

            if (name.ToFullString() == nameof(ICompiledLifetimeSelector.WithTransientLifetime))
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("ServiceLifetime"),
                    IdentifierName("Transient")
                );
            }

            if (name.ToFullString() == nameof(ICompiledLifetimeSelector.WithLifetime) && expression.ArgumentList.Arguments.Count == 1)
            {
                if (expression.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                    return memberAccessExpressionSyntax;
            }

            return null;
        }

        static IEnumerable<IServiceTypeDescriptor> HandleCompiledServiceTypeSelector(
            SourceGeneratorContext context,
            SemanticModel semanticModel,
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name.ToFullString() == nameof(ICompiledServiceTypeSelector.AsSelf))
            {
                yield return new SelfServiceTypeDescriptor();
                yield break;
            }

            if (name.ToFullString() == nameof(ICompiledServiceTypeSelector.AsImplementedInterfaces))
            {
                yield return new ImplementedInterfacesServiceTypeDescriptor();
                yield break;
            }

            if (name.ToFullString() == nameof(ICompiledServiceTypeSelector.AsMatchingInterface))
            {
                yield return new MatchingInterfaceServiceTypeDescriptor();
                yield break;
            }

            if (name.ToFullString() == nameof(ICompiledServiceTypeSelector.UsingAttributes))
            {
                yield return new UsingAttributeServiceTypeDescriptor();
                yield break;
            }

            if (name.ToFullString() == nameof(ICompiledServiceTypeSelector.AsSelfWithInterfaces))
            {
                yield return new SelfServiceTypeDescriptor();
                yield return new ImplementedInterfacesServiceTypeDescriptor();
                yield break;
            }

            if (name is GenericNameSyntax genericNameSyntax && genericNameSyntax.TypeArgumentList.Arguments.Count == 1)
            {
                var typeSyntax = Helpers.ExtractSyntaxFromMethod(expression, name);
                if (typeSyntax == null)
                {
                    yield break;
                }

                var typeInfo = semanticModel.GetTypeInfo(typeSyntax).Type;
                switch (typeInfo)
                {
                    case INamedTypeSymbol nts:
                        yield return new CompiledServiceTypeDescriptor(nts);
                        yield break;
                    default:
                        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnhandledSymbol, name.GetLocation()));
                        yield break;
                }
            }
        }

        static IAssemblyDescriptor? HandleCompiledAssemblySelector(
            SemanticModel semanticModel,
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name.ToFullString() == nameof(ICompiledAssemblySelector.FromAssembly))
                return new AssemblyDescriptor(semanticModel.Compilation.Assembly);
            if (name.ToFullString() == nameof(ICompiledAssemblySelector.FromAssemblies))
                return new AllAssemblyDescriptor();

            var typeSyntax = Helpers.ExtractSyntaxFromMethod(expression, name);
            if (typeSyntax == null)
                return null;

            var typeInfo = semanticModel.GetTypeInfo(typeSyntax).Type;
            if (typeInfo == null)
                return null;
            return typeInfo switch
            {
                INamedTypeSymbol nts when name.ToFullString().StartsWith(nameof(ICompiledAssemblySelector.FromAssemblyDependenciesOf)) =>
                    new CompiledAssemblyDependenciesDescriptor(nts),
                INamedTypeSymbol nts when name.ToFullString().StartsWith(nameof(ICompiledAssemblySelector.FromAssemblyOf)) =>
                    new CompiledAssemblyDescriptor(nts),
                _ => null
            };
        }

        static ClassFilter HandleCompiledImplementationTypeSelector(
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name.ToFullString() == nameof(ICompiledImplementationTypeSelector.AddClasses) && expression.ArgumentList.Arguments.Count is >= 0 and <= 2)
            {
                foreach (var argument in expression.ArgumentList.Arguments)
                {
                    if (argument.Expression is LiteralExpressionSyntax literalExpressionSyntax && literalExpressionSyntax.Token.IsKind(SyntaxKind.TrueKeyword))
                    {
                        return ClassFilter.PublicOnly;
                    }
                }
            }

            return ClassFilter.All;
        }

        static IEnumerable<ITypeFilterDescriptor> HandleCompiledImplementationTypeFilter(
            SourceGeneratorContext context,
            SemanticModel semanticModel,
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name.ToFullString() == nameof(ICompiledImplementationTypeFilter.AssignableToAny))
            {
                foreach (var argument in expression.ArgumentList.Arguments!)
                {
                    if (argument.Expression is TypeOfExpressionSyntax typeOfExpressionSyntax)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(typeOfExpressionSyntax.Type).Type;
                        switch (typeInfo)
                        {
                            case INamedTypeSymbol nts:
                                yield return new CompiledAssignableToAnyTypeFilterDescriptor(nts);
                                continue;

                            default:
                                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnhandledSymbol, name.GetLocation()));
                                yield break;
                        }
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustBeTypeOf, argument.Expression.GetLocation()));
                    yield return new CompiledAbortTypeFilterDescriptor(context.Compilation.ObjectType);
                    yield break;
                }

                yield break;
            }

            if (name is GenericNameSyntax genericNameSyntax && genericNameSyntax.TypeArgumentList!.Arguments.Count == 1)
            {
                if (genericNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.AssignableTo))
                {
                    var type = Helpers.ExtractSyntaxFromMethod(expression, name);
                    if (type != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(type).Type;
                        switch (typeInfo)
                        {
                            case INamedTypeSymbol nts:
                                yield return new CompiledAssignableToTypeFilterDescriptor(nts);
                                yield break;

                            default:
                                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnhandledSymbol, name.GetLocation()));
                                yield break;
                        }
                    }

                    yield break;
                }

                if (genericNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.WithAttribute))
                {
                    var type = Helpers.ExtractSyntaxFromMethod(expression, name);
                    if (type != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(type).Type;
                        switch (typeInfo)
                        {
                            case INamedTypeSymbol nts:
                                yield return new CompiledWithAttributeFilterDescriptor(nts);
                                yield break;

                            default:
                                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnhandledSymbol, name.GetLocation()));
                                yield break;
                        }
                    }

                    yield break;
                }

                if (genericNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.WithoutAttribute))
                {
                    var type = Helpers.ExtractSyntaxFromMethod(expression, name);
                    if (type != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(type).Type;
                        switch (typeInfo)
                        {
                            case INamedTypeSymbol nts:
                                yield return new CompiledWithoutAttributeFilterDescriptor(nts);
                                yield break;

                            default:
                                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnhandledSymbol, name.GetLocation()));
                                yield break;
                        }
                    }

                    yield break;
                }

                NamespaceFilter? filter = null;
                if (genericNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.InExactNamespaceOf))
                {
                    filter = NamespaceFilter.Exact;
                }

                if (genericNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.InNamespaceOf))
                {
                    filter = NamespaceFilter.In;
                }

                if (genericNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.NotInNamespaceOf))
                {
                    filter = NamespaceFilter.NotIn;
                }

                if (filter.HasValue)
                {
                    var symbol = semanticModel.GetTypeInfo(genericNameSyntax.TypeArgumentList.Arguments![0]).Type!;
                    yield return new NamespaceFilterDescriptor(filter.Value, new [] { symbol.ContainingNamespace.ToDisplayString() });
                }

                yield break;
            }

            if (name is SimpleNameSyntax simpleNameSyntax)
            {
                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.AssignableTo))
                {
                    var type = Helpers.ExtractSyntaxFromMethod(expression, name);
                    if (type != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(type).Type;
                        switch (typeInfo)
                        {
                            case INamedTypeSymbol nts:
                                yield return new CompiledAssignableToTypeFilterDescriptor(nts);
                                yield break;
                            default:
                                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnhandledSymbol, name.GetLocation()));
                                yield break;
                        }
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustBeTypeOf, simpleNameSyntax.Identifier.GetLocation()));
                    yield return new CompiledAbortTypeFilterDescriptor(context.Compilation.ObjectType);
                    yield break;
                }

                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.WithAttribute))
                {
                    var type = Helpers.ExtractSyntaxFromMethod(expression, name);
                    if (type != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(type).Type;
                        switch (typeInfo)
                        {
                            case INamedTypeSymbol nts:
                                yield return new CompiledWithAttributeFilterDescriptor(nts);
                                yield break;

                            default:
                                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnhandledSymbol, name.GetLocation()));
                                yield break;
                        }
                    }

                    yield break;
                }

                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.WithoutAttribute))
                {
                    var type = Helpers.ExtractSyntaxFromMethod(expression, name);
                    if (type != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(type).Type;
                        switch (typeInfo)
                        {
                            case INamedTypeSymbol nts:
                                yield return new CompiledWithoutAttributeFilterDescriptor(nts);
                                yield break;

                            default:
                                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnhandledSymbol, name.GetLocation()));
                                yield break;
                        }
                    }

                    yield break;
                }

                NamespaceFilter? filter = null;
                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.InExactNamespaces) ||
                    simpleNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.InExactNamespaceOf))
                {
                    filter = NamespaceFilter.Exact;
                }

                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.InNamespaces) ||
                    simpleNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.InNamespaceOf))
                {
                    filter = NamespaceFilter.In;
                }

                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.NotInNamespaces) ||
                    simpleNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.NotInNamespaceOf))
                {
                    filter = NamespaceFilter.NotIn;
                }

                if (filter.HasValue)
                {
                    var namespaces = expression.ArgumentList.Arguments
                        .Select(argument =>
                        {
                            switch (argument.Expression)
                            {
                                case LiteralExpressionSyntax literalExpressionSyntax when literalExpressionSyntax.Token.IsKind(SyntaxKind.StringLiteralToken):
                                    return literalExpressionSyntax.Token.ValueText!;
                                case TypeOfExpressionSyntax typeOfExpressionSyntax:
                                {
                                    var symbol = semanticModel.GetTypeInfo(typeOfExpressionSyntax.Type).Type!;
                                    return symbol.ContainingNamespace.ToDisplayString()!;
                                }
                                default:
                                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NamespaceMustBeAString, argument.GetLocation()));
                                    return null!;
                            }
                        })
                        .Where(z => !string.IsNullOrWhiteSpace(z))
                        .ToArray();

                    yield return new NamespaceFilterDescriptor(filter.Value, namespaces);
                }
            }
        }
    }
}
