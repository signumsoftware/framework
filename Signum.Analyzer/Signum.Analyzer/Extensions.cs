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

        public static bool IsLite(this TypeInfo e)
        {
            if (e.Type is INamedTypeSymbol namedSymbol && namedSymbol.MetadataName == "Lite`1" && namedSymbol.ContainingNamespace.ToString() == "Signum.Entities")
            {
                return true;
            }
            return false;
        }

        public static INamedTypeSymbol GetLiteEntityType(this TypeInfo e)
        {
            var namedSymbol = e.Type as INamedTypeSymbol;
            
            return namedSymbol?.TypeArguments.FirstOrDefault() as INamedTypeSymbol; 
        }

        public static bool IsEntity(this TypeInfo e)
        {
            if (e.Type is INamedTypeSymbol namedSymbol && GetBaseTypesAndThis(namedSymbol).Any(t => t.Name == "Entity" && t.ContainingNamespace.ToString() == "Signum.Entities"))
            {
                return true;
            }
            return false;
        }
    }
}
