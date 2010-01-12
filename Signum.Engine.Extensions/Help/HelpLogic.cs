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
        static DirectedGraph<Assembly> Assemblies;
        static List<HashSet<Assembly>> SortedAssemblies; 
        static Dictionary<Type, EntityHelp> TypeToHelpFiles;
        static Dictionary<string, Type> CleanNameToType;

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

            Dictionary<Assembly, string> dict = TypeToHelpFiles[entityType];

            Dictionary<Assembly, EntityHelp> entities = dict.SelectDictionary(k => k, fn =>
                EntityHelp.Load(entityType, Find(XDocument.Load(fn), entityType), fn));

            return entities; 
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
            //Debugger.Break(); 

            Type[] types = sender.Tables.Select(t => t.Key).ToArray();

            Type[] operations = (from t in types
                                 from oi in OperationLogic.GetAllOperationInfos(t)
                                 select oi.Key.GetType()).ToArray();
            Type[] queries = (from o in DynamicQueryManager.Current.GetQueryNames()
                              select o.GetType()).ToArray();

            Dictionary<string, Assembly> assemblies =
                 types.Concat(operations).Concat(queries).Select(q => q.Assembly).Distinct().ToDictionary(a => a.GetName().Name);

            Assemblies = DirectedGraph<Assembly>.Generate(assemblies.Values, a =>
                a.GetReferencedAssemblies().Select(an => assemblies.TryGetC(a.GetName().Name)).NotNull());

            SortedAssemblies = Assemblies.CompilationOrderGroups().ToList();

            XmlSchemaSet schemas = new XmlSchemaSet();
            Stream str = typeof(HelpLogic).Assembly.GetManifestResourceStream("Signum.Engine.Extensions.Help.SignumFrameworkHelp.xsd");
            schemas.Add("", XmlReader.Create(str));

            var typesDic = types.ToDictionary(a => a.FullName);


            if (!Directory.Exists(HelpDirectory))
            {
                TypeToHelpFiles = new Dictionary<Type, Dictionary<Assembly, string>>();
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
                                    TargetAssembly = assemblies[doc.Document.Root.Attribute(_TargetAssembly).Value],
                                    OverriderAssembly = doc.Document.Root.Attribute(_OverriderAssembly).TryCC(xa => assemblies[xa.Value]),
                                    Type = type,
                                }).ToList();

            var duplicatedTypes = (from info in typeHelpInfo
                                   group info by new { info.Type, info.OverriderAssembly } into g
                                   where g.Count() > 1
                                   select "Type: {0} Assembly: {1} Files: {2}".Formato(
                                    g.Key.Type.Name,
                                    (g.Key.OverriderAssembly ?? g.Key.Type.Assembly).GetName().Name,
                                    g.ToString(f => f.File.Substring(HelpDirectory.Length), ", "))).ToString("\r\n");


            if (duplicatedTypes.HasText())
                throw new InvalidOperationException(Resources.ThereAreDuplicatedTypesInTheXMLHelp + duplicatedTypes.Indent(2));

            TypeToHelpFiles = typeHelpInfo.AgGroupToDictionary(a => a.Type, gr => gr.ToDictionary(a => a.OverriderAssembly ?? a.TargetAssembly, a => a.File));

            CleanNameToType = typeHelpInfo.Select(t => t.Type).ToDictionary(t => Reflector.CleanTypeName(t));
        }

        static string FileName(XDocument document)
        {
            var target = document.Root.Attribute(_TargetAssembly);
            var overrider = document.Root.Attribute(_OverriderAssembly);

            if (overrider != null)
                return "{0}-{1}.help".Formato(target.Value, overrider.Value);

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

            IEnumerable<XDocument> mainHelp = (from targetAssembly in Assemblies
                                               from nameSpace in SchemaTypes(targetAssembly).Select(a => a.Namespace).Distinct()
                                               select Create(targetAssembly, nameSpace)).NotNull();

            foreach (XDocument document in mainHelp)
            {
                string path = Path.Combine(HelpDirectory, FileName(document));
                path = FileTools.AvailableFileName(path);
                document.Save(path);
            }

            IEnumerable<XDocument> overrides = (from overriderAssembly in Assemblies
                                                from targetAssembly in Assemblies.IndirectlyRelatedTo(overriderAssembly)
                                                select CreateOverride(targetAssembly, overriderAssembly)).NotNull().ToList();

            foreach (XDocument document in overrides)
            {
                string path = Path.Combine(HelpDirectory, FileName(document));
                path = FileTools.AvailableFileName(path);
                document.Save(path);
            }
        }

        static IEnumerable<Type> SchemaTypes(Assembly targetAssembly)
        {
            return Schema.Current.Tables.Keys.Where(t => !t.IsEnumProxy() && t.Assembly == targetAssembly);
        }

        public static XDocument Create(Assembly targetAssembly, string nameSpace)
        {
            HashSet<Assembly> forbiddenAssemblies = Assemblies.InverseRelatedTo(targetAssembly).ToHashSet();

            var types = (from t in SchemaTypes(targetAssembly)
                         where t.Namespace == nameSpace
                         select EntityHelp.Create(t, targetAssembly, forbiddenAssemblies)).ToList();

            XDocument result = new XDocument(
                 new XDeclaration("1.0", "utf-8", "yes"),
                 new XElement(_Help,
                    new XAttribute(_TargetAssembly, targetAssembly.GetName().Name),
                    new XAttribute(_Language, CultureInfo.CurrentCulture.Name),
                    new XElement(_Namespace, new XAttribute(_Name, nameSpace),
                        types.Select(h => h.ToXml())
                    )) //Namespace
                );//Document

            return result;
        }

        public static XDocument CreateOverride(Assembly targetAssembly, Assembly overriderAssembly)
        {
            var types = (from t in SchemaTypes(targetAssembly)
                         let h = EntityHelp.CreateOverride(t, targetAssembly, overriderAssembly)
                         where h.HasHelp()
                         select h).ToList();

            if (types.Count == 0)
                return null;

            XDocument result = new XDocument(
                 new XDeclaration("1.0", "utf-8", "yes"),
                 new XElement(_Help,
                    new XAttribute(_TargetAssembly, targetAssembly.GetName().Name),
                    new XAttribute(_OverriderAssembly, overriderAssembly.GetName().Name),
                    new XAttribute(_Language, CultureInfo.CurrentCulture.Name),
                    types.GroupBy(h => h.Type.Namespace)
                    .Select(g => new XElement(_Namespace, new XAttribute(_Name, g.Key), 
                        g.Select(h => h.ToXml())
                    )) //Namespace
                ));//Document

            return result;
        }

        static readonly XName _Help = "Help";
        static readonly XName _TargetAssembly = "TargetAssembly";
        static readonly XName _OverriderAssembly = "OverriderAssembly";
        static readonly XName _Language = "Language";
        static readonly XName _Name = "Name";
        static readonly XName _Entity = "Entity";
        static readonly XName _Namespace = "Namespace";

    }
}
