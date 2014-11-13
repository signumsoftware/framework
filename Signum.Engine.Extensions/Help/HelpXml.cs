using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Help;
using Signum.Entities.Reflection;
using Signum.Utilities;

namespace Signum.Engine.Help
{
    public static class HelpXml
    {
     


        public static class AppendixXml
        {
            public static readonly XName _Appendix = "Appendix";
            static readonly XName _Name = "Name";
            static readonly XName _Culture = "Culture";
            static readonly XName _Title = "Title";
            static readonly XName _Description = "Description";

            public static XDocument ToXDocument(AppendixHelpDN entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Appendix,
                           new XAttribute(_Name, entity.UniqueName),
                           new XAttribute(_Culture, entity.Culture),
                           new XAttribute(_Title, entity.Title),
                           new XElement(_Description, entity.Description)
                       )
                    );
            }

            public static ImportAction Load(XDocument document)
            {
                XElement element = document.Element(_Appendix);

                var ci = CultureInfoLogic.CultureInfoToEntity.Value.GetOrThrow(element.Attribute(_Culture).Value);
                var name = element.Attribute(_Name).Value;

                var entity = Database.Query<AppendixHelpDN>().SingleOrDefaultEx(a => a.Culture == ci && a.UniqueName == name) ??
                    new AppendixHelpDN
                    {
                         Culture = ci,
                         UniqueName = name,
                    }; 
             
                entity.Title = element.Attribute(_Title).Value;
                entity.Description = element.Element(_Description).Value;

                return Save(entity);
            }
        }

        public static class NamespaceXml
        {
            public static readonly XName _Namespace = "Namespace";
            static readonly XName _Culture = "Culture";
            static readonly XName _Name = "Name";
            static readonly XName _Title = "Title";
            static readonly XName _Description = "Description";

            public static XDocument ToXDocument(NamespaceHelpDN entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Namespace,
                           new XAttribute(_Name, entity.Name),
                           new XAttribute(_Title, entity.Title),
                           new XElement(_Description, entity.Description)
                       )
                    );
            }

            internal static string GetNamespaceName(XDocument document, string fileName)
            {
                if (document.Root.Name != _Namespace)
                    throw new InvalidOperationException("{0} does not have a {1} root".Formato(fileName, _Namespace));

                var result = document.Root.Attribute(_Name).Try(a => a.Value);

                if (string.IsNullOrEmpty(result))
                    throw new InvalidOperationException("{0} does not have a {1} attribute".Formato(fileName, _Name));

                return result;
            }

            public static ImportAction Load(XDocument document, Dictionary<string, string> namespaces)
            {
                XElement element = document.Element(_Name);

                var ci = CultureInfoLogic.CultureInfoToEntity.Value.GetOrThrow(element.Attribute(_Culture).Value);
                var name = SelectInteractive(element.Attribute(_Name).Value, namespaces, "namespaces");

                if (name == null)
                    return ImportAction.Skipped;

                var entity = Database.Query<NamespaceHelpDN>().SingleOrDefaultEx(a => a.Culture == ci && a.Name == name) ?? new NamespaceHelpDN
                    {
                        Culture = ci,
                        Name = name,
                    };

                entity.Title = element.Attribute(_Title).Value;
                entity.Description = element.Element(_Description).Value;

                return Save(entity);
            }
        }

        public static class QueryXml
        {
            static readonly XName _Name = "Name";
            static readonly XName _Key = "Key";
            static readonly XName _Description = "Description";
            static readonly XName _Culture = "Culture";
            public static readonly XName _Query = "Query";
            static readonly XName _Columns = "Columns";
            static readonly XName _Column = "Column";

            public static XDocument ToXDocument(QueryHelpDN entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Query,
                           new XAttribute(_Key, entity.Query.Name),
                           entity.Description.HasText() ? new XElement(_Description, entity.Description) : null,
                            entity.Columns.Any() ?
                               new XElement(_Columns,
                                   entity.Columns.Select(c => new XElement(_Column,
                                       new XAttribute(_Name, c.ColumnName),
                                       c.Description))
                               ) : null
                           )
                       );
            }


            public static ImportAction Load(XDocument document)
            {
                XElement element = document.Element(_Query);
                var ci = CultureInfoLogic.CultureInfoToEntity.Value.GetOrThrow(element.Attribute(_Culture).Value);
                var queryName = SelectInteractive(element.Attribute(_Key).Value, QueryLogic.QueryNames, "queries");

                if (queryName == null)
                    return ImportAction.Skipped;

                var query = QueryLogic.GetQuery(queryName);

                var entity = Database.Query<QueryHelpDN>().SingleOrDefaultEx(a => a.Culture == ci && a.Query == query) ??
                    new QueryHelpDN
                    {
                        Culture = ci,
                        Query = query,
                    };

                entity.Description = element.Element(_Description).Try(d => d.Value);

                var cols = element.Element(_Columns);
                if (cols != null)
                {
                    var queryColumns = DynamicQueryManager.Current.GetQuery(query).Core.Value.StaticColumns.Select(a => a.Name).ToDictionary(a => a);

                    foreach (var item in cols.Elements(_Column))
                    {
                        string name = item.Attribute(_Name).Value;
                        name = SelectInteractive(name, queryColumns, "columns of {0}".Formato(queryName));

                        if (name == null)
                            continue;

                        var col = entity.Columns.SingleOrDefaultEx(c => c.ColumnName == name);
                        if (col != null)
                        {
                            col.Description = item.Value;
                        }
                        else
                        {
                            entity.Columns.Add(new QueryColumnHelpDN
                            {
                                ColumnName = name,
                                Description = item.Value
                            });
                        }
                    }
                }

                return Save(entity);
            }
        }

        public static class OperationXml
        {
            static readonly XName _Name = "Name";
            static readonly XName _Key = "Key";
            static readonly XName _Description = "Description";
            static readonly XName _Culture = "Culture";
            public static readonly XName _Operation = "Operation";

            public static XDocument ToXDocument(OperationHelpDN entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Operation,
                           new XAttribute(_Key, entity.Operation.Key),
                           entity.Description.HasText() ? new XElement(_Description, entity.Description) : null
                           )
                       );
            }


            public static ImportAction Load(XDocument document)
            {
                XElement element = document.Element(_Operation);
                var ci = CultureInfoLogic.CultureInfoToEntity.Value.GetOrThrow(element.Attribute(_Culture).Value);
                var queryName = SelectInteractive(element.Attribute(_Key).Value, QueryLogic.QueryNames, "queries");

                if (queryName == null)
                    return ImportAction.Skipped;

                var query = QueryLogic.GetQuery(queryName);

                var entity = Database.Query<QueryHelpDN>().SingleOrDefaultEx(a => a.Culture == ci && a.Query == query) ??
                    new QueryHelpDN
                    {
                        Culture = ci,
                        Query = query,
                    };

                entity.Description = element.Element(_Description).Try(d => d.Value);

                return Save(entity);
            }
        }

        public static class EntityXml
        {
            static readonly XName _FullName = "FullName";
            static readonly XName _Name = "Name";
            static readonly XName _Key = "Key";
            static readonly XName _Culture = "Culture";
            public static readonly XName _Entity = "Entity";
            static readonly XName _Description = "Description";
            static readonly XName _Properties = "Properties";
            static readonly XName _Property = "Property";
            static readonly XName _Operations = "Operations";
            static readonly XName _Operation = "Operation";
            static readonly XName _Queries = "Queries";
            static readonly XName _Query = "Query";
            static readonly XName _Language = "Language";

            public static XDocument ToXDocument(EntityHelpDN entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(_Entity,
                           new XAttribute(_FullName, entity.Type.FullClassName),
                           entity.Description.HasText() ? new XElement(_Description, entity.Description) : null,
                           entity.Properties.Any() ? new XElement(_Properties,
                               entity.Properties.Select(p => new XElement(_Property,
                                   new XAttribute(_Name, p.Property.Path),
                                   p.Description))
                           ) : null,
                           entity.Operations.Any() ? new XElement(_Operations,
                               entity.Operations.Select(o => new XElement(_Operation,
                                   new XAttribute(_Key, o.Operation.Key),
                                   o.Description))
                           ) : null
                       )
                   );
            }

            public static ImportAction Load(XDocument document, Dictionary<string, Type> typesByFullName)
            {
                XElement element = document.Element(_Entity);
                var ci = CultureInfoLogic.CultureInfoToEntity.Value.GetOrThrow(element.Attribute(_Culture).Value);
                var fullName = element.Attribute(_FullName).Value;
                var type = SelectInteractive(fullName, typesByFullName, "types");

                if(type == null)
                    return ImportAction.Skipped;

                var typeDN = type.ToTypeDN();

                var entity = Database.Query<EntityHelpDN>().SingleOrDefaultEx(a => a.Culture == ci && a.Type == typeDN) ??
                    new EntityHelpDN
                    {
                        Culture = ci,
                        Type = typeDN,
                    };

                entity.Description = element.Element(_Description).Try(d => d.Value);

                var props = element.Element(_Properties);
                if (props != null)
                {
                    var properties = PropertyRouteLogic.RetrieveOrGenerateProperties(typeDN).ToDictionary(a => a.Path);

                    foreach (var item in props.Elements(_Property))
                    {
                        string name = item.Attribute(_Name).Value;

                        var property = SelectInteractive(name, properties, "properties for {0}".Formato(type.Name));

                        if (name == null)
                            continue;

                        var col = property.IsNew ? null : entity.Properties.SingleOrDefaultEx(c => c.Property == property);
                        if (col != null)
                        {
                            col.Description = item.Value;
                        }
                        else
                        {
                            entity.Properties.Add(new PropertyRouteHelpDN
                            {
                                Property = property,
                                Description = item.Value
                            });
                        }
                    }
                }


                return Save(entity);
            }


            public static string GetEntityFullName(XDocument document, string fileName)
            {
                if (document.Root.Name != _Entity)
                    throw new InvalidOperationException("{0} does not have a {1} root".Formato(fileName, _Entity));

                var result = document.Root.Attribute(_FullName).Try(a => a.Value);

                if (string.IsNullOrEmpty(result))
                    throw new InvalidOperationException("{0} does not have a {1} attribute".Formato(fileName, _FullName));

                return result;
            }
        }

        public static ImportAction Save(Entity entity)
        {
            if (!GraphExplorer.HasChanges(entity))
                return ImportAction.NoChanges;

            var result = entity.IsNew ? ImportAction.Inserted : ImportAction.Updated;

            using (OperationLogic.AllowSave(entity.GetType()))
                entity.Save();

            return result;
        }

        public static string EntitiesDirectory = "Entity";
        public static string QueriesDirectory = "Query";
        public static string NamespacesDirectory = "Namespace";
        public static string AppendicesDirectory = "Appendix";

        public static T SelectInteractive<T>(string str, Dictionary<string, T> dictionary, string context) where T :class
        {
            T result = dictionary.TryGetC(str);
            
            if(result != null)
                return result;

            StringDistance sd = new StringDistance();

            var list = dictionary.Keys.Select(s => new { s, lcs = sd.LongestCommonSubsequence(str, s) }).OrderByDescending(s => s.lcs).Select(a => a.s).ToList();

            var cs = new ConsoleSwitch<int, string>("{0} has been renamed in {1}".Formato(str, context));
            cs.Load(list);
            string selected = cs.Choose();

            if (selected == null)
                return null;

            return dictionary.GetOrThrow(selected); 

        }

        static string RemoveInvalid(string name)
        {
            return Regex.Replace(name, "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]", "");
        }

        public static void ExportAll(string directoryName = "../../Help")
        {
            bool? replace = null;

            foreach (var ah in Database.Query<AppendixHelpDN>())
            {
                string path = Path.Combine(directoryName, ah.Culture.Name, AppendicesDirectory, "{0}.{1}.help".Formato(RemoveInvalid(ah.UniqueName), ah.Culture.Name));

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".Formato(path)))
                    AppendixXml.ToXDocument(ah).Save(path);
            }

            foreach (var nh in Database.Query<NamespaceHelpDN>())
            {
                string path = Path.Combine(directoryName, nh.Culture.Name, NamespacesDirectory, "{0}.{1}.help".Formato(RemoveInvalid(nh.Name), nh.Culture.Name));

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".Formato(path)))
                    NamespaceXml.ToXDocument(nh).Save(path);
            }

            foreach (var eh in Database.Query<EntityHelpDN>())
            {
                string path = Path.Combine(directoryName, eh.Culture.Name, EntitiesDirectory, "{0}.{1}.help".Formato(RemoveInvalid(eh.Type.CleanName), eh.Culture.Name));

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".Formato(path)))
                    EntityXml.ToXDocument(eh).Save(path);
            }

            foreach (var qh in Database.Query<QueryHelpDN>())
            {
                string path = Path.Combine(directoryName, qh.Culture.Name, QueriesDirectory, "{0}.{1}.help".Formato(RemoveInvalid(qh.Query.Key), qh.Culture.Name));

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".Formato(path)))
                    QueryXml.ToXDocument(qh).Save(path);
            }

            foreach (var qh in Database.Query<OperationHelpDN>())
            {
                string path = Path.Combine(directoryName, qh.Culture.Name, QueriesDirectory, "{0}.{1}.help".Formato(RemoveInvalid(qh.Operation.Key), qh.Culture.Name));

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".Formato(path)))
                    OperationXml.ToXDocument(qh).Save(path);
            }
        }

        public static void ImportAll(string directoryName = "../../Help")
        {
            var namespaces = HelpLogic.AllTypes().Select(a => a.Namespace).Distinct().ToDictionary(a => a);

            var types = HelpLogic.AllTypes().ToDictionary(a=>a.FullName);

            foreach (var path in Directory.GetFiles(directoryName, "*.help", SearchOption.AllDirectories))
            {
                try
                {
                    XDocument doc = XDocument.Load(path);

                    ImportAction action = 
                        doc.Root.Name == AppendixXml._Appendix ? AppendixXml.Load(doc):
                        doc.Root.Name == NamespaceXml._Namespace ? NamespaceXml.Load(doc, namespaces):
                        doc.Root.Name == EntityXml._Entity ? EntityXml.Load(doc, types):
                        doc.Root.Name == QueryXml._Query ? QueryXml.Load(doc) :
                        doc.Root.Name == OperationXml._Operation ? OperationXml.Load(doc) :
                        new InvalidOperationException("Unknown Xml root: " + doc.Root.Name).Throw<ImportAction>();

                    ConsoleColor color =
                        action == ImportAction.Inserted ? ConsoleColor.Green :
                        action == ImportAction.Updated ? ConsoleColor.DarkGreen :
                        action == ImportAction.Skipped ? ConsoleColor.Yellow :
                        action == ImportAction.NoChanges ? ConsoleColor.DarkGray :
                        new InvalidOperationException("Unexpected action").Throw<ConsoleColor>();

                    SafeConsole.WriteLineColor(color, " {0} {1}".Formato(action, path));
                }
                catch (Exception e)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Red, " Error {0}:\r\n\t".Formato(path) + e.Message);
                }
            }
        }
    }

    public enum ImportAction
    {
        Skipped,
        NoChanges,
        Updated,
        Inserted,
    }
}
