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
        ModifiableState modified;
       
        [HiddenProperty]
        public ModifiableState Modified
        {
            get { return modified; }
            internal set
            {
                if(modified == ModifiableState.Sealed)
                    throw new InvalidOperationException("The instance {0} is sealed and can not be modified".Formato(this));

                modified = value;
            }
        }

        /// <summary>
        /// True if SelfModified or (saving) and Modified
        /// </summary>
        public bool IsGraphModified
        {
            get { return Modified == ModifiableState.Modified || Modified == ModifiableState.SelfModified; }
        }

        protected internal virtual void SetSelfModified()
        {
            Modified = ModifiableState.SelfModified;
        }

        protected internal virtual void PreSaving(ref bool graphModified)
        {
        }

        protected internal virtual void PostRetrieving()
        {
        }
    }

    public enum ModifiableState
    {
        SelfModified,
        /// <summary>
        /// Recursively Clean (only valid during saving)
        /// </summary>
        Clean,
        /// <summary>
        /// Recursively Modified (only valid during saving)
        /// </summary>
        Modified,
        Sealed, 
    }
}
