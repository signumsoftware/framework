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
    public class AutoPropertyAnalyzer : DiagnosticAnalyzer
    {
        public static readonly string DiagnosticId = "SF0001";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, 
            "Use auto-properties in entities",
            "Property '{0}' could be transformed to auto-property", "Entities", 
            DiagnosticSeverity.Warning, 
            isEnabledByDefault: true,
            description: "Entities with auto-properties will be converted by a Signum.MsBuildTask into the expanded pattern");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzePropertySymbol, SyntaxKind.PropertyDeclaration);
        }


        static void AnalyzePropertySymbol(SyntaxNodeAnalysisContext context)
        {
            var property = (PropertyDeclarationSyntax)context.Node;
            var classParent = property.Parent as ClassDeclarationSyntax;
            if (classParent == null)
                return;

            FieldDeclarationSyntax field = PreviosField(classParent, property);
            if (field == null)
                return;

            var getter = property.AccessorList.Accessors.SingleOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
            if (!IsValidGetter(getter, field))
                return;

            var setter = property.AccessorList.Accessors.SingleOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
            if (!IsValidSetter(setter, field))
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(classParent);

            if (InheritsFrom(symbol, "Signum.Entities.ModifiableEntity"))
            {
                var diagnostic = Diagnostic.Create(Rule, property.Identifier.GetLocation(), property.Identifier.ToString());

                context.ReportDiagnostic(diagnostic);
            }
        }

        public static FieldDeclarationSyntax PreviosField(ClassDeclarationSyntax classParent, PropertyDeclarationSyntax property)
        {
            var index = classParent.Members.IndexOf(property);
            if (index == -1 || index == 0)
                return null;

            return classParent.Members[index - 1] as FieldDeclarationSyntax;
        }

        static bool IsValidGetter(AccessorDeclarationSyntax getter, FieldDeclarationSyntax field)
        {
            var exp = (getter.Body?.Statements.Only() as ReturnStatementSyntax)?.Expression;

            if (exp == null)
                return false;

            if (RemoveThis((exp as InvocationExpressionSyntax)?.Expression)?.Identifier.ToFullString() == "Get")
                exp = ((InvocationExpressionSyntax)exp).ArgumentList.Arguments.FirstOrDefault()?.Expression;
            
            return RemoveThis(exp)?.Identifier.Text == field.Declaration.Variables.Only()?.Identifier.Text;
        }

        private static IdentifierNameSyntax RemoveThis(ExpressionSyntax exp)
        {
            if (((exp as MemberAccessExpressionSyntax)?.Expression is ThisExpressionSyntax))
                exp = (exp as MemberAccessExpressionSyntax).Name;
            return exp as IdentifierNameSyntax;
        }

        static bool IsValidSetter(AccessorDeclarationSyntax setter, FieldDeclarationSyntax field)
        {
            var exp = (setter.Body?.Statements.Only() as ExpressionStatementSyntax)?.Expression;

            if (exp == null)
                return false;

            var inv = exp as InvocationExpressionSyntax;
            if (inv == null)
                return false;

            var methodName = RemoveThis(inv.Expression)?.Identifier.ToFullString();
            if (methodName != "Set" && methodName != "SetToStr")
                return false;

            var firsArg = ((InvocationExpressionSyntax)exp).ArgumentList.Arguments.FirstOrDefault()?.Expression;

            return RemoveThis(firsArg)?.Identifier.Text == field.Declaration.Variables.Only()?.Identifier.Text;
        }

        static bool InheritsFrom(INamedTypeSymbol symbol, string fullName)
        {
            while (true)
            {
                if (symbol.ToString() == fullName)
                    return true;

                if (symbol.BaseType == null)
                    return false;

                symbol = symbol.BaseType;
            }
        }
    }

    public static class Extensions
    {
        public static T Only<T>(this IEnumerable<T> collection) where T : class
        {
            if (collection.Count() != 1)
                return null;

            return collection.Single();
        }
    }
}
