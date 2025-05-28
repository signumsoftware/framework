
namespace Signum.Templating;

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class ModelConverterSymbol : Symbol
{
    private ModelConverterSymbol() { }

    public ModelConverterSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}
