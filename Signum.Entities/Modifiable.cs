using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Collections.ObjectModel;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection; 

namespace Signum.Entities
{
    [Serializable]
    public abstract class Modifiable 
    {
        [Ignore]
        bool? modified;

        [HiddenProperty]
        public bool? Modified
        {
            get { return modified; }
            internal set
            {
                if (value == null)
                    CleanSelfModified();
                modified = value;
            }
        }

        [HiddenProperty]
        public abstract bool SelfModified { get; }

        protected abstract void CleanSelfModified();

        protected internal virtual void PreSaving(ref bool graphModified)
        {
        }

        protected internal virtual void PostRetrieving()
        {
        }
    }
}
