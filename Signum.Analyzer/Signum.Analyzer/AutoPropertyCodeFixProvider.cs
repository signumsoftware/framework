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
            
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create("Convert to auto-property", c => AutoPropertyFixer.FixAllProperties(context.Document, declaration, c), "Convert to auto-property"),
                diagnostic);

            //context.RegisterCodeFix(
            //    CodeAction.Create("Convert to auto-property", c => AutoPropertyFixer.FixProperty(context.Document, declaration, c), "Convert to auto-property"),
            //    diagnostic);
        }
    }



    public static class AutoPropertyFixer
    {
        public static async Task<Solution> FixAllProperties(Document document, ClassDeclarationSyntax parentClass, CancellationToken cancellationToken)
        {
            var currentSolution = document.Project.Solution;
            var currentDocument = document;
            var currentClass = parentClass;

            while(true)
            {
                var property = currentClass.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => AutoPropertyAnalyzer.IsSimpleProperty(p, currentClass));
                if (property == null)
                    return currentSolution;
                try {
                    currentSolution = await FixProperty(currentDocument, property, cancellationToken);
                    currentDocument = currentSolution.GetDocument(currentDocument.Id);
                    currentClass = (await currentDocument.GetSyntaxRootAsync()).DescendantNodes().OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault(a => a.Identifier.ToString() == currentClass.Identifier.ToString());
                }
                catch (Exception)
                {
                    return currentSolution;
                }
            }
        }

        public static async Task<Solution> FixProperty(Document document, PropertyDeclarationSyntax property, CancellationToken cancellationToken)
        {
            var classParent = (ClassDeclarationSyntax)property.Parent;

            var field = AutoPropertyAnalyzer.PreviosField(classParent, property);
            var fieldVariable = field.Declaration.Variables.Single();

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetDeclaredSymbol(fieldVariable);
            var solution = document.Project.Solution;
            solution = await Renamer.RenameSymbolAsync(solution, symbol, property.Identifier.ToString(), solution.Workspace.Options, cancellationToken);

            document = solution.GetDocument(document.Id);
            var root = await document.GetSyntaxRootAsync();
            classParent = root.DescendantNodes().OfType<ClassDeclarationSyntax>().SingleOrDefault(c => c.Identifier.IsEquivalentTo(classParent.Identifier));
            field = classParent.Members.OfType<FieldDeclarationSyntax>().SingleOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.ToString() == property.Identifier.ToString()));
            fieldVariable = field.Declaration.Variables.Single();

            var oldProperty = classParent.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(a => a.Identifier.ToString() == property.Identifier.ToString());

            var newProperty = SyntaxFactory.PropertyDeclaration(
                new SyntaxList<AttributeListSyntax>().AddRange(field.AttributeLists).AddRange(oldProperty.AttributeLists),
                oldProperty.Modifiers,
                oldProperty.Type,
                null,
                oldProperty.Identifier,
                SyntaxFactory.AccessorList(SyntaxFactory.List(
                    property.AccessorList.Accessors.Select(a => a.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                )));
            
            var leading = field.DescendantTokens().First().LeadingTrivia;

            var first = newProperty.DescendantTokens().First();
            newProperty = newProperty.ReplaceToken(first, first.WithLeadingTrivia(leading));


            if (fieldVariable.Initializer != null)
                newProperty = newProperty.WithInitializer(fieldVariable.Initializer).WithSemicolonToken(field.SemicolonToken);

            var members = classParent.Members.Replace(oldProperty, newProperty);
            members = members.Remove(members.Single(m => m.IsEquivalentTo(field)));
            var newClass = classParent.WithMembers(members);

            var docNode = await document.GetSyntaxRootAsync(cancellationToken);
            docNode = docNode.ReplaceNode(classParent, newClass);

            docNode = Formatter.Format(docNode, solution.Workspace);
            var resultSolution = solution.WithDocumentSyntaxRoot(document.Id, docNode);

            return resultSolution;
        }
    }
}