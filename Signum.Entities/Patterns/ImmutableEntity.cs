using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities
{
    [Serializable]
    public class ImmutableEntity: IdentifiableEntity
    {
        bool allowChange;
        public bool AllowChange
        {
            get { return allowChange; }
            set { allowChange = value; }
        }

        protected override bool Set<T>(ref T variable, T value, string propertyName)
        {
            if (allowChange)
                return base.Set(ref variable, value, propertyName);
            else
                return base.SetIfNew(ref variable, value, propertyName);
        }

        protected internal override void PreSaving()
        {
            if (!IsNew && !AllowChange && SelfModified)
                throw new ApplicationException("Attempt to save a not new modified ImmutableEntity");

            if (IsNew)
                base.PreSaving(); //Do not re-update
        }
    }

}
