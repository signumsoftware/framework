using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reports;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Extensions.Properties;

namespace Signum.Engine.Reports
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
                                                  Entity = s.ToLite(),
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
                                                          Entity = e.ToLite(),
                                                          e.Id,
                                                          Nombre = e.Name,
                                                          Reports = e.ExcelReports.Count(),
                                                      }).ToDynamic();
                }

            }
            else if (compositeReport)
                throw new InvalidOperationException(Resources.ExcelReportArgumentIsNecessaryForCompositeReports);
        }


        public static List<Lite<ExcelReportDN>> GetExcelReports(string queryName)
        {
            return (from er in Database.Query<ExcelReportDN>()
                    where er.QueryName == QueryUtils.GetQueryName(queryName)
                    select er.ToLite()).ToList(); 
        }
    }
}
