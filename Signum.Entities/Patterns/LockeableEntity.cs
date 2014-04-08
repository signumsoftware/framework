using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.ComponentModel;
using Signum.Utilities;
using System.Runtime.CompilerServices;

namespace Signum.Entities.Patterns
{
    [Serializable]
    public abstract class LockableEntity : Entity
    {
        bool locked;
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (UnsafeSet(ref locked, value))
                    ItemLockedChanged(Locked);
            }
        }

        protected bool UnsafeSet<T>(ref T field, T value, [CallerMemberNameAttribute]string automaticPropertyName = null)
        {
            return base.Set<T>(ref field, value, automaticPropertyName);
        }

        protected virtual void ItemLockedChanged(bool locked)
        {
        }

        protected override bool Set<T>(ref T field, T value, [CallerMemberNameAttribute]string automaticPropertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (this.locked)
                throw new ApplicationException(EntityMessage.AttemptToModifyLockedEntity0.NiceToString().Formato(this.ToString()));

            return base.Set<T>(ref field, value, automaticPropertyName);
        }

        public IDisposable AllowTemporalUnlock()
        {
            bool old = this.locked;
            this.locked = false;
            return new Disposable(() => this.locked = old);
        }
    }

    public enum EntityMessage
    {
        [Description("Attempt to modify locked entity {0}")]
        AttemptToModifyLockedEntity0
    }
}
