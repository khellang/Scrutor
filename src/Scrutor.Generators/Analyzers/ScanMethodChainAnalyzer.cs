using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scrutor.Generators.Models;
using Scrutor.Generators.Utilities;

namespace Scrutor.Generators.Analyzers;

/// <summary>
/// Analyzes Scan() method chains to extract registration configurations.
/// </summary>
internal static class ScanMethodChainAnalyzer
{
    /// <summary>
    /// Checks if a syntax node is potentially a Scan() invocation.
    /// </summary>
    public static bool IsScanInvocation(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        var methodName = GetMethodName(invocation);
        return methodName == "Scan";
    }

    /// <summary>
    /// Analyzes a Scan() invocation and returns interceptor info.
    /// </summary>
    public static ScanInterceptorInfo? AnalyzeForInterceptor(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.Node is not InvocationExpressionSyntax scanInvocation)
        {
            return null;
        }

        var semanticModel = context.SemanticModel;

        // Verify this is actually IServiceCollection.Scan
        var symbolInfo = semanticModel.GetSymbolInfo(scanInvocation, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var containingType = methodSymbol.ContainingType;
        if (containingType?.GetFullyQualifiedNameWithoutGlobal() != "Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions")
        {
            return null;
        }

        // Get the location of the "Scan" method name for interception
        var scanLocation = GetScanMethodLocation(scanInvocation);
        if (scanLocation == null)
        {
            return null;
        }

        var lineSpan = scanLocation.GetLineSpan();
        var filePath = lineSpan.Path;
        var line = lineSpan.StartLinePosition.Line + 1;
        var column = lineSpan.StartLinePosition.Character + 1;

        var info = new ScanInterceptorInfo
        {
            FilePath = filePath,
            Line = line,
            Column = column,
            Location = scanInvocation.GetLocation()
        };

        if (scanInvocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        var scanArg = scanInvocation.ArgumentList.Arguments[0].Expression;

        if (scanArg is SimpleLambdaExpressionSyntax lambda)
        {
            AnalyzeLambdaBody(lambda.Body, info, semanticModel, cancellationToken);
            info.CallChainText = ExtractCallChainText(lambda.Body);
        }
        else if (scanArg is ParenthesizedLambdaExpressionSyntax parenLambda)
        {
            AnalyzeLambdaBody(parenLambda.Body, info, semanticModel, cancellationToken);
            info.CallChainText = ExtractCallChainText(parenLambda.Body);
        }
        else
        {
            info.HasUnsupportedPatterns = true;
            info.UnsupportedPatternDescription = "Method group or variable reference in Scan() configuration";
        }

        return info;
    }

    private static Location? GetScanMethodLocation(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.GetLocation();
        }
        return null;
    }

    private static void AnalyzeLambdaBody(
        SyntaxNode? body,
        ScanInterceptorInfo info,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (body == null) return;

        var config = info.Configuration;

        if (body is InvocationExpressionSyntax chainRoot)
        {
            AnalyzeMethodChain(chainRoot, config, semanticModel, cancellationToken);
        }
        else if (body is BlockSyntax block)
        {
            foreach (var statement in block.Statements)
            {
                if (statement is ExpressionStatementSyntax exprStmt &&
                    exprStmt.Expression is InvocationExpressionSyntax invocation)
                {
                    AnalyzeMethodChain(invocation, config, semanticModel, cancellationToken);
                }
            }
        }

        info.UsesAttributes = config.UsesAttributes;
        info.HasUnsupportedPatterns = config.HasUnsupportedPatterns;
        info.UnsupportedPatternDescription = config.UnsupportedPatternDescription;
    }

    private static void AnalyzeMethodChain(
        InvocationExpressionSyntax invocation,
        ScanConfiguration config,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var calls = new List<InvocationExpressionSyntax>();
        CollectChainedCalls(invocation, calls);

        foreach (var call in calls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AnalyzeChainedMethod(call, config, semanticModel, cancellationToken);
        }
    }

    private static void CollectChainedCalls(InvocationExpressionSyntax invocation, List<InvocationExpressionSyntax> calls)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is InvocationExpressionSyntax innerInvocation)
        {
            CollectChainedCalls(innerInvocation, calls);
        }
        calls.Add(invocation);
    }

    private static void AnalyzeChainedMethod(
        InvocationExpressionSyntax invocation,
        ScanConfiguration config,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var methodName = GetMethodName(invocation);
        if (methodName == null) return;

        switch (methodName)
        {
            case "FromAssemblyOf":
                AnalyzeFromAssemblyOf(invocation, config, semanticModel);
                break;

            case "FromCallingAssembly":
            case "FromExecutingAssembly":
            case "FromEntryAssembly":
            case "FromApplicationDependencies":
            case "FromAssemblies":
            case "FromAssembliesOf":
            case "FromDependencyContext":
                config.HasUnsupportedPatterns = true;
                config.UnsupportedPatternDescription = $"{methodName}() requires runtime assembly discovery";
                break;

            case "AddClasses":
                AnalyzeAddClasses(invocation, config, semanticModel, cancellationToken);
                break;

            case "AsImplementedInterfaces":
                config.ServiceTypeMappings.Add(new ServiceTypeMapping
                {
                    Kind = ServiceTypeMappingKind.AsImplementedInterfaces
                });
                break;

            case "AsSelf":
                config.ServiceTypeMappings.Add(new ServiceTypeMapping
                {
                    Kind = ServiceTypeMappingKind.AsSelf
                });
                break;

            case "AsSelfWithInterfaces":
                config.ServiceTypeMappings.Add(new ServiceTypeMapping
                {
                    Kind = ServiceTypeMappingKind.AsSelfWithInterfaces
                });
                break;

            case "AsMatchingInterface":
                config.ServiceTypeMappings.Add(new ServiceTypeMapping
                {
                    Kind = ServiceTypeMappingKind.AsMatchingInterface
                });
                break;

            case "As":
                AnalyzeAsMethod(invocation, config, semanticModel);
                break;

            case "WithSingletonLifetime":
                config.Lifetime = ServiceLifetime.Singleton;
                break;

            case "WithScopedLifetime":
                config.Lifetime = ServiceLifetime.Scoped;
                break;

            case "WithTransientLifetime":
                config.Lifetime = ServiceLifetime.Transient;
                break;

            case "WithLifetime":
                AnalyzeWithLifetime(invocation, config, semanticModel);
                break;

            case "WithServiceKey":
                AnalyzeWithServiceKey(invocation, config, semanticModel);
                break;

            case "UsingRegistrationStrategy":
                AnalyzeRegistrationStrategy(invocation, config, semanticModel);
                break;

            case "UsingAttributes":
                config.UsesAttributes = true;
                break;
        }
    }

    private static void AnalyzeFromAssemblyOf(
        InvocationExpressionSyntax invocation,
        ScanConfiguration config,
        SemanticModel semanticModel)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName)
        {
            foreach (var typeArg in genericName.TypeArgumentList.Arguments)
            {
                var typeInfo = semanticModel.GetTypeInfo(typeArg);
                if (typeInfo.Type != null)
                {
                    config.AssemblySourceTypes.Add(typeInfo.Type.GetFullyQualifiedName());
                }
            }
        }
    }

    private static void AnalyzeAddClasses(
        InvocationExpressionSyntax invocation,
        ScanConfiguration config,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var filter = new TypeFilter { Kind = TypeFilterKind.All };

        if (invocation.ArgumentList.Arguments.Count > 0)
        {
            var arg = invocation.ArgumentList.Arguments[0].Expression;

            if (arg is SimpleLambdaExpressionSyntax lambda)
            {
                AnalyzeTypeFilterLambda(lambda.Body, filter, config, semanticModel, cancellationToken);
            }
            else if (arg is ParenthesizedLambdaExpressionSyntax parenLambda)
            {
                AnalyzeTypeFilterLambda(parenLambda.Body, filter, config, semanticModel, cancellationToken);
            }
            else if (arg is LiteralExpressionSyntax literal && literal.Kind() == SyntaxKind.TrueLiteralExpression)
            {
                // publicOnly = true (default)
            }
            else if (arg is LiteralExpressionSyntax literal2 && literal2.Kind() == SyntaxKind.FalseLiteralExpression)
            {
                config.IncludeNonPublic = true;
            }
        }

        // Check for publicOnly parameter (second argument)
        if (invocation.ArgumentList.Arguments.Count > 1)
        {
            var publicOnlyArg = invocation.ArgumentList.Arguments[1].Expression;
            if (publicOnlyArg is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.FalseLiteralExpression })
            {
                config.IncludeNonPublic = true;
            }
        }

        config.TypeFilters.Add(filter);
    }

    private static void AnalyzeTypeFilterLambda(
        SyntaxNode? body,
        TypeFilter filter,
        ScanConfiguration config,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (body == null) return;

        if (body is InvocationExpressionSyntax invocation)
        {
            AnalyzeFilterMethodChain(invocation, filter, config, semanticModel, cancellationToken);
        }
    }

    private static void AnalyzeFilterMethodChain(
        InvocationExpressionSyntax invocation,
        TypeFilter filter,
        ScanConfiguration config,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var calls = new List<InvocationExpressionSyntax>();
        CollectChainedCalls(invocation, calls);

        foreach (var call in calls)
        {
            var methodName = GetMethodName(call);
            if (methodName == null) continue;

            switch (methodName)
            {
                case "AssignableTo":
                    // Handle generic version: AssignableTo<T>()
                    if (call.Expression is MemberAccessExpressionSyntax ma &&
                        ma.Name is GenericNameSyntax gn &&
                        gn.TypeArgumentList.Arguments.Count > 0)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(gn.TypeArgumentList.Arguments[0]);
                        if (typeInfo.Type != null)
                        {
                            filter.Kind = TypeFilterKind.AssignableTo;
                            filter.TypeFullName = typeInfo.Type.GetFullyQualifiedName();
                        }
                    }
                    // Handle non-generic version: AssignableTo(typeof(T)) for open generics
                    else if (call.ArgumentList.Arguments.Count > 0 &&
                             call.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOf)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(typeOf.Type);
                        if (typeInfo.Type != null)
                        {
                            filter.Kind = TypeFilterKind.AssignableTo;
                            filter.TypeFullName = typeInfo.Type.GetFullyQualifiedName();
                        }
                    }
                    break;

                case "AssignableToAny":
                    config.HasUnsupportedPatterns = true;
                    config.UnsupportedPatternDescription = "AssignableToAny() with multiple types";
                    break;

                case "InNamespace":
                case "InNamespaces":
                    filter.Kind = TypeFilterKind.InNamespace;
                    if (call.ArgumentList.Arguments.Count > 0)
                    {
                        var arg = call.ArgumentList.Arguments[0].Expression;
                        if (arg is LiteralExpressionSyntax literal)
                        {
                            filter.Namespace = literal.Token.ValueText;
                        }
                    }
                    break;

                case "InExactNamespace":
                case "InExactNamespaces":
                    filter.Kind = TypeFilterKind.InExactNamespace;
                    if (call.ArgumentList.Arguments.Count > 0)
                    {
                        var arg = call.ArgumentList.Arguments[0].Expression;
                        if (arg is LiteralExpressionSyntax literal)
                        {
                            filter.Namespace = literal.Token.ValueText;
                        }
                    }
                    break;

                case "WithAttribute":
                    if (call.Expression is MemberAccessExpressionSyntax ma2 &&
                        ma2.Name is GenericNameSyntax gn2 &&
                        gn2.TypeArgumentList.Arguments.Count > 0)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(gn2.TypeArgumentList.Arguments[0]);
                        if (typeInfo.Type != null)
                        {
                            filter.Kind = TypeFilterKind.WithAttribute;
                            filter.TypeFullName = typeInfo.Type.GetFullyQualifiedName();
                        }
                    }
                    break;

                case "WithoutAttribute":
                    if (call.Expression is MemberAccessExpressionSyntax ma3 &&
                        ma3.Name is GenericNameSyntax gn3 &&
                        gn3.TypeArgumentList.Arguments.Count > 0)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(gn3.TypeArgumentList.Arguments[0]);
                        if (typeInfo.Type != null)
                        {
                            filter.Kind = TypeFilterKind.WithoutAttribute;
                            filter.TypeFullName = typeInfo.Type.GetFullyQualifiedName();
                        }
                    }
                    break;

                case "Where":
                    config.HasUnsupportedPatterns = true;
                    config.UnsupportedPatternDescription = "Custom Where() predicate in type filter";
                    break;
            }
        }
    }

    private static void AnalyzeAsMethod(
        InvocationExpressionSyntax invocation,
        ScanConfiguration config,
        SemanticModel semanticModel)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName &&
            genericName.TypeArgumentList.Arguments.Count > 0)
        {
            var typeInfo = semanticModel.GetTypeInfo(genericName.TypeArgumentList.Arguments[0]);
            if (typeInfo.Type != null)
            {
                config.ServiceTypeMappings.Add(new ServiceTypeMapping
                {
                    Kind = ServiceTypeMappingKind.AsExplicitType,
                    ExplicitTypeFullName = typeInfo.Type.GetFullyQualifiedName()
                });
            }
        }
        else if (invocation.ArgumentList.Arguments.Count > 0)
        {
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                if (arg.Expression is TypeOfExpressionSyntax typeOf)
                {
                    var typeInfo = semanticModel.GetTypeInfo(typeOf.Type);
                    if (typeInfo.Type != null)
                    {
                        config.ServiceTypeMappings.Add(new ServiceTypeMapping
                        {
                            Kind = ServiceTypeMappingKind.AsExplicitType,
                            ExplicitTypeFullName = typeInfo.Type.GetFullyQualifiedName()
                        });
                    }
                }
                else
                {
                    config.HasUnsupportedPatterns = true;
                    config.UnsupportedPatternDescription = "Dynamic type selector in As()";
                }
            }
        }
    }

    private static void AnalyzeWithLifetime(
        InvocationExpressionSyntax invocation,
        ScanConfiguration config,
        SemanticModel semanticModel)
    {
        if (invocation.ArgumentList.Arguments.Count > 0)
        {
            var arg = invocation.ArgumentList.Arguments[0].Expression;

            if (arg is MemberAccessExpressionSyntax memberAccess)
            {
                var name = memberAccess.Name.Identifier.Text;
                config.Lifetime = name switch
                {
                    "Singleton" => ServiceLifetime.Singleton,
                    "Scoped" => ServiceLifetime.Scoped,
                    "Transient" => ServiceLifetime.Transient,
                    _ => ServiceLifetime.Transient
                };
            }
            else
            {
                config.HasUnsupportedPatterns = true;
                config.UnsupportedPatternDescription = "Dynamic lifetime selector";
            }
        }
    }

    private static void AnalyzeWithServiceKey(
        InvocationExpressionSyntax invocation,
        ScanConfiguration config,
        SemanticModel semanticModel)
    {
        if (invocation.ArgumentList.Arguments.Count > 0)
        {
            var arg = invocation.ArgumentList.Arguments[0].Expression;

            if (arg is LiteralExpressionSyntax literal)
            {
                config.ServiceKey = literal.Token.ValueText;
            }
            else
            {
                var constantValue = semanticModel.GetConstantValue(arg);
                if (constantValue.HasValue)
                {
                    config.ServiceKey = constantValue.Value?.ToString();
                }
                else
                {
                    config.HasUnsupportedPatterns = true;
                    config.UnsupportedPatternDescription = "Dynamic service key selector";
                }
            }
        }
    }

    private static void AnalyzeRegistrationStrategy(
        InvocationExpressionSyntax invocation,
        ScanConfiguration config,
        SemanticModel semanticModel)
    {
        if (invocation.ArgumentList.Arguments.Count > 0)
        {
            var arg = invocation.ArgumentList.Arguments[0].Expression;

            if (arg is MemberAccessExpressionSyntax memberAccess)
            {
                var name = memberAccess.Name.Identifier.Text;
                config.Strategy = name switch
                {
                    "Skip" => RegistrationStrategyKind.Skip,
                    "Append" => RegistrationStrategyKind.Append,
                    "Throw" => RegistrationStrategyKind.Skip,
                    _ => RegistrationStrategyKind.Append
                };
            }
            else if (arg is InvocationExpressionSyntax strategyInvocation)
            {
                var strategyMethodName = GetMethodName(strategyInvocation);
                if (strategyMethodName == "Replace")
                {
                    config.Strategy = RegistrationStrategyKind.Replace;
                }
            }
        }
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => null
        };
    }

    private static string? ExtractCallChainText(SyntaxNode? body)
    {
        if (body == null) return null;

        // Get the source text and normalize it
        var text = body.ToFullString().Trim();

        // Normalize whitespace: replace newlines and multiple spaces with single space
        var normalized = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        // Limit length for readability
        if (normalized.Length > 200)
        {
            normalized = normalized.Substring(0, 197) + "...";
        }

        return normalized;
    }
}
