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
        static Dictionary<Type, EntityHelp> TypeToHelpFiles;
        static Dictionary<string, Type> CleanNameToType;
        public static List<EntityHelp> EntitiesHelp = new List<EntityHelp>();

        public static Type ToType(string s)
        {
            return CleanNameToType[s];
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

        static XElement Find(XDocument document, Type type)
        {
            return document
                .Elements(_Namespace).Single(ns => ns.Attribute(_Namespace).Value == type.Namespace)
                .Elements(_Entity).Single(a => a.Attribute(_Name).Value == type.Name);
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

        static void Schema_Initialize(Schema sender)
        {
            Type[] types = sender.Tables.Select(t => t.Key).ToArray();

            XmlSchemaSet schemas = new XmlSchemaSet();
            Stream str = typeof(HelpLogic).Assembly.GetManifestResourceStream("Signum.Engine.Extensions.Help.SignumFrameworkHelp.xsd");
            schemas.Add("", XmlReader.Create(str));

            var typesDic = types.ToDictionary(a => a.FullName);

            if (!Directory.Exists(HelpDirectory))
            {
                TypeToHelpFiles =  new Dictionary<Type, EntityHelp>();
                return;
            }

            var documents = Directory.GetFiles(HelpDirectory, "*.help").Select(f => new { File = f, Document = XDocument.Load(f) }).ToList();

            List<XmlSchemaException> exceptions = new List<XmlSchemaException>();
            foreach (var doc in documents)
            {
                doc.Document.Validate(schemas, (s, e) => exceptions.Add(e.Exception));
            }

            if (exceptions.Count != 0)
            {
                string errorText = Resources.ErrorParsingXMLHelpFiles + exceptions.ToString(e => "{0} ({1}:{2}): {3}".Formato(
                    e.SourceUri.Substring(HelpDirectory.Length), e.LineNumber, e.LinePosition, e.Message), "\r\n").Indent(3);

                throw new InvalidOperationException(errorText);
            }

            var typeHelpInfo = (from doc in documents
                                from ns in doc.Document.Root.Elements(_Namespace)
                                from entity in ns.Elements(_Entity)
                                let type = typesDic.TryGetC(ns.Attribute(_Name).Value + "." + entity.Attribute(_Name).Value)
                                where type != null
                                select new
                                {
                                    File = doc.File,
                                    Type = type,
                                }).ToList();

            var duplicatedTypes = (from info in typeHelpInfo
                                   group info by info.Type into g
                                   where g.Count() > 1
                                   select "Type: {0} Files: {1}".Formato(
                                    g.Key.Name,
                                    g.ToString(f => f.File.Substring(HelpDirectory.Length), ", "))).ToString("\r\n");


            if (duplicatedTypes.HasText())
                throw new InvalidOperationException(Resources.ThereAreDuplicatedTypesInTheXMLHelp + duplicatedTypes.Indent(2));

            //tipo a entityHelp
            TypeToHelpFiles = typeHelpInfo.ToDictionary(p=>p.Type,p=> GetEntityHelp(p.Type, p.File));

            CleanNameToType = typeHelpInfo.Select(t => t.Type).ToDictionary(t => Reflector.CleanTypeName(t));
        }

        static EntityHelp GetEntityHelp (Type type, string sourceFile)
        {
            XElement element = XDocument.Load(sourceFile).Element(_Help)
                .Elements(_Namespace).Single(ns=>ns.Attribute(_Name).Value == type.Namespace)
                .Elements(_Entity).Single(el=>el.Attribute(_Name).Value == type.Name);
            return EntityHelp.Load(type, element, sourceFile);
        }

        static string FileName(XDocument document)
        {
            var target = document.Root.Attribute(_Assembly);

            var onlyNameSpace = document.Root.Elements(_Namespace).Only();
            if (onlyNameSpace != null)
                return "{0}.help".Formato(onlyNameSpace.Attribute(_Name).Value);

            return "{0}.help".Formato(target.Value);
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
                    Console.WriteLine("There are files, remove? (y/n)");
                    if (Console.ReadLine().ToLower() != "y")
                        return;

                    foreach (var f in files)
                    {
                        File.Delete(f);
                    }
                }
            }
           
            IEnumerable<XDocument> mainHelp = (from type in Schema.Current.Tables.Keys.Where(t => !t.IsEnumProxy())
                                               group type by new {type.Assembly, type.Namespace} into g
                                               select Create(g.Key.Assembly, g.Key.Namespace, g.ToList())).NotNull();

            foreach (XDocument document in mainHelp)
            {
                string path = Path.Combine(HelpDirectory, FileName(document));
                path = FileTools.AvailableFileName(path);
                document.Save(path);
            }
        }


        public static XDocument Create(Assembly targetAssembly, string nameSpace, IEnumerable<Type> types)
        {
            XDocument result = new XDocument(
                 new XDeclaration("1.0", "utf-8", "yes"),
                 new XElement(_Help,
                    new XAttribute(_Assembly, targetAssembly.GetName().Name),
                    new XAttribute(_Language, CultureInfo.CurrentCulture.Name),
                    new XElement(_Namespace, new XAttribute(_Name, nameSpace),
                        types.Select(t => EntityHelp.Create(t).ToXml())
                    )) //Namespace
                );//Document

            foreach (Type type in types)
                EntitiesHelp.Add(EntityHelp.Create(type));

            return result;
        }

        static readonly XName _Help = "Help";
        static readonly XName _Assembly = "Assembly";
        static readonly XName _Language = "Language";
        static readonly XName _Name = "Name";
        static readonly XName _Entity = "Entity";
        static readonly XName _Namespace = "Namespace";
    }
}
