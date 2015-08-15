using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Signum.Analyzer
{
    public static class Extensions
    {
        public static T Only<T>(this IEnumerable<T> collection) where T : class
        {
            if (collection.Count() != 1)
                return null;

            return collection.Single();
        }

        public static QualifiedNameSyntax Qualified(this NameSyntax left, string ident)
        {
            return SyntaxFactory.QualifiedName(left, SyntaxFactory.IdentifierName(ident));
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetInheritedMembers(this ITypeSymbol containingType)
        {
            return containingType.GetBaseTypesAndThis().SelectMany(x => x.GetMembers());
        }
    }
}
