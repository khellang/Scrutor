using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Scrutor.Analyzers
{
    static class StatementGeneration
    {
        private static MemberAccessExpressionSyntax Describe = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName("ServiceDescriptor"),
            IdentifierName("Describe")
        );

        public static InvocationExpressionSyntax GenerateServiceFactory(
            CSharpCompilation compilation,
            NameSyntax strategyName,
            NameSyntax serviceCollectionName,
            INamedTypeSymbol serviceType,
            INamedTypeSymbol implementationType,
            ExpressionSyntax lifetime
        )
        {
            var serviceTypeExpression = GetTypeOfExpression(compilation, serviceType, implementationType);
            var isAccessible = compilation.IsSymbolAccessibleWithin(implementationType, compilation.Assembly);

            if (isAccessible)
            {
                var implementationTypeExpression = SimpleLambdaExpression(Parameter(Identifier("_")))
                    .WithExpressionBody(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("_"), GenericName("GetRequiredService")
                                .WithTypeArgumentList(
                                    TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(Helpers.GetGenericDisplayName(implementationType))))
                                )
                            )
                        )
                    );

                return GenerateServiceType(strategyName, serviceCollectionName, serviceTypeExpression, implementationTypeExpression, lifetime);
            }
            else
            {
                var implementationTypeExpression = SimpleLambdaExpression(Parameter(Identifier("_")))
                    .WithExpressionBody(
                        BinaryExpression(
                            SyntaxKind.AsExpression,
                            InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("_"), IdentifierName("GetRequiredService"))
                                )
                                .WithArgumentList(
                                    ArgumentList(SingletonSeparatedList(Argument(GetTypeOfExpression(compilation, implementationType, serviceType))))
                                ),
                            IdentifierName(Helpers.GetGenericDisplayName(serviceType))
                        )
                    );
                return GenerateServiceType(strategyName, serviceCollectionName, serviceTypeExpression, implementationTypeExpression, lifetime);
            }
        }

        public static InvocationExpressionSyntax GenerateServiceType(
            CSharpCompilation compilation,
            NameSyntax strategyName,
            NameSyntax serviceCollectionName,
            INamedTypeSymbol serviceType,
            INamedTypeSymbol implementationType,
            ExpressionSyntax lifetime
        )
        {
            var serviceTypeExpression = GetTypeOfExpression(compilation, serviceType, implementationType);
            var implementationTypeExpression = GetTypeOfExpression(compilation, implementationType, serviceType);
            return GenerateServiceType(strategyName, serviceCollectionName, serviceTypeExpression, implementationTypeExpression, lifetime);
        }

        private static InvocationExpressionSyntax GenerateServiceType(
            NameSyntax strategyName,
            NameSyntax serviceCollectionName,
            ExpressionSyntax serviceTypeExpression,
            ExpressionSyntax implementationTypeExpression,
            ExpressionSyntax lifetime
        ) => InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    strategyName,
                    IdentifierName("Apply")
                )
            )
            .WithArgumentList(
                ArgumentList(
                    SeparatedList(
                        new[]
                        {
                            Argument(serviceCollectionName),
                            Argument(
                                InvocationExpression(Describe)
                                    .WithArgumentList(
                                        ArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                    Argument(serviceTypeExpression),
                                                    Argument(implementationTypeExpression),
                                                    Argument(lifetime)
                                                }
                                            )
                                        )
                                    )
                            )
                        }
                    )
                )
            );

        public static IEnumerable<MemberDeclarationSyntax> AssemblyDeclaration(IAssemblySymbol symbol)
        {
            var name = Helpers.AssemblyVariableName(symbol);
            var assemblyName = LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(symbol.Identity.GetDisplayName(true)));

            yield return FieldDeclaration(VariableDeclaration(IdentifierName("AssemblyName"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier($"_{name}"))
                    ))
                )
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)));
            yield return PropertyDeclaration(IdentifierName("AssemblyName"), Identifier(name))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        AssignmentExpression(
                            SyntaxKind.CoalesceAssignmentExpression,
                            IdentifierName(Identifier($"_{name}")),
                            ObjectCreationExpression(IdentifierName("AssemblyName"))
                                .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(assemblyName)))
                                )
                        )
                    )
                )
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        public static bool RemoveImplicitGenericConversion(
            CSharpCompilation compilation,
            INamedTypeSymbol assignableToType,
            INamedTypeSymbol type
        )
        {
            if (SymbolEqualityComparer.Default.Equals(assignableToType, type)) return true;
            if (assignableToType.Arity > 0 && assignableToType.IsUnboundGenericType)
            {
                var matchingBaseTypes = Helpers.GetBaseTypes(compilation, type)
                    .Select(z => z.IsGenericType ? z.IsUnboundGenericType ? z : z.ConstructUnboundGenericType() : null!)
                    .Where(z => z is not null)
                    .Where(symbol => compilation.HasImplicitConversion(symbol, assignableToType));
                if (matchingBaseTypes.Any())
                {
                    return false;
                }

                var matchingInterfaces = type.AllInterfaces
                    .Select(z => z.IsGenericType ? z.IsUnboundGenericType ? z : z.ConstructUnboundGenericType() : null!)
                    .Where(z => z is not null)
                    .Where(symbol => compilation.HasImplicitConversion(symbol, assignableToType));
                if (matchingInterfaces.Any())
                {
                    return false;
                }

                return true;
            }

            return !compilation.HasImplicitConversion(type, assignableToType);
        }

        private static ExpressionSyntax GetTypeOfExpression(CSharpCompilation compilation, INamedTypeSymbol type, INamedTypeSymbol? relatedType)
        {
            if (type.IsUnboundGenericType && relatedType != null)
            {
                if (relatedType.IsGenericType && relatedType.Arity == type.Arity)
                {
                    type = type.Construct(relatedType.TypeArguments.ToArray());
                }
                else
                {
                    var baseType = Helpers.GetBaseTypes(compilation, type).FirstOrDefault(z => z.IsGenericType && compilation.HasImplicitConversion(z, type));
                    if (baseType == null)
                    {
                        baseType = type.AllInterfaces.FirstOrDefault(z => z.IsGenericType && compilation.HasImplicitConversion(z, type));
                    }

                    if (baseType != null)
                    {
                        type = type.Construct(baseType.TypeArguments.ToArray());
                    }
                }
            }

            if (compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
            {
                return TypeOfExpression(ParseTypeName(Helpers.GetGenericDisplayName(type)));
            }

            if (type.IsGenericType && !type.IsOpenGenericType())
            {
                var result = compilation.IsSymbolAccessibleWithin(type.ConstructUnboundGenericType(), compilation.Assembly);
                if (result)
                {
                    var name = ParseTypeName(type.ConstructUnboundGenericType().ToDisplayString());
                    if (name is GenericNameSyntax genericNameSyntax)
                    {
                        name = genericNameSyntax.WithTypeArgumentList(TypeArgumentList(SeparatedList<TypeSyntax>(
                            genericNameSyntax.TypeArgumentList.Arguments.Select(_ => OmittedTypeArgument()).ToArray()
                        )));
                    }

                    return InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, TypeOfExpression(name), IdentifierName("MakeGenericType")))
                        .WithArgumentList(ArgumentList(SeparatedList(
                            type.TypeArguments
                                .Select(t => Argument(GetTypeOfExpression(compilation, (t as INamedTypeSymbol)!, null)))
                        )));
                }
            }

            return GetPrivateType(compilation, type);
        }

        private static InvocationExpressionSyntax GetPrivateType(CSharpCompilation compilation, INamedTypeSymbol type)
        {
            var expression = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("context"),
                                    IdentifierName("LoadFromAssemblyName"))
                            )
                            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                Argument(IdentifierName(Helpers.AssemblyVariableName(type.ContainingAssembly)))
                            ))),
                        IdentifierName("GetType")
                    )
                )
                .WithArgumentList(
                    ArgumentList(SingletonSeparatedList(
                        Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(Helpers.GetFullMetadataName(type))))
                    ))
                );
            if (type.IsGenericType && !type.IsOpenGenericType())
            {
                return InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, IdentifierName("MakeGenericType")))
                    .WithArgumentList(ArgumentList(SeparatedList(
                        type.TypeArguments
                            .Select(t => Argument(GetTypeOfExpression(compilation, (t as INamedTypeSymbol)!, null)))
                    )));
            }

            return expression;
        }

        public static bool IsOpenGenericType(this INamedTypeSymbol type)
        {
            return type.IsGenericType && (type.IsUnboundGenericType || type.TypeArguments.All(z => z.TypeKind == TypeKind.TypeParameter));
        }
    }
}
