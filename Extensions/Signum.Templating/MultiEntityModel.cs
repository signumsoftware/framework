namespace Signum.Templating;

public class MultiEntityModel : ModelEntity
{
    [ImplementedByAll]
    [NoRepeatValidator]
    public MList<Lite<Entity>> Entities { get; set; } = new MList<Lite<Entity>>();
}
