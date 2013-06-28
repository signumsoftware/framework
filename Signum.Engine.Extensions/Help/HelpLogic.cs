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

        public static Lazy<HelpState> State = new Lazy<HelpState>(Schema_Initialize, System.Threading.LazyThreadSafetyMode.PublicationOnly);

      
        public static NamespaceHelp GetNamespace(string @namespace)
        {
            return State.Value.Namespaces.TryGetC(@namespace);
        }

        public static List<NamespaceHelp> GetNamespaces()
        {
            return State.Value.Namespaces.Select(kvp => kvp.Value).ToList();
        }

        public static List<AppendixHelp> GetAppendices()
        {
            return State.Value.Appendices.Select(kvp => kvp.Value).ToList();
        }

        public static AppendixHelp GetAppendix(string appendix)
        {
            return State.Value.Appendices.TryGetC(appendix);
        }

        public static Type[] AllTypes()
        {
            return State.Value.TypeToHelpFiles.Keys.ToArray();
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
            return State.Value.TypeToHelpFiles[entityType];
        }

        public static List<KeyValuePair<Type, EntityHelp>> GetEntitiesHelp()
        {
            return State.Value.TypeToHelpFiles.ToList();
        }

        public static QueryHelp GetQueryHelp(string query)
        {
            return State.Value.QueryColumns[QueryLogic.TryToQueryName(query)];
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
            }
        }

        public static void ReloadDocumentEntity(EntityHelp entityHelp)
        {
            State.Value.TypeToHelpFiles[entityHelp.Type] = EntityHelp.Create(entityHelp.Type).Load();
        }

        public static void ReloadDocumentQuery(QueryHelp queryHelp)
        {
            State.Value.QueryColumns[queryHelp.Key] = QueryHelp.Create(queryHelp.Key).Load();
        }

        public static void ReloadDocumentNamespace(NamespaceHelp namespaceHelp)
        {
            State.Value.Namespaces[namespaceHelp.Name] = NamespaceHelp.Create(namespaceHelp.Name).Load();
        }

        public static void ReloadDocumentAppendix(AppendixHelp appendixHelp)
        {
            State.Value.Appendices[appendixHelp.Name] = AppendixHelp.Load(XDocument.Load(appendixHelp.FileName), appendixHelp.FileName);
        }

        static HelpState Schema_Initialize()
        {
            if (!Directory.Exists(HelpDirectory))
                throw new InvalidOperationException("Help directory does not exist ('{0}')".Formato(HelpDirectory));

            HelpState result = new HelpState();

            Type[] types = Schema.Current.Tables.Keys.Where(t => !t.IsEnumEntity()).ToArray();
            result.TypeToHelpFiles = types.Select(t => EntityHelp.Create(t).Load()).ToDictionary(a => a.Type);
            result.Namespaces = types.Select(t => t.Namespace).Distinct().Select(ns => NamespaceHelp.Create(ns).Load()).ToDictionary(a => a.Name);
         
            //Scope
            {   

                //tipo a entityHelp
              
            }

            //Scope
            {
                var namespacesDocuments = LoadDocuments(NamespacesDirectory);
                var nameSpaceInfo = from doc in namespacesDocuments
                                    let namespaceName = NamespaceHelp.GetNamespaceName(doc.Document)
                                    where namespaceName != null
                                    select NamespaceHelp.Load(doc.Document, doc.File);

                result.Namespaces = nameSpaceInfo.ToDictionary(a => a.Name);
            }

            //Scope
            {
                var appendicesDocuments = LoadDocuments(AppendicesDirectory);
                var appendixInfo = from doc in appendicesDocuments
                                   let namespaceName = AppendixHelp.GetAppendixName(doc.Document)
                                   where namespaceName != null
                                   select AppendixHelp.Load(doc.Document, doc.File);

                result.Appendices = appendixInfo.ToDictionary(a => a.Name);
            }

            //Scope
            {
                var queriesDocuments = LoadDocuments(QueriesDirectory);
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

        static Lazy<XmlSchemaSet> Schemas = new Lazy<XmlSchemaSet>(() =>
        {
            XmlSchemaSet schemas = new XmlSchemaSet();
            Stream str = typeof(HelpLogic).Assembly.GetManifestResourceStream("Signum.Engine.Extensions.Help.SignumFrameworkHelp.xsd");
            schemas.Add("", XmlReader.Create(str));
            return schemas;
        });

        static List<string> LoadDocuments(string subdirectory)
        {
            return Directory.GetFiles(Path.Combine(HelpDirectory, subdirectory), "*.help").ToList();
        }

        internal static XDocument LoadAndValidate(string fileName)
        {
            var document = XDocument.Load(fileName); 

            List<Tuple<XmlSchemaException, string>> exceptions = new List<Tuple<XmlSchemaException, string>>();

            document.Document.Validate(Schemas.Value, (s, e) => exceptions.Add(Tuple.Create(e.Exception, fileName)));

            if (exceptions.Any())
                throw new InvalidOperationException("Error Parsing XML Help Files: " + exceptions.ToString(e => "{0} ({1}:{2}): {3}".Formato(
                 e.Item2, e.Item1.LineNumber, e.Item1.LinePosition, e.Item1.Message), "\r\n").Indent(3));

            return document;
        }

        class FileXDocument
        {
            public XDocument Document;
            public string File;
        }

        public static void SyncronizeAll()
        {
            if (!Directory.Exists(HelpDirectory))
            {
                Directory.CreateDirectory(HelpDirectory);
            }

            Type[] types = Schema.Current.Tables.Keys.Where(t => !t.IsEnumEntity()).ToArray();
           
            Replacements replacements = new Replacements();
            //Namespaces
            {
                var namespacesDocuments = LoadDocuments(NamespacesDirectory);
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
                 },
                 (nameSpace, oldFile, type) =>
                 {
                     NamespaceHelp.Synchronize(oldFile, type);
                 });
            }

            //Types
            {   
                var should = types.ToDictionary(type => type.FullName);

                var entitiesDocuments = LoadDocuments(EntitiesDirectory);
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
                    },
                    (fullName, oldFile, type) =>
                    {
                        EntityHelp.Synchronize(oldFile, type);
                    });
            }

            //Queries
            {
                var should = (from type in types
                              from key in DynamicQueryManager.Current.GetTypeQueries(type).Keys
                              select key).Distinct().ToDictionary(q => QueryUtils.GetQueryUniqueKey(q), "Queries in HelpFiles");

                var queriesDocuments = LoadDocuments(QueriesDirectory);
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
                    (fullName, query) =>{},
                    (fullName, oldFile, query) =>
                    {
                        QueryHelp.Synchronize(oldFile, QueryUtils.GetQueryUniqueKey(query));
                    });

            }
        }

    }
}