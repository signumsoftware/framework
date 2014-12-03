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

            public static XDocument ToXDocument(AppendixHelpEntity entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Appendix,
                           new XAttribute(_Name, entity.UniqueName),
                           new XAttribute(_Culture, entity.Culture.Name),
                           new XAttribute(_Title, entity.Title),
                           entity.Description.HasText() ? new XElement(_Description, entity.Description) : null
                       )
                    );
            }

            public static ImportAction Load(XDocument document)
            {
                XElement element = document.Element(_Appendix);

                var ci = CultureInfoLogic.CultureInfoToEntity.Value.GetOrThrow(element.Attribute(_Culture).Value);
                var name = element.Attribute(_Name).Value;

                var entity = Database.Query<AppendixHelpEntity>().SingleOrDefaultEx(a => a.Culture == ci && a.UniqueName == name) ??
                    new AppendixHelpEntity
                    {
                         Culture = ci,
                         UniqueName = name,
                    }; 
             
                entity.Title = element.Attribute(_Title).Value;
                element.Element(_Description).TryDo(d => entity.Description = d.Value);

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

            public static XDocument ToXDocument(NamespaceHelpEntity entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Namespace,
                           new XAttribute(_Name, entity.Name),
                           new XAttribute(_Culture, entity.Culture.Name),
                           new XAttribute(_Title, entity.Title),
                           entity.Description.HasText() ? new XElement(_Description, entity.Description) : null
                       )
                    );
            }

            internal static string GetNamespaceName(XDocument document, string fileName)
            {
                if (document.Root.Name != _Namespace)
                    throw new InvalidOperationException("{0} does not have a {1} root".FormatWith(fileName, _Namespace));

                var result = document.Root.Attribute(_Name).Try(a => a.Value);

                if (string.IsNullOrEmpty(result))
                    throw new InvalidOperationException("{0} does not have a {1} attribute".FormatWith(fileName, _Name));

                return result;
            }

            public static ImportAction Load(XDocument document, Dictionary<string, string> namespaces)
            {
                XElement element = document.Element(_Namespace);

                var ci = CultureInfoLogic.CultureInfoToEntity.Value.GetOrThrow(element.Attribute(_Culture).Value);
                var name = SelectInteractive(element.Attribute(_Name).Value, namespaces, "namespaces");

                if (name == null)
                    return ImportAction.Skipped;

                var entity = Database.Query<NamespaceHelpEntity>().SingleOrDefaultEx(a => a.Culture == ci && a.Name == name) ?? new NamespaceHelpEntity
                    {
                        Culture = ci,
                        Name = name,
                    };

                entity.Title = element.Attribute(_Title).Value;
                element.Element(_Description).TryDo(d => entity.Description = d.Value);

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

            public static XDocument ToXDocument(QueryHelpEntity entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Query,
                           new XAttribute(_Key, entity.Query.Key),
                           new XAttribute(_Culture, entity.Culture.Name),
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

                var query = QueryLogic.GetQueryEntity(queryName);

                var entity = Database.Query<QueryHelpEntity>().SingleOrDefaultEx(a => a.Culture == ci && a.Query == query) ??
                    new QueryHelpEntity
                    {
                        Culture = ci,
                        Query = query,
                    };

                element.Element(_Description).TryDo(d => entity.Description = d.Value);

                var cols = element.Element(_Columns);
                if (cols != null)
                {
                    var queryColumns = DynamicQueryManager.Current.GetQuery(queryName).Core.Value.StaticColumns.Select(a => a.Name).ToDictionary(a => a);

                    foreach (var item in cols.Elements(_Column))
                    {
                        string name = item.Attribute(_Name).Value;
                        name = SelectInteractive(name, queryColumns, "columns of {0}".FormatWith(queryName));

                        if (name == null)
                            continue;

                        var col = entity.Columns.SingleOrDefaultEx(c => c.ColumnName == name);
                        if (col != null)
                        {
                            col.Description = item.Value;
                        }
                        else
                        {
                            entity.Columns.Add(new QueryColumnHelpEntity
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

            public static XDocument ToXDocument(OperationHelpEntity entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Operation,
                           new XAttribute(_Key, entity.Operation.Key),
                           new XAttribute(_Culture, entity.Culture.Name),
                           entity.Description.HasText() ? new XElement(_Description, entity.Description) : null
                           )
                       );
            }


            public static ImportAction Load(XDocument document)
            {
                XElement element = document.Element(_Operation);
                var ci = CultureInfoLogic.CultureInfoToEntity.Value.GetOrThrow(element.Attribute(_Culture).Value);
                var operation = SelectInteractive(element.Attribute(_Key).Value, SymbolLogic<OperationSymbol>.Symbols.ToDictionary(a => a.Key), "operation");

                if (operation == null)
                    return ImportAction.Skipped;

                var entity = Database.Query<OperationHelpEntity>().SingleOrDefaultEx(a => a.Culture == ci && a.Operation == operation) ??
                    new OperationHelpEntity
                    {
                        Culture = ci,
                        Operation = operation,
                    };

                element.Element(_Description).Try(d => entity.Description = d.Value);

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

            public static XDocument ToXDocument(EntityHelpEntity entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(_Entity,
                           new XAttribute(_FullName, entity.Type.FullClassName),
                           new XAttribute(_Culture, entity.Culture.Name),
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

                var typeEntity = type.ToTypeEntity();

                var entity = Database.Query<EntityHelpEntity>().SingleOrDefaultEx(a => a.Culture == ci && a.Type == typeEntity) ??
                    new EntityHelpEntity
                    {
                        Culture = ci,
                        Type = typeEntity,
                    };

                element.Element(_Description).TryDo(d => entity.Description = d.Value);

                var props = element.Element(_Properties);
                if (props != null)
                {
                    var properties = PropertyRouteLogic.RetrieveOrGenerateProperties(typeEntity).ToDictionary(a => a.Path);

                    foreach (var item in props.Elements(_Property))
                    {
                        string name = item.Attribute(_Name).Value;

                        var property = SelectInteractive(name, properties, "properties for {0}".FormatWith(type.Name));

                        if (name == null)
                            continue;

                        var col = property.IsNew ? null : entity.Properties.SingleOrDefaultEx(c => c.Property.Is(property));
                        if (col != null)
                        {
                            col.Description = item.Value;
                        }
                        else
                        {
                            entity.Properties.Add(new PropertyRouteHelpEntity
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
                    throw new InvalidOperationException("{0} does not have a {1} root".FormatWith(fileName, _Entity));

                var result = document.Root.Attribute(_FullName).Try(a => a.Value);

                if (string.IsNullOrEmpty(result))
                    throw new InvalidOperationException("{0} does not have a {1} attribute".FormatWith(fileName, _FullName));

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
        public static string OperationsDirectory = "Operation";
        public static string NamespacesDirectory = "Namespace";
        public static string AppendicesDirectory = "Appendix";

        public static T SelectInteractive<T>(string str, Dictionary<string, T> dictionary, string context) where T :class
        {
            T result = dictionary.TryGetC(str);
            
            if(result != null)
                return result;

            StringDistance sd = new StringDistance();

            var list = dictionary.Keys.Select(s => new { s, lcs = sd.LongestCommonSubsequence(str, s) }).OrderByDescending(s => s.lcs).Select(a => a.s).ToList();

            var cs = new ConsoleSwitch<int, string>("{0} has been renamed in {1}".FormatWith(str, context));
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

            foreach (var ah in Database.Query<AppendixHelpEntity>())
            {
                string path = Path.Combine(directoryName, ah.Culture.Name, AppendicesDirectory, "{0}.{1}.help".FormatWith(RemoveInvalid(ah.UniqueName), ah.Culture.Name));

                FileTools.CreateParentDirectory(path);

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".FormatWith(path)))
                    AppendixXml.ToXDocument(ah).Save(path);
            }

            foreach (var nh in Database.Query<NamespaceHelpEntity>())
            {
                string path = Path.Combine(directoryName, nh.Culture.Name, NamespacesDirectory, "{0}.{1}.help".FormatWith(RemoveInvalid(nh.Name), nh.Culture.Name));

                FileTools.CreateParentDirectory(path);

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".FormatWith(path)))
                    NamespaceXml.ToXDocument(nh).Save(path);
            }

            foreach (var eh in Database.Query<EntityHelpEntity>())
            {
                string path = Path.Combine(directoryName, eh.Culture.Name, EntitiesDirectory, "{0}.{1}.help".FormatWith(RemoveInvalid(eh.Type.CleanName), eh.Culture.Name));

                FileTools.CreateParentDirectory(path);

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".FormatWith(path)))
                    EntityXml.ToXDocument(eh).Save(path);
            }

            foreach (var qh in Database.Query<QueryHelpEntity>())
            {
                string path = Path.Combine(directoryName, qh.Culture.Name, QueriesDirectory, "{0}.{1}.help".FormatWith(RemoveInvalid(qh.Query.Key), qh.Culture.Name));

                FileTools.CreateParentDirectory(path);

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".FormatWith(path)))
                    QueryXml.ToXDocument(qh).Save(path);
            }

            foreach (var qh in Database.Query<OperationHelpEntity>())
            {
                string path = Path.Combine(directoryName, qh.Culture.Name, OperationsDirectory, "{0}.{1}.help".FormatWith(RemoveInvalid(qh.Operation.Key), qh.Culture.Name));

                FileTools.CreateParentDirectory(path);

                if (!File.Exists(path) || SafeConsole.Ask(ref replace, "Overwrite {0}?".FormatWith(path)))
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

                    SafeConsole.WriteLineColor(color, " {0} {1}".FormatWith(action, path));
                }
                catch (Exception e)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Red, " Error {0}:\r\n\t".FormatWith(path) + e.Message);
                }
            }
        }

        public static void ImportExportHelp()
        {
            ImportExportHelp("../../Help");
        }

        public static void ImportExportHelp(string directoryName)
        {
            retry:
             Console.WriteLine("You want to export (e) or import (i) Help? (nothing to exit)");

            switch (Console.ReadLine().ToLower())
            {
                case "": return;
                case "e": ExportAll(directoryName); break;
                case "i": ImportAll(directoryName); break;
                default:
                    goto retry;
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
