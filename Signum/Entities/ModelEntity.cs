namespace Signum.Entities;

public abstract class ModelEntity : ModifiableEntity, IRootEntity
{
    protected internal override void PreSaving(PreSavingContext ctx)
    {

    }

    protected internal override void PostRetrieving(PostRetrievingContext ctx)
    {
        throw new InvalidOperationException("ModelEntities are not meant to be retrieved");
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => this.GetType().NiceName());
}
