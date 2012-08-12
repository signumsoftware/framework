using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Entities.Chart;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Utilities;
using System.IO;

namespace Signum.Engine.Chart
{
    public static class ChartScriptLogic
    {
        public static ResetLazy<List<ChartScriptDN>> Scripts = GlobalLazy.Create(() =>
            Database.Query<ChartScriptDN>().ToList())
            .InvalidateWith(typeof(ChartScriptDN));

        internal static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ChartScriptDN>();

                dqm[typeof(ChartScriptDN)] = (from uq in Database.Query<ChartScriptDN>()
                                              select new
                                              {
                                                  Entity = uq,
                                                  uq.Id,
                                                  uq.Name,
                                                  uq.GroupBy,
                                                  uq.Columns.Count,
                                                  uq.Icon,
                                              }).ToDynamic();
                
                new BasicConstructFrom<ChartScriptDN, ChartScriptDN>(ChartScriptOperations.Clone)
                {
                    Construct = (cs, _) => new ChartScriptDN
                    {
                        Name = cs.Name,
                        GroupBy = cs.GroupBy,
                        Icon = cs.Icon,
                        Columns = cs.Columns.Select(col => new ChartScriptColumnDN
                        {
                            ColumnType = col.ColumnType,
                            DisplayName = col.DisplayName,
                            IsGroupKey = col.IsGroupKey,
                            IsOptional = col.IsOptional,
                        }).ToMList(),
                        Script = cs.Script,
                    }
                }.Register();

                new BasicDelete<ChartScriptDN>(ChartScriptOperations.Delete)
                {
                    CanDelete = c => Database.Query<UserChartDN>().Any(a => a.ChartScript == c) ? "There are {0} in the database using {1}".Formato(typeof(UserChartDN).NicePluralName(), c) : null,
                    Delete = (c, _) => c.Delete(),
                }.Register();
            }
        }

        public static void ExportAllScripts(string folderName)
        {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);

            foreach (var s in Scripts.Value)
            {
                if (s.Icon != null)
                    s.Icon.Retrieve();

                s.ExportXml().Save(Path.Combine(folderName, s.Name + ".xml"));
            }
        }
    }
}
