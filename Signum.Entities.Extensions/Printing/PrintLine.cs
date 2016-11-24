using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Processes;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Entities.Files;

namespace Signum.Entities.Printing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class PrintLineEntity : Entity, IProcessLineDataEntity
    {
        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        public EmbeddedFilePathEntity File { get; set; }

        public Lite<PrintPackageEntity> Package { get; set; }

        public DateTime? PrintedOn { get; set; }

        [ImplementedBy()]
        public Lite<Entity> Referred { get; set; }

        public Lite<ExceptionEntity> Exception { get; set; }
    }

}
