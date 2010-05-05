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
using Signum.Entities.Operations;
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
using Signum.Engine.Extensions.Properties;
using Signum.Utilities.Reflection;
using System.Diagnostics;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;


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

        static Dictionary<Type, EntityHelp> TypeToHelpFiles
        {
            get { return HelpLogic.typeToHelpFiles.ThrowIfNullC(Resources.HelpNotLoaded); }
            set { HelpLogic.typeToHelpFiles = value; }
        }

        static Dictionary<Type, EntityHelp> typeToHelpFiles;
        static Dictionary<string, Type> CleanNameToType;
        static Dictionary<string, Type> NameToType;

        static Dictionary<string, NamespaceHelp> Namespaces;
        static Dictionary<string, AppendixHelp> Appendices;

        static Dictionary<Type, List<object>> TypeToQueryFiles;
        static Dictionary<object, QueryHelp> QueryColumns;

        public static Type ToType(string s)
        {
            return ToType(s, true);
        }

        public static Type ToType(string s, bool throwException)
        {
            if (!throwException && CleanNameToType.ContainsKey(s) || throwException)
                return CleanNameToType[s];
            else
                return null;
        }

        public static Type GetNameToType(string s, bool throwException)
        {
            if (!throwException && NameToType.ContainsKey(s) || throwException)
                return NameToType[s];
            else
                return null;
        }

        public static NamespaceHelp GetNamespace(string @namespace)
        {
            return Namespaces.TryGetC(@namespace);
        }

        public static List<AppendixHelp> GetAppendices()
        {
            return Appendices.Select(kvp => kvp.Value).ToList();
        }

        public static AppendixHelp GetAppendix(string appendix)
        {
            return Appendices.TryGetC(appendix);
        }

        public static Type[] AllTypes()
        {
            return TypeToHelpFiles.Keys.ToArray();
        }

        public static string EntityUrl(Type entityType)
        {
            return BaseUrl + "/" + Reflector.CleanTypeName(entityType);
        }

        public static string OperationUrl(Type entityType, Enum operation)
        {
            return HelpLogic.EntityUrl(entityType) + "#" + "o-" + OperationDN.UniqueKey(operation).Replace('.', '_');
        }

        public static string PropertyUrl(Type entityType, string property)
        {
            return HelpLogic.EntityUrl(entityType) + "#" + "p-" + property;
        }

        public static string QueryUrl(Type entityType)
        {
            return HelpLogic.EntityUrl(entityType) + "#" + "q-" + entityType.FullName.Replace(".", "_");
        }
        
        public static string QueryUrl(Enum query)
        {
            return HelpLogic.EntityUrl(DynamicQueryManager.Current[query].EntityColumn().DefaultEntityType())
                + "#" + "q-" + QueryUtils.GetQueryName(query).ToString().Replace(".", "_");
        }

        public static EntityHelp GetEntityHelp(Type entityType)
        {
            return TypeToHelpFiles[entityType];
        }

        public static List<KeyValuePair<Type, EntityHelp>> GetEntitiesHelp()
        {
            return TypeToHelpFiles.ToList();
        }

        public static QueryHelp GetQueryHelp(string query)
        {
            return QueryColumns[QueryLogic.TryToQueryName(query)];
        }

        public static List<QueryHelp> GetQueryHelps(Type type)
        {
            return TypeToQueryFiles.TryGetC(type) != null ?
                (from o in TypeToQueryFiles.TryGetC(type)
                     select QueryColumns[o]).ToList()
                : null;
        }

        public static Type FromCleanName(string cleanName)
        {
            return CleanNameToType.GetOrThrow(cleanName, Resources.NoHelpFor0.Formato(cleanName));
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Initializing(InitLevel.Level4BackgroundProcesses, Schema_Initialize);
            }
        }

        public static void ReloadDocumentEntity(EntityHelp entityHelp)
        {
            TypeToHelpFiles[entityHelp.Type] = EntityHelp.Load(entityHelp.Type, XDocument.Load(entityHelp.FileName), entityHelp.FileName);
        }

        public static void ReloadDocumentQuery(QueryHelp queryHelp)
        {
            QueryColumns[queryHelp.Key] = QueryHelp.Load(XDocument.Load(queryHelp.FileName), queryHelp.FileName);
        }

        public static void ReloadDocumentNamespace(NamespaceHelp namespaceHelp)
        {
            Namespaces[namespaceHelp.Name] = NamespaceHelp.Load(XDocument.Load(namespaceHelp.FileName), namespaceHelp.FileName);
        }

        public static void ReloadDocumentAppendix(AppendixHelp appendixHelp)
        {
            Appendices[appendixHelp.Name] = AppendixHelp.Load(XDocument.Load(appendixHelp.FileName), appendixHelp.FileName);
        }

        static void Schema_Initialize(Schema sender)
        {
            Type[] types = sender.Tables.Select(t => t.Key).ToArray();

            var typesDic = types.ToDictionary(a => a.FullName);

            if (!Directory.Exists(HelpDirectory))
            {
                TypeToHelpFiles = new Dictionary<Type, EntityHelp>();
                return;
            }


            var entitiesDocuments = LoadDocuments(EntitiesDirectory);
            var namespacesDocuments = LoadDocuments(NamespacesDirectory);
            var queriesDocuments = LoadDocuments(QueriesDirectory);
            var appendicesDocuments = LoadDocuments(AppendicesDirectory);

            //Scope
            {
                var typeHelpInfo = from doc in entitiesDocuments
                                   let typeName = EntityHelp.GetEntityFullName(doc.Document)
                                   where typeName != null
                                   select EntityHelp.Load(typesDic.GetOrThrow(typeName, Resources.NoTypeWithFullName0FoundInTheSchema), doc.Document, doc.File);


                //tipo a entityHelp
                TypeToHelpFiles = typeHelpInfo.ToDictionary(p => p.Type);

                CleanNameToType = typeHelpInfo.Select(t => t.Type).ToDictionary(t => Reflector.CleanTypeName(t));
                NameToType = typeHelpInfo.Select(t => t.Type).ToDictionary(t => t.Name);
            }

            //Scope
            {
                var nameSpaceInfo = from doc in namespacesDocuments
                                    let namespaceName = NamespaceHelp.GetNamespaceName(doc.Document)
                                    where namespaceName != null
                                    select NamespaceHelp.Load(doc.Document, doc.File);

                Namespaces = nameSpaceInfo.ToDictionary(a => a.Name);
            }

            //Scope
            {
                var appendixInfo = from doc in appendicesDocuments
                                   let namespaceName = AppendixHelp.GetAppendixName(doc.Document)
                                   where namespaceName != null
                                   select AppendixHelp.Load(doc.Document, doc.File);

                Appendices = appendixInfo.ToDictionary(a => a.Name);
            }

            //Scope
            {
                var queriesInfo = from doc in queriesDocuments
                                  let queryName = QueryHelp.GetQueryFullName(doc.Document)
                                  where queryName != null
                                  select QueryHelp.Load(doc.Document, doc.File);


                TypeToQueryFiles = queriesInfo.GroupToDictionary(qh=>GetQueryType(qh.Key), qh=>qh.Key);
                
                QueryColumns = queriesInfo.ToDictionary(a => a.Key);
            }
        }

        public static Type GetQueryType(object query)
        {
            return DynamicQueryManager.Current[query].EntityColumn().DefaultEntityType();
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
                doc.Document.Validate(schemas, (s, e) => exceptions.Add(Tuple.New(e.Exception, doc.File)));
            }

            if (exceptions.Count != 0)
            {
                string errorText = Resources.ErrorParsingXMLHelpFiles + exceptions.ToString(e => "{0} ({1}:{2}): {3}".Formato(
                    e.Second, e.First.LineNumber, e.First.LinePosition, e.First.Message), "\r\n").Indent(3);

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

            Type[] types = Schema.Current.Tables.Keys.Where(t => !t.IsEnumProxy()).ToArray();

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
                foreach (var v in DynamicQueryManager.Current.GetQueryNames(type))
                    QueryHelp.Create(v.Key).Save();
            }
        }

        public static void SyncronizeAll()
        {
            if (!Directory.Exists(HelpDirectory))
            {
                Directory.CreateDirectory(HelpDirectory);
            }

            Type[] types = Schema.Current.Tables.Keys.Where(t => !t.IsEnumProxy()).ToArray();
            var entitiesDocuments = LoadDocuments(EntitiesDirectory);
            var namespacesDocuments = LoadDocuments(NamespacesDirectory);
            var queriesDocuments = LoadDocuments(QueriesDirectory);
            //var appendicesDocuments = LoadDocuments(AppendicesFolder);

            Replacements replacements = new Replacements();
            //Scope
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

            //Scope
            {
                var should = types.ToDictionary(type => type.FullName);

                var current = (from doc in entitiesDocuments 
                               let name = EntityHelp.GetEntityFullName(doc.Document)
                               where name != null
                               select new
                               {
                                   TypeName = name,
                                   File = doc.File,
                               }).ToDictionary(a => a.TypeName, a => a.File);

             
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
                              let keys = DynamicQueryManager.Current.GetQueryNames(type).Keys
                              from key in keys
                              select key).ToDictionary(q => QueryUtils.GetQueryName(q));

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
                        QueryHelp.Synchronize(oldFile, QueryUtils.GetQueryName(query));


                    });

    }
}

    }
}