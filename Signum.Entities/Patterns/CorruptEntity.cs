using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities
{
    [Serializable] // Just a pattern
    public class CorruptEntity : Entity
    {
        bool corrupt;
        public bool Corrupt
        {
            get { return corrupt; }
            set { Set(ref corrupt, value, () => Corrupt); }
        }

        protected internal override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            if (Corrupt)
            {
                string integrity = base.IdentifiableIntegrityCheck(); // So, no corruption allowed
                if (string.IsNullOrEmpty(integrity))
                    this.Corrupt = false;
            }
        }

        public override string IdentifiableIntegrityCheck()
        {
            using (Corrupt ? Corruption.Allow() : null)
            {
                return base.IdentifiableIntegrityCheck();
            }
        }
    }

    public static class Corruption
    {
        static readonly IVariable<bool> allowed = Statics.ThreadVariable<bool>("corruptionAllowed");

        public static bool Strict { get { return !allowed.Value; } }

        public static IDisposable Allow()
        {
            if (allowed.Value) return null;
            allowed.Value = true;
            return new Disposable(() => allowed.Value = false);
        }

        public static IDisposable Deny()
        {
            if (!allowed.Value) return null;
            allowed.Value = false;
            return new Disposable(() => allowed.Value = true);
        }
    }

}
