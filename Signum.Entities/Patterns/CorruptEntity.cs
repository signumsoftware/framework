using System;
using System.Collections.Generic;
using Signum.Utilities;

namespace Signum.Entities
{
    [Serializable]
    public class CorruptMixin : MixinEntity
    {
        CorruptMixin(ModifiableEntity mainEntity, MixinEntity next) : base(mainEntity, next) 
        {
            if (!(mainEntity is Entity))
                throw new InvalidOperationException("mainEntity should be an Entity");
        }

        public bool Corrupt { get; set; }

        protected internal override void PreSaving(PreSavingContext ctx)
        {
            base.PreSaving(ctx);

            if (Corrupt)
            {
                var integrity = ((Entity)MainEntity).EntityIntegrityCheckBase(); // So, no corruption allowed
                if (integrity == null)
                {
                    this.Corrupt = false;
                    if (!((Entity)MainEntity).IsNew)
                        Corruption.OnCorruptionRemoved((Entity)MainEntity);
                }
                else if (((Entity)MainEntity).IsNew)
                    Corruption.OnSaveCorrupted((Entity)MainEntity, integrity);
            }
        }
    }

    public static class Corruption
    {
        static readonly Variable<bool> allowed = Statics.ThreadVariable<bool>("corruptionAllowed");

        public static bool Strict { get { return !allowed.Value; } }

        public static IDisposable? AllowScope()
        {
            if (allowed.Value)
                return null;
            allowed.Value = true;
            return new Disposable(() => allowed.Value = false);
        }

        public static IDisposable? DenyScope()
        {
            if (!allowed.Value)
                return null;
            allowed.Value = false;
            return new Disposable(() => allowed.Value = true);
        }

        public static event Action<Entity, Dictionary<Guid, IntegrityCheck>>? SaveCorrupted;

        public static void OnSaveCorrupted(Entity corruptEntity, Dictionary<Guid, IntegrityCheck> integrity)
        {
            SaveCorrupted?.Invoke(corruptEntity, integrity);
        }

        public static event Action<Entity>? CorruptionRemoved;

        public static void OnCorruptionRemoved(Entity corruptEntity)
        {
            CorruptionRemoved?.Invoke(corruptEntity);
        }
    }

}
