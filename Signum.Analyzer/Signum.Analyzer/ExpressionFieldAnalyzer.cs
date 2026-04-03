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
public class ExpressionFieldAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF0002";

    internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
        "Use ExpressionFieldAttribute in non-trivial method or property",
        "'{0}' should reference an static field of type Expression<T> with the same signature ({1})", "Expressions",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A property or method can use ExpressionFieldAttribute pointing to an static fied of type Expression<T> to use it in LINQ queries.");

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
            if (name != "ExpressionField")
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

            var argument = att.ArgumentList?.Arguments.Select(a => a.Expression).FirstOrDefault();
            if (argument == null)
                return;

            var val = context.SemanticModel.GetConstantValue(argument);

            string fieldName = val.HasValue ? (val.Value as string) : null;

            if (fieldName == null)
            {
                Diagnostic(context, ident, argument.GetLocation(), "invalid field name");
                return;
            }

            var typeSyntax = member.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeSyntax == null)
                return;

            var type = context.SemanticModel.GetDeclaredSymbol(typeSyntax);
            var fieldSymbol = type.GetMembers().OfType<IFieldSymbol>().SingleOrDefault(a => a.Name == fieldName);

            if (fieldSymbol == null)
            {
                Diagnostic(context, ident, att.GetLocation(), string.Format("field '{0}' not found", fieldName));
                return;
            }

            var memberSymbol = context.SemanticModel.GetDeclaredSymbol(member);

            var expressionType = GetExpressionType(memberSymbol, context.SemanticModel);

            if (!expressionType.Equals(fieldSymbol.Type, SymbolEqualityComparer.IncludeNullability))
            {
                var minimalParts = expressionType.ToMinimalDisplayString(context.SemanticModel, member.GetLocation().SourceSpan.Start);
                var insteadOfParts = fieldSymbol.Type.ToMinimalDisplayString(context.SemanticModel, typeSyntax.GetLocation().SourceSpan.Start);
                Diagnostic(context, ident, att.GetLocation(), string.Format("type of '{0}' should be '{1}' instead of '{2}'", fieldName, minimalParts, insteadOfParts));
                return;
            }
        }
        catch (Exception e)
        {
            throw new Exception(context.SemanticModel.SyntaxTree.FilePath + "\n" + e.Message + "\n" + e.StackTrace);
        }
    }

    private static INamedTypeSymbol GetExpressionType(ISymbol memberSymbol, SemanticModel sm)
    {
        var parameters = memberSymbol is IMethodSymbol symbol ? symbol.Parameters.Select(p => (p.Type, p.NullableAnnotation)).ToList() : new List<(ITypeSymbol, NullableAnnotation)>();

        if (!memberSymbol.IsStatic)
            parameters.Insert(0, ((ITypeSymbol)memberSymbol.ContainingSymbol, NullableAnnotation.NotAnnotated));

        var returnType = memberSymbol is IMethodSymbol mi ? (mi.ReturnType, mi.ReturnNullableAnnotation) :
            memberSymbol is IPropertySymbol pi ? (pi.Type, pi.NullableAnnotation) :
            throw new InvalidOperationException("Unexpected member");

        parameters.Add(returnType);

        var expression = sm.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
        var func = sm.Compilation.GetTypeByMetadataName("System.Func`" + parameters.Count);

        var funcConstruct = func.Construct(
            parameters.Select(a => a.Item1).ToImmutableArray(),
            parameters.Select(a => a.Item2).ToImmutableArray()).WithNullableAnnotation(NullableAnnotation.NotAnnotated);

        var expConstruct = expression.Construct(
            ImmutableArray.Create((ITypeSymbol)funcConstruct),
            ImmutableArray.Create(NullableAnnotation.NotAnnotated));
        
        return (INamedTypeSymbol)expConstruct.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
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

    private static void Diagnostic(SyntaxNodeAnalysisContext context, string identifier, Location location, string error, bool fixable = false)
    {
        var properties = ImmutableDictionary<string, string>.Empty.Add("fixable", fixable.ToString());
        var diagnostic = Microsoft.CodeAnalysis.Diagnostic.Create(Rule, location, properties, identifier, error);

        context.ReportDiagnostic(diagnostic);
    }
}
