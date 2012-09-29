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
using System.Xml.Linq;
using Signum.Engine.Authorization;

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

        public static void ImportExportScripts(string folderName)
        {
            Console.WriteLine("You want to export (e), import (i) ChartScripts? (nothing to exit)".Formato(folderName));

            string answer = Console.ReadLine();

            if (answer.ToLower() == "e")
            {
                ExportAllScripts(folderName);
            }
            else if (answer.ToLower() == "i")
            {
                ImportAllScripts(folderName);
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

        public static void ImportAllScripts(string folderName)
        {
            string[] fileNames = Directory.GetFiles(folderName, "*.xml");

            var charts = Database.Query<ChartScriptDN>().ToDictionary(a => a.Name); 

            bool overriteAll = false;
            bool forceAll = false;
            foreach (var item in fileNames)
            {
                var name = Path.GetFileNameWithoutExtension(item);

                var previous = charts.TryGetC(name);
                if (previous != null && previous.Icon != null)
                    previous.Icon.Retrieve();

                var script = previous ?? new ChartScriptDN();

                try
                {
                    script.ImportXml(XDocument.Load(item), name, false);
                }
                catch (FormatException f)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Yellow, f.Message);
                    if (AskYesNoAll("Foce {0}? (*yes, no, all)".Formato(name), ref forceAll))
                        script.ImportXml(XDocument.Load(item), name, true);
                }

                if (previous != null)
                {
                    if (previous.HasChanges() && AskYesNoAll("Override {0}? (*yes, no, all)".Formato(name), ref overriteAll))
                    {
                        previous.Save();
                        Console.WriteLine("{0} overriden.".Formato(name));
                    }
                }
                else
                {
                    script.Save();
                    Console.WriteLine("{0} created.".Formato(name));
                }
            }
        }

        private static bool AskYesNoAll(string message, ref bool all)
        {
            if (all)
                return true;

            while (true)
            {
                Console.Write(message);

                var str = Console.ReadLine();

                if (!str.HasText())
                    return true;

                var c = char.ToLower(str[0]);

                if (c == 'a' || c == 'y')
                {
                    all = c == 'a';
                    return true;
                }
                else if (c == 'n')
                    return false;

            }
        }

     
    }
}
