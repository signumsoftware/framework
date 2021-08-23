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
        
        private static readonly DiagnosticDescriptor RuleCastToEntityEntity = new DiagnosticDescriptor(DiagnosticId,
            "Prevents direct convertion from Lite<T> to T",
            "Impossible to convert Lite<T> to T, consider using Entity or Retrieve", "Lite",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Checks direct convertion from Lite<T> to T. C# doesn't catch this because Lite<T> is implemented as an interface to have co-variance.");

        private static readonly DiagnosticDescriptor RuleCastToLiteEntity = new DiagnosticDescriptor(DiagnosticId,
             "Prevents direct convertion from T to Lite<T>",
             "Impossible to convert T to Lite<T>, consider using ToLite or ToLiteFat", "Lite",
             DiagnosticSeverity.Error,
             isEnabledByDefault: true,
             description: "Checks direct convertion from T to Lite<T>. C# doesn't catch this because Lite<T> is implemented as an interface to have co-variance.");

        public static DiagnosticDescriptor RuleCastToLiteEntity1 => RuleCastToLiteEntity;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleCastToEntityEntity, RuleCastToLiteEntity1); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CastExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IsPatternExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is CastExpressionSyntax c)
            {
                CheckTypes(context,
                    oldType: context.SemanticModel.GetTypeInfo(c.Expression).Type,
                    newType: context.SemanticModel.GetTypeInfo(c).Type
                );
            }
            else if (context.Node is BinaryExpressionSyntax b)
            {
                if (b.Kind() == SyntaxKind.AsExpression)
                {
                    CheckTypes(context,  
                        oldType: context.SemanticModel.GetTypeInfo(b.Left).Type,
                        newType : context.SemanticModel.GetTypeInfo(b).Type
                    );
                }
                else if (b.Kind() == SyntaxKind.IsExpression)
                {
                    CheckTypes(context,
                        oldType: context.SemanticModel.GetTypeInfo(b.Left).Type,
                        newType: context.SemanticModel.GetTypeInfo(b.Right).Type
                    );
                }
                else throw new InvalidOperationException("Unexpected");
            }
            else if (context.Node is IsPatternExpressionSyntax ip)
            {
                var oldType = context.SemanticModel.GetTypeInfo(ip.Expression).Type;
                if(oldType.IsEntity() || oldType.IsLite())
                {
                    CheckPattern(context, oldType, ip.Pattern);
                }
            }
            else throw new InvalidOperationException("Unexpected " + context.Node);
        }

        private static void CheckPattern(SyntaxNodeAnalysisContext context, ITypeSymbol oldType, PatternSyntax syntax)
        {
            switch (syntax)
            {
                case BinaryPatternSyntax bp:
                    CheckPattern(context, oldType, bp.Left);
                    CheckPattern(context, oldType, bp.Right);
                    break;

                case ConstantPatternSyntax cp:
                    CheckTypes(context, oldType, context.SemanticModel.GetTypeInfo(cp.Expression).Type);
                    break;

                case DeclarationPatternSyntax dp:
                    CheckTypes(context, oldType, context.SemanticModel.GetTypeInfo(dp.Type).Type, dp.GetLocation());
                    break;

                case DiscardPatternSyntax: break;

                case ParenthesizedPatternSyntax p:
                    CheckPattern(context, oldType, p.Pattern);
                    break;

                case RecursivePatternSyntax rp:

                    ITypeSymbol ti;
                    if (rp.Type != null)
                    {
                        ti = context.SemanticModel.GetTypeInfo(rp.Type).Type;
                        CheckTypes(context, oldType, ti, rp.GetLocation());
                    }
                    else
                    {
                        ti = oldType;
                    }

                    if (rp.PropertyPatternClause != null)
                        foreach (var sp in rp.PropertyPatternClause.Subpatterns)
                        {
                            var member = ti.GetMembers(sp.NameColon.Name.Identifier.ToString()).Only();
                            if(member is IPropertySymbol ps)
                            {
                                CheckPattern(context, ps.Type, sp.Pattern);
                            }
                            else if(member is IFieldSymbol fi)
                            {
                                CheckPattern(context, fi.Type, sp.Pattern);
                            }
                        }

                    break;

                case TypePatternSyntax tp:
                    CheckTypes(context, oldType, context.SemanticModel.GetTypeInfo(tp.Type).Type, tp.GetLocation());
                    break;

                case UnaryPatternSyntax up:
                    CheckPattern(context, oldType, up.Pattern);
                    break;

                case VarPatternSyntax: break;

                default:
                    break;
            }

        }

        private static void CheckTypes(SyntaxNodeAnalysisContext context, ITypeSymbol oldType, ITypeSymbol newType, Location location = null)
        {
            if (newType.IsLite() && oldType.IsEntity())
                context.ReportDiagnostic(Diagnostic.Create(RuleCastToLiteEntity1, location ?? context.Node.GetLocation()));
            else if (newType.IsEntity() && oldType.IsLite())
                context.ReportDiagnostic(Diagnostic.Create(RuleCastToEntityEntity, location ?? context.Node.GetLocation()));
        }
    }

}
