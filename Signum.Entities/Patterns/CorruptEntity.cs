using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities
{
    [Serializable]
    public class CorruptMixin : MixinEntity
    {
        CorruptMixin(Entity mainEntity, MixinEntity next) : base(mainEntity, next) { }

        public bool Corrupt { get; set; }

        protected internal override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            if (Corrupt)
            {
                var integrity = MainEntity.IdentifiableIntegrityCheckBase(); // So, no corruption allowed
                if (integrity == null)
                {
                    this.Corrupt = false;
                    if (!MainEntity.IsNew)
                        Corruption.OnCorruptionRemoved(MainEntity);
                }
                else if (MainEntity.IsNew)
                    Corruption.OnSaveCorrupted(MainEntity, integrity);
            }
        }
    }

    public static class Corruption
    {
        static readonly Variable<bool> allowed = Statics.ThreadVariable<bool>("corruptionAllowed");

        public static bool Strict { get { return !allowed.Value; } }

        public static IDisposable AllowScope()
        {
            if (allowed.Value) return null;
            allowed.Value = true;
            return new Disposable(() => allowed.Value = false);
        }

        public static IDisposable DenyScope()
        {
            if (!allowed.Value) return null;
            allowed.Value = false;
            return new Disposable(() => allowed.Value = true);
        }

        public static event Action<Entity, Dictionary<Guid, Dictionary<string, string>>> SaveCorrupted;

        public static void OnSaveCorrupted(Entity corruptEntity, Dictionary<Guid, Dictionary<string, string>> integrity)
        {
            if (SaveCorrupted != null)
                SaveCorrupted(corruptEntity, integrity);
        }

        public static event Action<Entity> CorruptionRemoved;

        public static void OnCorruptionRemoved(Entity corruptEntity)
        {
            if (CorruptionRemoved != null)
                CorruptionRemoved(corruptEntity);
        }
    }

}
