using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities
{
    [Serializable]
    public abstract class EmbeddedEntity : ModifiableEntity
    {
        [Ignore] //Used for JSon serialization when returning json
        public PropertyRoute FromPropertyRoute;
     
    }
}
