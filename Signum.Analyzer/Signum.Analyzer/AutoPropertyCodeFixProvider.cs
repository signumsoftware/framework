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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoPropertyCodeFixProvider)), Shared]
    public class AutoPropertyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AutoPropertyAnalyzer.DiagnosticId); }
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
            
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();
            
            context.RegisterCodeFix(
                CodeAction.Create("Convert to auto-property", c => MakeUppercaseAsync(context.Document, declaration, c)),
                diagnostic);
        }

        private async Task<Solution> MakeUppercaseAsync(Document document, PropertyDeclarationSyntax property, CancellationToken cancellationToken)
        {
            var classParent = (ClassDeclarationSyntax)property.Parent;

            var field = AutoPropertyAnalyzer.PreviosField(classParent, property);

            var modifiers = property.Modifiers;
            if(field.AttributeLists.Count == 0)
            {
                modifiers = modifiers.Replace(
                    modifiers.First(),
                    modifiers.First().WithLeadingTrivia(field.DescendantTokens().First().LeadingTrivia)); 
            }   

            var newProperty = SyntaxFactory.PropertyDeclaration(
                new SyntaxList<AttributeListSyntax>().AddRange(field.AttributeLists).AddRange(property.AttributeLists),
                modifiers,
                property.Type,
                null,
                property.Identifier,
                SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                })));
            
            var members = classParent.Members.Replace(property, newProperty);
            members = members.Remove(members.Single(m => m.IsEquivalentTo(field)));
            var newClass = classParent.WithMembers(members);

            var fieldName = field.Declaration.Variables.Single().Identifier.ToString();

            newClass = newClass.ReplaceNodes(
                newClass.DescendantNodes().OfType<IdentifierNameSyntax>().Where(a => a.Identifier.ToString() == fieldName),
                (a, b)=> a.WithIdentifier(SyntaxFactory.Identifier(a.GetLeadingTrivia(), SyntaxKind.IdentifierToken, property.Identifier.Text, property.Identifier.Text, a.GetTrailingTrivia())));

            var docNode = await document.GetSyntaxRootAsync(cancellationToken);
            docNode = docNode.ReplaceNode(classParent, newClass);
            
            docNode = Formatter.Format(docNode, document.Project.Solution.Workspace);
            var originalSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, docNode);
         
            return originalSolution;
        }
    }
}