using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.Reports
{
    [Serializable]
    public class CompositeReportDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 200)]
        string nombre;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 200)]
        public string Nombre
        {
            get { return nombre; }
            set { Set(ref nombre, value, "Nombre"); }
        }
        
        MList<Lazy<ExcelReportDN>> excelReports;
        public MList<Lazy<ExcelReportDN>> ExcelReports
        {
            get { return excelReports; }
            set { Set(ref excelReports, value, "Excelreports"); }
        }

        public override string ToString()
        {
            return nombre ;
        }
    }
}
