using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Collections.ObjectModel;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using System.Diagnostics;

namespace Signum.Entities
{
    [Serializable]
    public abstract class Modifiable 
    {
        [Ignore, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ModifiedState modified;
       
        [HiddenProperty]
        public ModifiedState Modified
        {
            get { return modified; }
            protected internal set
            {
                if (modified == ModifiedState.Sealed)
                    throw new InvalidOperationException("The instance {0} is sealed and can not be modified".FormatWith(this));

                modified = value;
            }
        }

        public void SetCleanModified(bool isSealed)
        {
            Modified = isSealed ? ModifiedState.Sealed : ModifiedState.Clean;
        }

        /// <summary>
        /// True if SelfModified or (saving) and Modified
        /// </summary>
        [HiddenProperty]
        public bool IsGraphModified
        {
            get { return Modified == ModifiedState.Modified || Modified == ModifiedState.SelfModified; }
        }

        protected internal virtual void SetSelfModified()
        {
            Modified = ModifiedState.SelfModified;
        }

        protected internal virtual void PreSaving(ref bool graphModified)
        {
        }

        protected internal virtual void PostRetrieving()
        {
        }
    }

    public enum ModifiedState
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
