using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;

namespace Signum.Analyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoExpressionFieldFixProvider)), Shared]
public class AutoExpressionFieldFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return ImmutableArray.Create(AutoExpressionFieldAnalyzer.DiagnosticId); }
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();

        if (diagnostic.Properties["fixable"] == "True")
        {
            if (diagnostic.Properties.TryGetValue("explicitConvert", out var newType))
            {
                context.RegisterCodeFix(
                    CodeAction.Create($"Add Explicit Cast to '{newType}'", c => InsertExplicitCast(context.Document, declaration, c), "InsertExplicitCast"),
                diagnostic);
            }
            else
            {
                context.RegisterCodeFix(
                    CodeAction.Create("Add As.Expression(() => )", c => AddAsExpression(context.Document, declaration, c), "AddAsExpression"),
                    diagnostic);
            }
        }
    }

    private async Task<Document> InsertExplicitCast(Document document, MemberDeclarationSyntax declaration, CancellationToken c)
    {
        var bodyExpression = GetSingleBody(declaration);

        var invoke = (InvocationExpressionSyntax)bodyExpression;

        var lambda = (LambdaExpressionSyntax)invoke.ArgumentList.Arguments[0].Expression;

        var docRoot = await document.GetSyntaxRootAsync(c);

        var type = declaration is MethodDeclarationSyntax syntax ? syntax.ReturnType :
            ((PropertyDeclarationSyntax)declaration).Type;

        var newRoot = docRoot.ReplaceNode(lambda.Body, SyntaxFactory.CastExpression(type, (ExpressionSyntax)lambda.Body));

        return document.WithSyntaxRoot(newRoot);
    }

    private async Task<Document> AddAsExpression(Document document, MemberDeclarationSyntax declaration, CancellationToken c)
    {
        //var typeSyntax = declaration.Ancestors().OfType<TypeDeclarationSyntax>().First();

        //var sm = await document.GetSemanticModelAsync(c);

        //var type = sm.GetDeclaredSymbol(typeSyntax, c);

        //var symbol = sm.GetDeclaredSymbol(declaration);

        //string name = declaration is MethodDeclarationSyntax ?
        //    ((MethodDeclarationSyntax)declaration).Identifier.ToString() :
        //    ((PropertyDeclarationSyntax)declaration).Identifier.ToString();

        var bodyExpression = GetSingleBody(declaration);
  
        var newBody = GetNewBody(bodyExpression);

        newBody = newBody.WithLeadingTrivia(bodyExpression.GetLeadingTrivia());

        var docRoot = await document.GetSyntaxRootAsync(c);

        var newRoot = docRoot.ReplaceNode(bodyExpression, newBody);

        return document.WithSyntaxRoot(newRoot);
    }

    private InvocationExpressionSyntax GetNewBody(ExpressionSyntax bodyExpression)
    {
        var member = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("As"),
            SyntaxFactory.IdentifierName("Expression"));

        var lambda = SyntaxFactory.ParenthesizedLambdaExpression(bodyExpression); 

        return SyntaxFactory.InvocationExpression(member,
            SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(lambda)))
            .NormalizeWhitespace();
    }

    public static ExpressionSyntax GetSingleBody(MemberDeclarationSyntax member)
    {
        if (member is MethodDeclarationSyntax method)
        {
            if (method.ExpressionBody != null)
                return method.ExpressionBody.Expression;

            return ((ReturnStatementSyntax)method.Body.Statements.Single()).Expression;
        }
        else
        {
            var property = (PropertyDeclarationSyntax)member;

            if (property.ExpressionBody != null)
                return property.ExpressionBody.Expression;

            var getter = property.AccessorList.Accessors.Single(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);

            return ((ReturnStatementSyntax)getter.Body.Statements.Single()).Expression;
        }
    }
}

