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
    public class WindowsCodeGenerator
    {
        public string SolutionName;
        public string SolutionFolder;

        public Schema CurrentSchema;

        public virtual void GenerateWindowsFromEntities()
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

                foreach (var t in mod.Types)
                {
                    string view = WriteViewFile(t);
                    if (view != null)
                    {
                        string fullFileName = Path.Combine(projectFolder, GetViewFileName(mod, t));
                        FileTools.CreateParentDirectory(fullFileName);
                        if (!File.Exists(fullFileName) || SafeConsole.Ask(ref overwriteFiles, "Overwrite {0}?".FormatWith(fullFileName)))
                            File.WriteAllText(fullFileName, view);


                        fullFileName += ".cs";

                        string codeBehind = WriteViewCodeBehindFile(t);
                        if (!File.Exists(fullFileName) || SafeConsole.Ask(ref overwriteFiles, "Overwrite {0}?".FormatWith(fullFileName)))
                            File.WriteAllText(fullFileName, codeBehind);
                    }
                }
            }
        }


        protected virtual string GetProjectFolder()
        {
            return Path.Combine(SolutionFolder, SolutionName + ".Windows");
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
            Dictionary<Type, bool> types = CandiateTypes().ToDictionary(a => a, Schema.Current.Tables.ContainsKey);

            return CodeGenerator.GetModules(types, this.SolutionName);
        }

        protected virtual List<Type> CandiateTypes()
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

            sb.AppendLine();

            sb.Append(WriteStartMethod(mod).Indent(4));

            sb.AppendLine("}");
            return sb.ToString();
        }


        protected virtual string GetClientNamespace(Module mod)
        {
            return SolutionName + ".Windows." + mod.ModuleName;
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
                "System.Windows",
                "System.Windows.Controls",
                "System.Windows.Data",
                "System.Windows.Media",
                "Signum.Utilities",
                "Signum.Entities",
                "Signum.Windows",
                "Signum.Windows.Operations",
            };

            result.AddRange(mod.Types.Select(t => t.Namespace).Distinct());

            result.AddRange(mod.Types.Select(t => GetViewNamespace(t)).Distinct());

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

            return "new {0}<{1}>() {{ View = {2} => new {3}() }},".FormatWith(
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

        protected virtual string GetViewFileName(Module m, Type t)
        {
            return "Controls\\" + m.ModuleName+ "\\" + GetViewName(t) + ".xaml";
        }

        protected virtual string GetViewName(Type type)
        {
            return Reflector.CleanTypeName(type);
        }

        protected virtual List<string> GetViewCodeBehindUsingNamespaces(Type type)
        {
            var result = new List<string>()
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Windows",
                "System.Windows.Controls",
                "System.Windows.Data",
                "System.Windows.Documents",
                "System.Windows.Input",
                "System.Windows.Media",
                "System.Windows.Media.Imaging",
                "System.Windows.Navigation",
                "System.Windows.Shapes",
                "Signum.Windows",
                "Signum.Entities",
            };

            result.AddRange(type.Namespace);

            return result;
        }

        protected virtual string WriteViewCodeBehindFile(Type type)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ns in GetViewCodeBehindUsingNamespaces(type))
                sb.AppendLine("using {0};".FormatWith(ns));

            sb.AppendLine();
            sb.AppendLine("namespace {0}".FormatWith(GetViewNamespace(type)));
            sb.AppendLine("{");

            string viewClass = WriteViewCodeBehindClass(type);
            if (viewClass != null)
                sb.Append(viewClass.Indent(4));

            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string WriteViewCodeBehindClass(Type type)
        {
            string viewName = GetViewName(type);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Interaction logic for {0}.xaml".FormatWith(viewName));
            sb.AppendLine("/// </summary>");
            sb.AppendLine("public partial class {0} : UserControl".FormatWith(viewName));
            sb.AppendLine("{");
            sb.AppendLine("    public {0}()".FormatWith(viewName));
            sb.AppendLine("    {");
            sb.AppendLine("        InitializeComponent();");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        protected virtual string GetViewNamespace(Type type)
        {
            return this.SolutionName + ".Windows.Controls" + type.Namespace.RemovePrefix(this.SolutionName + ".Entities");
        }


        protected virtual string WriteViewFile(Type type)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<UserControl x:Class=\"{0}.{1}\"".FormatWith(this.GetViewNamespace(type), this.GetViewName(type)));
            foreach (var item in GetViewUsingNamespaces(type))
                sb.AppendLine("    xmlns{0}=\"{1}\"".FormatWith(item.Key.HasText() ? ":" + item.Key : "", item.Value));

            if (typeof(IRootEntity).IsAssignableFrom(type))
                sb.AppendLine("    m:Common.TypeContext=\"{{x:Type d:{0}}}\"".FormatWith(type.Name));
            sb.AppendLine("    MinWidth=\"300\">");
            sb.AppendLine("    <StackPanel>");

            foreach (var pi in GetProperties(type))
            {
                string prop = WriteProperty(pi);
                if (prop != null)
                    sb.Append(prop.Indent(8));
            }

            sb.AppendLine("    </StackPanel>");
            sb.AppendLine("</UserControl>");

            return sb.ToString();
        }

        private Dictionary<string, string> GetViewUsingNamespaces(Type type)
        {
            return new Dictionary<string, string>
            {
                {"", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"}, 
                {"x", "http://schemas.microsoft.com/winfx/2006/xaml"}, 
                {"m", "clr-namespace:Signum.Windows;assembly=Signum.Windows"}, 
                {"d", "clr-namespace:{0};assembly={1}".FormatWith(type.Namespace, type.Assembly.GetName().Name) }, 
                {"s", "clr-namespace:{0}.Windows".FormatWith(this.SolutionName) }, 
                {"sc", "clr-namespace:{0}".FormatWith(this.GetViewNamespace(type))}, 
            };
        }

        protected virtual string WriteProperty(PropertyInfo pi)
        {
            if (pi.PropertyType.IsLite() || pi.PropertyType.IsIEntity())
                return WriteEntityProperty(pi);

            if (pi.PropertyType.IsEmbeddedEntity())
                return WriteEmbeddedProperty(pi);

            if (pi.PropertyType.IsMList())
                return WriteMListProperty(pi);

            if (IsValue(pi.PropertyType))
                return WriteValueLine(pi);

            return null;
        }

        protected virtual string WriteMListProperty(PropertyInfo pi)
        {
            var elementType = pi.PropertyType.ElementType().CleanType();

            if (!(elementType.IsLite() || elementType.IsModifiableEntity()))
                return "<!--{0} not supported-->//\r\n".FormatWith(pi.PropertyType.TypeName());
            
            var eka = elementType.GetCustomAttribute<EntityKindAttribute>();

            if (elementType.IsEmbeddedEntity() || (eka.EntityKind == EntityKind.Part || eka.EntityKind == EntityKind.SharedPart))
                return "<m:EntityRepeater m:Common.Route=\"{0}\" />\r\n".FormatWith(pi.Name);

            return "<m:EntityStrip m:Common.Route=\"{0}\" />\r\n".FormatWith(pi.Name);
        }

        protected virtual string WriteEmbeddedProperty(PropertyInfo pi)
        {
            return "<m:EntityDetail m:Common.Route=\"{0}\" />\r\n".FormatWith(pi.Name);
        }

        protected virtual string WriteEntityProperty(PropertyInfo pi)
        {
            Type type = pi.PropertyType.CleanType();

            var eka = type.GetCustomAttribute<EntityKindAttribute>();

            if (eka == null)
                throw new InvalidOperationException("'{0}' does not have EntityKindAttribute".FormatWith(type.Name));

            if (eka.EntityKind == EntityKind.Part || eka.EntityKind == EntityKind.SharedPart)
                return "<m:EntityDetail m:Common.Route=\"{0}\" />\r\n".FormatWith(pi.Name);

            if (eka.IsLowPopulation)
                return "<m:EntityCombo m:Common.Route=\"{0}\" />\r\n".FormatWith(pi.Name);

            return "<m:EntityLine m:Common.Route=\"{0}\" />\r\n".FormatWith(pi.Name);
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

        protected virtual string WriteValueLine(PropertyInfo pi)
        {
            return "<m:ValueLine m:Common.Route=\"{0}\" />\r\n".FormatWith(pi.Name);
        }

        protected virtual IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            return Reflector.PublicInstanceDeclaredPropertiesInOrder(type); 
        }

    }
}
