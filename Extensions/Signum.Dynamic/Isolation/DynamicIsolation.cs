using Signum.Isolation;

namespace Signum.Dynamic.Isolation;

public class DynamicIsolationMixin : MixinEntity
{
    DynamicIsolationMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    public IsolationStrategy IsolationStrategy { get; set; } = IsolationStrategy.None;

    protected override void CopyFrom(MixinEntity mixin, object[] args)
    {
        IsolationStrategy = ((DynamicIsolationMixin)mixin).IsolationStrategy;
    }
}
