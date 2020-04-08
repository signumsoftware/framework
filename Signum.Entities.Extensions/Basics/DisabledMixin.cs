using System;

namespace Signum.Entities.Basics
{

    [Serializable]
    public class DisabledMixin : MixinEntity
    {
        DisabledMixin(ModifiableEntity mainEntity, MixinEntity next)
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
