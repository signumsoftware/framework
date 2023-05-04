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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LiteEqualityCodeFixProvider)), Shared]
public class LiteEqualityCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return ImmutableArray.Create(
            LiteEqualityAnalyzer.RuleEqualsLite.Id,
            LiteEqualityAnalyzer.RuleEqualsEntity.Id,
            LiteEqualityAnalyzer.RuleLiteEntity.Id, 
            LiteEqualityAnalyzer.RuleEntityTypes.Id); }
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

        var equals = root.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First(a => a.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression);

        context.RegisterCodeFix(
            CodeAction.Create("Use '.Is() extension method", c => UseIsExtensionMethod(context.Document, equals, c), "UseIsExtensionMethod"),
            diagnostic);
    }

    private async Task<Document> UseIsExtensionMethod(Document document, BinaryExpressionSyntax equals, CancellationToken c)
    {
        var isCall =
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                equals.Left.WithoutTrivia(),
                SyntaxFactory.IdentifierName("Is")),
                   SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                        SyntaxFactory.Argument(
                            equals.Right.WithoutTrivia()))));

        var docRoot = await document.GetSyntaxRootAsync(c);

        var newRoot = docRoot.ReplaceNode(equals,
            equals.Kind() == SyntaxKind.NotEqualsExpression ?
            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, isCall) : isCall);

        return document.WithSyntaxRoot(newRoot);
    }

  
}

