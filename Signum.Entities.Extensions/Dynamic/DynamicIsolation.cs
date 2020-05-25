using Signum.Entities;
using Signum.Entities.Isolation;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Signum.Entities.Dynamic
{
    [Serializable]
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
