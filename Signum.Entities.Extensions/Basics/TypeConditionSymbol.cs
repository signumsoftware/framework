
namespace Signum.Entities.Basics;

public class TypeConditionSymbol : Symbol
{
    private TypeConditionSymbol() { }

    public TypeConditionSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}
