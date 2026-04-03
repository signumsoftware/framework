using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Signum.Authorization.Rules;
internal class TypeConditionSetComparer : IEqualityComparer<FrozenSet<TypeConditionSymbol>>
{
    private TypeConditionSetComparer() { }

    public static readonly TypeConditionSetComparer Instance = new TypeConditionSetComparer();

    public bool Equals(FrozenSet<TypeConditionSymbol>? x, FrozenSet<TypeConditionSymbol>? y)
    {
        return x == null && y == null || x != null && y != null && x.SetEquals(y);
    }

    public int GetHashCode([DisallowNull] FrozenSet<TypeConditionSymbol> obj)
    {
        var hash = 17;
        foreach (var rule in obj)
            hash = hash * 31 + rule.GetHashCode();

        return hash;
    }
}
