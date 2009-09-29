using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities
{
    [Serializable]
    public class ImmutableEntity: IdentifiableEntity
    {
        bool allowTemporaly = false;
        public bool AllowChange
        {
            get { return allowTemporaly || IsNew; }
            set { allowTemporaly = value; Notify("AllowChange"); }
        }

        protected override bool Set<T>(ref T variable, T value, string propertyName)
        {
            if (AllowChange)
                return base.Set(ref variable, value, propertyName);
            else
                return base.SetIfNew(ref variable, value, propertyName);
        }

        protected internal override void PreSaving()
        {
            if (AllowChange)
                base.PreSaving();
            else
                if (SelfModified)
                    throw new ApplicationException("Attempt to save a not new modified ImmutableEntity");
        }
    }

}
