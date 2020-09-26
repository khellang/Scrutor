using System;
using System.Collections.Generic;
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
                    // report diagnostic
                    return;
                }

                if (simpleLambdaExpressionSyntax.ExpressionBody == null)
                {
                    // we don't support blocks
                    // report diagnostic
                    return;
                }

                if (!(simpleLambdaExpressionSyntax.ExpressionBody is InvocationExpressionSyntax body))
                {
                    // we only support invocation expressions
                    // report diagnostic
                    return;
                }

                expression = body;
            }

            rootExpression = expression;

            if (expression.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax
             && memberAccessExpressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                if (memberAccessExpressionSyntax.Expression is InvocationExpressionSyntax childExpression)
                {
                    HandleInvocationExpressionSyntax(
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
                        typeFilters.AddRange(HandleCompiledImplementationTypeFilter(semanticModel, expression, memberAccessExpressionSyntax.Name));
                    }

                    if (typeName == "Scrutor.Static.ICompiledServiceTypeSelector")
                    {
                        serviceTypes.AddRange(HandleCompiledServiceTypeSelector(semanticModel, expression, memberAccessExpressionSyntax.Name));
                    }

                    if (typeName == "Scrutor.Static.ICompiledLifetimeSelector")
                    {
                        serviceTypes.AddRange(HandleCompiledServiceTypeSelector(semanticModel, expression, memberAccessExpressionSyntax.Name));
                        lifetimeExpressionSyntax = HandleCompiledLifetimeSelector(semanticModel, expression, memberAccessExpressionSyntax.Name) ?? lifetimeExpressionSyntax;
                    }
                }

                foreach (var argument in expression.ArgumentList.Arguments)
                {
                    HandleInvocationExpressionSyntax(
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
            SemanticModel semanticModel,
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name.ToFullString() == nameof(ICompiledLifetimeSelector.WithSingletonLifetime))
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("ServiceLifetime"),
                    SyntaxFactory.IdentifierName("Singleton")
                );
            }

            if (name.ToFullString() == nameof(ICompiledLifetimeSelector.WithScopedLifetime))
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("ServiceLifetime"),
                    SyntaxFactory.IdentifierName("Scoped")
                );
            }

            if (name.ToFullString() == nameof(ICompiledLifetimeSelector.WithTransientLifetime))
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("ServiceLifetime"),
                    SyntaxFactory.IdentifierName("Transient")
                );
            }

            if (name.ToFullString() == nameof(ICompiledLifetimeSelector.WithLifetime) && expression.ArgumentList.Arguments.Count == 1)
            {
                if (expression.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                    return memberAccessExpressionSyntax;
                throw new NotSupportedException();
            }

            return null;
        }

        static IEnumerable<IServiceTypeDescriptor> HandleCompiledServiceTypeSelector(
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

            if (name.ToFullString() == nameof(ICompiledServiceTypeSelector.AsSelfWithInterfaces))
            {
                yield return new SelfServiceTypeDescriptor();
                yield return new ImplementedInterfacesServiceTypeDescriptor();
                yield break;
            }

            if (name is GenericNameSyntax genericNameSyntax && genericNameSyntax.TypeArgumentList.Arguments.Count == 1)
            {
                var typeSyntax = ExtractSyntaxFromMethod(expression, name);
                if (typeSyntax == null)
                {
                    yield break;
                }

                var typeInfo = semanticModel.GetTypeInfo(typeSyntax).Type;
                switch (typeInfo)
                {
                    case null:
                        yield break;
                    case INamedTypeSymbol nts:
                        yield return new CompiledServiceTypeDescriptor(nts);
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

            var typeSyntax = ExtractSyntaxFromMethod(expression, name);
            if (typeSyntax == null)
                return null;

            var typeInfo = semanticModel.GetTypeInfo(typeSyntax).Type;
            if (typeInfo == null)
                return null;
            return typeInfo switch
            {
                INamedTypeSymbol nts when name.ToFullString() == nameof(ICompiledAssemblySelector.FromAssemblyDependenciesOf) => new CompiledAssemblyDependenciesDescriptor(
                    nts
                ),
                INamedTypeSymbol nts when name.ToFullString() == nameof(ICompiledAssemblySelector.FromAssemblyOf) => new CompiledAssemblyDescriptor(nts),
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
            SemanticModel semanticModel,
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name.ToFullString() == nameof(ICompiledImplementationTypeFilter.AssignableToAny))
            {
                // diagnostic if not using typeof
                foreach (var argument in expression.ArgumentList.Arguments!)
                {
                    if (argument.Expression is TypeOfExpressionSyntax typeOfExpressionSyntax)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(typeOfExpressionSyntax.Type).Type;
                        switch (typeInfo)
                        {
                            case null:
                                yield break;
                            case INamedTypeSymbol nts:
                                yield return new CompiledAssignableToAnyTypeFilterDescriptor(nts);
                                continue;
                        }
                    }

                    // todo diagnostic
                }

                yield break;
            }

            if (name is GenericNameSyntax genericNameSyntax && genericNameSyntax.TypeArgumentList!.Arguments.Count == 1)
            {
                if (genericNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.AssignableTo))
                {
                    var type = ExtractSyntaxFromMethod(expression, name);
                    if (type != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(type).Type;
                        switch (typeInfo)
                        {
                            case null:
                                yield break;
                            case INamedTypeSymbol nts:
                                yield return new CompiledAssignableToTypeFilterDescriptor(nts);
                                yield break;
                        }
                    }

                    // todo diagnostic
                    yield break;
                }

                NamespaceFilter? filter = null;
                if (genericNameSyntax.Identifier.ToFullString() == nameof(ICompiledImplementationTypeFilter.InExactNamespaces))
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
                    var symbol = semanticModel.GetDeclaredSymbol(genericNameSyntax.TypeArgumentList.Arguments![0]);
                    yield return new NamespaceFilterDescriptor(filter.Value, symbol.ContainingNamespace.ToDisplayString());
                }

                // todo diagnostic
                yield break;
            }

            if (name is SimpleNameSyntax simpleNameSyntax)
            {
                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.AssignableTo))
                {
                    var type = ExtractSyntaxFromMethod(expression, name);
                    if (type != null)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(type).Type;
                        switch (typeInfo)
                        {
                            case null:
                                yield break;
                            case INamedTypeSymbol nts:
                                yield return new CompiledAssignableToTypeFilterDescriptor(nts);
                                yield break;
                        }
                    }

                    // todo diagnostic
                    yield break;
                }

                NamespaceFilter? filter = null;
                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.InExactNamespaces))
                {
                    filter = NamespaceFilter.Exact;
                }

                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.InNamespaces))
                {
                    filter = NamespaceFilter.In;
                }

                if (simpleNameSyntax.ToFullString() == nameof(ICompiledImplementationTypeFilter.NotInNamespaces))
                {
                    filter = NamespaceFilter.NotIn;
                }

                if (filter.HasValue)
                {
                    foreach (var argument in expression.ArgumentList.Arguments!)
                    {
                        if (argument.Expression is LiteralExpressionSyntax literalExpressionSyntax
                         && literalExpressionSyntax.Token.IsKind(SyntaxKind.StringLiteralExpression))
                        {
                            yield return new NamespaceFilterDescriptor(filter.Value, literalExpressionSyntax.Token.ValueText);
                        }
                        else
                        {
                            // todo diagnostic
                        }
                    }

                    yield break;
                }
            }
        }

        static TypeSyntax? ExtractSyntaxFromMethod(
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name is GenericNameSyntax genericNameSyntax)
            {
                if (genericNameSyntax.TypeArgumentList.Arguments.Count == 1)
                {
                    return genericNameSyntax.TypeArgumentList.Arguments[0];
                }
            }

            if (name is SimpleNameSyntax)
            {
                if (expression.ArgumentList.Arguments.Count == 1 && expression.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOfExpression)
                {
                    return typeOfExpression.Type;
                }
            }

            // we only support typeof or closed generic arguments
            // report diagnostic

            return null;
        }
    }
}
