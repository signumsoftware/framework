using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Signum.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AutoExpressionFieldAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF0001";

    internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
        "Call As.Expression in a method or property with AutoExpressionFieldAttribute",
        "'{0}' should call As.Expression(() => ...) ({1})", "Expressions",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A Property or Method can use AutoExpressionFieldAttribute and As.Expression(() => ...) to extract their implementation to a hidden static field with the expression tree, that will be used by Signum LINQ provider to translate it to SQL.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(AnalyzeAttributeSymbol, SyntaxKind.Attribute);
    }

    static void AnalyzeAttributeSymbol(SyntaxNodeAnalysisContext context)
    {
        try
        {
            var att = (AttributeSyntax)context.Node;

            var name = att.Name.ToString();
            if (name != "AutoExpressionField")
                return;

            var member = att.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            var method = member as MethodDeclarationSyntax;
            var prop = member as PropertyDeclarationSyntax;

            var ident = prop?.Identifier.ToString() ?? method?.Identifier.ToString();

            if (method != null)
            {
                if (method.ReturnType.ToString() == "void")
                {
                    Diagnostic(context, ident, method.ReturnType.GetLocation(), "no return type");
                    return;
                }

                foreach (var param in method.ParameterList.Parameters)
                {
                    if (param.Modifiers.Any(a => a.Kind() != SyntaxKind.ThisKeyword))
                    {
                        Diagnostic(context, ident, param.Modifiers.First().GetLocation(), "complex parameter '" + param.Identifier.ToString() + "'");
                        return;
                    }
                }
            }

            ExpressionSyntax expr = GetSingleBody(context, ident, att, member);

            if (expr == null)
                return;

            if (!(expr is InvocationExpressionSyntax inv) || !(context.SemanticModel.GetSymbolInfo(inv) is SymbolInfo si))
            {
                Diagnostic(context, ident, att.GetLocation(), "no As.Expression", fixable: true);
                return;
            }

            if (si.Symbol != null ? !IsExpressionAs(si.Symbol) : !si.CandidateSymbols.Any(s => IsExpressionAs(s)))
            {
                Diagnostic(context, ident, att.GetLocation(), "no As.Expression", fixable: true);
                return;
            }

            var args = inv.ArgumentList.Arguments;

            if (args.Count == 0 || !(args[0].Expression is ParenthesizedLambdaExpressionSyntax))
            {
                Diagnostic(context, ident, att.GetLocation(), "the call to As.Expression should have a lambda as argument", fixable: false);
                return;
            }

            var type = context.SemanticModel.GetTypeInfo(inv);

            if(!type.ConvertedNullability.Equals(type.Nullability) || 
              !type.Type.Equals(type.ConvertedType, SymbolEqualityComparer.IncludeNullability))
            {
                var position = member.GetLocation().SourceSpan.Start;
                var current = type.Type.ToMinimalDisplayString(context.SemanticModel, type.Nullability.FlowState, position);
                var converted = type.ConvertedType.ToMinimalDisplayString(context.SemanticModel, type.ConvertedNullability.FlowState, position);

                Diagnostic(context, ident, att.GetLocation(), $"the call to As.Expression returns '{current}' but is implicitly converted to '{converted}'", fixable: true, explicitConvert: converted);
                return;
            }
        }
        catch (Exception e)
        {
            throw new Exception(context.SemanticModel.SyntaxTree.FilePath + "\n" + e.Message + "\n" + e.StackTrace);
        }
    }

    private static bool IsExpressionAs(ISymbol symbol)
    {
        return symbol.Name == "Expression" &&
                symbol.ContainingType.Name == "As" &&
               symbol.ContainingNamespace.ToString() == "Signum.Utilities";
    }

    public static ExpressionSyntax GetSingleBody(SyntaxNodeAnalysisContext context, string ident, AttributeSyntax att, MemberDeclarationSyntax member)
    {
        if (member is MethodDeclarationSyntax method)
        {
            if (method.ExpressionBody != null)
                return method.ExpressionBody.Expression;

            return OnlyReturn(context, ident, att, method.Body.Statements);
        }
        else if (member is PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null)
                return property.ExpressionBody.Expression;

            var getter = property.AccessorList.Accessors.SingleOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);

            if (getter == null)
            {
                Diagnostic(context, ident, att.GetLocation(), "no getter");
                return null;
            }

            if (property.AccessorList.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration))
            {
                Diagnostic(context, ident, att.GetLocation(), "setter not allowed");
                return null;
            }

            if (getter.Body == null)
            {
                Diagnostic(context, ident, getter.GetLocation(), "no getter body");
                return null;
            }

            return OnlyReturn(context, ident, att, getter.Body.Statements);
        }

        Diagnostic(context, ident, att.GetLocation(), "no property or method");
        return null;
    }

    internal static ExpressionSyntax OnlyReturn(SyntaxNodeAnalysisContext context, string ident, AttributeSyntax att, SyntaxList<StatementSyntax> statements)
    {
        var only = statements.Only();

        if (only == null)
        {
            Diagnostic(context, ident, att.GetLocation(), statements.Count + " statements");
            return null;
        }

        if (!(only is ReturnStatementSyntax ret))
        {
            Diagnostic(context, ident, only.GetLocation(), "no return");
            return null;
        }

        if (ret.Expression == null)
        {
            Diagnostic(context, ident, only.GetLocation(), "no return expression");
            return null;
        }

        return ret.Expression;
    }

    private static void Diagnostic(SyntaxNodeAnalysisContext context, string identifier, Location location, string error, bool fixable = false, string explicitConvert = null)
    {
        var properties = ImmutableDictionary<string, string>.Empty.Add("fixable", fixable.ToString());
        if (explicitConvert != null)
            properties = properties.Add("explicitConvert", explicitConvert);

        var diagnostic = Microsoft.CodeAnalysis.Diagnostic.Create(Rule, location, properties, identifier, error);

        context.ReportDiagnostic(diagnostic);
    }
}
