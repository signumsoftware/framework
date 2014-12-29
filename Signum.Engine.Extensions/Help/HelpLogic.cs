using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Reflection;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Operations;
using Signum.Entities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Globalization;
using Signum.Engine.Maps;
using System.Linq.Expressions;
using Signum.Engine.Linq;
using System.IO;
using System.Xml;
using System.Resources;
using Signum.Utilities.Reflection;
using System.Diagnostics;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities.Basics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Signum.Entities.Help;
using Signum.Entities.Translation;
using Signum.Services;
using Signum.Engine.WikiMarkup;
using Signum.Engine.Authorization;


namespace Signum.Engine.Help
{
    public static class HelpLogic
    {
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<Type, EntityHelp>>> Types;
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, NamespaceHelp>>> Namespaces;
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, AppendixHelp>>> Appendices;
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<object, QueryHelp>>> Queries;
        public static ResetLazy<ConcurrentDictionary<CultureInfo, Dictionary<OperationSymbol, OperationHelp>>> Operations;

        public static Lazy<Dictionary<Type, List<object>>> TypeToQuery = new Lazy<Dictionary<Type, List<object>>>(() =>
        {
            var dqm = DynamicQueryManager.Current;

            return (from qn in dqm.GetQueryNames()
                    let imp = dqm.GetEntityImplementations(qn)
                    where !imp.IsByAll
                    from t in imp.Types
                    group qn by t into g
                    select KVP.Create(g.Key, g.ToList())).ToDictionary();

        });

        public static Dictionary<string, NamespaceHelp> CachedNamespacesHelp()
        {
            return Namespaces.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
            {
                var namespaces = AllTypes().GroupBy(type => type.Namespace);

                var dic = Database.Query<NamespaceHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.Name);

                return namespaces.ToDictionary(gr => gr.Key, gr => new NamespaceHelp(gr.Key, ci, dic.TryGetC(gr.Key), gr.ToArray()));
            }));
        }

        public static NamespaceHelp GetNamespaceHelp(string @namespace)
        {
            return CachedNamespacesHelp().GetOrThrow(@namespace).Do(a => a.AssertAllowed());
        }

        public static IEnumerable<NamespaceHelp> GetNamespaceHelps()
        {
            return CachedNamespacesHelp().Values.Where(a => a.IsAllowed() == null);
        }



        public static Dictionary<string, AppendixHelp> CachedAppendicesHelp()
        {
            return Appendices.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
                Database.Query<AppendixHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.UniqueName, a => new AppendixHelp(ci, a))));
        }

        public static AppendixHelp GetAppendixHelp(string name)
        {
            return CachedAppendicesHelp().GetOrThrow(name);
        }

        public static IEnumerable<AppendixHelp> GetAppendixHelps()
        {
            return CachedAppendicesHelp().Values.Where(a => a.IsAllowed() == null);
        }



        public static Dictionary<Type, EntityHelp> CachedEntityHelp()
        {
            return Types.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
            {
                using (ExecutionMode.Global())
                {
                    var dic = Database.Query<EntityHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.Type.ToType());

                    return AllTypes().ToDictionary(t => t, t => new EntityHelp(t, ci, dic.TryGetC(t)));
                }
            }));
        }

        public static EntityHelp GetEntityHelp(Type type)
        {
            return CachedEntityHelp().GetOrThrow(type).Do(a => a.AssertAllowed());
        }

        public static IEnumerable<EntityHelp> GetEntityHelps()
        {
            return CachedEntityHelp().Values.Where(a => a.IsAllowed() == null);
        }



        public static Dictionary<object, QueryHelp> CachedQueriesHelp()
        {
            return Queries.Value.GetOrAdd(GetCulture(), ci => GlobalContext(() =>
            {
                var dic = Database.Query<QueryHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.Query.ToQueryName());

                return AllQueries().ToDictionary(t => t, t => new QueryHelp(t, ci, dic.TryGetC(t)));
            }));
        }

        public static QueryHelp GetQueryHelp(object queryName)
        {
            return CachedQueriesHelp().GetOrThrow(queryName).Do(a => a.AssertAllowed());
        }


        public static Dictionary<OperationSymbol, OperationHelp> CachedOperationsHelp()
        {
            return Operations.Value.GetOrAdd(GetCulture(), ci =>
            {
                var dic = Database.Query<OperationHelpEntity>().Where(n => n.Culture == ci.ToCultureInfoEntity()).ToDictionary(a => a.Operation);

                return OperationLogic.AllSymbols().ToDictionary(o => o, o => new OperationHelp(o, ci, dic.TryGetC(o)));
            }).Where(a => OperationLogic.OperationAllowed(a.Key, inUserInterface: true)).ToDictionary();
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

            if (Schema.Current.ForceCultureInfo != null && dic.ContainsKey(Schema.Current.ForceCultureInfo.Name))
                return Schema.Current.ForceCultureInfo;

            throw new InvalidOperationException("No compatible CultureInfo found in the database for {0}".FormatWith(ci.Name));
        }



        public static List<Type> AllTypes()
        {
            return Schema.Current.Tables.Keys.Where(t => !t.IsEnumEntity()).ToList();
        }

        public static List<object> AllQueries()
        {
            return (from type in AllTypes()
                    from key in DynamicQueryManager.Current.GetTypeQueries(type).Keys
                    select key).Distinct().ToList();
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EntityHelpEntity>();
                sb.Include<NamespaceHelpEntity>();
                sb.Include<AppendixHelpEntity>();
                sb.Include<QueryHelpEntity>();
                sb.Include<OperationHelpEntity>();

                sb.AddUniqueIndex((EntityHelpEntity e) => new { e.Type, e.Culture });
                sb.AddUniqueIndexMList((EntityHelpEntity e) => e.Properties, mle => new { mle.Parent, mle.Element.Property });
                sb.AddUniqueIndex((NamespaceHelpEntity e) => new { e.Name, e.Culture });
                sb.AddUniqueIndex((AppendixHelpEntity e) => new { Name = e.UniqueName, e.Culture });
                sb.AddUniqueIndex((QueryHelpEntity e) => new { e.Query, e.Culture });
                sb.AddUniqueIndexMList((QueryHelpEntity e) => e.Columns, mle => new { mle.Parent, mle.Element.ColumnName });
                sb.AddUniqueIndex((OperationHelpEntity e) => new { e.Operation, e.Culture });

                Types = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<Type, EntityHelp>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<Type, EntityHelp>>(),
                    invalidateWith: new InvalidateWith(typeof(EntityHelpEntity)));

                Namespaces = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, NamespaceHelp>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<string, NamespaceHelp>>(),
                    invalidateWith: new InvalidateWith(typeof(NamespaceHelpEntity)));

                Appendices = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<string, AppendixHelp>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<string, AppendixHelp>>(),
                    invalidateWith: new InvalidateWith(typeof(AppendixHelpEntity)));

                Queries = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<object, QueryHelp>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<object, QueryHelp>>(),
                   invalidateWith: new InvalidateWith(typeof(QueryHelpEntity)));

                Operations = sb.GlobalLazy<ConcurrentDictionary<CultureInfo, Dictionary<OperationSymbol, OperationHelp>>>(() => new ConcurrentDictionary<CultureInfo, Dictionary<OperationSymbol, OperationHelp>>(),
                    invalidateWith: new InvalidateWith(typeof(OperationHelpEntity)));

                dqm.RegisterQuery(typeof(EntityHelpEntity), () =>
                    from e in Database.Query<EntityHelpEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Type,
                        Description = e.Description.Etc(100)
                    });

                dqm.RegisterQuery(typeof(NamespaceHelpEntity), () =>
                    from n in Database.Query<NamespaceHelpEntity>()
                    select new
                    {
                        Entity = n,
                        n.Id,
                        n.Name,
                        n.Culture,
                        Description = n.Description.Etc(100)
                    });

                dqm.RegisterQuery(typeof(AppendixHelpEntity), () =>
                    from a in Database.Query<AppendixHelpEntity>()
                    select new
                    {
                        Entity = a,
                        a.Id,
                        a.UniqueName,
                        a.Culture,
                        a.Title,
                        Description = a.Description.Etc(100)
                    });

                dqm.RegisterQuery(typeof(QueryHelpEntity), () =>
                     from q in Database.Query<QueryHelpEntity>()
                     select new
                     {
                         Entity = q,
                         q.Id,
                         q.Query,
                         q.Culture,
                         Description = q.Description.Etc(100)
                     });


                dqm.RegisterQuery(typeof(OperationHelpEntity), () =>
                     from o in Database.Query<OperationHelpEntity>()
                     select new
                     {
                         Entity = o,
                         o.Id,
                         o.Operation,
                         o.Culture,
                         Description = o.Description.Etc(100)
                     });

                new Graph<AppendixHelpEntity>.Execute(AppendixHelpOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();

                new Graph<NamespaceHelpEntity>.Execute(NamespaceHelpOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();

                new Graph<EntityHelpEntity>.Execute(EntityHelpOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();

                new Graph<QueryHelpEntity>.Execute(QueryHelpOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();
                OperationLogic.SetProtectedSave<QueryHelpEntity>(false);

                new Graph<OperationHelpEntity>.Execute(OperationHelpOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();
                OperationLogic.SetProtectedSave<OperationHelpEntity>(false);

                sb.Schema.Synchronizing += Schema_Synchronizing;

                sb.Schema.Table<OperationSymbol>().PreDeleteSqlSync += operation =>
                    Administrator.UnsafeDeletePreCommand(Database.Query<OperationHelpEntity>().Where(e => e.Operation == (OperationSymbol)operation));

                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += type =>
                    Administrator.UnsafeDeletePreCommand(Database.Query<EntityHelpEntity>().Where(e => e.Type == (TypeEntity)type));

                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += query =>
                    Administrator.UnsafeDeletePreCommand(Database.Query<QueryHelpEntity>().Where(e => e.Query == (QueryEntity)query));

                PermissionAuthLogic.RegisterPermissions(HelpPermissions.ViewHelp);
            }
        }

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            bool any =
                Database.Query<EntityHelpEntity>().Any() ||
                Database.Query<OperationHelpEntity>().Any() ||
                Database.Query<QueryHelpEntity>().Any() ||
                Database.Query<NamespaceHelpEntity>().Any() ||
                Database.Query<AppendixHelpEntity>().Any();

            if (!(any && replacements.Interactive && SafeConsole.Ask("Synchronize Help content?")))
                return null;

            SyncData data = new SyncData
            {
                Namespaces = AllTypes().Select(a => a.Namespace).ToHashSet(),
                Appendices = Database.Query<AppendixHelpEntity>().Select(a => a.UniqueName).ToHashSet(),
                StringDistance = new StringDistance()
            };

            var ns = SynchronizeNamespace(replacements, data);
            var appendix = SynchronizeAppendix(replacements, data);
            var types = SynchronizeTypes(replacements, data);
            var queries = SynchronizeQueries(replacements, data);
            var operations = SynchronizeOperations(replacements, data);

            return SqlPreCommand.Combine(Spacing.Double, ns, appendix, types, queries);
        }

        public class SyncData
        {
            public HashSet<string> Namespaces;
            public HashSet<string> Appendices;
            public StringDistance StringDistance;
        }

        static SqlPreCommand SynchronizeQueries(Replacements replacements, SyncData data)
        {
            var dic = Database.Query<QueryHelpEntity>().ToList();

            if (dic.IsEmpty())
                return null;

            var queriesByKey = DynamicQueryManager.Current.GetQueryNames().ToDictionary(a => QueryUtils.GetQueryUniqueKey(a));

            var table = Schema.Current.Table<QueryHelpEntity>();

            var replace = replacements.TryGetC(QueryLogic.QueriesKey);

            return dic.Select(qh =>
            {
                object queryName = queriesByKey.TryGetC(replace.TryGetC(qh.Query.Key) ?? qh.Query.Key);

                if (queryName == null)
                    return null; //PreDeleteSqlSync

                if (qh.Columns.Any())
                {
                    var columns = DynamicQueryManager.Current.GetQuery(queryName).Core.Value.StaticColumns;

                    Synchronizer.SynchronizeReplacing(replacements, "ColumnsOfQuery:" + QueryUtils.GetQueryUniqueKey(queryName),
                        columns.ToDictionary(a => a.Name),
                        qh.Columns.ToDictionary(a => a.ColumnName),
                        null,
                        (qn, oldQ) => qh.Columns.Remove(oldQ),
                        (qn, newQ, oldQ) =>
                        {
                            oldQ.ColumnName = newQ.Name;
                        });

                    foreach (var col in qh.Columns)
                        col.Description = SynchronizeContent(col.Description, replacements, data);
                }

                qh.Description = SynchronizeContent(qh.Description, replacements, data);

                return table.UpdateSqlSync(qh);
            }).Combine(Spacing.Simple);
        }

        static SqlPreCommand SynchronizeOperations(Replacements replacements, SyncData data)
        {
            var dic = Database.Query<OperationHelpEntity>().ToList();

            if (dic.IsEmpty())
                return null;

            var queriesByKey = DynamicQueryManager.Current.GetQueryNames().ToDictionary(a => QueryUtils.GetQueryUniqueKey(a));

            var table = Schema.Current.Table<OperationHelpEntity>();

            var replace = replacements.TryGetC(QueryLogic.QueriesKey);

            using (replacements.WithReplacedDatabaseName())
                return dic.Select(qh =>
                {
                    qh.Description = SynchronizeContent(qh.Description, replacements, data);

                    return table.UpdateSqlSync(qh);
                }).Combine(Spacing.Simple);
        }

        static SqlPreCommand SynchronizeTypes(Replacements replacements, SyncData data)
        {
            var dic = Database.Query<EntityHelpEntity>().ToList();

            if (dic.IsEmpty())
                return null;

            var typesByTableName = Schema.Current.Tables.ToDictionary(kvp => kvp.Value.Name.Name, kvp => kvp.Key);

            var replace = replacements.TryGetC(Replacements.KeyTables);

            var table = Schema.Current.Table<EntityHelpEntity>();

            using (replacements.WithReplacedDatabaseName())
                return dic.Select(eh =>
                {
                    Type type = typesByTableName.TryGetC(replace.TryGetC(eh.Type.TableName) ?? eh.Type.TableName);

                    if (type == null)
                        return null; //PreDeleteSqlSync

                    var repProperties = replacements.TryGetC(PropertyRouteLogic.PropertiesFor.FormatWith(type.FullName));
                    var routes = PropertyRoute.GenerateRoutes(type).ToDictionary(pr => { var ps = pr.PropertyString(); return repProperties.TryGetC(ps) ?? ps; });
                    eh.Properties.RemoveAll(p => !routes.ContainsKey(p.Property.Path));
                    foreach (var prop in eh.Properties)
                        prop.Description = SynchronizeContent(prop.Description, replacements, data);

                    eh.Description = SynchronizeContent(eh.Description, replacements, data);

                    return table.UpdateSqlSync(eh);
                }).Combine(Spacing.Simple);
        }

        static SqlPreCommand SynchronizeNamespace(Replacements replacements, SyncData data)
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
                    e.Name = replacements.TryGetC("namespaces").TryGetC(e.Name) ?? e.Name;

                    if (!data.Namespaces.Contains(e.Name))
                        return table.DeleteSqlSync(e);

                    e.Description = SynchronizeContent(e.Description, replacements, data);

                    return table.UpdateSqlSync(e);
                }).Combine(Spacing.Simple);
        }

        static SqlPreCommand SynchronizeAppendix(Replacements replacements, SyncData data)
        {
            var entities = Database.Query<AppendixHelpEntity>().ToList();

            if (entities.IsEmpty())
                return null;

            var table = Schema.Current.Table<AppendixHelpEntity>();

            using (replacements.WithReplacedDatabaseName())
                return entities.Select(e =>
                {
                    e.Description = SynchronizeContent(e.Description, replacements, data);

                    return table.UpdateSqlSync(e);
                }).Combine(Spacing.Simple);
        }




        static Lazy<XmlSchemaSet> Schemas = new Lazy<XmlSchemaSet>(() =>
        {
            XmlSchemaSet schemas = new XmlSchemaSet();
            Stream str = typeof(HelpLogic).Assembly.GetManifestResourceStream("Signum.Engine.Extensions.Help.SignumFrameworkHelp.xsd");
            schemas.Add("", XmlReader.Create(str));
            return schemas;
        });

        internal static XDocument LoadAndValidate(string fileName)
        {
            var document = XDocument.Load(fileName);

            List<Tuple<XmlSchemaException, string>> exceptions = new List<Tuple<XmlSchemaException, string>>();

            document.Document.Validate(Schemas.Value, (s, e) => exceptions.Add(Tuple.Create(e.Exception, fileName)));

            if (exceptions.Any())
                throw new InvalidOperationException("Error Parsing XML Help Files: " + exceptions.ToString(e => "{0} ({1}:{2}): {3}".FormatWith(
                 e.Item2, e.Item1.LineNumber, e.Item1.LinePosition, e.Item1.Message), "\r\n").Indent(3));

            return document;
        }

        public static readonly Regex HelpLinkRegex = new Regex(@"^(?<letter>[^:]+):(?<link>[^\|]*)(\|(?<text>.*))?$");

        static string SynchronizeContent(string content, Replacements r, SyncData data)
        {
            if (content == null)
                return null;

            return WikiMarkup.WikiParserExtensions.TokenRegex.Replace(content, m =>
            {
                var m2 = HelpLinkRegex.Match(m.Groups["content"].Value);

                if (!m2.Success)
                    return m.Value;

                string letter = m2.Groups["letter"].Value;
                string link = m2.Groups["link"].Value;
                string text = m2.Groups["text"].Value;

                switch (letter)
                {
                    case WikiFormat.EntityLink:
                        {
                            string type = r.SelectInteractive(link, TypeLogic.NameToType.Keys, "Type", data.StringDistance);

                            if (type == null)
                                return Link(letter + "-error", link, text);

                            return Link(letter, type, text);
                        }
                    case WikiFormat.PropertyLink:
                        {
                            string type = r.SelectInteractive(link.Before("."), TypeLogic.NameToType.Keys, "Type", data.StringDistance);

                            if (type == null)
                                return Link(letter + "-error", link, text);

                            var routes = PropertyRoute.GenerateRoutes(TypeLogic.GetType(type)).Select(a => a.PropertyString()).ToList();

                            string pr = r.SelectInteractive(link.After('.'), routes, "PropertyRoutes-" + type, data.StringDistance);

                            if (pr == null)
                                return Link(letter + "-error", link, text);

                            return Link(letter, type + "." + pr, text);
                        }
                    case WikiFormat.QueryLink:
                        {
                            string query = r.SelectInteractive(link, QueryLogic.QueryNames.Keys, "Query", data.StringDistance);

                            if (query == null)
                                return Link(letter + "-error", link, text);

                            return Link(letter, query, text);
                        }
                    case WikiFormat.OperationLink:
                        {
                            string operation = r.SelectInteractive(link, SymbolLogic<OperationSymbol>.AllUniqueKeys(), "Operation", data.StringDistance);

                            if (operation == null)
                                return Link(letter + "-error", link, text);

                            return Link(letter, operation, text);
                        }
                    case WikiFormat.Hyperlink: return m.Value;
                    case WikiFormat.NamespaceLink:
                        {
                            string @namespace = r.SelectInteractive(link, data.Namespaces, "Namespace", data.StringDistance);

                            if (@namespace == null)
                                return Link(letter + "-error", link, text);

                            return Link(letter, @namespace, text);
                        }
                    case WikiFormat.AppendixLink:
                        {
                            string appendix = r.SelectInteractive(link, data.Appendices, "Appendices", data.StringDistance);

                            if (appendix == null)
                                return Link(letter + "-error", link, text);

                            return Link(letter, appendix, text);
                        }
                    default:
                        break;
                }

                return m.Value;
            });
        }

        static string Link(string letter, string link, string text)
        {
            if (text.HasText())
                return "[{0}:{1}|{2}]".FormatWith(letter, link, text);
            else
                return "[{0}:{1}]".FormatWith(letter, link);
        }

        public static EntityHelpService GetEntityHelpService(Type type)
        {
            var entity = GetEntityHelp(type);

            return new EntityHelpService
            {
                Type = type,

                Info = GetHelpToolTipInfo(type.NiceName(), entity.Info, entity.Description, HelpUrls.EntityUrl(type)),

                Operations = entity.Operations.Where(o => o.Value.IsAllowed() == null).ToDictionary(kvp => kvp.Key,
                    kvp => GetHelpToolTipInfo(kvp.Value.OperationSymbol.NiceToString(), kvp.Value.Info, kvp.Value.UserDescription, HelpUrls.OperationUrl(type, kvp.Value.OperationSymbol))),

                Properties = entity.Properties.Where(o => o.Value.IsAllowed() == null).ToDictionary(kvp => kvp.Key,
                    kvp => GetHelpToolTipInfo(kvp.Value.PropertyInfo.NiceName(), kvp.Value.Info, kvp.Value.UserDescription, HelpUrls.PropertyUrl(kvp.Value.PropertyRoute))),
            };
        }

        public static QueryHelpService GetQueryHelpService(object queryName)
        {
            var entity = GetQueryHelp(queryName);

            var url = HelpUrls.QueryUrl(queryName);

            return new QueryHelpService
            {
                QueryName = queryName,

                Info = GetHelpToolTipInfo(QueryUtils.GetNiceName(queryName), entity.Info, entity.UserDescription, url),

                Columns = entity.Columns.Where(a => a.Value.IsAllowed() == null).ToDictionary(kvp => kvp.Key, kvp =>
                    GetHelpToolTipInfo(kvp.Value.NiceName, kvp.Value.Info, kvp.Value.UserDescription, url)),
            };
        }

        public static Dictionary<PropertyRoute, HelpToolTipInfo> GetPropertyRoutesService(List<PropertyRoute> routes)
        {
            return routes.Where(p => p.IsAllowed() == null).GroupBy(a => a.RootType).SelectMany(r =>
            {
                var entity = GetEntityHelp(r.Key);

                return r.Select(pr =>
                {
                    var simp = pr.SimplifyToPropertyOrRoot();

                    if (simp.PropertyRouteType == PropertyRouteType.Root)
                    {
                        return KVP.Create(pr, GetHelpToolTipInfo(simp.RootType.NiceName(), entity.Info, entity.Description, HelpUrls.EntityUrl(pr.RootType)));
                    }
                    else
                    {
                        var prop = entity.Properties.GetOrThrow(simp);

                        return KVP.Create(pr, GetHelpToolTipInfo(prop.PropertyInfo.NiceName(), prop.Info, prop.UserDescription, HelpUrls.PropertyUrl(pr)));
                    }
                });
            }).ToDictionary();
        }

        static HelpToolTipInfo GetHelpToolTipInfo(string title, string info, string description, string link)
        {
            return new HelpToolTipInfo
            {
                Title = title,
                Info = info.HasText() ? HelpWiki.NoLinkWikiSettings.WikiParse(info) : info,
                Description = description.HasText() ? HelpWiki.NoLinkWikiSettings.WikiParse(description) : description,
                Link = link
            };
        }

    }

    public static class WikiFormat
    {
        public const string EntityLink = "e";
        public const string PropertyLink = "p";
        public const string QueryLink = "q";
        public const string OperationLink = "o";
        public const string Hyperlink = "h";
        public const string NamespaceLink = "n";
        public const string AppendixLink = "a";

        public const string Separator = ":";
    }
}
