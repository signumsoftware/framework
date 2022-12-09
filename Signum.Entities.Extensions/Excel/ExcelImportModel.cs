using Signum.Entities.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Excel;


public class ExcelImportModel : ModelEntity
{
    public FileEmbedded ExcelFile { get; set; }

}
