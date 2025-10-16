using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Signum.Engine.Sync;
using Signum.Basics;
using Signum.Omnibox;
using Signum.Files;
using Signum.Entities;
using DocumentFormat.OpenXml.Vml.Office;
using Signum.API;

namespace Signum.Help;

public static class HelpLogic
{
    public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<Type, List<TypeHelp>>>> Types = null!;
    public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, List<NamespaceHelp>>>> Namespaces = null!;
    public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, List<AppendixHelpEntity>>>> Appendices = null!;
    public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<object, List<QueryHelp>>>> Queries = null!;

    public static Func<AppendixHelpEntity, bool> IsApplicableAppendix = e => true;
    public static Func<NamespaceHelpEntity, bool> IsApplicableNamespace = e => true;
    public static Func<TypeHelpEntity, bool> IsApplicableTypeHelp = e => true;
    public static Func<QueryHelpEntity, bool> IsApplicableQueryHelp = e => true;

    public static Lazy<Dictionary<Type, List<object>>> TypeToQuery = new Lazy<Dictionary<Type, List<object>>>(() =>
    {
        var queries = QueryLogic.Queries;

        return (from qn in queries.GetQueryNames()
                let imp = queries.GetEntityImplementations(qn)
                where !imp.IsByAll
                from t in imp.Types
                group qn by t into g
                select KeyValuePair.Create(g.Key, g.ToList())).ToDictionary();

    });

    public static void Start(SchemaBuilder sb, IFileTypeAlgorithm helpImagesAlgorithm)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

            sb.Include<TypeHelpEntity>()
                .WithUniqueIndex(e => new { e.Type, e.Culture })
                .WithUniqueIndexMList(e => e.Properties, mle => new { mle.Parent, mle.Element.Property })
                .WithUniqueIndexMList(e => e.Operations, mle => new { mle.Parent, mle.Element.Operation })
                .WithSave(TypeHelpOperation.Save, (t, _ ) => InlineImagesLogic.SynchronizeInlineImages(t))
                .WithDelete(TypeHelpOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Type,
                    Description = e.Description.Try(d => d.Etc(100))
                });



            sb.Include<NamespaceHelpEntity>()
                .WithUniqueIndex(e => new { e.Name, e.Culture })
                .WithSave(NamespaceHelpOperation.Save, (n, _ ) => InlineImagesLogic.SynchronizeInlineImages(n))
                .WithDelete(NamespaceHelpOperation.Delete)
                .WithQuery(() => n => new
                {
                    Entity = n,
                    n.Id,
                    n.Name,
                    n.Culture,
                    Description = n.Description.Try(d => d.Etc(100))
                });

            sb.Include<AppendixHelpEntity>()
                .WithUniqueIndex(e => new { e.UniqueName, e.Culture })
                .WithSave(AppendixHelpOperation.Save, (a, _) => InlineImagesLogic.SynchronizeInlineImages(a))
                .WithDelete(AppendixHelpOperation.Delete)
                .WithQuery(() => a => new
                {
                    Entity = a,
                    a.Id,
                    a.UniqueName,
                    a.Culture,
                    a.Title,
                    Description = a.Description.Try(d => d.Etc(100))
                });

            sb.Include<QueryHelpEntity>()
                .WithUniqueIndex(e => new { e.Query, e.Culture })
                .WithUniqueIndexMList(e => e.Columns, mle => new { mle.Parent, mle.Element.ColumnName })
                .WithSave(QueryHelpOperation.Save)
                .WithDelete(QueryHelpOperation.Delete)
                .WithQuery(() => q => new
                {
                    Entity = q,
                    q.Id,
                    q.Query,
                    q.Culture,
                    Description = q.Description.Try(d => d.Etc(100))
                });

            sb.Include<HelpImageEntity>()
                .WithQuery(() => q => new
                {
                    Entity = q,
                    q.Id,
                    q.CreationDate,
                    q.File,
                    q.Target,
                });

            sb.Schema.EntityEvents<AppendixHelpEntity>().PreUnsafeDelete += query => { query.SelectMany(a => a.Images()).UnsafeDelete(); return null; };
            sb.Schema.EntityEvents<NamespaceHelpEntity>().PreUnsafeDelete += query => { query.SelectMany(a => a.Images()).UnsafeDelete(); return null; };
            sb.Schema.EntityEvents<TypeHelpEntity>().PreUnsafeDelete += query => { query.SelectMany(a => a.Images()).UnsafeDelete(); return null; };
            sb.Schema.EntityEvents<QueryHelpEntity>().PreUnsafeDelete += query => { query.SelectMany(a => a.Images()).UnsafeDelete(); return null; };
            FileTypeLogic.Register(HelpImageFileType.Image, helpImagesAlgorithm);

            sb.Schema.Synchronizing += Schema_Synchronizing;

            sb.Schema.EntityEvents<OperationSymbol>().PreDeleteSqlSync += operation =>
                Administrator.UnsafeDeletePreCommandMList((TypeHelpEntity eh) => eh.Operations, Database.MListQuery((TypeHelpEntity eh) => eh.Operations).Where(mle => mle.Element.Operation.Is(operation)));

            sb.Schema.EntityEvents<PropertyRouteEntity>().PreDeleteSqlSync += property =>
                Administrator.UnsafeDeletePreCommandMList((TypeHelpEntity eh) => eh.Properties, Database.MListQuery((TypeHelpEntity eh) => eh.Properties).Where(mle => mle.Element.Property.Is(property)));

            sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type =>
                Administrator.UnsafeDeletePreCommand(Database.Query<TypeHelpEntity>().Where(e => e.Type.Is(type)));

            sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += query =>
                Administrator.UnsafeDeletePreCommand(Database.Query<QueryHelpEntity>().Where(e => e.Query.Is(query)));

            Types = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<Type, List<TypeHelp>>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<Type, List<TypeHelp>>>(),
             invalidateWith: new InvalidateWith(typeof(TypeHelpEntity)));

            Namespaces = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, List<NamespaceHelp>>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<string, List<NamespaceHelp>>>(),
                invalidateWith: new InvalidateWith(typeof(NamespaceHelpEntity)));

            Appendices = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, List<AppendixHelpEntity>>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<string, List<AppendixHelpEntity>>>(),
                invalidateWith: new InvalidateWith(typeof(AppendixHelpEntity)));

            Queries = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<object, List<QueryHelp>>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<object, List<QueryHelp>>>(),
               invalidateWith: new InvalidateWith(typeof(QueryHelpEntity)));

            PermissionLogic.RegisterPermissions(HelpPermissions.ViewHelp);

            if(sb.WebServerBuilder != null)
            {
                OmniboxParser.Generators.Add(new HelpModuleOmniboxResultGenerator());
            }
        }

    public static NamespaceHelp GetNamespaceHelp(string @namespace) => CachedNamespacesHelp().GetOrThrow(@namespace).SingleOrDefaultEx(a => a.DBEntity == null || IsApplicableNamespace(a.DBEntity)) ?? new NamespaceHelp(@namespace, GetCulture(), null, AllTypes().Where(a => a.Namespace == @namespace).ToArray());
    public static IEnumerable<NamespaceHelp> GetNamespaceHelps() => CachedNamespacesHelp().Values.SelectMany(a => a).Select(a => GetNamespaceHelp(a.Namespace)).Where(a => a.IsAllowed() == null);
    public static Dictionary<string, List<NamespaceHelp>> CachedNamespacesHelp()
    {
        return Namespaces.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
        {
            var namespaces = AllTypes().GroupBy(type => type.Namespace!);
            
            var dic = Database.Query<NamespaceHelpEntity>().Where(n => n.Culture.Is(ci.ToCultureInfoEntity())).GroupToDictionary(a => a.Name);

            var result = namespaces.ToDictionary(gr => gr.Key, gr => dic.TryGetC(gr.Key)?.Select(a => new NamespaceHelp(gr.Key, ci, a, gr.ToArray())).ToList() ?? new List<NamespaceHelp>() { new NamespaceHelp(gr.Key, ci, null, gr.ToArray()) });

            return result;
        }));
    }

    public static AppendixHelpEntity GetAppendixHelp(string uniqueName) => CachedAppendicesHelp().TryGetC(uniqueName)?.SingleOrDefaultEx(a => IsApplicableAppendix(a)) ?? new AppendixHelpEntity() { Culture = GetCulture().ToCultureInfoEntity(), UniqueName = uniqueName };
    public static IEnumerable<AppendixHelpEntity> GetAppendixHelps() => CachedAppendicesHelp().Values.SelectMany(a => a).Where(a => IsApplicableAppendix(a));
    public static Dictionary<string, List<AppendixHelpEntity>> CachedAppendicesHelp()
    {
        return Appendices.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
        {
            var result = Database.Query<AppendixHelpEntity>().Where(n => n.Culture.Is(ci.ToCultureInfoEntity())).GroupToDictionary(a => a.UniqueName);

            return result;
        }));
    }

    public static TypeHelp GetTypeHelp(Type type) => CachedEntityHelp().GetOrThrow(type).SingleOrDefaultEx(a => a.DBEntity == null || IsApplicableTypeHelp(a.DBEntity)) ?? new TypeHelp(type, GetCulture(), null);
    public static IEnumerable<TypeHelp> GetEntityHelps() => CachedEntityHelp().Values.SelectMany(a => a).Select(a => GetTypeHelp(a.Type)).Where(a => a.IsAllowed() == null);
    public static Dictionary<Type, List<TypeHelp>> CachedEntityHelp()
    {
        return Types.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
        {
            var dic = Database.Query<TypeHelpEntity>().Where(n => n.Culture.Is(ci.ToCultureInfoEntity())).GroupToDictionary(a => a.Type.ToType());

            var result = AllTypes().ToDictionary(t => t, t => dic.TryGetC(t)?.Select(th => new TypeHelp(t, ci, th)).ToList() ?? new List<TypeHelp>() { new TypeHelp(t, ci, null) });

            return result;
        }));
    }

    public static QueryHelp GetQueryHelp(object queryName) => CachedQueriesHelp().GetOrThrow(queryName).SingleOrDefaultEx(a => a.DBEntity == null || IsApplicableQueryHelp(a.DBEntity)) ?? new QueryHelp(queryName, GetCulture(), null);
    public static Dictionary<object, List<QueryHelp>> CachedQueriesHelp()
    {
        return Queries.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
        {
            var dic = Database.Query<QueryHelpEntity>().Where(n => n.Culture.Is(ci.ToCultureInfoEntity())).GroupToDictionary(a => a.Query.ToQueryName());

            var result = AllQueries().ToDictionary(t => t, t => dic.TryGetC(t)?.Select(qh => new QueryHelp(t, ci, qh)).ToList() ?? new());

            return result;
        }));
    }

    public static T GlobalContext<T>(Func<T> customFunc)
    {
        using (var tr = Transaction.ForceNew())
        using (ExecutionMode.Global())
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            return tr.Commit(customFunc());
        }
    }

    public static CultureInfo GetCulture()
    {
        var dic = CultureInfoLogic.CultureInfoToEntity.Value;

        var ci = CultureInfo.CurrentCulture;

        if (!ci.IsNeutralCulture)
            ci = ci.Parent;

        if (dic.ContainsKey(ci.Name))
            return ci;

        if (Schema.Current.ForceCultureInfo != null && dic.ContainsKey(Schema.Current.ForceCultureInfo!.Name))
            return Schema.Current.ForceCultureInfo!;

        throw new InvalidOperationException("No compatible CultureInfo found in the database for {0}".FormatWith(ci.Name));
    }



    public static List<Type> AllTypes()
    {
        return Schema.Current.Tables.Keys.Where(t => !t.IsEnumEntity()).ToList();
    }

    public static List<object> AllQueries()
    {
        return (from type in AllTypes()
                from key in QueryLogic.Queries.GetTypeQueries(type).Keys
                select key).Distinct().ToList();
    }



    static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        bool any =
            Database.Query<TypeHelpEntity>().Any() ||
            Database.Query<QueryHelpEntity>().Any() ||
            Database.Query<NamespaceHelpEntity>().Any() ||
            Database.Query<AppendixHelpEntity>().Any();

        if (!(any && replacements.Interactive && SafeConsole.Ask("Synchronize Help content?")))
            return null;

        SyncData data = new SyncData(
            namespaces: AllTypes().Select(a => a.Namespace!).ToHashSet(),
            appendices: Database.Query<AppendixHelpEntity>().Select(a => a.UniqueName).ToHashSet()
        );

        var ns = SynchronizeNamespace(replacements, data);
        var appendix = SynchronizeAppendix(replacements, data);
        var types = SynchronizeTypes(replacements, data);
        var queries = SynchronizeQueries(replacements, data);

        return SqlPreCommand.Combine(Spacing.Double, ns, appendix, types, queries);
    }

    public class SyncData
    {
        public HashSet<string> Namespaces;
        public HashSet<string> Appendices;
        public StringDistance StringDistance = new StringDistance();

        public SyncData(HashSet<string> namespaces, HashSet<string> appendices)
        {
            Namespaces = namespaces;
            Appendices = appendices;
        }
    }

    static SqlPreCommand? SynchronizeQueries(Replacements replacements, SyncData data)
    {
        var dic = Database.Query<QueryHelpEntity>().ToList();

        if (dic.IsEmpty())
            return null;

        var queryKeys = QueryLogic.Queries.GetQueryNames().ToDictionary(a => QueryUtils.GetKey(a));

        var table = Schema.Current.Table<QueryHelpEntity>();

        var replace = replacements.TryGetC(QueryLogic.QueriesKey);

        return dic.Select(qh =>
        {
            object? queryName = queryKeys.TryGetC(replace?.TryGetC(qh.Query.Key) ?? qh.Query.Key);
            if (queryName == null)
                return null; //PreDeleteSqlSync

            if (qh.Columns.Any())
            {
                var columns = QueryLogic.Queries.GetQuery(queryName).Core.Value.StaticColumns;

                Synchronizer.SynchronizeReplacing(replacements, "ColumnsOfQuery:" + QueryUtils.GetKey(queryName),
                    columns.ToDictionary(a => a.Name),
                    qh.Columns.ToDictionary(a => a.ColumnName),
                    null,
                    (qn, oldQ) => qh.Columns.Remove(oldQ),
                    (qn, newQ, oldQ) =>
                    {
                        oldQ.ColumnName = newQ.Name;
                    });

                foreach (var col in qh.Columns)
                    col.Description = SynchronizeContent(col.Description, replacements, data)!;
            }

            qh.Description = SynchronizeContent(qh.Description, replacements, data);

            return table.UpdateSqlSync(qh, h => h.Query.Key == qh.Query.Key);
        }).Combine(Spacing.Simple);
    }

    static SqlPreCommand? SynchronizeTypes(Replacements replacements, SyncData data)
    {
        var dic = Database.Query<TypeHelpEntity>().ToList();

        if (dic.IsEmpty())
            return null;

        var typesByTableName = Schema.Current.Tables.ToDictionary(kvp => kvp.Value.Name.Name, kvp => kvp.Key);

        var replace = replacements.TryGetC(Replacements.KeyTables);

        var table = Schema.Current.Table<TypeHelpEntity>();

        using (replacements.WithReplacedDatabaseName())
            return dic.Select(eh =>
            {
                Type? type = typesByTableName.TryGetC(replace?.TryGetC(eh.Type.TableName) ?? eh.Type.TableName);
                if (type == null)
                    return null; //PreDeleteSqlSync

                var repProperties = replacements.TryGetC(PropertyRouteLogic.PropertiesFor.FormatWith(eh.Type.CleanName));
                var routes = PublicRoutes(type).ToDictionary(pr => { var ps = pr.PropertyString(); return repProperties?.TryGetC(ps) ?? ps; });
                eh.Properties.RemoveAll(p => !routes.ContainsKey(p.Property.Path));
                foreach (var prop in eh.Properties)
                    prop.Description = SynchronizeContent(prop.Description, replacements, data);

                var resOperations = replacements.TryGetC(typeof(OperationSymbol).Name);
                var operations = OperationLogic.TypeOperations(type).ToDictionary(o => { var key = o.OperationSymbol.Key; return resOperations?.TryGetC(key) ?? key; });
                eh.Operations.RemoveAll(p => !operations.ContainsKey(p.Operation.Key));
                foreach (var op in eh.Operations)
                    op.Description = SynchronizeContent(op.Description, replacements, data);

                eh.Description = SynchronizeContent(eh.Description, replacements, data);

                return table.UpdateSqlSync(eh, e => e.Type.CleanName == eh.Type.CleanName);
            }).Combine(Spacing.Simple);
    }

    public static List<PropertyRoute> PublicRoutes(Type type)
    {
        return PropertyRoute.GenerateRoutes(type).Where(a => ReflectionServer.InTypeScript(a)).ToList();
    }


    static SqlPreCommand? SynchronizeNamespace(Replacements replacements, SyncData data)
    {
        var entities = Database.Query<NamespaceHelpEntity>().ToList();

        if (entities.IsEmpty())
            return null;

        var current = entities.Select(a => a.Name).ToHashSet();

        replacements.AskForReplacements(current, data.Namespaces, "namespaces");

        var table = Schema.Current.Table<NamespaceHelpEntity>();

        using (replacements.WithReplacedDatabaseName())
            return entities.Select(e =>
            {
                e.Name = replacements.TryGetC("namespaces")?.TryGetC(e.Name) ?? e.Name;

                if (!data.Namespaces.Contains(e.Name))
                    return table.DeleteSqlSync(e, n => n.Name == e.Name);

                e.Description = SynchronizeContent(e.Description, replacements, data);

                return table.UpdateSqlSync(e, n => n.Name == e.Name);
            }).Combine(Spacing.Simple);
    }

    static SqlPreCommand? SynchronizeAppendix(Replacements replacements, SyncData data)
    {
        var entities = Database.Query<AppendixHelpEntity>().ToList();

        if (entities.IsEmpty())
            return null;

        var table = Schema.Current.Table<AppendixHelpEntity>();

        using (replacements.WithReplacedDatabaseName())
            return entities.Select(e =>
            {
                e.Description = SynchronizeContent(e.Description, replacements, data);

                return table.UpdateSqlSync(e, n => n.UniqueName == e.UniqueName);
            }).Combine(Spacing.Simple);
    }

    static Lazy<XmlSchemaSet> Schemas = new Lazy<XmlSchemaSet>(() =>
    {
        XmlSchemaSet schemas = new XmlSchemaSet();
        Stream str = typeof(HelpLogic).Assembly.GetManifestResourceStream("Signum.Engine.Extensions.Help.SignumFrameworkHelp.xsd")!;
        schemas.Add("", XmlReader.Create(str));
        return schemas;
    });

    internal static XDocument LoadAndValidate(string fileName)
    {
        var document = XDocument.Load(fileName);

        List<(XmlSchemaException exception, string filename)> exceptions = new List<(XmlSchemaException exception, string filename)>();

        document.Document!.Validate(Schemas.Value, (s, e) => exceptions.Add((e.Exception, fileName)));

        if (exceptions.Any())
            throw new InvalidOperationException("Error Parsing XML Help Files: " + exceptions.ToString(e => "{0} ({1}:{2}): {3}".FormatWith(
             e.filename, e.exception.LineNumber, e.exception.LinePosition, e.Item1.Message), "\n").Indent(3));

        return document;
    }

    public static readonly Regex TokenRegex = new Regex(@"\[(?<content>([^\[\]]|\[\[|\]\])+)\]");

    public static readonly Regex HelpLinkRegex = new Regex(@"^(?<letter>[^:]+):(?<link>[^\|]*)?$");

    [return: NotNullIfNotNull("content")]
    static string? SynchronizeContent(string? content, Replacements r, SyncData data)
    {
        if (content == null)
            return null;

        return TokenRegex.Replace(content, m =>
        {
            var m2 = HelpLinkRegex.Match(m.Groups["content"].Value);

            if (!m2.Success)
                return m.Value;

            string letter = m2.Groups["letter"].Value;
            string link = m2.Groups["link"].Value;

            switch (letter)
            {
                case HelpLinkPrefix.TypeLink:
                    {
                        string? type = r.SelectInteractive(link, TypeLogic.NameToType.Keys, "Type", data.StringDistance);
                        if (type == null)
                            return HelpLink(letter + "-error", link);

                        return HelpLink(letter, type);
                    }
                case HelpLinkPrefix.PropertyLink:
                    {
                        string? type = r.SelectInteractive(link.Before("."), TypeLogic.NameToType.Keys, "Type", data.StringDistance);
                        if (type == null)
                            return HelpLink(letter + "-error", link);

                        var routes = PropertyRoute.GenerateRoutes(TypeLogic.GetType(type)).Select(a => a.PropertyString()).ToList();

                        string? pr = r.SelectInteractive(link.After('.'), routes, "PropertyRoutes-" + type, data.StringDistance);
                        if (pr == null)
                            return HelpLink(letter + "-error", link);

                        return HelpLink(letter, type + "." + pr);
                    }
                case HelpLinkPrefix.QueryLink:
                    {
                        string? query = r.SelectInteractive(link, QueryLogic.QueryNames.Keys, "Query", data.StringDistance);
                        if (query == null)
                            return HelpLink(letter + "-error", link);

                        return HelpLink(letter, query);
                    }
                case HelpLinkPrefix.OperationLink:
                    {
                        string? operation = r.SelectInteractive(link, SymbolLogic<OperationSymbol>.AllUniqueKeys(), "Operation", data.StringDistance);
                        if (operation == null)
                            return HelpLink(letter + "-error", link);

                        return HelpLink(letter, operation);
                    }
                case HelpLinkPrefix.NamespaceLink:
                    {
                        string? @namespace = r.SelectInteractive(link, data.Namespaces, "Namespace", data.StringDistance);
                        if (@namespace == null)
                            return HelpLink(letter + "-error", link);

                        return HelpLink(letter, @namespace);
                    }
                case HelpLinkPrefix.AppendixLink:
                    {
                        string? appendix = r.SelectInteractive(link, data.Appendices, "Appendices", data.StringDistance);
                        if (appendix == null)
                            return HelpLink(letter + "-error", link);

                        return HelpLink(letter, appendix);
                    }
                default:
                    break;
            }

            return m.Value;
        });
    }

    static string HelpLink(string letter, string link)
    {
        return "[{0}:{1}]".FormatWith(letter, link);
    }
}

public static class HelpLinkPrefix
{
    public const string TypeLink = "t";
    public const string PropertyLink = "p";
    public const string QueryLink = "q";
    public const string OperationLink = "o";
    public const string NamespaceLink = "n";
    public const string AppendixLink = "a";
}
