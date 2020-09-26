using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Scrutor.Analyzers
{
    internal class StaticScrutorSyntaxReceiver : ISyntaxReceiver
    {
        public List<ExpressionSyntax> ScanStaticExpressions { get; } = new List<ExpressionSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InvocationExpressionSyntax ies)
            {
                if (ies.Expression is MemberAccessExpressionSyntax mae
                 && mae.Name.ToFullString().EndsWith("ScanStatic", StringComparison.Ordinal)
                 && ies.ArgumentList.Arguments.Count is 1 or 2)
                {
                    ScanStaticExpressions.Add(ies.ArgumentList.Arguments[0].Expression);
                }
            }
        }
    }
}
