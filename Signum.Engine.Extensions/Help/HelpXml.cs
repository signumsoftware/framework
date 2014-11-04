using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Help;
using Signum.Utilities;

namespace Signum.Engine.Help
{
    public static class HelpXml
    {
        public static class AppendixXml
        {
            static readonly XName _Appendix = "Appendix";
            static readonly XName _Name = "Name";
            static readonly XName _Title = "Title";
            static readonly XName _Description = "Description";

            public static XDocument ToXDocument(AppendixHelpDN entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Appendix,
                           new XAttribute(_Name, entity.UniqueName),
                           new XAttribute(_Title, entity.Title),
                           new XElement(_Description, entity.Description)
                       )
                    );
            }

            public static void Load(AppendixHelpDN entity, XDocument document)
            {
                XElement ns = document.Element(_Appendix);
                if (ns.Element(_Name).Value != entity.UniqueName)
                    throw new InvalidOperationException("Name of the entity {0} does not match the one in the Xml document {1}".Formato(entity.UniqueName, ns.Element(_Name).Value));

                entity.Title = ns.Attribute(_Title).Value;
                entity.Description = ns.Element(_Description).Value;
            }

            public static string GetAppendixName(XDocument document)
            {
                if (document.Root.Name == _Appendix)
                    return document.Root.Attribute(_Name).Value;
                return null;
            }
        }

        public static class NamespaceXml
        {
            public static string GetFilePath(string @namespace)
            {
                return Path.Combine(Path.Combine(HelpLogic.HelpDirectory, HelpLogic.NamespacesDirectory), "{0}.help".Formato(@namespace));
            }

            static readonly XName _Namespace = "Namespace";
            static readonly XName _Name = "Name";
            static readonly XName _Description = "Description";

            public static XDocument ToXDocument(NamespaceHelpDN entity)
            {
                return new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement(_Namespace,
                           new XAttribute(_Name, entity.Name),
                           new XElement(_Description, entity.Description)
                       )
                    );
            }

            public static void Load(NamespaceHelpDN entity, XDocument document)
            {
                XElement ns = document.Element(_Namespace);
                if (ns.Element(_Name).Value != entity.Name)
                    throw new InvalidOperationException("Name of the entity {0} does not match the one in the Xml document {1}".Formato(entity.Name, ns.Element(_Name).Value));
                
                entity.Description = ns.Element(_Description).Value;
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


            //public static void Synchronize(string fileName, XDocument loadedDoc, string nameSpace, Func<string, string> syncContent)
            //{
            //    XElement loadedNs = loadedDoc.Element(_Namespace);

            //    var created = NamespaceHelp.Create(nameSpace);
            //    created.Description = syncContent(loadedNs.Element(_Description).Try(a => a.Value));

            //    if (fileName != created.FileName)
            //    {
            //        Console.WriteLine("FileName changed {0} -> {1}".Formato(fileName, created.FileName));
            //        File.Move(fileName, created.FileName);
            //    }

            //    if (!created.Save())
            //        Console.WriteLine("File deleted {1}".Formato(fileName, created.FileName));
            //}
        }

        public static class QueryXml
        {
            public static string GetFilePath(object key)
            {
                return Path.Combine(Path.Combine(HelpLogic.HelpDirectory, HelpLogic.QueriesDirectory), "{0}.help".Formato(QueryUtils.GetQueryUniqueKey(key)));
            }

            static readonly XName _Name = "Name";
            static readonly XName _Key = "Key";
            static readonly XName _Description = "Description";
            static readonly XName _Query = "Query";
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


            public static void Load(QueryHelpDN entity, XDocument document)
            {
                XElement element = document.Element(_Query);

                if (element.Attribute(_Key).Value != entity.Query.Name)
                    throw new InvalidOperationException("QueryName of the entity {0} does not match the one in the Xml document {1}".Formato(entity.Query.Name, element.Attribute(_Key).Value));

                entity.Description = element.Element(_Description).Try(d => d.Value);

                entity.Columns = element.Element(_Columns) == null ? new MList<QueryColumnHelpDN>() :
                    element.Element(_Columns).Elements(_Column).Select(c => new QueryColumnHelpDN
                    {
                        ColumnName = c.Attribute(_Name).Value,
                        Description = c.Value
                    }).ToMList();
            }

            public static string GetQueryFullName(XDocument document, string fileName)
            {
                if (document.Root.Name != _Query)
                    throw new InvalidOperationException("{0} does not have a {1} root".Formato(fileName, _Query));

                var result = document.Root.Attribute(_Key).Try(a => a.Value);

                if (string.IsNullOrEmpty(result))
                    throw new InvalidOperationException("{0} does not have a {1} attribute".Formato(fileName, _Key));

                return result;
            }

         

            //public static void Synchronize(string fileName, XDocument loaded, object queryName, Func<string, string> syncContent)
            //{
            //    XElement loadedQuery = loaded.Element(_Query);
            //    var created = QueryHelp.Create(queryName);

            //    bool changed = false;
            //    HelpTools.SynchronizeElements(loadedQuery, _Columns, _Columns, _Name, created.Columns, "Columns of {0}".Formato(QueryUtils.GetQueryUniqueKey(queryName)),
            //      (qc, element) => qc.UserDescription = syncContent(element.Element(_Description).Try(a => a.Value)),
            //      (action, column) =>
            //      {
            //          if (!changed)
            //          {
            //              Console.WriteLine("Synchronized {0} ".Formato(fileName));
            //              changed = true;
            //          }
            //          Console.WriteLine("  Column {0}: {1}".Formato(action, column));
            //      });

            //    created.UserDescription = syncContent(loadedQuery.Element(_Description).Try(a => a.Value));

            //    if (fileName != created.FileName)
            //    {
            //        Console.WriteLine("FileName changed {0} -> {1}".Formato(fileName, created.FileName));
            //        File.Move(fileName, created.FileName);
            //    }

            //    if (!created.Save())
            //        Console.WriteLine("File deleted {1}".Formato(fileName, created.FileName));
            //}
        }

        public static class EntityXml
        {
            public static string GetPath(Type type)
            {
                return Path.Combine(Path.Combine(HelpLogic.HelpDirectory, HelpLogic.EntitiesDirectory), "{0}.help".Formato(type.FullName));
            }

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

            public static void Load(EntityHelpDN entity, XDocument document)
            {
                XElement element = document.Element(_Entity);

                Type type = TypeLogic.DnToType.GetOrThrow(entity.Type); 

                entity.Description = element.Element(_Description).Try(d => d.Value);
                
                entity.Properties = element.Element(_Properties).Try(ps=>ps.Elements(_Property)).EmptyIfNull().Select(p=>new PropertyRouteHelpDN
                {
                     Property = PropertyRoute.Parse(type, p.Attribute(_Name).Value).ToPropertyRouteDN(),
                     Description = p.Value
                }).ToMList();
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

            //public static void Synchronize(string fileName, XDocument loaded, Type type, Func<string, string> syncContent)
            //{
            //    XElement loadedEntity = loaded.Element(_Entity);
            //    EntityHelp created = EntityHelp.Create(type);

            //    bool changed = false;
            //    Action change = () =>
            //    {
            //        if (!changed)
            //        {
            //            Console.WriteLine("Synchronized {0} ".Formato(fileName));
            //            changed = true;
            //        }
            //    };

            //    created.Description = syncContent(loadedEntity.Element(_Description).Try(a => a.Value));

            //    HelpTools.SynchronizeElements(loadedEntity, _Properties, _Property, _Name, created.Properties, "Properties of {0}".Formato(type.Name),
            //        (ph, elem) => ph.UserDescription = syncContent(elem.Value),
            //        (action, prop) =>
            //        {
            //            change();
            //            Console.WriteLine("  Property {0}: {1}".Formato(action, prop));
            //        });

            //    HelpTools.SynchronizeElements(loadedEntity, _Operations, _Operation, _Key, created.Operations.SelectDictionary(os => os.Key, v => v), "Operations of {0}".Formato(type.Name),
            //        (oh, op) => oh.UserDescription = syncContent(op.Value),
            //        (action, op) =>
            //        {
            //            change();
            //            Console.WriteLine("  Operation {0}: {1}".Formato(action, op));
            //        });


            //    if (fileName != created.FileName)
            //    {
            //        Console.WriteLine("FileName changed {0} -> {1}".Formato(fileName, created.FileName));
            //        File.Move(fileName, created.FileName);
            //    }

            //    if (!created.Save())
            //        Console.WriteLine("File deleted {1}".Formato(fileName, created.FileName));
            //}


            static readonly XName _FullName = "FullName";
            static readonly XName _Name = "Name";
            static readonly XName _Key = "Key";
            static readonly XName _Entity = "Entity";
            static readonly XName _Description = "Description";
            static readonly XName _Properties = "Properties";
            static readonly XName _Property = "Property";
            static readonly XName _Operations = "Operations";
            static readonly XName _Operation = "Operation";
            static readonly XName _Queries = "Queries";
            static readonly XName _Query = "Query";
            static readonly XName _Language = "Language";
        }

        //public static void SyncronizeAll()
        //{
        //    if (!Directory.Exists(HelpDirectory))
        //    {
        //        Directory.CreateDirectory(HelpDirectory);
        //    }

        //    Type[] types = Schema.Current.Tables.Keys.Where(t => !t.IsEnumEntity()).ToArray();

        //    Replacements r = new Replacements();

        //    StringDistance sd = new StringDistance();

        //    var namespaces = types.Select(type => type.Namespace).ToHashSet();

        //    //Namespaces
        //    {
        //        var namespacesDocuments = FileNames(NamespacesDirectory)
        //            .Select(fn => new { FileName = fn, XDocument = LoadAndValidate(fn) })
        //            .ToDictionary(p => NamespaceHelp.GetNamespaceName(p.XDocument, p.FileName), "Namespaces in HelpFiles");

        //        HelpTools.SynchronizeReplacing(r, "Namespace", namespacesDocuments, namespaces.ToDictionary(a => a),
        //         (nameSpace, pair) =>
        //         {
        //             File.Delete(pair.FileName);
        //             Console.WriteLine("Deleted {0}".Formato(pair.FileName));
        //         },
        //         (nameSpace, _) =>
        //         {
        //         },
        //         (nameSpace, pair, _) =>
        //         {
        //             NamespaceHelp.Synchronize(pair.FileName, pair.XDocument, nameSpace, s => SyncronizeContent(s, r, sd, namespaces));
        //         });
        //    }

        //    //Types
        //    {
        //        var should = types.ToDictionary(type => type.FullName);

        //        var current = FileNames(EntitiesDirectory)
        //            .Select(fn => new { FileName = fn, XDocument = LoadAndValidate(fn) })
        //            .ToDictionary(a => EntityHelp.GetEntityFullName(a.XDocument, a.FileName), "Types in HelpFiles");


        //        HelpTools.SynchronizeReplacing(r, "Type", current, should,
        //            (fullName, pair) =>
        //            {
        //                File.Delete(pair.FileName);
        //                Console.WriteLine("Deleted {0}".Formato(pair.FileName));
        //            },
        //            (fullName, type) =>
        //            {
        //            },
        //            (fullName, pair, type) =>
        //            {
        //                EntityHelp.Synchronize(pair.FileName, pair.XDocument, type, s => SyncronizeContent(s, r, sd, namespaces));
        //            });
        //    }

        //    //Queries
        //    {
        //        var should = .ToDictionary(q => QueryUtils.GetQueryUniqueKey(q));

        //        var current = FileNames(QueriesDirectory)
        //            .Select(fn => new { FileName = fn, XDocument = LoadAndValidate(fn) })
        //            .ToDictionary(p => QueryHelp.GetQueryFullName(p.XDocument, p.FileName), "Queries in HelpFiles");

        //        HelpTools.SynchronizeReplacing(r, "Query", current, should,
        //            (fullName, pair) =>
        //            {
        //                File.Delete(pair.FileName);
        //                Console.WriteLine("Deleted {0}".Formato(pair.FileName));
        //            },
        //            (fullName, query) => { },
        //            (fullName, oldFile, query) =>
        //            {
        //                QueryHelp.Synchronize(oldFile.FileName, oldFile.XDocument, query, s => SyncronizeContent(s, r, sd, namespaces));
        //            });
        //    }
        //}

    }
}
