using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reports;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Engine.Maps;

namespace Signum.Engine.Extensions.Reports
{
    public static class ReportsLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool excelReport, bool compositeReport)
        {
            if (excelReport)
            {
                sb.Include<ExcelReportDN>();
                dqm[typeof(ExcelReportDN)] = (from s in Database.Query<ExcelReportDN>()
                                             select new
                                             {
                                                 Entity = s.ToLazy(),
                                                 s.Id,
                                                 s.QueryName,
                                                 s.File.FileName,
                                                 s.DisplayName,
                                                 s.Deleted,
                                             }).ToDynamic();
                if (compositeReport)
                {
                    sb.Include<CompositeReportDN>();
                    dqm[typeof(CompositeReportDN)] = (from e in Database.Query<CompositeReportDN>()
                                                      select new
                                                      {
                                                          Entity = e.ToLazy(),
                                                          e.Id,
                                                          Nombre = e.Name,
                                                          Reports = e.ExcelReports.Count(),
                                                      }).ToDynamic();
                }

            }
            else if (compositeReport)
                throw new InvalidOperationException("ExcelReport is necessary for CompositeReports");
        }

    }
}
