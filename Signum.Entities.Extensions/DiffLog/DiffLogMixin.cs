using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.DiffLog
{
    [Serializable]
    public class DiffLogMixin : MixinEntity
    {
        protected DiffLogMixin(IdentifiableEntity mainEntity, MixinEntity next)
            : base(mainEntity, next)
        {
        }

        [SqlDbType(Size = int.MaxValue)]
        string startGraph;
        public string StartGraph
        {
            get { return startGraph; }
            set { Set(ref startGraph, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string endGraph;
        public string EndGraph
        {
            get { return endGraph; }
            set { Set(ref endGraph, value); }
        }
    }

    public enum DiffLogMessage
    {
        PreviousLog,
        NextLog,
        CurrentEntity,
    }
}
