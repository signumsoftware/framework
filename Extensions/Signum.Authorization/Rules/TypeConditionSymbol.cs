namespace Signum.Authorization.Rules;

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class TypeConditionSymbol : Symbol
{
    private TypeConditionSymbol() { }

    public TypeConditionSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}
