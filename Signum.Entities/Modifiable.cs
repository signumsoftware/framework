using System;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System.Diagnostics;
using System.Collections.Generic;

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

        public virtual void SetSelfModified()
        {
            Modified = ModifiedState.SelfModified;
        }

        protected internal virtual void PreSaving(PreSavingContext ctx)
        {
        }

        protected internal virtual void PostRetrieving(PostRetrievingContext ctx)
        {
        }
    }

    public class PreSavingContext
    {
        internal PreSavingContext(DirectedGraph<Modifiable> graph)
        {
            this.Graph = graph;
        }

        public DirectedGraph<Modifiable> Graph { get; private set; }
        public bool IsGraphInvalidated { get; private set; }
        public void InvalidateGraph()
        {
            this.IsGraphInvalidated = true;
        }
    }

    public class PostRetrievingContext
    {
        public Dictionary<Modifiable, ModifiedState> ForceModifiedState = new Dictionary<Modifiable, ModifiedState>();
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
