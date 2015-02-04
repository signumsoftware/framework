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
using Signum.Engine.Isolation;
using Signum.Entities.Isolation;
using Signum.Engine.Templating;

namespace Signum.Engine.Word
{
    public class WordTemplateParameters : TemplateParameters
    {
        public WordTemplateParameters(IEntity entity, CultureInfo culture, Dictionary<QueryToken, ResultColumn> columns, IEnumerable<ResultRow> rows) : 
              base(entity, culture, columns, rows)
        { }

        public ISystemWordTemplate SystemWordTemplate;

        public override object GetModel()
        {
            if (SystemWordTemplate == null)
                throw new ArgumentException("There is no SystemWordTemplate set");

            return SystemWordTemplate;
        }
    }

    public interface ISystemWordTemplate
    {
        Entity UntypedEntity { get; }

        List<Filter> GetFilters(QueryDescription qd);
    }

    public abstract class SystemWordTemplate<T> : ISystemWordTemplate
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

        static ResetLazy<Dictionary<Lite<SystemWordTemplateEntity>, List<Lite<WordTemplateEntity>>>> SystemWordTemplateToWordTemplates;
        static Dictionary<Type, SystemWordTemplateInfo> systemWordReports = new Dictionary<Type, SystemWordTemplateInfo>();
        public static ResetLazy<Dictionary<Type, SystemWordTemplateEntity>> TypeToSystemWordTemplate;
        public static ResetLazy<Dictionary<SystemWordTemplateEntity, Type>> SystemWordTemplateToType;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Generating += Schema_Generating;
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Include<SystemWordTemplateEntity>();

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
                    select new { swe = et.SystemWordTemplate, et = et.ToLite() })
                    .GroupToDictionary(pair => pair.swe.ToLite(), pair => pair.et),
                    new InvalidateWith(typeof(SystemWordTemplateEntity), typeof(WordTemplateEntity)));

                TypeToSystemWordTemplate = sb.GlobalLazy(() =>
                {
                    var dbSystemWordReports = Database.RetrieveAll<SystemWordTemplateEntity>();
                    return EnumerableExtensions.JoinStrict(
                        dbSystemWordReports, systemWordReports.Keys, swr => swr.FullClassName, type => type.FullName,
                        (swr, type) => KVP.Create(type, swr), "caching WordTemplates. Consider synchronize").ToDictionary();
                }, new InvalidateWith(typeof(SystemWordTemplateEntity)));

                sb.Schema.Initializing += () => TypeToSystemWordTemplate.Load();

                SystemWordTemplateToType = sb.GlobalLazy(() => TypeToSystemWordTemplate.Value.Inverse(),
                    new InvalidateWith(typeof(SystemWordTemplateEntity)));
            }
        }

        internal static WordTemplateEntity CreateDefaultTemplate(SystemWordTemplateEntity systemWordReport)
        {
            SystemWordTemplateInfo info = systemWordReports.GetOrThrow(SystemWordTemplateToType.Value.GetOrThrow(systemWordReport));

            WordTemplateEntity template = info.DefaultTemplateConstructor();

            if (template.Name == null)
                template.Name = systemWordReport.FullClassName;

            template.SystemWordTemplate = systemWordReport;
            template.Active = true;
            template.Query = QueryLogic.GetQueryEntity(info.QueryName);

            return template;
        }


        public static byte[] CreateReport(this ISystemWordTemplate systemWordTemplate, bool avoidConversion = false)
        {
            WordTemplateEntity rubish;
            return systemWordTemplate.CreateReport(out rubish, avoidConversion);
        }

        public static byte[] CreateReport(this ISystemWordTemplate systemWordTemplate, out WordTemplateEntity template, bool avoidConversion = false)
        {
            SystemWordTemplateEntity system = GetSystemWordTemplate(systemWordTemplate.GetType());

            template = GetDefaultTemplate(system);

            return WordTemplateLogic.CreateReport(template.ToLite(), systemWordTemplate.UntypedEntity, systemWordTemplate, avoidConversion); 
        }

        public static SystemWordTemplateEntity GetSystemWordTemplate(Type type)
        {
            return TypeToSystemWordTemplate.Value.GetOrThrow(type);
        }

        public static WordTemplateEntity GetDefaultTemplate(SystemWordTemplateEntity systemWordTemplate)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<WordTemplateEntity>(userInterface: false);

            var templates = SystemWordTemplateToWordTemplates.Value.TryGetC(systemWordTemplate.ToLite()).EmptyIfNull().Select(a => WordTemplateLogic.WordTemplatesLazy.Value.GetOrThrow(a));

            templates = templates.Where(isAllowed);

            if (templates.IsNullOrEmpty())
            {
                using (ExecutionMode.Global())
                using (OperationLogic.AllowSave<WordTemplateEntity>())
                using (Transaction tr = Transaction.ForceNew())
                {
                    var template = CreateDefaultTemplate(systemWordTemplate);

                    template.Save();

                    return tr.Commit(template);
                }
            }

            return templates.Where(t => t.IsActiveNow()).SingleEx(() => "Active WordTemplates for SystemWordTemplate {0}".FormatWith(systemWordTemplate));
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

            using (replacements.WithReplacedDatabaseName())
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
            var baseType = model.Follow(a => a.BaseType).FirstOrDefault(b => b.IsInstantiationOf(typeof(SystemWordTemplate<>)));

            if (baseType != null)
            {
                return baseType.GetGenericArguments()[0];
            }

            throw new InvalidOperationException("Unknown queryName from {0}, set the argument queryName in RegisterSystemEmail".FormatWith(model.TypeName()));
        }

        public static Type ToType(this SystemWordTemplateEntity systemWordTemplate)
        {
            if (systemWordTemplate == null)
                return null;

            return SystemWordTemplateToType.Value.GetOrThrow(systemWordTemplate);
        }
    }
}
