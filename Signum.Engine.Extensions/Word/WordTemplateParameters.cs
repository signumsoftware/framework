using Signum.Engine.Basics;
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
            public Func<WordReportTemplateEntity> DefaultTemplateConstructor;
            public object QueryName;
        }

        static ResetLazy<Dictionary<Lite<SystemWordReportEntity>, List<WordReportTemplateEntity>>> SystemWordReportToWordReportTemplates;
        static Dictionary<Type, SystemWordReportInfo> systemWordReports = new Dictionary<Type, SystemWordReportInfo>();
        static ResetLazy<Dictionary<Type, SystemWordReportEntity>> systemWordReportToEntity;
        static ResetLazy<Dictionary<SystemWordReportEntity, Type>> systemWordReportToType;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;

                dqm.RegisterQuery(typeof(SystemWordReportEntity), () =>
                    (from se in Database.Query<SystemWordReportEntity>()
                     select new
                     {
                         Entity = se,
                         se.Id,
                         se.FullClassName,
                     }));
                
                new Graph<WordReportTemplateEntity>.ConstructFrom<SystemWordReportEntity>(WordReportTemplateOperation.CreateWordReportTemplateFromSystemWordReport)
                {
                    Construct = (se, _) => CreateDefaultTemplate(se)
                }.Register();

                SystemWordReportToWordReportTemplates = sb.GlobalLazy(() => (
                    from et in Database.Query<WordReportTemplateEntity>()
                    where et.SystemWordReport != null
                        && (et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                    select new { swe = et.SystemWordReport, et })
                    .GroupToDictionary(pair => pair.swe.ToLite(), pair => pair.et),
                    new InvalidateWith(typeof(SystemWordReportEntity), typeof(WordReportTemplateEntity)));

                systemWordReportToEntity = sb.GlobalLazy(() =>
                {
                    var dbSystemWordReports = Database.RetrieveAll<SystemWordReportEntity>();
                    return EnumerableExtensions.JoinStrict(
                        dbSystemWordReports, systemWordReports.Keys, swr => swr.FullClassName, type => type.FullName,
                        (swr, type) => KVP.Create(type, swr), "caching EmailTemplates. Consider synchronize").ToDictionary();
                }, new InvalidateWith(typeof(SystemWordReportEntity)));

                systemWordReportToType = sb.GlobalLazy(() => systemWordReportToEntity.Value.Inverse(),
                    new InvalidateWith(typeof(SystemWordReportEntity)));
            }
        }

        internal static WordReportTemplateEntity CreateDefaultTemplate(SystemWordReportEntity systemWordReport)
        {
            SystemWordReportInfo info = systemWordReports.GetOrThrow(systemWordReportToType.Value.GetOrThrow(systemWordReport));

            WordReportTemplateEntity template = info.DefaultTemplateConstructor();

            if (template.Name == null)
                template.Name = systemWordReport.FullClassName;

            template.SystemWordReport = systemWordReport;
            template.Active = true;
            template.Query = QueryLogic.GetQuery(info.QueryName);

            return template;
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<SystemWordReportEntity>();

            return (from ei in GenerateTemplates()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        internal static List<SystemWordReportEntity> GenerateTemplates()
        {
            var list = (from type in systemWordReports.Keys
                        select new SystemWordReportEntity
                        {
                            FullClassName = type.FullName
                        }).ToList();
            return list;
        }

        static readonly string systemTemplatesReplacementKey = "SystemWordReport";

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<SystemWordReportEntity>();

            Dictionary<string, SystemWordReportEntity> should = GenerateTemplates().ToDictionary(s => s.FullClassName);
            Dictionary<string, SystemWordReportEntity> old = Administrator.TryRetrieveAll<SystemWordReportEntity>(replacements).ToDictionary(c =>
                c.FullClassName);

            replacements.AskForReplacements(
                old.Keys.ToHashSet(),
                should.Keys.ToHashSet(), systemTemplatesReplacementKey);

            Dictionary<string, SystemWordReportEntity> current = replacements.ApplyReplacementsToOld(old, systemTemplatesReplacementKey);

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

        public static void RegisterSystemWordReport<T>(Func<WordReportTemplateEntity> defaultTemplateConstructor, object queryName = null)
         where T : ISystemWordReport
        {
            RegisterSystemWordReport(typeof(T), defaultTemplateConstructor, queryName);
        }

        public static void RegisterSystemWordReport(Type model, Func<WordReportTemplateEntity> defaultTemplateConstructor, object queryName = null)
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

            throw new InvalidOperationException("Unknown queryName from {0}, set the argument queryName in RegisterSystemEmail".FormatWith(model.TypeName()));
        }
    }
}
