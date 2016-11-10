using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LiteEqualityAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SF0003";
        
        private static DiagnosticDescriptor RuleLiteEntity = new DiagnosticDescriptor(DiagnosticId,
            "Prevents comparisons between Lite<T> and T",
            "Impossible to compare Lite<T> and T. Consider using RefersTo method", "Lite",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Checks that Lite<T> and T are not compared directly. C# doesn't catch this because Lite<T> is implemented as an interface to have co-variance");

        private static DiagnosticDescriptor RuleEntityTypes = new DiagnosticDescriptor(DiagnosticId,
            "Prevents comparisons between Lite<A> and Lite<B>",
            "Impossible to compare Lite<{0}> and Lite<{1}>", "Lite",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Checks that Lite<T> and T are not compared directly. C# doesn't catch this because Lite<T> is implemented as an interface to have co-variance");


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleEntityTypes, RuleLiteEntity); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.EqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.NotEqualsExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {

            var equalsExpression = (BinaryExpressionSyntax)context.Node;

            var left = context.SemanticModel.GetTypeInfo(equalsExpression.Left);
            var right = context.SemanticModel.GetTypeInfo(equalsExpression.Right);

            if (
                left.IsLite() && right.IsEntity() ||
                left.IsEntity() && right.IsLite())
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleLiteEntity, equalsExpression.GetLocation()));
            }
            else if (
                left.IsLite() && right.IsLite())
            {
                var tLeft = left.GetLiteEntityType();
                var tRight = right.GetLiteEntityType();

                if (tLeft != null && 
                    tRight != null &&
                    !tLeft.IsAbstract &&
                    !tRight.IsAbstract &&
                    !tLeft.GetBaseTypesAndThis().Contains(tRight) &&
                    !tRight.GetBaseTypesAndThis().Contains(tLeft))
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleEntityTypes, equalsExpression.GetLocation(), tLeft.Name, tRight.Name));
                }
            }
        }

    }

}
