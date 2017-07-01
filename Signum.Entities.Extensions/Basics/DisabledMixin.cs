using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Basics
{

    [Serializable]
    public class DisabledMixin : MixinEntity
    {
        DisabledMixin(Entity mainEntity, MixinEntity next)
            : base(mainEntity, next)
        {
        }

        public bool IsDisabled { get; set; }
        
        protected override void CopyFrom(MixinEntity mixin, object[] args)
        {
            this.IsDisabled = ((DisabledMixin)mixin).IsDisabled;
        }
    }

    [AutoInit]
    public static class DisableOperation
    {
        public static readonly ExecuteSymbol<Entity> Disable;
        public static readonly ExecuteSymbol<Entity> Enabled;
    }

    public enum DisabledMessage
    {
        ParentIsDisabled,
    }
}
