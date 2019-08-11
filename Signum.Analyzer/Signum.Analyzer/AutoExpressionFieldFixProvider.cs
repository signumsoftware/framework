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

namespace Signum.Analyzer
{
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
                context.RegisterCodeFix(
                    CodeAction.Create("Add As.Expression(() => )", c => AddAsExpression(context.Document, declaration, c), "AddAsExpression"),
                    diagnostic);
            }
        }

        private async Task<Document> AddAsExpression(Document document, MemberDeclarationSyntax declaration, CancellationToken c)
        {
            var typeSyntax = declaration.Ancestors().OfType<TypeDeclarationSyntax>().First();

            var sm = await document.GetSemanticModelAsync(c);

            var type = sm.GetDeclaredSymbol(typeSyntax, c);

            var symbol = sm.GetDeclaredSymbol(declaration);

            string name = declaration is MethodDeclarationSyntax ?
                ((MethodDeclarationSyntax)declaration).Identifier.ToString() :
                ((PropertyDeclarationSyntax)declaration).Identifier.ToString();

            var bodyExpression = GetSingleBody(declaration);
      
            var newBody = GetNewBody(bodyExpression);

            newBody = newBody.WithLeadingTrivia(bodyExpression.GetLeadingTrivia());

            var docRoot = await document.GetSyntaxRootAsync();

            var newDoc = docRoot.ReplaceNode(bodyExpression, newBody);

            return document.WithSyntaxRoot(newDoc);
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

        //private void MoveInitialTrivia(ref ExpressionSyntax bodyExpression, ref InvocationExpressionSyntax newDeclaration)
        //{
        //    var leadingTrivia = newDeclaration.GetLeadingTrivia();

        //    if (!leadingTrivia.Any())
        //        return;

        //    var last = leadingTrivia.Last();

        //    if (leadingTrivia.Any() && (SyntaxKind)last.RawKind == SyntaxKind.WhitespaceTrivia)
        //    {
        //        newField = newField.WithLeadingTrivia(leadingTrivia);

        //        newDeclaration = newDeclaration.WithLeadingTrivia(last);
        //    }
        //}

        public static GenericNameSyntax GetExpressionTypeSyntax(List<ParameterSyntax> parameterList, TypeSyntax returnType)
        {
            var funcType = SyntaxFactory.GenericName(SyntaxFactory.Identifier("Func"),
                SyntaxFactory.TypeArgumentList().AddArguments(parameterList.Select(a => a.Type).Concat(new[] { returnType }).ToArray()));

            var expressionType = SyntaxFactory.GenericName(SyntaxFactory.Identifier("Expression"),
                SyntaxFactory.TypeArgumentList().AddArguments(funcType));
            return expressionType;
        }


        public static List<ParameterSyntax> GetParameters(MemberDeclarationSyntax declaration, INamedTypeSymbol type, ISymbol symbol)
        {
            var parameterList = declaration is MethodDeclarationSyntax ?
                ((MethodDeclarationSyntax)declaration).ParameterList.Parameters.ToList() :
                new List<ParameterSyntax>();

            if (!symbol.IsStatic)
                parameterList.Insert(0, SyntaxFactory.Parameter(SyntaxFactory.Identifier("@this")).WithType(SyntaxFactory.IdentifierName(type.Name)));

            return parameterList;
        }


        public static ExpressionSyntax GetSingleBody(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax)
            {
                var method = (MethodDeclarationSyntax)member;

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

}
