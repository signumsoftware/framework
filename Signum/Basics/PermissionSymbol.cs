namespace Signum.Basics;

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class PermissionSymbol : Symbol
{
    private PermissionSymbol() { }

    public PermissionSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}
