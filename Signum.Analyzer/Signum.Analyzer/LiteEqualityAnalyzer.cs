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

        private static readonly LocalizableString Title = "You should not compare Lite<T> and T directly";
        private static readonly LocalizableString MessageFormat = $"You should not compare Lite<T> and T directly";
        private static readonly LocalizableString Description = "You should not compare Lite<T> and T directly";
        private const string Category = "Equality";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.EqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.NotEqualsExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {

            var equalsExpression = (BinaryExpressionSyntax)context.Node;

            var lefttypeInfo = context.SemanticModel.GetTypeInfo(equalsExpression.Left);
            var rightTypeInfo = context.SemanticModel.GetTypeInfo(equalsExpression.Right);

            if (lefttypeInfo.IsLite())
            {
                if (rightTypeInfo.IsEntity())
                {
                    raiseDiagnostic(equalsExpression, context);
                }
            }

            if (rightTypeInfo.IsLite())
            {
                if (lefttypeInfo.IsEntity())
                {
                    raiseDiagnostic(equalsExpression, context);
                }
            }
        }

        private void raiseDiagnostic(BinaryExpressionSyntax equalsExpression, SyntaxNodeAnalysisContext context)
        {
            var diagnostic = Diagnostic.Create(Rule, equalsExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

    }

}
