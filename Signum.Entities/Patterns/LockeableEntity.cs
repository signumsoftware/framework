using System;
using System.Collections.Generic;
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

        protected bool UnsafeSet<T>(ref T field, T value, [CallerMemberNameAttribute]string? automaticPropertyName = null)
        {
            return base.Set<T>(ref field, value, automaticPropertyName);
        }

        protected virtual void ItemLockedChanged(bool locked)
        {
        }

        protected override bool Set<T>(ref T field, T value, [CallerMemberNameAttribute]string? automaticPropertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (this.locked)
                throw new ApplicationException(EntityMessage.AttemptToSet0InLockedEntity1.NiceToString(this.GetType().GetProperty(automaticPropertyName, flags).NiceName(), this.ToString()));

            return base.Set<T>(ref field, value, automaticPropertyName);
        }

        protected override void ChildCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            if (this.locked)
                throw new ApplicationException(EntityMessage.AttemptToAddRemove0InLockedEntity1.NiceToString(sender.GetType().ElementType().NicePluralName(), this.ToString()));

            base.ChildCollectionChanged(sender, args);
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
        [Description("Attempt to set {0} in locked entity {1}")]
        AttemptToSet0InLockedEntity1,
        [Description("Attempt to add/remove {0} in locked entity {1}")]
        AttemptToAddRemove0InLockedEntity1
    }
}
