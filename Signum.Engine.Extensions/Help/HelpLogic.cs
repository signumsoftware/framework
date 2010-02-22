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


namespace Signum.Engine.Help
{
    public static class HelpLogic
    {       
        public static string HelpDirectory = "HelpXml";
        public static string BaseUrl = "Help";

        static Dictionary<Type, EntityHelp> TypeToHelpFiles
        {
            get { return HelpLogic._TypeToHelpFiles.ThrowIfNullC("No se ha cargado Help"); }
            set { HelpLogic._TypeToHelpFiles = value; }
        }

        static Dictionary<Type, EntityHelp> _TypeToHelpFiles;
        static Dictionary<string, Type> CleanNameToType;
        static Dictionary<string, Type> NameToType;

        static Dictionary<string, NamespaceHelp> Namespaces;
        static Dictionary<string, AppendixHelp> Appendices;

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

        public static EntityHelp GetEntityHelp(Type entityType)
        {
            return TypeToHelpFiles[entityType]; 
        }

        public static List<KeyValuePair<Type,EntityHelp>> GetEntitiesHelp()
        {
            return TypeToHelpFiles.ToList();
        }

        public static Type FromCleanName(string cleanName)
        {
            return CleanNameToType.GetOrThrow(cleanName, "No help for " + cleanName); 
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.Initializing(InitLevel.Level4BackgroundProcesses, Schema_Initialize); 
            }
        }

        public static void ReloadDocument(EntityHelp entityHelp)
        {
            TypeToHelpFiles[entityHelp.Type] = EntityHelp.Load(entityHelp.Type, XDocument.Load(entityHelp.FileName), entityHelp.FileName);
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


            var documents = LoadDocuments();

            //Scope
            {
                var typeHelpInfo = from doc in documents
                                   let typeName = EntityHelp.GetEntityFullName(doc.Document)
                                   where typeName != null
                                   select EntityHelp.Load(typesDic.GetOrThrow(typeName, "No Type with FullName {0} found in the schema"), doc.Document, doc.File);


                //tipo a entityHelp
                TypeToHelpFiles = typeHelpInfo.ToDictionary(p => p.Type);

                CleanNameToType = typeHelpInfo.Select(t => t.Type).ToDictionary(t => Reflector.CleanTypeName(t));
                NameToType = typeHelpInfo.Select(t => t.Type).ToDictionary(t => t.Name);
            }

            //Scope
            {
                var nameSpaceInfo = from doc in documents
                                   let namespaceName = NamespaceHelp.GetNamespaceName(doc.Document)
                                   where namespaceName != null
                                   select NamespaceHelp.Load(doc.Document, doc.File);

                Namespaces = nameSpaceInfo.ToDictionary(a => a.Name);
            }

            //Scope
            {
                var appendixInfo = from doc in documents
                                    let namespaceName = AppendixHelp.GetApendixName(doc.Document)
                                    where namespaceName != null
                                    select AppendixHelp.Load(doc.Document, doc.File);

                Appendices = appendixInfo.ToDictionary(a => a.Name);
            }

        }

        private static List<FileXDocument> LoadDocuments()
        {
            XmlSchemaSet schemas = new XmlSchemaSet();
            Stream str = typeof(HelpLogic).Assembly.GetManifestResourceStream("Signum.Engine.Extensions.Help.SignumFrameworkHelp.xsd");
            schemas.Add("", XmlReader.Create(str));

            var documents = Directory.GetFiles(HelpDirectory, "*.help").Select(f => new FileXDocument { File = f, Document = XDocument.Load(f) }).ToList();

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

            foreach (var ns in types.Select(a=>a.Namespace).Distinct())
            {
                NamespaceHelp.Create(ns).Save();
            }
        }

        public static void SyncronizeAll()
        {
            if (!Directory.Exists(HelpDirectory))
            {
                Directory.CreateDirectory(HelpDirectory);
            }

            Type[] types = Schema.Current.Tables.Keys.Where(t => !t.IsEnumProxy()).ToArray();
            var documents = LoadDocuments();

            Replacements replacements = new Replacements();
            //Scope
            {
                var should = types.Select(type => type.Namespace).Distinct().ToDictionary(a => a);

                var current = (from doc in documents
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

                var current = (from doc in documents 
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

         
        }

    }
}
