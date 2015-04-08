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
using Signum.Engine.Cache;

namespace Signum.Engine.Chart
{
    public static class ChartScriptLogic
    {
        public static ResetLazy<Dictionary<string, ChartScriptEntity>> Scripts { get; private set; }

        internal static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ChartScriptEntity>();

                dqm.RegisterQuery(typeof(ChartScriptEntity), () =>
                    from uq in Database.Query<ChartScriptEntity>()
                    select new
                    {
                        Entity = uq,
                        uq.Id,
                        uq.Name,
                        uq.GroupBy,
                        uq.Columns.Count,
                        uq.Icon,
                    });

                Scripts = sb.GlobalLazy(() => Database.Query<ChartScriptEntity>().ToDictionary(a=>a.Name),
                    new InvalidateWith(typeof(ChartScriptEntity)));

                RegisterOperations();
            }
        }

        private static void RegisterOperations()
        {
            new Graph<ChartScriptEntity>.Execute(ChartScriptOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                Execute = (cs, _) => { }
            }.Register();

            new Graph<ChartScriptEntity>.ConstructFrom<ChartScriptEntity>(ChartScriptOperation.Clone)
            {
                Construct = (cs, _) => new ChartScriptEntity
                {
                    Name = cs.Name,
                    GroupBy = cs.GroupBy,
                    Icon = cs.Icon,
                    Columns = cs.Columns.Select(col => new ChartScriptColumnEntity
                    {
                        ColumnType = col.ColumnType,
                        DisplayName = col.DisplayName,
                        IsGroupKey = col.IsGroupKey,
                        IsOptional = col.IsOptional,
                    }).ToMList(),
                    Script = cs.Script,
                }
            }.Register();


            new Graph<ChartScriptEntity>.Delete(ChartScriptOperation.Delete)
            {
                CanDelete = c => Database.Query<UserChartEntity>().Any(a => a.ChartScript == c) ? "There are {0} in the database using {1}".FormatWith(typeof(UserChartEntity).NicePluralName(), c) : null,
                Delete = (c, _) => c.Delete(),
            }.Register();
        }

        public static void ImportExportChartScripts()
        {
            ImportExportChartScripts(GetDefaultFolderName());
        }

        public static void ImportExportChartScripts(string folderName)
        {
            Console.WriteLine("You want to export (e), import (i) ChartScripts? (nothing to exit)");

            string answer = Console.ReadLine();

            if (answer.ToLower() == "e")
            {
                ExportChartScripts(folderName);
            }
            else if (answer.ToLower() == "i")
            {
                ImportChartScripts(folderName);
            }
        }

        public static string DefaultFolderDevelopment = @"..\..\..\Extensions\Signum.Engine.Extensions\Chart\ChartScripts";
        public static string DefaultFolderProduction = @"ChartScripts";
        private static string GetDefaultFolderName()
        {
            if (Directory.Exists(DefaultFolderDevelopment))
            {
                if (Directory.Exists(DefaultFolderProduction))
                    return SafeConsole.Ask("In Production?") ? DefaultFolderProduction : DefaultFolderDevelopment;

                return DefaultFolderDevelopment;
            }

            if (Directory.Exists(DefaultFolderProduction))
                return DefaultFolderProduction;

            throw new InvalidOperationException("Default ChartScripts folder not found");
        }

        public static void ExportChartScripts(string folderName)
        {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);

            var files = Directory.GetFiles(folderName, "*.xml").ToDictionary(Path.GetFileNameWithoutExtension);

            var charts = Database.Query<ChartScriptEntity>().ToDictionary(a => a.Name);

            Options options = new Options();

            Func<ChartScriptEntity, string> fileName = cs => Path.Combine(folderName, cs.Name + ".xml");

            Synchronizer.SynchronizeReplacing(new Replacements(), "scripts",
                charts,
                files,
                (name, script) => script.ExportXml().Save(fileName(script)),
                (name, file) =>
                {
                    if (AskYesNoAll("Remove {0} file?".FormatWith(name), ref options.RemoveOld))
                        File.Delete(file);
                },
                (name, script, file) =>
                {
                    var newFileName = fileName(script);

                    if (file != newFileName)
                        File.Delete(file);

                    if (script.Icon != null)
                        script.Icon.Retrieve();

                    script.ExportXml().Save(newFileName);
                });
        }

        public class Options
        {
            public bool RemoveOld;
            public bool OverrideAll;
            public bool ForceAll;
        }

        public static void ImportChartScriptsAuto()
        {
            ImportChartScripts(GetDefaultFolderName(), new Options
            {
                RemoveOld = true,
                OverrideAll = true,
                ForceAll = true
            });
        }

        public static void ImportChartScripts(string folderName, Options options = null)
        {
            var files = Directory.GetFiles(folderName, "*.xml").ToDictionary(Path.GetFileNameWithoutExtension);

            var charts = Database.Query<ChartScriptEntity>().ToDictionary(a => a.Name);

            options = options ?? new Options();

            using (OperationLogic.AllowSave<ChartScriptEntity>())
                Synchronizer.SynchronizeReplacing(new Replacements(), "scripts",
                    files,
                    charts,
                    (name, file) =>
                    {
                        var cs = new ChartScriptEntity();
                        cs.ImportXml(XDocument.Load(file), name, force: false);
                        cs.Save();

                        Console.WriteLine("{0} entity created.".FormatWith(name));
                    },
                    (name, script) =>
                    {
                        if (AskYesNoAll("Remove {0} entity?".FormatWith(name), ref options.RemoveOld))
                        {
                            try
                            {
                                script.Delete();
                                Console.WriteLine("{0} entity removed.".FormatWith(name));
                            }
                            catch (Exception e)
                            {
                                SafeConsole.WriteLineColor(ConsoleColor.Red, "Error removing {0} entity: {1}".FormatWith(name, e.Message));
                            }
                        }
                    },
                    (name, file, script) =>
                    {
                        var xDoc = XDocument.Load(file);
                        if (script.Icon != null)
                            script.Icon.Retrieve();
                        try
                        {
                            script.ImportXml(xDoc, name, false);
                        }
                        catch (FormatException f)
                        {
                            SafeConsole.WriteLineColor(ConsoleColor.Yellow, f.Message);
                            if (AskYesNoAll("Force {0}?".FormatWith(name), ref options.ForceAll))
                                script.ImportXml(xDoc, name, true);
                        }

                        if (script.HasChanges() && AskYesNoAll("Override {0} entity?".FormatWith(name), ref options.OverrideAll))
                        {
                            script.Save();
                            Console.WriteLine("{0} entity overriden.".FormatWith(name));
                        }
                    });
        }



        private static bool AskYesNoAll(string message, ref bool all)
        {
            if (all)
                return true;

            while (true)
            {
                Console.Write(message + "(*yes, no, all)");

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

        public static ChartScriptEntity GetChartScript(string chartScriptName)
        {
            return Scripts.Value.GetOrThrow(chartScriptName);
        }
    }
}
