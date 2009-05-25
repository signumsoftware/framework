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
        protected bool modified = false;

        [DoNotValidate]
        public bool Modified
        {
            get { return modified || SelfModified; }
            set
            {
                if (value)
                    modified = true;
                else
                {
                    SelfModified = false;
                    modified = false;
                }
            }
        }

        [DoNotValidate]
        public abstract bool SelfModified { get; internal set; }

        public virtual string IntegrityCheck()
        {
            return null;
        }

        public string FullIntegrityCheck()
        {
            return GraphExplorer.GraphIntegrityCheck(this, ModifyInspector.FullExplore);
        }

        public Dictionary<Modifiable, string> FullIntegrityCheckDictionary()
        {
            return GraphExplorer.GraphIntegrityCheckDictionary(this, ModifyInspector.FullExplore);
        }

        protected internal virtual void PreSaving()
        {
        }

        protected internal virtual void PostRetrieving()
        {
        }
    }
}
