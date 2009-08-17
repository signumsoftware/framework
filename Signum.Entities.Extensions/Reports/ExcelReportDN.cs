using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Extensions.Properties;
using Signum.Utilities;
using Signum.Entities.Files;

namespace Signum.Entities.Reports
{
    [Serializable, LocDescription]
    public class ExcelReportDN : IdentifiableEntity
    {
        [NotNullable]
        string queryName;
        [StringLengthValidator(Min = 3), LocDescription]
        public string QueryName
        {
            get { return queryName; }
            set { Set(ref queryName, value, "QueryName"); }
        }

        [NotNullable]
        string displayName;
        [StringLengthValidator(Min = 3), LocDescription]
        public string DisplayName
        {
            get { return displayName; }
            set { SetToStr(ref displayName, value, "DisplayName"); }
        }

        EmbeddedFileDN file;
        [NotNullValidator, LocDescription]
        public EmbeddedFileDN File
        {
            get { return file; }
            set { Set(ref file, value, "File"); }
        }

        bool deleted;
        [LocDescription]
        public bool Deleted
        {
            get { return deleted; }
            set { Set(ref deleted, value, "Deleted"); }
        }

        public override string ToString()
        {
            return displayName + (deleted ? Resources.Deleted : "");
        }
    }
}
