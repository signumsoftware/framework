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
    public class LiteCastAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SF0004";
        
        private static DiagnosticDescriptor RuleCastToEntityEntity = new DiagnosticDescriptor(DiagnosticId,
            "Prevents direct convertion from Lite<T> to T",
            "Impossible to convert Lite<T> to T. Consider using Entity or Retrieve", "Lite",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Checks direct convertion from Lite<T> to T. C# doesn't catch this because Lite<T> is implemented as an interface to have co-variance");

        private static DiagnosticDescriptor RuleCastToLiteEntity = new DiagnosticDescriptor(DiagnosticId,
             "Prevents direct convertion from T to Lite<T>",
             "Impossible to convert T to Lite<T>. Consider using ToLite or ToLiteFat", "Lite",
             DiagnosticSeverity.Error,
             isEnabledByDefault: true,
             description: "Checks direct convertion from T to Lite<T>. C# doesn't catch this because Lite<T> is implemented as an interface to have co-variance");


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleCastToEntityEntity, RuleCastToLiteEntity); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CastExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IsExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            TypeInfo newType;
            TypeInfo oldType;

            if (context.Node is CastExpressionSyntax c)
            {
                newType = context.SemanticModel.GetTypeInfo(c);
                oldType = context.SemanticModel.GetTypeInfo(c.Expression);
            }
            else if (context.Node is BinaryExpressionSyntax b)
            {
                if (b.Kind() == SyntaxKind.AsExpression)
                {
                    newType = context.SemanticModel.GetTypeInfo(b);
                    oldType = context.SemanticModel.GetTypeInfo(b.Left);
                }
                else if(b.Kind() == SyntaxKind.IsExpression)
                {
                    newType = context.SemanticModel.GetTypeInfo(b.Right);
                    oldType = context.SemanticModel.GetTypeInfo(b.Left);
                }
                else throw new InvalidOperationException("Unexpected");
            }
            else throw new InvalidOperationException("Unexpected");

            if (newType.IsLite() && oldType.IsEntity())
                context.ReportDiagnostic(Diagnostic.Create(RuleCastToLiteEntity, context.Node.GetLocation()));
            else if (newType.IsEntity() && oldType.IsLite())
                context.ReportDiagnostic(Diagnostic.Create(RuleCastToEntityEntity, context.Node.GetLocation()));
        }

    }

}
