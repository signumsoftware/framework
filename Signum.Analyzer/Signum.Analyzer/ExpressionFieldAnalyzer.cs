using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Signum.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExpressionFieldAnalyzer : DiagnosticAnalyzer
    {
        public static readonly string DiagnosticId = "SF0002";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            "Use ExpressionFieldAttribute in non-trivial method or property",
            "'{0}' should be a simple evaluation of an static Expression<T> field with the same signature ({1})", "Expressions",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A Property or Method can use ExpressionFieldAttribute to expand theid body when used in LINQ queries");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzePropertySymbol, SyntaxKind.Attribute);
        }


        static void AnalyzePropertySymbol(SyntaxNodeAnalysisContext context)
        {
            var att = (AttributeSyntax)context.Node;

            if (!att.ToString().EndsWith("ExpressionField"))
                return;

            if (att.ArgumentList != null && att.ArgumentList.Arguments.Any())
                return; // Implement in the future

            var member = att.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            var method = member as MethodDeclarationSyntax;
            var prop = member as PropertyDeclarationSyntax;

            var ident = prop?.Identifier.ToString() ?? method?.Identifier.ToString();

            if (method != null)
            {
                if (method.ReturnType.ToString() == "void")
                {
                    Diagnostic(context, ident, method.ReturnType.GetLocation(), "no return type", fixable: false);
                    return;
                }

                foreach (var param in method.ParameterList.Parameters)
                {
                    if (param.Modifiers.Any(a => a.Kind() != SyntaxKind.ThisKeyword))
                    {
                        Diagnostic(context, ident, param.Modifiers.First().GetLocation(), "complex paramerer '" + param.Identifier.ToString() + "'");
                        return;
                    }
                }
            }

            ExpressionSyntax expr = GetSingleBody(context, ident, att, member);

            if (expr == null)
                return;

            var inv = expr as InvocationExpressionSyntax;
            if (inv == null)
            {
                Diagnostic(context, ident, expr.GetLocation(), "no invocation", fixable: true);
                return;
            }

            var memberAccess = inv.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null || memberAccess.Name.ToString() != "Evaluate")
            {
                Diagnostic(context, ident, expr.GetLocation(), "no Evaluate", fixable: true);
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol as IFieldSymbol;
            if (symbol == null || !symbol.IsStatic)
            {
                Diagnostic(context, ident, memberAccess.Expression.GetLocation(), "no static field");
                return;
            }

            var args = inv.ArgumentList.Arguments;

            var isStatic = member is MethodDeclarationSyntax ?
                ((MethodDeclarationSyntax)member).Modifiers.Any(a => a.Kind() == SyntaxKind.StaticKeyword) :
                ((PropertyDeclarationSyntax)member).Modifiers.Any(a => a.Kind() == SyntaxKind.StaticKeyword);

            if (!isStatic)
            {
                if (args.Count == 0 || !(args[0].Expression is ThisExpressionSyntax))
                {
                    Diagnostic(context, ident, inv.ArgumentList.OpenParenToken.GetLocation(), "first argument should be 'this'");
                    return;
                }
            }

            int positon = isStatic ? 0 : 1;
            if (method != null)
            {
                foreach (var item in method.ParameterList.Parameters)
                {
                    if (args.Count <= positon)
                    {
                        Diagnostic(context, ident, inv.ArgumentList.CloseParenToken.GetLocation(), string.Format("missing argument '{0}'", item.Identifier));
                        return;
                    }
                    var arg = args[positon++];

                    if (arg.ToString() != item.Identifier.ToString())
                    {
                        Diagnostic(context, ident, arg.GetLocation(), string.Format("missing argument '{0}'", item.Identifier));
                        return;
                    }
                }
            }

            if (args.Count > positon)
            {
                Diagnostic(context, ident, args[positon].GetLocation(), "extra parameters");
                return;
            }
        }

        internal static ExpressionSyntax GetSingleBody(SyntaxNodeAnalysisContext context, string ident, AttributeSyntax att, MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax)
            {
                var method = (MethodDeclarationSyntax)member;

                if (method.ExpressionBody != null)
                    return method.ExpressionBody.Expression;
                
                return OnlyReturn(context, ident, att, method.Body.Statements);
            }
            else if (member is PropertyDeclarationSyntax)
            {
                var property = (PropertyDeclarationSyntax)member;

                if (property.ExpressionBody != null)
                    return property.ExpressionBody.Expression;

                var getter = property.AccessorList.Accessors.SingleOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);

                if (getter == null)
                {
                    Diagnostic(context, ident, att.GetLocation(), "no getter");
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

        internal static ExpressionSyntax OnlyReturn(SyntaxNodeAnalysisContext context,string ident, AttributeSyntax att, SyntaxList<StatementSyntax> statements)
        {
            var only = statements.Only();

            if (only == null)
            {
                Diagnostic(context, ident, att.GetLocation(), statements.Count + " statements");
                return null;
            }

            var ret = only as ReturnStatementSyntax;
            if (ret == null)
            {
                Diagnostic(context, ident, only.GetLocation(), "no return");
                return null;
            }

            if(ret.Expression == null)
            {
                Diagnostic(context, ident, only.GetLocation(), "no return expression");
                return null;
            }
            
            return ret.Expression;
        }
        
        private static void Diagnostic(SyntaxNodeAnalysisContext context, string identifier, Location location, string error, bool fixable = false)
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("fixable", fixable.ToString());
            var diagnostic = Microsoft.CodeAnalysis.Diagnostic.Create(Rule, location, properties, identifier, error);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
