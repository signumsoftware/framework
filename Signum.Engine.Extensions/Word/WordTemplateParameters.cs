using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Word;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Word
{
    public class WordTemplateParameters
    {
        public CultureInfo CultureInfo;
        public IEntity Entity;
        public ISystemWordReport SystemWordReport;
        public Dictionary<QueryToken, ResultColumn> Columns;
    }

    public interface ISystemWordReport
    {
        Entity UntypedEntity { get; }

        List<Filter> GetFilters(QueryDescription qd);
    }

    public abstract class SystemWordEmail<T> : ISystemWordReport
       where T : Entity
    {
        public T Entity { get; set; }

        Entity ISystemWordReport.UntypedEntity
        {
            get { return Entity; }
        }

        public virtual List<Filter> GetFilters(QueryDescription qd)
        {
            return new List<Filter>
            {
                new Filter(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, Entity.ToLite())
            };
        }
    }

    public static class SystemWordReportLogic
    {
        class SystemWordReportInfo
        {
            public Func<WordReportTemplateDN> DefaultTemplateConstructor;
            public object QueryName;
        }

        static ResetLazy<Dictionary<Lite<SystemWordReportDN>, List<WordReportTemplateDN>>> SystemWordReportToWordReportTemplates;
        static Dictionary<Type, SystemWordReportInfo> systemWordReports = new Dictionary<Type, SystemWordReportInfo>();
        static ResetLazy<Dictionary<Type, SystemWordReportDN>> systemWordReportToDN;
        static ResetLazy<Dictionary<SystemWordReportDN, Type>> systemWordReportToType;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;

                dqm.RegisterQuery(typeof(SystemWordReportDN), () =>
                    (from se in Database.Query<SystemWordReportDN>()
                     select new
                     {
                         Entity = se,
                         se.Id,
                         se.FullClassName,
                     }));
                
                new Graph<WordReportTemplateDN>.ConstructFrom<SystemWordReportDN>(WordReportTemplateOperation.CreateWordReportTemplateFromSystemWordReport)
                {
                    Construct = (se, _) => CreateDefaultTemplate(se)
                }.Register();

                SystemWordReportToWordReportTemplates = sb.GlobalLazy(() => (
                    from et in Database.Query<WordReportTemplateDN>()
                    where et.SystemWordReport != null
                        && (et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                    select new { swe = et.SystemWordReport, et })
                    .GroupToDictionary(pair => pair.swe.ToLite(), pair => pair.et),
                    new InvalidateWith(typeof(SystemWordReportDN), typeof(WordReportTemplateDN)));

                systemWordReportToDN = sb.GlobalLazy(() =>
                {
                    var dbSystemWordReports = Database.RetrieveAll<SystemWordReportDN>();
                    return EnumerableExtensions.JoinStrict(
                        dbSystemWordReports, systemWordReports.Keys, swr => swr.FullClassName, type => type.FullName,
                        (swr, type) => KVP.Create(type, swr), "caching EmailTemplates. Consider synchronize").ToDictionary();
                }, new InvalidateWith(typeof(SystemWordReportDN)));

                systemWordReportToType = sb.GlobalLazy(() => systemWordReportToDN.Value.Inverse(),
                    new InvalidateWith(typeof(SystemWordReportDN)));
            }
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<SystemWordReportDN>();

            return (from ei in GenerateTemplates()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        internal static List<SystemWordReportDN> GenerateTemplates()
        {
            var list = (from type in systemWordReports.Keys
                        select new SystemWordReportDN
                        {
                            FullClassName = type.FullName
                        }).ToList();
            return list;
        }

        static readonly string systemTemplatesReplacementKey = "SystemWordReport";

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<SystemWordReportDN>();

            Dictionary<string, SystemWordReportDN> should = GenerateTemplates().ToDictionary(s => s.FullClassName);
            Dictionary<string, SystemWordReportDN> old = Administrator.TryRetrieveAll<SystemWordReportDN>(replacements).ToDictionary(c =>
                c.FullClassName);

            replacements.AskForReplacements(
                old.Keys.ToHashSet(),
                should.Keys.ToHashSet(), systemTemplatesReplacementKey);

            Dictionary<string, SystemWordReportDN> current = replacements.ApplyReplacementsToOld(old, systemTemplatesReplacementKey);

            return Synchronizer.SynchronizeScript(should, current,
                (tn, s) => table.InsertSqlSync(s),
                (tn, c) => table.DeleteSqlSync(c),
                (tn, s, c) =>
                {
                    var oldClassName = c.FullClassName;
                    c.FullClassName = s.FullClassName;
                    return table.UpdateSqlSync(c, comment: oldClassName);
                },
                Spacing.Double);
        }

        public static void RegisterSystemWordReport<T>(Func<WordReportTemplateDN> defaultTemplateConstructor, object queryName = null)
         where T : ISystemWordReport
        {
            RegisterSystemWordReport(typeof(T), defaultTemplateConstructor, queryName);
        }

        public static void RegisterSystemWordReport(Type model, Func<WordReportTemplateDN> defaultTemplateConstructor, object queryName = null)
        {
            if (defaultTemplateConstructor == null)
                throw new ArgumentNullException("defaultTemplateConstructor");

            systemWordReports[model] = new SystemWordReportInfo
            {
                DefaultTemplateConstructor = defaultTemplateConstructor,
                QueryName = queryName ?? GetDefaultQueryName(model),
            };
        }

        static object GetDefaultQueryName(Type model)
        {
            var baseType = model.Follow(a => a.BaseType).FirstOrDefault(b => b.IsInstantiationOf(typeof(SystemWordEmail<>)));

            if (baseType != null)
            {
                return baseType.GetGenericArguments()[0];
            }

            throw new InvalidOperationException("Unknown queryName from {0}, set the argument queryName in RegisterSystemEmail".Formato(model.TypeName()));
        }
    }
}
