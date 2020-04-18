using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Help;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

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
                element.Element(_Description)?.Do(d => entity.Description = d.Value);

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
                           entity.Title.HasText() ? new XAttribute(_Title, entity.Title) : null,
                           entity.Description.HasText() ? new XElement(_Description, entity.Description) : null
                       )
                    );
            }

            internal static string GetNamespaceName(XDocument document, string fileName)
            {
                if (document.Root.Name != _Namespace)
                    throw new InvalidOperationException("{0} does not have a {1} root".FormatWith(fileName, _Namespace));

                var result = document.Root.Attribute(_Name)?.Value;

                if (!result.HasText())
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

                entity.Title = element.Attribute(_Title)?.Value;
                entity.Description = element.Element(_Description)?.Value;

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

                entity.Description = element.Element(_Description)?.Value;

                var cols = element.Element(_Columns);
                if (cols != null)
                {
                    var queryColumns = QueryLogic.Queries.GetQuery(queryName).Core.Value.StaticColumns.Select(a => a.Name).ToDictionary(a => a);

                    foreach (var item in cols.Elements(_Column))
                    {
                        string? name = item.Attribute(_Name).Value;
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
                            entity.Columns.Add(new QueryColumnHelpEmbedded
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
#pragma warning disable 414
            static readonly XName _Queries = "Queries";
            static readonly XName _Query = "Query";
            static readonly XName _Language = "Language";
#pragma warning restore 414

            public static XDocument ToXDocument(TypeHelpEntity entity)
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

                var entity = Database.Query<TypeHelpEntity>().SingleOrDefaultEx(a => a.Culture == ci && a.Type == typeEntity) ??
                    new TypeHelpEntity
                    {
                        Culture = ci,
                        Type = typeEntity,
                    };

                element.Element(_Description)?.Do(d => entity.Description = d.Value);

                var props = element.Element(_Properties);
                if (props != null)
                {
                    var properties = PropertyRouteLogic.RetrieveOrGenerateProperties(typeEntity).ToDictionary(a => a.Path);

                    foreach (var item in props.Elements(_Property))
                    {
                        string name = item.Attribute(_Name).Value;

                        var property = SelectInteractive(name, properties, "properties for {0}".FormatWith(type.Name));
                        if (property == null)
                            continue;

                        var col = property.IsNew ? null : entity.Properties.SingleOrDefaultEx(c => c.Property.Is(property));
                        if (col != null)
                        {
                            col.Description = item.Value;
                        }
                        else
                        {
                            entity.Properties.Add(new PropertyRouteHelpEmbedded
                            {
                                Property = property,
                                Description = item.Value
                            });
                        }
                    }
                }

                var opers = element.Element(_Operations);
                if (opers != null)
                {
                    var operations = OperationLogic.TypeOperations(typeEntity.ToType()).ToDictionary(a => a.OperationSymbol.Key, a => a.OperationSymbol);

                    foreach (var item in opers.Elements(_Operation))
                    {
                        string name = item.Attribute(_Name).Value;
                        var operation = SelectInteractive(name, operations, "operations for {0}".FormatWith(type.Name));

                        if (operation == null)
                            continue;

                        var col = entity.Operations.SingleOrDefaultEx(c => c.Operation.Is(operation));
                        if (col != null)
                        {
                            col.Description = item.Value;
                        }
                        else
                        {
                            entity.Operations.Add(new OperationHelpEmbedded
                            {
                                Operation = operation,
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

                var result = document.Root.Attribute(_FullName)?.Value;

                if (!result.HasText())
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

        public static string TypesDirectory = "Types";
        public static string QueriesDirectory = "Query";
        public static string OperationsDirectory = "Operation";
        public static string NamespacesDirectory = "Namespace";
        public static string AppendicesDirectory = "Appendix";

        public static T? SelectInteractive<T>(string str, Dictionary<string, T> dictionary, string context) where T :class
        {
            T? result = dictionary.TryGetC(str);
            
            if(result != null)
                return result;

            StringDistance sd = new StringDistance();

            var list = dictionary.Keys.Select(s => new { s, lcs = sd.LongestCommonSubsequence(str, s) }).OrderByDescending(s => s.lcs!).Select(a => a.s!).ToList();

            var cs = new ConsoleSwitch<int, string>("{0} has been renamed in {1}".FormatWith(str, context));
            cs.Load(list);

            string? selected = cs.Choose();
            if (selected == null)
                return null;

            return dictionary.GetOrThrow(selected); 

        }

        static string RemoveInvalid(string name)
        {
            return Regex.Replace(name, "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]", "");
        }

        public static void ExportAll(string directoryName)
        {
            HashSet<CultureInfo> cultures = new HashSet<CultureInfo>();
            cultures.AddRange(Database.Query<AppendixHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()));
            cultures.AddRange(Database.Query<NamespaceHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()));
            cultures.AddRange(Database.Query<TypeHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()));
            cultures.AddRange(Database.Query<QueryHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()));
            if (Directory.Exists(directoryName))
                cultures.AddRange(new DirectoryInfo(directoryName).GetDirectories().Select(c => CultureInfo.GetCultureInfo(c.Name)));

            foreach (var ci in cultures)
            {
                ExportCulture(directoryName, ci);
            }       
        }

        public static void ExportCulture(string directoryName, CultureInfo ci)
        {
            bool? replace = null;
            bool? delete = null;

            SyncFolder(ref replace, ref delete, Path.Combine(directoryName, ci.Name, AppendicesDirectory),
                Database.Query<AppendixHelpEntity>().Where(ah => ah.Culture.Name == ci.Name).ToList(),
                ah => "{0}.{1}.help".FormatWith(RemoveInvalid(ah.UniqueName), ah.Culture.Name),
                ah => AppendixXml.ToXDocument(ah));

            SyncFolder(ref replace, ref delete, Path.Combine(directoryName, ci.Name, NamespacesDirectory),
                Database.Query<NamespaceHelpEntity>().Where(nh => nh.Culture.Name == ci.Name).ToList(),
                nh => "{0}.{1}.help".FormatWith(RemoveInvalid(nh.Name), nh.Culture.Name),
                nh => NamespaceXml.ToXDocument(nh));

            SyncFolder(ref replace, ref delete, Path.Combine(directoryName, ci.Name, TypesDirectory),
                Database.Query<TypeHelpEntity>().Where(th => th.Culture.Name == ci.Name).ToList(),
                th => "{0}.{1}.help".FormatWith(RemoveInvalid(th.Type.CleanName), th.Culture.Name),
                th => EntityXml.ToXDocument(th));

            SyncFolder(ref replace, ref delete, Path.Combine(directoryName, ci.Name, QueriesDirectory),
                Database.Query<QueryHelpEntity>().Where(qh => qh.Culture.Name == ci.Name).ToList(),
                qh => "{0}.{1}.help".FormatWith(RemoveInvalid(qh.Query.Key), qh.Culture.Name),
                qh => QueryXml.ToXDocument(qh));
        }

        public static void SyncFolder<T>(ref bool? replace, ref bool? delete, string folder, List<T> should,  Func<T, string> fileName, Func<T, XDocument> toXML)
        {
            if (should.Any() && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var deleteLocal = delete;
            var replaceLocal = replace;

            SafeConsole.WriteLineColor(ConsoleColor.Gray, "Exporting to " + folder);
            Synchronizer.Synchronize(
                newDictionary: should.ToDictionary(fileName),
                oldDictionary: Directory.GetFiles(folder).ToDictionary(a => Path.GetFileName(a)),
                createNew: (fileName, entity) => {
                    toXML(entity).Save(Path.Combine(folder, fileName));
                    SafeConsole.WriteLineColor(ConsoleColor.Green, " Created " + fileName);
                },
                removeOld: (fileName, fullName) =>
                {
                    if (SafeConsole.Ask(ref deleteLocal, "Delete {0}?".FormatWith(fileName)))
                    {
                        File.Delete(fullName);
                        SafeConsole.WriteLineColor(ConsoleColor.Red, " Deleted " + fileName);
                    }
                }, merge: (fileName, entity, fullName) =>
                {
                    var xml = toXML(entity);

                    var newBytes = new MemoryStream().Do(ms => xml.Save(ms)).ToArray();
                    var oldBytes = File.ReadAllBytes(fullName);

                    if (!MemComparer.Equals(newBytes, oldBytes))
                    {
                        if (SafeConsole.Ask(ref replaceLocal, " Override {0}?".FormatWith(fileName)))
                        {
                            xml.Save(Path.Combine(folder, fileName));
                            SafeConsole.WriteLineColor(ConsoleColor.Yellow, " Overriden " + fileName);
                        }
                    }
                    else
                    {
                        SafeConsole.WriteLineColor(ConsoleColor.DarkGray, " Identical " + fileName);
                    }
                });

            delete = deleteLocal;
            replace = replaceLocal;
        }

        public static void ImportAll(string directoryName)
        {
            var namespaces = HelpLogic.AllTypes().Select(a => a.Namespace!).Distinct().ToDictionary(a => a);

            var types = HelpLogic.AllTypes().ToDictionary(a => a.FullName!);

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
                        throw new InvalidOperationException("Unknown Xml root: " + doc.Root.Name);

                    ConsoleColor color =
                        action == ImportAction.Inserted ? ConsoleColor.Green :
                        action == ImportAction.Updated ? ConsoleColor.DarkGreen :
                        action == ImportAction.Skipped ? ConsoleColor.Yellow :
                        action == ImportAction.NoChanges ? ConsoleColor.DarkGray :
                        throw new InvalidOperationException("Unexpected action");

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
            ImportExportHelp(@"..\..\..\Help");
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
