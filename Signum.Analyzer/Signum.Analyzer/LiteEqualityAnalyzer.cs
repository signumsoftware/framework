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

namespace Signum.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LiteEqualityAnalyzer : DiagnosticAnalyzer
{
    internal readonly static DiagnosticDescriptor RuleEqualsLite = new DiagnosticDescriptor("SF0031",
        "Prevents unintended reference comparison between two Lite<T>",
        "Avoid comparing two Lite<T> by reference, consider using 'Is' extension method", "Lite",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Checks that two Lite<T> not compared directly, preventing an unintended reference comparison.");

    internal readonly static DiagnosticDescriptor RuleEqualsEntity = new DiagnosticDescriptor("SF0032",
        "Prevents unintended reference comparison between two Entities",
        "Avoid comparing two Entities by reference, consider using 'Is' extension method", "Lite",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Checks that two Lite<T> not compared directly, preventing an unintended reference comparison.");

    internal readonly static DiagnosticDescriptor RuleLiteEntity = new DiagnosticDescriptor("SF0033",
        "Prevents comparisons between Lite<T> and T",
        "Impossible to compare Lite<T> and T, consider using 'Is' extension method", "Lite",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Checks that two Entities are not compared directly. C# doesn't catch this because Lite<T> is implemented as an interface to have co-variance.");

    internal readonly static DiagnosticDescriptor RuleEntityTypes = new DiagnosticDescriptor("SF0034",
        "Prevents comparisons between Lite<A> and Lite<B>",
        "Impossible to compare Lite<{0}> and Lite<{1}>", "Lite",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Checks that Lite<T> and T are not compared directly. C# doesn't catch this because Lite<T> is implemented as an interface to have co-variance.");


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleEqualsLite, RuleEqualsEntity, RuleEntityTypes, RuleLiteEntity); } }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.EqualsExpression);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.NotEqualsExpression);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {

        var equalsExpression = (BinaryExpressionSyntax)context.Node;

        var left = context.SemanticModel.GetTypeInfo(equalsExpression.Left).Type;
        var right = context.SemanticModel.GetTypeInfo(equalsExpression.Right).Type;

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
                tLeft.TypeKind != TypeKind.Interface &&
                tRight.TypeKind != TypeKind.Interface &&
                !tLeft.GetBaseTypesAndThis().Contains(tRight, SymbolEqualityComparer.Default) &&
                !tRight.GetBaseTypesAndThis().Contains(tLeft, SymbolEqualityComparer.Default))
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleEntityTypes, equalsExpression.GetLocation(), tLeft.Name, tRight.Name));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleEqualsLite, equalsExpression.GetLocation()));
            }
        }
        else if (
            left.IsEntity() && right.IsEntity())
        {
            context.ReportDiagnostic(Diagnostic.Create(RuleEqualsEntity, equalsExpression.GetLocation()));
        }
    }
}
