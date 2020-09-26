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

        private static InvocationExpressionSyntax GetPrivateType(string typeName)
        {
            return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Type"),
                        IdentifierName("GetType")
                    )
                )
               .WithArgumentList(
                    ArgumentList(
                        SeparatedList<ArgumentSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal(typeName)
                                    )
                                ),
                                Token(SyntaxKind.CommaToken),
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.TrueLiteralExpression
                                    )
                                )
                            }
                        )
                    )
                );
        }

        public static InvocationExpressionSyntax GenerateServiceFactory(
            NameSyntax strategyName,
            NameSyntax serviceCollectionName,
            INamedTypeSymbol serviceType,
            INamedTypeSymbol implementationType,
            ExpressionSyntax lifetime
        )
        {
            var serviceTypeExpression = GetTypeOfExpression(serviceType);
            var implementationTypeExpression = SimpleLambdaExpression(Parameter(Identifier("_")))
               .WithExpressionBody(
                    InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("_"), IdentifierName("GetRequiredService")))
                       .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(GetTypeOfExpression(implementationType)))))
                );
            return GenerateServiceType(strategyName, serviceCollectionName, serviceTypeExpression, implementationTypeExpression, lifetime);
        }

        public static InvocationExpressionSyntax GenerateServiceType(
            NameSyntax strategyName,
            NameSyntax serviceCollectionName,
            INamedTypeSymbol serviceType,
            INamedTypeSymbol implementationType,
            ExpressionSyntax lifetime
        )
        {
            var serviceTypeExpression = GetTypeOfExpression(serviceType);
            var implementationTypeExpression = GetTypeOfExpression(implementationType);
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

        private static ExpressionSyntax GetTypeOfExpression(INamedTypeSymbol type) => type.DeclaredAccessibility == Accessibility.Public
            ? TypeOfExpression(
                ParseTypeName(type.ToDisplayString()) // might be a better way to do this
            ) as ExpressionSyntax
            : GetPrivateType(type.ToDisplayString());
    }
}
