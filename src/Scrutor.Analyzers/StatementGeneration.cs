using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Scrutor.Analyzers
{
    static class StatementGeneration
    {
        private static Regex SpecialCharacterRemover = new Regex("[^\\w\\d]");

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
            var serviceTypeExpression = GetTypeOfExpression(compilation, serviceType);
            var isAccessible = compilation.IsSymbolAccessibleWithin(implementationType, compilation.Assembly);

            if (isAccessible)
            {
                var implementationTypeExpression = SimpleLambdaExpression(Parameter(Identifier("_")))
                    .WithExpressionBody(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("_"), GenericName("GetRequiredService")
                                .WithTypeArgumentList(
                                    TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(Helpers.GetFullMetadataName(implementationType))))
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
                                    ArgumentList(SingletonSeparatedList(Argument(GetTypeOfExpression(compilation, implementationType))))
                                ),
                            IdentifierName(Helpers.GetFullMetadataName(serviceType))
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
            var serviceTypeExpression = GetTypeOfExpression(compilation, serviceType);
            var implementationTypeExpression = GetTypeOfExpression(compilation, implementationType);
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

        private static ExpressionSyntax GetTypeOfExpression(CSharpCompilation compilation, INamedTypeSymbol type) =>
            compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)
                ? TypeOfExpression(
                    ParseTypeName(type.ToDisplayString()) // might be a better way to do this
                ) as ExpressionSyntax
                : GetPrivateType(type);

        public static string AssemblyVariableName(IAssemblySymbol symbol) => SpecialCharacterRemover.Replace(symbol.Identity.GetDisplayName(true), "");

        public static IEnumerable<MemberDeclarationSyntax> AssemblyDeclaration(IAssemblySymbol symbol)
        {
            var name = AssemblyVariableName(symbol);
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

        private static InvocationExpressionSyntax GetPrivateType(INamedTypeSymbol type)
        {
            return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("context"),
                                    IdentifierName("LoadFromAssemblyName"))
                            )
                            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                Argument(IdentifierName(AssemblyVariableName(type.ContainingAssembly)))
                            ))),
                        IdentifierName("GetType")
                    )
                )
                .WithArgumentList(
                    ArgumentList(SingletonSeparatedList(
                        Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(Helpers.GetFullMetadataName(type))))
                    ))
                );
        }
    }
}
