
namespace Signum.Scheduler;

[EntityKind(EntityKind.SystemString, EntityData.Master)]
public class SimpleTaskSymbol : Symbol, ITaskEntity
{
    private SimpleTaskSymbol() { }

    public SimpleTaskSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}
