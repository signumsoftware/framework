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
    public class ReactCodeGenerator
    {
        public string SolutionName;
        public string SolutionFolder;

        public Schema CurrentSchema;

        public virtual void GenerateReactFromEntities()
        {
            CurrentSchema = Schema.Current;

            GetSolutionInfo(out SolutionFolder, out SolutionName);

            string projectFolder = GetProjectFolder();

            if (!Directory.Exists(projectFolder))
                throw new InvalidOperationException("{0} not found. Override GetProjectFolder".FormatWith(projectFolder));

            bool? overwriteFiles = null;

            foreach (var mod in GetModules())
            {
                WriteFile(() => WriteClientFile(mod), ()=>GetClientFile(mod), ref overwriteFiles);
                WriteFile(() => WriteTypingsFile(mod), ()=> GetTypingsFile(mod), ref overwriteFiles);

                foreach (var t in mod.Types)
                {
                    WriteFile(() => WriteEntityComponentFile(t), () => GetViewFileName(mod, t), ref overwriteFiles);
                }

                WriteFile(() => WriteServerFile(mod), () => ServerFileName(mod), ref overwriteFiles);
                WriteFile(() => WriteControllerFile(mod), () => ControllerFileName(mod), ref overwriteFiles);
            }
        }

        protected virtual void WriteFile(Func<string> getContent, Func<string> getFileName, ref bool? overwriteFiles)
        {
            var content = getContent();
            if (content == null)
                return;


            var fileName = getFileName();


            FileTools.CreateParentDirectory(fileName);
            if (!File.Exists(fileName) || SafeConsole.Ask(ref overwriteFiles, "Overwrite {0}?".FormatWith(fileName)))
                File.WriteAllText(fileName, content, Encoding.UTF8);
        }

        protected virtual string GetProjectFolder()
        {
            return Path.Combine(SolutionFolder, SolutionName + ".React");
        }

        protected virtual void GetSolutionInfo(out string solutionFolder, out string solutionName)
        {
            CodeGenerator.GetSolutionInfo(out solutionFolder, out solutionName);
        }

        protected virtual string ServerFileName(Module m)
        {
            return BaseFileName(m) + m.ModuleName + "Server.cs";
        }
        
        protected virtual string GetViewFileName(Module m, Type t)
        {
            return BaseFileName(m)  + "Templates\\" + GetViewName(t) + ".tsx";
        }

        protected virtual string ControllerFileName(Module m)
        {
            return BaseFileName(m) + m.ModuleName + "Controller.cs";
        }

        protected virtual string GetClientFile(Module m)
        {
            return BaseFileName(m) + m.ModuleName + "Client.tsx";
        }

        protected virtual string GetTypingsFile(Module m)
        {
            return BaseFileName(m) + m.Types.First().Namespace + ".t4s";
        }

        protected virtual string BaseFileName(Module m)
        {
            return Path.Combine(GetProjectFolder(), "App\\" + m.ModuleName + "\\");
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

        protected virtual string WriteServerFile(Module mod)
        {
            if (!ShouldWriteServerFile(mod))
                return null;

            StringBuilder sb = new StringBuilder();
            foreach (var item in GetServerUsingNamespaces(mod))
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine("namespace " + GetServerNamespace(mod));
            sb.AppendLine("{");
            sb.Append(WriteServerClass(mod).Indent(4));
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual bool ShouldWriteServerFile(Module mod)
        {
            return SafeConsole.Ask($"Write Server File for {mod.ModuleName}?");
        }

        protected virtual string GetServerNamespace(Module mod)
        {
            return SolutionName + ".React." + mod.ModuleName;
        }

        protected virtual string WriteServerClass(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static class " + mod.ModuleName + "Server");
            sb.AppendLine("{");

            sb.AppendLine();

            sb.Append(WriteServerStartMethod(mod).Indent(4));

            sb.AppendLine("}");
            return sb.ToString();
        }

        protected virtual string WriteServerStartMethod(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static void Start()");
            sb.AppendLine("{");
            sb.AppendLine("}");

            return sb.ToString();
        }


        protected virtual string WriteControllerFile(Module mod)
        {
            if (!ShouldWriteControllerFile(mod))
                return null;

            StringBuilder sb = new StringBuilder();
            foreach (var item in GetServerUsingNamespaces(mod))
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine("namespace " + GetServerNamespace(mod));
            sb.AppendLine("{");
            sb.Append(WriteControllerClass(mod).Indent(4));
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual bool ShouldWriteControllerFile(Module mod)
        {
            return SafeConsole.Ask($"Write Controller File for {mod.ModuleName}?");
        }

        protected virtual string WriteControllerClass(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public class " + mod.ModuleName + "Controller : ApiController");
            sb.AppendLine("{");

            sb.AppendLine();

            sb.Append(WriteControllerExampleMethod(mod).Indent(4));

            sb.AppendLine("}");
            return sb.ToString();
        }

        protected virtual string WriteControllerExampleMethod(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"//[Route(\"api/{mod.ModuleName.ToLower()}/login\"), HttpPost]");
            sb.AppendLine(@"//public MyResponse Login([FromBody]MyRequest data)");
            sb.AppendLine(@"//{");
            sb.AppendLine(@"//}");
            return sb.ToString();
        }
        

        protected virtual List<string> GetServerUsingNamespaces(Module mod)
        {
            var result = new List<string>()
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Reflection",
                "System.Web.Http",
                "Signum.Utilities",
                "Signum.Entities",
                "Signum.Engine",
                "Signum.Engine.Operations",
                "Signum.React",
            };

            result.AddRange(mod.Types.Select(t => t.Namespace).Distinct());

            return result;
        }


        protected virtual string WriteClientFile(Module mod)
        {
            var fra = FrameworkRelativePath(false);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("import * as React from 'react'");
            sb.AppendLine("import { Route } from 'react-router'");
            sb.AppendLine("import { ajaxPost, ajaxGet } from '" + fra + "Signum.React/Scripts/Services';");
            sb.AppendLine("import { EntitySettings, ViewPromise } from '" + fra + "Signum.React/Scripts/Navigator'");
            sb.AppendLine("import * as Navigator from '" + fra + "Signum.React/Scripts/Navigator'");
            sb.AppendLine("import { EntityOperationSettings } from '" + fra + "Signum.React/Scripts/Operations'");
            sb.AppendLine("import * as Operations from '" + fra + "Signum.React/Scripts/Operations'");

            foreach (var gr in mod.Types.GroupBy(a => a.Namespace))
            {
                sb.AppendLine("import { "
                    + gr.Select(t => t.Name).GroupsOf(5).ToString(a => a.ToString(", "), ",\r\n")
                    + " } from './" + gr.Key + "'");
            }

            sb.AppendLine();
            sb.AppendLine(WriteClientStartMethod(mod));

            return sb.ToString();
        }

        protected virtual string WriteTypingsFile(Module mod)
        {
            return "";
        }

        private static string[] GetTypingsImports()
        {
            return new[] { "Files", "Mailing", "SMS", "Processes", "Basics", "Scheduler" };
        }

        protected virtual string FrameworkRelativePath(bool inView)
        {
            var result = "../../../Framework/";

            return inView ? "../" + result : result;
        }

        protected virtual string ExtensonsRelativePath(bool inView)
        {
            var result = "../../../Extensions/";

            return inView ? "../" + result : result;
        }

        protected virtual string WriteClientStartMethod(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("export function start(options: { routes: JSX.Element[] }) {");
            sb.AppendLine("");

            string entitySettings = WritetEntitySettings(mod);
            if (entitySettings != null)
                sb.Append(entitySettings.Indent(4));

            sb.AppendLine();

            string operationSettings = WriteOperationSettings(mod);
            if (operationSettings != null)
                sb.Append(operationSettings.Indent(4));
            
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string WritetEntitySettings(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (var t in mod.Types)
            {
                string es = GetEntitySetting(t);
                if (es != null)
                    sb.AppendLine(es); 
            }
            return sb.ToString();
        }

        protected virtual string GetEntitySetting(Type type)
        {
            var v = GetVarName(type);

            return "Navigator.addSettings(new EntitySettings({0}, {1} => import('./Templates/{2}')));".FormatWith(
                type.Name, v, GetViewName(type));
        }

        protected virtual string GetVarName(Type type)
        {
            return type.Name.Substring(0, 1).ToLower();
        }

        protected virtual string WriteOperationSettings(Module mod)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Operations.addSettings(new EntityOperationSettings(MyEntityOperations.Save, {}));");
            return sb.ToString();
        }


        protected virtual string GetViewName(Type type)
        {
            return Reflector.CleanTypeName(type);
        }


        protected virtual string WriteEntityComponentFile(Type type)
        {
            var frp = FrameworkRelativePath(true);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("import * as React from 'react'");
            sb.AppendLine("import { "  + type.Name + " } from '../" + type.Namespace + "'");
            sb.AppendLine("import { TypeContext, ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, FormGroup, FormGroupStyle, FormGroupSize } from '" + frp + "Signum.React/Scripts/Lines'");
            sb.AppendLine("import { SearchControl, ValueSearchControl, FilterOperation, OrderType, PaginationMode } from '" + frp + "Signum.React/Scripts/Search'");
            
            var v = GetVarName(type);


            var getControlName = 


            sb.AppendLine();
            sb.AppendLine("export default class {0} extends React.Component<{{ ctx: TypeContext<{1}> }}> {{".FormatWith(GetViewName(type), type.Name));
            sb.AppendLine("");
            sb.AppendLine("    render() {");
            sb.AppendLine("        var ctx = this.props.ctx;");
            sb.AppendLine("        return (");
            sb.AppendLine("            <div>");

            foreach (var pi in GetProperties(type))
            {
                string prop = WriteProperty(pi, v);
                if (prop != null)
                    sb.AppendLine(prop.Indent(16));
            }

            sb.AppendLine("            </div>");
            sb.AppendLine("        );");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string WriteProperty(PropertyInfo pi, string v)
        {
            if (pi.PropertyType.IsLite() || pi.PropertyType.IsIEntity())
                return WriteEntityProperty(pi, v);

            if (pi.PropertyType.IsEmbeddedEntity())
                return WriteEmbeddedProperty(pi, v);

            if (pi.PropertyType.IsMList())
                return WriteMListProperty(pi, v);

            if (IsValue(pi.PropertyType))
                return WriteValueLine(pi, v);

            return null;
        }

        protected virtual string WriteMListProperty(PropertyInfo pi, string v)
        {
            var elementType = pi.PropertyType.ElementType().CleanType();

            if (!(elementType.IsLite() || elementType.IsModifiableEntity()))
                return $"{{ /* {pi.PropertyType.TypeName()} not supported */ }}";

            var eka = elementType.GetCustomAttribute<EntityKindAttribute>();

            if (elementType.IsEmbeddedEntity() || (eka.EntityKind == EntityKind.Part || eka.EntityKind == EntityKind.SharedPart))
                return "<EntityRepeater ctx={{ctx.subCtx({0} => {0}.{1})}} />".FormatWith(v, pi.Name.FirstLower());

            return "<EntityStrip ctx={{ctx.subCtx({0} => {0}.{1})}} />".FormatWith(v, pi.Name.FirstLower());
        }

        protected virtual string WriteEmbeddedProperty(PropertyInfo pi, string v)
        {
            return "<EntityDetail ctx={{ctx.subCtx({0} => {0}.{1})}} />".FormatWith(v, pi.Name.FirstLower());
        }

        protected virtual string WriteEntityProperty(PropertyInfo pi, string v)
        {
            Type type = pi.PropertyType.CleanType();

            var eka = type.GetCustomAttribute<EntityKindAttribute>();

            if (eka == null)
                return "<EntityLine ctx={{ctx.subCtx({0} => {0}.{1})}} />".FormatWith(v, pi.Name.FirstLower());

            if (eka.EntityKind == EntityKind.Part || eka.EntityKind == EntityKind.SharedPart)
                return "<EntityDetail ctx={{ctx.subCtx({0} => {0}.{1})}} />".FormatWith(v, pi.Name.FirstLower());

            if (eka.IsLowPopulation)
                return "<EntityCombo ctx={{ctx.subCtx({0} => {0}.{1})}} />".FormatWith(v, pi.Name.FirstLower());

            return "<EntityLine ctx={{ctx.subCtx({0} => {0}.{1})}} />".FormatWith(v, pi.Name.FirstLower());
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

        protected virtual string WriteValueLine(PropertyInfo pi, string v)
        {
            return "<ValueLine ctx={{ctx.subCtx({0} => {0}.{1})}} />".FormatWith(v, pi.Name.FirstLower());
        }

        protected virtual IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            return Reflector.PublicInstanceDeclaredPropertiesInOrder(type).Where(pi =>
            {
                var ts = pi.GetCustomAttribute<InTypeScriptAttribute>();
                if (ts != null)
                {
                    var inTS = ts.GetInTypeScript();

                    if (inTS != null)
                        return inTS.Value;
                }

                if (pi.HasAttribute<HiddenPropertyAttribute>() || pi.HasAttribute<ExpressionFieldAttribute>())
                    return false;

                return true;
            });
        }

    }
}
