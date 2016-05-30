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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExpressionFieldFixProvider)), Shared]
    public class ExpressionFieldFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ExpressionFieldAnalyzer.DiagnosticId); }
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
                    CodeAction.Create("Extract to Expression<T> static field", c => ExtractToExpressionTree(context.Document, declaration, c), "Convert to auto-property"),
                    diagnostic);
            }
        }

        private async Task<Document> ExtractToExpressionTree(Document document, MemberDeclarationSyntax declaration, CancellationToken c)
        {
            var typeSyntax = declaration.Ancestors().OfType<TypeDeclarationSyntax>().First();

            var sm = await document.GetSemanticModelAsync(c);

            var type = sm.GetDeclaredSymbol(typeSyntax, c);

            var symbol = sm.GetDeclaredSymbol(declaration);

            string name = declaration is MethodDeclarationSyntax ?
                ((MethodDeclarationSyntax)declaration).Identifier.ToString() :
                ((PropertyDeclarationSyntax)declaration).Identifier.ToString();

            string candidateName = name + "Expression";

            if (type.MemberNames.Contains(candidateName))
                candidateName = Enumerable.Range(2, 1000).Select(i => name + "Expression" + i).First(n => !type.MemberNames.Contains(n));

            List<ParameterSyntax> parameterList = GetParameters(declaration, type, symbol);

            TypeSyntax returnType = GetReturnType(declaration);

            var bodyExpression = GetSingleBody(declaration);
            var newField = GetStaticField(candidateName, parameterList, returnType, bodyExpression, sm, type);

            var newBody = GetNewBody(candidateName, parameterList);

            var newDeclaration = declaration.ReplaceNode(bodyExpression, newBody);

            MoveInitialTrivia(ref newField, ref newDeclaration);

            var newMembers = typeSyntax.Members.ReplaceRange(declaration, new[] { newField, newDeclaration });
            var newType = typeSyntax is StructDeclarationSyntax ?
                (TypeDeclarationSyntax)((StructDeclarationSyntax)typeSyntax).WithMembers(newMembers) :
                (TypeDeclarationSyntax)((ClassDeclarationSyntax)typeSyntax).WithMembers(newMembers);

            var docRoot = await document.GetSyntaxRootAsync();

            var newDoc = docRoot.ReplaceNode(typeSyntax, newType);

            var usings = newDoc.ChildNodes().OfType<UsingDirectiveSyntax>();

            if (usings.Any() && !usings.Any(a => a.Name.ToString() == "System.Linq.Expressions"))
                newDoc = newDoc.InsertNodesAfter(usings.LastOrDefault(), new[] {
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.IdentifierName("System").Qualified("Linq").Qualified("Expressions")
                        ).NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                });

            return document.WithSyntaxRoot(newDoc);
        }

        private void MoveInitialTrivia(ref FieldDeclarationSyntax newField, ref MemberDeclarationSyntax newDeclaration)
        {
            var leadingTrivia = newDeclaration.GetLeadingTrivia();

            if (!leadingTrivia.Any())
                return;

            var last = leadingTrivia.Last();

            if (leadingTrivia.Any() && (SyntaxKind)last.RawKind == SyntaxKind.WhitespaceTrivia)
            {
                newField = newField.WithLeadingTrivia(leadingTrivia);

                newDeclaration = newDeclaration.WithLeadingTrivia(last);
            }
        }

        private InvocationExpressionSyntax GetNewBody(string name, List<ParameterSyntax> parameterList)
        {
            var member = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(name),
                SyntaxFactory.IdentifierName("Evaluate"));

            var parameters = parameterList.Select(p => p.Identifier.ToString() == "@this"
            ? SyntaxFactory.Argument(SyntaxFactory.ThisExpression())
            : SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier)));

            return SyntaxFactory.InvocationExpression(member,
                SyntaxFactory.ArgumentList().AddArguments(parameters.ToArray()))
                .NormalizeWhitespace();
        }

        public static GenericNameSyntax GetExpressionTypeSyntax(List<ParameterSyntax> parameterList, TypeSyntax returnType)
        {
            var funcType = SyntaxFactory.GenericName(SyntaxFactory.Identifier("Func"),
                SyntaxFactory.TypeArgumentList().AddArguments(parameterList.Select(a => a.Type).Concat(new[] { returnType }).ToArray()));

            var expressionType = SyntaxFactory.GenericName(SyntaxFactory.Identifier("Expression"),
                SyntaxFactory.TypeArgumentList().AddArguments(funcType));
            return expressionType;
        }

        public static TypeSyntax GetReturnType(MemberDeclarationSyntax declaration)
        {
            return declaration is MethodDeclarationSyntax ?
               ((MethodDeclarationSyntax)declaration).ReturnType :
               ((PropertyDeclarationSyntax)declaration).Type;
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

        private FieldDeclarationSyntax GetStaticField(string name, List<ParameterSyntax> parameterList, TypeSyntax returType, ExpressionSyntax bodyExpression,  SemanticModel sm, ITypeSymbol type)
        {
            GenericNameSyntax expressionType = GetExpressionTypeSyntax(parameterList, returType);

            ExpressionSyntax newBody = bodyExpression;
            if (parameterList.Count > 0 & parameterList[0].Identifier.ToString() == "@this")
            {
                newBody = AddImplicitThis(newBody, sm, type);

                newBody = newBody.ReplaceNodes(newBody.DescendantNodes().OfType<ThisExpressionSyntax>().ToList(), 
                    (thisExp, _) => SyntaxFactory.IdentifierName(parameterList[0].Identifier));
            }

            if (newBody.Span.Length > 80)
                newBody = newBody.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            var parameters = parameterList.Select(p => SyntaxFactory.Parameter(p.Identifier)).ToArray();

            var lambda = parameters.Length == 1 ?
                (LambdaExpressionSyntax)SyntaxFactory.SimpleLambdaExpression(parameters.Single(), newBody) :
                (LambdaExpressionSyntax)SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.ParameterList().AddParameters(parameters), newBody);

            var variable = SyntaxFactory.VariableDeclaration(expressionType).AddVariables(
                    SyntaxFactory.VariableDeclarator(name).WithInitializer(
                        SyntaxFactory.EqualsValueClause(lambda)));

            return SyntaxFactory.FieldDeclaration(variable).WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .NormalizeWhitespace()
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
        }

        private ExpressionSyntax AddImplicitThis(ExpressionSyntax bodyExpression, SemanticModel sm, ITypeSymbol type)
        {
            var candidates =
                (from ident in bodyExpression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                 where ident.Parent.Kind() != SyntaxKind.SimpleMemberAccessExpression || ((MemberAccessExpressionSyntax)ident.Parent).Expression == ident
                 let symbol = sm.GetSymbolInfo(ident).Symbol
                 where !symbol.IsStatic && (symbol.Kind == SymbolKind.Method || symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Field)
                 where type.GetInheritedMembers().Contains(symbol)
                 select ident).ToList();

            var result = bodyExpression.ReplaceNodes(candidates, (ident, _) => SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(), ident));

            return result;
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