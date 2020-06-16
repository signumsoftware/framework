using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Reflection;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Operations;
using Signum.Utilities;
using System.Globalization;
using Signum.Engine.Maps;
using System.IO;
using System.Xml;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities.Basics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Signum.Entities.Help;
using Signum.Engine.Authorization;
using System.Diagnostics.CodeAnalysis;

namespace Signum.Engine.Help
{
    public static class HelpLogic
    {
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<Type, TypeHelp>>> Types = null!;
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, NamespaceHelp>>> Namespaces = null!;
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, AppendixHelpEntity>>> Appendices = null!;
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<object, QueryHelp>>> Queries = null!;

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

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<TypeHelpEntity>()
                    .WithUniqueIndex(e => new { e.Type, e.Culture })
                    .WithUniqueIndexMList(e => e.Properties, mle => new { mle.Parent, mle.Element.Property })
                    .WithUniqueIndexMList(e => e.Operations, mle => new { mle.Parent, mle.Element.Operation })
                    .WithSave(TypeHelpOperation.Save)
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
                    .WithSave(NamespaceHelpOperation.Save)
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
                    .WithSave(AppendixHelpOperation.Save)
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

                sb.Schema.Synchronizing += Schema_Synchronizing;

                sb.Schema.Table<OperationSymbol>().PreDeleteSqlSync += operation =>
                    Administrator.UnsafeDeletePreCommandMList((TypeHelpEntity eh) => eh.Operations, Database.MListQuery((TypeHelpEntity eh) => eh.Operations).Where(mle => mle.Element.Operation == (OperationSymbol)operation));

                sb.Schema.Table<PropertyRouteEntity>().PreDeleteSqlSync += property =>
                    Administrator.UnsafeDeletePreCommandMList((TypeHelpEntity eh) => eh.Properties, Database.MListQuery((TypeHelpEntity eh) => eh.Properties).Where(mle => mle.Element.Property == (PropertyRouteEntity)property));

                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += type =>
                    Administrator.UnsafeDeletePreCommand(Database.Query<TypeHelpEntity>().Where(e => e.Type == (TypeEntity)type));

                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += query =>
                    Administrator.UnsafeDeletePreCommand(Database.Query<QueryHelpEntity>().Where(e => e.Query == (QueryEntity)query));

                Types = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<Type, TypeHelp>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<Type, TypeHelp>>(),
                 invalidateWith: new InvalidateWith(typeof(TypeHelpEntity)));

                Namespaces = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, NamespaceHelp>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<string, NamespaceHelp>>(),
                    invalidateWith: new InvalidateWith(typeof(NamespaceHelpEntity)));

                Appendices = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, AppendixHelpEntity>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<string, AppendixHelpEntity>>(),
                    invalidateWith: new InvalidateWith(typeof(AppendixHelpEntity)));

                Queries = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<object, QueryHelp>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<object, QueryHelp>>(),
                   invalidateWith: new InvalidateWith(typeof(QueryHelpEntity)));

                PermissionAuthLogic.RegisterPermissions(HelpPermissions.ViewHelp);
            }
        }

        public static NamespaceHelp GetNamespaceHelp(string @namespace) => CachedNamespacesHelp().GetOrThrow(@namespace).Do(a => a.AssertAllowed());
        public static IEnumerable<NamespaceHelp> GetNamespaceHelps() => CachedNamespacesHelp().Values.Where(a => a.IsAllowed() == null);
        public static Dictionary<string, NamespaceHelp> CachedNamespacesHelp()
        {
            return Namespaces.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
            {
                var namespaces = AllTypes().GroupBy(type => type.Namespace!);

                var dic = Database.Query<NamespaceHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.Name);

                return namespaces.ToDictionary(gr => gr.Key, gr => new NamespaceHelp(gr.Key, ci, dic.TryGetC(gr.Key), gr.ToArray()));
            }));
        }


        public static AppendixHelpEntity GetAppendixHelp(string uniqueName) => CachedAppendicesHelp().GetOrThrow(uniqueName);
        public static IEnumerable<AppendixHelpEntity> GetAppendixHelps() => CachedAppendicesHelp().Values;
        public static Dictionary<string, AppendixHelpEntity> CachedAppendicesHelp()
        {
            return Appendices.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
                Database.Query<AppendixHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.UniqueName)));
        }


        public static TypeHelp GetTypeHelp(Type type) => CachedEntityHelp().GetOrThrow(type).Do(a => a.AssertAllowed());
        public static IEnumerable<TypeHelp> GetEntityHelps() => CachedEntityHelp().Values.Where(a => a.IsAllowed() == null);
        public static Dictionary<Type, TypeHelp> CachedEntityHelp()
        {
            return Types.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
            {
                using (ExecutionMode.Global())
                {
                    var dic = Database.Query<TypeHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.Type.ToType());

                    return AllTypes().ToDictionary(t => t, t => new TypeHelp(t, ci, dic.TryGetC(t)));
                }
            }));
        }

        public static QueryHelp GetQueryHelp(object queryName) => CachedQueriesHelp().GetOrThrow(queryName).Do(a => a.AssertAllowed());
        public static Dictionary<object, QueryHelp> CachedQueriesHelp()
        {
            return Queries.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
            {
                var dic = Database.Query<QueryHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.Query.ToQueryName());

                return AllQueries().ToDictionary(t => t, t => new QueryHelp(t, ci, dic.TryGetC(t)));
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

            if (dic.ContainsKey(ci.Name))
                return ci;

            if (dic.ContainsKey(ci.Parent.Name))
                return ci.Parent;

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
                    var routes = PropertyRoute.GenerateRoutes(type).ToDictionary(pr => { var ps = pr.PropertyString(); return repProperties?.TryGetC(ps) ?? ps; });
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

            document.Document.Validate(Schemas.Value, (s, e) => exceptions.Add((e.Exception, fileName)));

            if (exceptions.Any())
                throw new InvalidOperationException("Error Parsing XML Help Files: " + exceptions.ToString(e => "{0} ({1}:{2}): {3}".FormatWith(
                 e.filename, e.exception.LineNumber, e.exception.LinePosition, e.Item1.Message), "\r\n").Indent(3));

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
}
