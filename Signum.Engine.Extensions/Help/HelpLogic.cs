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


namespace Signum.Engine.Help
{
    public static class HelpLogic
    {
        public static string EntitiesDirectory = "";
        public static string QueriesDirectory = "Query";
        public static string NamespacesDirectory = "Namespace";
        public static string AppendicesDirectory = "Appendix";
        public static string HelpDirectory = "HelpXml";
        public static string BaseUrl = "Help";

        public class HelpState
        {
            public Dictionary<Type, EntityHelp> TypeToHelpFiles;

            public Dictionary<string, NamespaceHelp> Namespaces;
            public Dictionary<string, AppendixHelp> Appendices;

            public Dictionary<Type, List<object>> TypeToQueryFiles;
            public Dictionary<object, QueryHelp> QueryColumns;

            public List<QueryHelp> GetQueryHelps(Type type)
            {
                var list = TypeToQueryFiles.TryGetC(type);

                if(list == null)
                    return new List<QueryHelp>();

                return list.Select(o => QueryColumns[o]).ToList();
            }
        }

        internal static Lazy<HelpState> state = new Lazy<HelpState>(Schema_Initialize, System.Threading.LazyThreadSafetyMode.PublicationOnly);

      
        public static NamespaceHelp GetNamespace(string @namespace)
        {
            return state.Value.Namespaces.TryGetC(@namespace);
        }

        public static List<NamespaceHelp> GetNamespaces()
        {
            return state.Value.Namespaces.Select(kvp => kvp.Value).ToList();
        }

        public static List<AppendixHelp> GetAppendices()
        {
            return state.Value.Appendices.Select(kvp => kvp.Value).ToList();
        }

        public static AppendixHelp GetAppendix(string appendix)
        {
            return state.Value.Appendices.TryGetC(appendix);
        }

        public static Type[] AllTypes()
        {
            return state.Value.TypeToHelpFiles.Keys.ToArray();
        }

        public static string EntityUrl(Type entityType)
        {
            return BaseUrl + "/" + TypeLogic.GetCleanName(entityType);
        }

        public static string OperationUrl(Type entityType, Enum operation)
        {
            return HelpLogic.EntityUrl(entityType) + "#" + "o-" + OperationDN.UniqueKey(operation).Replace('.', '_');
        }

        public static string PropertyUrl(PropertyRoute route)
        {
            return HelpLogic.EntityUrl(route.RootType) + "#" + "p-" + route.PropertyString();
        }

        public static string QueryUrl(Type entityType)
        {
            return HelpLogic.EntityUrl(entityType) + "#" + "q-" + entityType.FullName.Replace(".", "_");
        }
        
        public static string QueryUrl(Enum query)
        {
            return HelpLogic.EntityUrl(GetQueryType(query))
                + "#" + "q-" + QueryUtils.GetQueryUniqueKey(query).ToString().Replace(".", "_");
        }

        public static EntityHelp GetEntityHelp(Type entityType)
        {
            return state.Value.TypeToHelpFiles[entityType];
        }

        public static List<KeyValuePair<Type, EntityHelp>> GetEntitiesHelp()
        {
            return state.Value.TypeToHelpFiles.ToList();
        }

        public static QueryHelp GetQueryHelp(string query)
        {
            return state.Value.QueryColumns[QueryLogic.TryToQueryName(query)];
        }

    

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
            }
        }

        public static void ReloadDocumentEntity(EntityHelp entityHelp)
        {
            state.Value.TypeToHelpFiles[entityHelp.Type] = EntityHelp.Load(entityHelp.Type, XDocument.Load(entityHelp.FileName), entityHelp.FileName);
        }

        public static void ReloadDocumentQuery(QueryHelp queryHelp)
        {
            state.Value.QueryColumns[queryHelp.Key] = QueryHelp.Load(XDocument.Load(queryHelp.FileName), queryHelp.FileName);
        }

        public static void ReloadDocumentNamespace(NamespaceHelp namespaceHelp)
        {
            state.Value.Namespaces[namespaceHelp.Name] = NamespaceHelp.Load(XDocument.Load(namespaceHelp.FileName), namespaceHelp.FileName);
        }

        public static void ReloadDocumentAppendix(AppendixHelp appendixHelp)
        {
            state.Value.Appendices[appendixHelp.Name] = AppendixHelp.Load(XDocument.Load(appendixHelp.FileName), appendixHelp.FileName);
        }

        static HelpState Schema_Initialize()
        {
            if (!Directory.Exists(HelpDirectory))
                throw new InvalidOperationException("Help directory does not exist ('{0}')".Formato(HelpDirectory));

            HelpState result = new HelpState();

            Type[] types = Schema.Current.Tables.Select(t => t.Key).ToArray();

            var typesDic = types.ToDictionary(a => a.FullName);

            var entitiesDocuments = LoadDocuments(EntitiesDirectory);
            var namespacesDocuments = LoadDocuments(NamespacesDirectory);
            var queriesDocuments = LoadDocuments(QueriesDirectory);
            var appendicesDocuments = LoadDocuments(AppendicesDirectory);

            //Scope
            {
                var typeHelpInfo = from doc in entitiesDocuments
                                   let typeName = EntityHelp.GetEntityFullName(doc.Document)
                                   where typeName != null
                                   select EntityHelp.Load(typesDic.GetOrThrow(typeName, "Not type with FullName {0} found in the schema"), doc.Document, doc.File);


                //tipo a entityHelp
                result.TypeToHelpFiles = typeHelpInfo.ToDictionary(p => p.Type);
            }

            //Scope
            {
                var nameSpaceInfo = from doc in namespacesDocuments
                                    let namespaceName = NamespaceHelp.GetNamespaceName(doc.Document)
                                    where namespaceName != null
                                    select NamespaceHelp.Load(doc.Document, doc.File);

                result.Namespaces = nameSpaceInfo.ToDictionary(a => a.Name);
            }

            //Scope
            {
                var appendixInfo = from doc in appendicesDocuments
                                   let namespaceName = AppendixHelp.GetAppendixName(doc.Document)
                                   where namespaceName != null
                                   select AppendixHelp.Load(doc.Document, doc.File);

                result.Appendices = appendixInfo.ToDictionary(a => a.Name);
            }

            //Scope
            {
                var queriesInfo = from doc in queriesDocuments
                                  let queryName = QueryHelp.GetQueryFullName(doc.Document)
                                  where queryName != null
                                  select QueryHelp.Load(doc.Document, doc.File);


                result.TypeToQueryFiles = queriesInfo.GroupToDictionary(qh => GetQueryType(qh.Key), qh => qh.Key);

                result.QueryColumns = queriesInfo.ToDictionary(a => a.Key);
            }

            return result;
        }

        public static Type GetQueryType(object query)
        {
            return DynamicQueryManager.Current.GetQuery(query).Core.Value.EntityColumn().Implementations.Value.Types.FirstEx();
        }

        private static List<FileXDocument> LoadDocuments(string subdirectory)
        {
            XmlSchemaSet schemas = new XmlSchemaSet();
            Stream str = typeof(HelpLogic).Assembly.GetManifestResourceStream("Signum.Engine.Extensions.Help.SignumFrameworkHelp.xsd");
            schemas.Add("", XmlReader.Create(str));

            var documents = Directory.GetFiles(Path.Combine(HelpDirectory,subdirectory), "*.help").Select(f => new FileXDocument { File = f, Document = XDocument.Load(f) }).ToList();

            List<Tuple<XmlSchemaException, string>> exceptions = new List<Tuple<XmlSchemaException, string>>();
            foreach (var doc in documents)
            {
                doc.Document.Validate(schemas, (s, e) => exceptions.Add(Tuple.Create(e.Exception, doc.File)));
            }

            if (exceptions.Count != 0)
            {
                string errorText = "Error Parsing XML Help Files: " + exceptions.ToString(e => "{0} ({1}:{2}): {3}".Formato(
                    e.Item2, e.Item1.LineNumber, e.Item1.LinePosition, e.Item1.Message), "\r\n").Indent(3);

                throw new InvalidOperationException(errorText);
            }
            return documents;
        }

        class FileXDocument
        {
            public XDocument Document;
            public string File;
        }


        public static void GenerateAll()
        {
            if (!Directory.Exists(HelpDirectory))
            {
                Directory.CreateDirectory(HelpDirectory);

                if (!Directory.Exists(Path.Combine(HelpDirectory, AppendicesDirectory)))
                    Directory.CreateDirectory(Path.Combine(HelpDirectory, AppendicesDirectory));

                if (!Directory.Exists(Path.Combine(HelpDirectory, EntitiesDirectory)))
                    Directory.CreateDirectory(Path.Combine(HelpDirectory, EntitiesDirectory));

                if (!Directory.Exists(Path.Combine(HelpDirectory, QueriesDirectory)))
                    Directory.CreateDirectory(Path.Combine(HelpDirectory, QueriesDirectory));

                if (!Directory.Exists(Path.Combine(HelpDirectory, NamespacesDirectory)))
                    Directory.CreateDirectory(Path.Combine(HelpDirectory, NamespacesDirectory));
            }
            else
            {
                string[] files = Directory.GetFiles(HelpDirectory, "*.help");

                if (files.Length != 0)
                {
                    Console.WriteLine("There are files in {0}, remove? (y/n)", HelpDirectory);
                    if (Console.ReadLine().ToLower() != "y")
                        return;

                    foreach (var f in files)
                    {
                        File.Delete(f);
                    }
                }
            }

            Type[] types = Schema.Current.Tables.Keys.Where(t => !t.IsEnumEntity()).ToArray();

            foreach (Type type in types)
            {
                EntityHelp.Create(type).Save();
            }

            foreach (var ns in types.Select(a => a.Namespace).Distinct())
            {
                NamespaceHelp.Create(ns).Save();
            }

            foreach (Type type in types)
            {
                foreach (var v in DynamicQueryManager.Current.GetTypeQueries(type))
                    QueryHelp.Create(v.Key).Save();
            }
        }

        public static void SyncronizeAll()
        {
            if (!Directory.Exists(HelpDirectory))
            {
                Directory.CreateDirectory(HelpDirectory);
            }

            Type[] types = Schema.Current.Tables.Keys.Where(t => !t.IsEnumEntity()).ToArray();
            var entitiesDocuments = LoadDocuments(EntitiesDirectory);
            var namespacesDocuments = LoadDocuments(NamespacesDirectory);
            var queriesDocuments = LoadDocuments(QueriesDirectory);
            //var appendicesDocuments = LoadDocuments(AppendicesFolder);

            Replacements replacements = new Replacements();
            //Namespaces
            {
                var should = types.Select(type => type.Namespace).Distinct().ToDictionary(a => a);

                var current = (from doc in namespacesDocuments
                               let name = NamespaceHelp.GetNamespaceName(doc.Document)
                               where name != null
                               select new
                               {
                                   Namespace = name,
                                   File = doc.File,
                               }).ToDictionary(a => a.Namespace, a => a.File);

                HelpTools.SynchronizeReplacing(replacements, "Namespace", current, should,
                 (nameSpace, oldFile) =>
                 {
                     File.Delete(oldFile);
                     Console.WriteLine("Deleted {0}".Formato(oldFile));
                 },
                 (nameSpace, _) =>
                 {
                     string fileName = NamespaceHelp.Create(nameSpace).Save();
                     Console.WriteLine("Created {0}".Formato(fileName));
                 },
                 (nameSpace, oldFile, type) =>
                 {
                     NamespaceHelp.Synchronize(oldFile, type);
                 });
            }

            //Types
            {
                var should = types.ToDictionary(type => type.FullName);

                var current = (from doc in entitiesDocuments 
                               let name = EntityHelp.GetEntityFullName(doc.Document)
                               where name != null
                               select new
                               {
                                   TypeName = name,
                                   File = doc.File,
                               }).ToDictionary(a => a.TypeName, a => a.File,"Types in HelpFiles");

             
                HelpTools.SynchronizeReplacing(replacements, "Type", current, should,
                    (fullName, oldFile) =>
                    {
                        File.Delete(oldFile);
                        Console.WriteLine("Deleted {0}".Formato(oldFile));
                    },
                    (fullName, type) =>
                    {
                        string fileName = EntityHelp.Create(type).Save();
                        Console.WriteLine("Created {0}".Formato(fileName));
                    },
                    (fullName, oldFile, type) =>
                    {
                        EntityHelp.Synchronize(oldFile, type);
                    });
            }

            //Queries
            {
                var should = (from type in types
                              let keys = DynamicQueryManager.Current.GetTypeQueries(type).Keys
                              from key in keys
                              select key).Distinct().ToDictionary(q => QueryUtils.GetQueryUniqueKey(q), "Queries in HelpFiles");

                var current = (from doc in queriesDocuments 
                               let name = QueryHelp.GetQueryFullName(doc.Document)
                               where name != null
                               select new
                               {
                                   QueryName = name,
                                   File = doc.File,
                               }).ToDictionary(a => a.QueryName, a => a.File);

                HelpTools.SynchronizeReplacing(replacements, "Query", current, should,
                    (fullName, oldFile) =>
                    {
                        File.Delete(oldFile);
                        Console.WriteLine("Deleted {0}".Formato(oldFile));
                    },
                    (fullName, query) =>
                    {
                        string fileName = QueryHelp.Create(query).Save();
                        Console.WriteLine("Created {0}".Formato(fileName));
                    },
                    (fullName, oldFile, query) =>
                    {
                        QueryHelp.Synchronize(oldFile, QueryUtils.GetQueryUniqueKey(query));
                    });

            }
        }

    }
}