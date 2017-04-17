using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.CodeGeneration
{
    public class WebCodeGenerator
    {
        public string SolutionName;
        public string SolutionFolder;

        public Schema CurrentSchema;

        public virtual void GenerateWebFromEntities()
        {
            CurrentSchema = Schema.Current;

            GetSolutionInfo(out SolutionFolder, out SolutionName);

            string projectFolder = GetProjectFolder();

            if (!Directory.Exists(projectFolder))
                throw new InvalidOperationException("{0} not found. Override GetProjectFolder".FormatWith(projectFolder));

            bool? overwriteFiles = null;

            foreach (var mod in GetModules())
            {
                string str = WriteClientFile(mod);
                if (str != null)
                {
                    string fullFileName = Path.Combine(projectFolder, GetClientFileName(mod));
                    FileTools.CreateParentDirectory(fullFileName);

                    if (!File.Exists(fullFileName) || SafeConsole.Ask(ref overwriteFiles, "Overwrite {0}?".FormatWith(fullFileName)))
                        File.WriteAllText(fullFileName, str);
                }

                string tsStr = WriteTypeScriptFile(mod);
                if (tsStr != null)
                {
                    string fullFileName = Path.Combine(projectFolder, GetTypeScriptFileName(mod));
                    FileTools.CreateParentDirectory(fullFileName);
                    if (!File.Exists(fullFileName) || SafeConsole.Ask(ref overwriteFiles, "Overwrite {0}?".FormatWith(fullFileName)))
                        File.WriteAllText(fullFileName, tsStr);
                }

                foreach (var t in mod.Types)
                {
                    string viewStr = WriteViewFile(t);
                    if (viewStr != null)
                    {
                        string fullFileName = Path.Combine(projectFolder, GetViewFileName(mod, t));
                        FileTools.CreateParentDirectory(fullFileName);
                        if (!File.Exists(fullFileName) || SafeConsole.Ask(ref overwriteFiles, "Overwrite {0}?".FormatWith(fullFileName)))
                            File.WriteAllText(fullFileName, viewStr);
                    }
                }
            }
        }


        protected virtual string GetProjectFolder()
        {
            return Path.Combine(SolutionFolder, SolutionName + ".Web");
        }

        protected virtual void GetSolutionInfo(out string solutionFolder, out string solutionName)
        {
            CodeGenerator.GetSolutionInfo(out solutionFolder, out solutionName);
        }

        protected virtual string GetClientFileName(Module t)
        {
            return "Code\\" + t.ModuleName + "Client.cs";
        }

        protected virtual IEnumerable<Module> GetModules()
        {
            Dictionary<Type, bool> types = CandidateTypes().ToDictionary(a => a, Schema.Current.Tables.ContainsKey);

            return CodeGenerator.GetModules(types, this.SolutionName);
        }

        protected virtual List<Type> CandidateTypes()
        {
            var assembly = Assembly.Load(Assembly.GetEntryAssembly().GetReferencedAssemblies().Single(a => a.Name == this.SolutionName + ".Entities"));

            return assembly.GetTypes().Where(t => t.IsModifiableEntity() && !t.IsAbstract && !typeof(MixinEntity).IsAssignableFrom(t)).ToList();
        }

        protected virtual string WriteClientFile(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in GetClienUsingNamespaces(mod))
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine("namespace " + GetClientNamespace(mod));
            sb.AppendLine("{");
            sb.Append(WriteClientClass(mod).Indent(4));
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string WriteClientClass(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static class " + mod.ModuleName + "Client");
            sb.AppendLine("{");

            sb.AppendLine(GetViewPrefix(mod).Indent(4));
            sb.AppendLine(GetJsModule(mod).Indent(4));
            sb.AppendLine();

            sb.Append(WriteStartMethod(mod).Indent(4));

            sb.AppendLine("}");
            return sb.ToString();
        }

        protected virtual string GetJsModule(Module mod)
        {
            return "public static JsModule {0}Module = new JsModule(\"{0}\");".FormatWith(mod.ModuleName);
        }

        protected virtual string GetViewPrefix(Module mod)
        {
           return "public static string ViewPrefix = \"~/Views/{0}/{1}.cshtml\";".FormatWith(mod.ModuleName, "{0}");
        }

        protected virtual string GetClientNamespace(Module mod)
        {
            return SolutionName + ".Web." + mod.ModuleName;
        }

        protected virtual List<string> GetClienUsingNamespaces(Module mod)
        {
            var result = new List<string>()
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Reflection",
                "System.Web.Mvc",
                "Signum.Utilities",
                "Signum.Entities",
                "Signum.Engine",
                "Signum.Engine.Operations",
                "Signum.Web",
                "Signum.Web.Operations",
            };

            result.AddRange(mod.Types.Select(t => t.Namespace).Distinct());

            return result;
        }

        protected virtual string WriteStartMethod(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static void Start()");
            sb.AppendLine("{");
            sb.AppendLine("    if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))");
            sb.AppendLine("    {");

            string entitySettings = WritetEntitySettings(mod);
            if (entitySettings != null)
                sb.Append(entitySettings.Indent(8));

            sb.AppendLine();

            string operationSettings = WritetOperationSettings(mod);
            if (operationSettings != null)
                sb.Append(operationSettings.Indent(8));

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string WritetEntitySettings(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Navigator.AddSettings(new List<EntitySettings>");
            sb.AppendLine("{");
            foreach (var t in mod.Types)
            {
                string es = GetEntitySetting(t);
                if (es != null)
                    sb.AppendLine(es.Indent(4)); 
            }
            sb.AppendLine("});");
            return sb.ToString();
        }

        protected virtual string GetEntitySetting(Type type)
        {
            var v = GetVarName(type);

            return "new {0}<{1}>() {{ PartialViewName = {2} => ViewPrefix.FormatWith(\"{3}\") }},".FormatWith(
                type.IsEmbeddedEntity() ? "EmbeddedEntitySettings" : "EntitySettings",
                type.Name, v, GetViewName(type));
        }

        protected virtual string GetVarName(Type type)
        {
            return type.Name.Substring(0, 1).ToLower();
          
        }

        protected virtual string WritetOperationSettings(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("OperationClient.AddSettings(new List<OperationSettings> ");
            sb.AppendLine("{");
            sb.AppendLine("    //new EntityOperationSettings<T>(operation){ ... }");
            sb.AppendLine("});");
            return sb.ToString();
        }

     

        private string WriteTypeScriptFile(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/// <reference path=\"../../framework/signum.web/signum/scripts/globals.ts\"/>");
            sb.AppendLine();
            sb.AppendLine("import Entities = require(\"Framework/Signum.Web/Signum/Scripts/Entities\")");
            sb.AppendLine("import Navigator = require(\"Framework/Signum.Web/Signum/Scripts/Navigator\")");
            sb.AppendLine("import Finder = require(\"Framework/Signum.Web/Signum/Scripts/Finder\")");
            sb.AppendLine("import Lines = require(\"Framework/Signum.Web/Signum/Scripts/Lines\")");
            sb.AppendLine("import Operations = require(\"Framework/Signum.Web/Signum/Scripts/Operations\")");

            return sb.ToString();
        }

        protected virtual string GetViewFileName(Module m, Type t)
        {
            return "Views\\" + m.ModuleName+ "\\" + GetViewName(t) + ".cshtml";
        }

        protected virtual string GetViewName(Type type)
        {
            return Reflector.CleanTypeName(type);
        }

        protected virtual string GetTypeScriptFileName(Module t)
        {
            return "Scripts\\" + t.ModuleName + ".ts";
        }

        protected virtual List<string> GetViewUsingNamespaces(Type type)
        {
            var result = new List<string>()
            {
            };

            result.AddRange(type.Namespace);

            return result;
        }

        protected virtual string WriteViewFile(Type type)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in GetViewUsingNamespaces(type))
                sb.AppendLine("@using {0};".FormatWith(item));

            var v = GetVarName(type);
            var vc = v + "c"; 

            sb.AppendLine();
            sb.AppendLine("@using (var {0} = Html.TypeContext<{1}>())".FormatWith(vc, type.Name));
            sb.AppendLine("{");

            foreach (var pi in GetProperties(type))
            {
                string prop = WriteProperty(pi, v, vc);
                if (prop != null)
                    sb.Append(prop.Indent(4));
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string WriteProperty(PropertyInfo pi, string v, string vc)
        {
            if (pi.PropertyType.IsLite() || pi.PropertyType.IsIEntity())
                return WriteEntityProperty(pi, v, vc);

            if (pi.PropertyType.IsEmbeddedEntity())
                return WriteEmbeddedProperty(pi, v, vc);

            if (pi.PropertyType.IsMList())
                return WriteMListProperty(pi, v, vc);

            if (IsValue(pi.PropertyType))
                return WriteValueLine(pi, v, vc);

            return null;
        }

        protected virtual string WriteMListProperty(PropertyInfo pi, string v, string vc)
        {
            var elementType = pi.PropertyType.ElementType().CleanType();

            if (!(elementType.IsLite() || elementType.IsModifiableEntity()))
                return "//{0} not supported\r\n".FormatWith(pi.PropertyType.TypeName());

            var eka = elementType.GetCustomAttribute<EntityKindAttribute>();

            if (elementType.IsEmbeddedEntity() || (eka.EntityKind == EntityKind.Part || eka.EntityKind == EntityKind.SharedPart))
                return "@Html.EntityRepeater({0}, {1} => {1}.{2})\r\n".FormatWith(vc, v, pi.Name);

            return "@Html.EntityStrip({0}, {1} => {1}.{2})\r\n".FormatWith(vc, v, pi.Name);
        }

        protected virtual string WriteEmbeddedProperty(PropertyInfo pi, string v, string vc)
        {
            return "@Html.EntityDetail({0}, {1} => {1}.{2})\r\n".FormatWith(vc, v, pi.Name);   
        }

        protected virtual string WriteEntityProperty(PropertyInfo pi, string v, string vc)
        {
            Type type = pi.PropertyType.CleanType();

            var eka = type.GetCustomAttribute<EntityKindAttribute>();

            if (eka == null)
                return "@Html.EntityLine({0}, {1} => {1}.{2})\r\n".FormatWith(vc, v, pi.Name); //Interface

            if (eka.EntityKind == EntityKind.Part || eka.EntityKind == EntityKind.SharedPart)
                return "@Html.EntityDetail({0}, {1} => {1}.{2})\r\n".FormatWith(vc, v, pi.Name);

            if (eka.IsLowPopulation)
                return "@Html.EntityCombo({0}, {1} => {1}.{2})\r\n".FormatWith(vc, v, pi.Name);

            return "@Html.EntityLine({0}, {1} => {1}.{2})\r\n".FormatWith(vc, v, pi.Name);
        }


        protected virtual bool IsValue(Type type)
        {
            type = type.UnNullify();

            if (type.IsEnum || type == typeof(TimeSpan) || type == typeof(ColorEmbedded))
                return true;

            TypeCode tc = Type.GetTypeCode(type);

            return tc != TypeCode.DBNull &&
                tc != TypeCode.Empty &&
                tc != TypeCode.Object;
        }

        protected virtual string WriteValueLine(PropertyInfo pi, string v, string vc)
        {
            return "@Html.ValueLine({0}, {1} => {1}.{2})\r\n".FormatWith(vc, v, pi.Name);
        }

        protected virtual IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            return Reflector.PublicInstanceDeclaredPropertiesInOrder(type); 
        }

    }
}
