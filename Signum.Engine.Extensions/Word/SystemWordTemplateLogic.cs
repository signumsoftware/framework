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
        public ISystemWordTemplate SystemWordTemplate;
        public Dictionary<QueryToken, ResultColumn> Columns;
    }

    public interface ISystemWordTemplate
    {
        Entity UntypedEntity { get; }

        List<Filter> GetFilters(QueryDescription qd);
    }

    public abstract class SystemWordEmail<T> : ISystemWordTemplate
       where T : Entity
    {
        public T Entity { get; set; }

        Entity ISystemWordTemplate.UntypedEntity
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

    public static class SystemWordTemplateLogic
    {
        class SystemWordTemplateInfo
        {
            public Func<WordTemplateEntity> DefaultTemplateConstructor;
            public object QueryName;
        }

        static ResetLazy<Dictionary<Lite<SystemWordTemplateEntity>, List<WordTemplateEntity>>> SystemWordTemplateToWordTemplates;
        static Dictionary<Type, SystemWordTemplateInfo> systemWordReports = new Dictionary<Type, SystemWordTemplateInfo>();
        static ResetLazy<Dictionary<Type, SystemWordTemplateEntity>> systemWordReportToEntity;
        static ResetLazy<Dictionary<SystemWordTemplateEntity, Type>> systemWordReportToType;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;

                dqm.RegisterQuery(typeof(SystemWordTemplateEntity), () =>
                    (from se in Database.Query<SystemWordTemplateEntity>()
                     select new
                     {
                         Entity = se,
                         se.Id,
                         se.FullClassName,
                     }));
                
                new Graph<WordTemplateEntity>.ConstructFrom<SystemWordTemplateEntity>(WordTemplateOperation.CreateWordTemplateFromSystemWordTemplate)
                {
                    Construct = (se, _) => CreateDefaultTemplate(se)
                }.Register();

                SystemWordTemplateToWordTemplates = sb.GlobalLazy(() => (
                    from et in Database.Query<WordTemplateEntity>()
                    where et.SystemWordTemplate != null
                        && (et.Active && (et.EndDate == null || et.EndDate > TimeZoneManager.Now))
                    select new { swe = et.SystemWordTemplate, et })
                    .GroupToDictionary(pair => pair.swe.ToLite(), pair => pair.et),
                    new InvalidateWith(typeof(SystemWordTemplateEntity), typeof(WordTemplateEntity)));

                systemWordReportToEntity = sb.GlobalLazy(() =>
                {
                    var dbSystemWordReports = Database.RetrieveAll<SystemWordTemplateEntity>();
                    return EnumerableExtensions.JoinStrict(
                        dbSystemWordReports, systemWordReports.Keys, swr => swr.FullClassName, type => type.FullName,
                        (swr, type) => KVP.Create(type, swr), "caching EmailTemplates. Consider synchronize").ToDictionary();
                }, new InvalidateWith(typeof(SystemWordTemplateEntity)));

                systemWordReportToType = sb.GlobalLazy(() => systemWordReportToEntity.Value.Inverse(),
                    new InvalidateWith(typeof(SystemWordTemplateEntity)));
            }
        }

        internal static WordTemplateEntity CreateDefaultTemplate(SystemWordTemplateEntity systemWordReport)
        {
            SystemWordTemplateInfo info = systemWordReports.GetOrThrow(systemWordReportToType.Value.GetOrThrow(systemWordReport));

            WordTemplateEntity template = info.DefaultTemplateConstructor();

            if (template.Name == null)
                template.Name = systemWordReport.FullClassName;

            template.SystemWordTemplate = systemWordReport;
            template.Active = true;
            template.Query = QueryLogic.GetQueryEntity(info.QueryName);

            return template;
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<SystemWordTemplateEntity>();

            return (from ei in GenerateTemplates()
                    select table.InsertSqlSync(ei)).Combine(Spacing.Simple);
        }

        internal static List<SystemWordTemplateEntity> GenerateTemplates()
        {
            var list = (from type in systemWordReports.Keys
                        select new SystemWordTemplateEntity
                        {
                            FullClassName = type.FullName
                        }).ToList();
            return list;
        }

        static readonly string systemTemplatesReplacementKey = "SystemWordReport";

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<SystemWordTemplateEntity>();

            Dictionary<string, SystemWordTemplateEntity> should = GenerateTemplates().ToDictionary(s => s.FullClassName);
            Dictionary<string, SystemWordTemplateEntity> old = Administrator.TryRetrieveAll<SystemWordTemplateEntity>(replacements).ToDictionary(c =>
                c.FullClassName);

            replacements.AskForReplacements(
                old.Keys.ToHashSet(),
                should.Keys.ToHashSet(), systemTemplatesReplacementKey);

            Dictionary<string, SystemWordTemplateEntity> current = replacements.ApplyReplacementsToOld(old, systemTemplatesReplacementKey);

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

        public static void RegisterSystemWordReport<T>(Func<WordTemplateEntity> defaultTemplateConstructor, object queryName = null)
         where T : ISystemWordTemplate
        {
            RegisterSystemWordReport(typeof(T), defaultTemplateConstructor, queryName);
        }

        public static void RegisterSystemWordReport(Type model, Func<WordTemplateEntity> defaultTemplateConstructor, object queryName = null)
        {
            if (defaultTemplateConstructor == null)
                throw new ArgumentNullException("defaultTemplateConstructor");

            systemWordReports[model] = new SystemWordTemplateInfo
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
