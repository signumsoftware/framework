using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.ComponentModel;
using Signum.Utilities;

namespace Signum.Entities
{
    [Serializable]
    public class ImmutableEntity : IdentifiableEntity
    {
        [Ignore]
        bool allowTemporaly = false;

        public bool AllowChange
        {
            get { return allowTemporaly || IsNew; }
            set { allowTemporaly = value; Notify(() => AllowChange); }
        }

        protected override bool Set<T>(ref T variable, T value, Expression<Func<T>> property)
        {
            if (AllowChange)
                return base.Set(ref variable, value, property);
            else
                return base.SetIfNew(ref variable, value, property);
        }

        protected internal override void PreSaving(ref bool graphModified)
        {
            if (AllowChange)
                base.PreSaving(ref graphModified);
            else
                if (Modified == ModifiedState.SelfModified)
                    throw new InvalidOperationException("Attempt to save a not new modified ImmutableEntity");
        }

        public IDisposable AllowChanges()
        {
            bool old = this.AllowChange;
            this.AllowChange = true;
            return new Disposable(() => this.AllowChange = old);
        }
    }

}
