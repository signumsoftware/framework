using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities
{
    [Serializable]
    public class ImmutableEntity: IdentifiableEntity
    {
        protected override bool Set<T>(ref T variable, T value, string propertyName)
        {
            return base.SetIfNew(ref variable, value, propertyName);
        }

        protected internal override void PreSaving()
        {
            if (!IsNew && SelfModified)
                throw new ApplicationException("Attempt to save a not new modified ImmutableEntity");

            if (IsNew)
                base.PreSaving(); //Do not re-update
        }
    }
}
