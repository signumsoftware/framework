using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Extensions.Properties;
using Signum.Utilities;

namespace Signum.Entities.Reports
{
    [Serializable]
    public class CompositeReportDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 200)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value, () => Name); }
        }
        
        MList<Lite<ExcelReportDN>> excelReports;
        public MList<Lite<ExcelReportDN>> ExcelReports
        {
            get { return excelReports; }
            set { Set(ref excelReports, value, () => ExcelReports); }
        }

        public override string ToString()
        {
            return name ;
        }
    }
}
