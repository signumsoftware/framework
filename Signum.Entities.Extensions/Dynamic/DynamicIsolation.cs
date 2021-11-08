using Signum.Entities.Isolation;

namespace Signum.Entities.Dynamic
{
    public class DynamicIsolationMixin : MixinEntity
    {
        DynamicIsolationMixin(ModifiableEntity mainEntity, MixinEntity? next)
            : base(mainEntity, next)
        {
        }

        public IsolationStrategy IsolationStrategy { get; set; } = IsolationStrategy.None;
        
        protected override void CopyFrom(MixinEntity mixin, object[] args)
        {
            this.IsolationStrategy = ((DynamicIsolationMixin)mixin).IsolationStrategy;
        }
    }
}
