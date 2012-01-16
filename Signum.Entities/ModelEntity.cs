using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities
{
    [Serializable]
    public abstract class ModelEntity : EmbeddedEntity, IRootEntity
    {
        protected internal override void PreSaving(ref bool graphModified)
        {
        
        }

        protected internal override void PostRetrieving()
        {
            throw new InvalidOperationException("ModelEntities are not meant to be retrieved"); 
        }
    }
}
