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
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.CodeGeneration
{
    public class LogicCodeGenerator
    {
        public string SolutionName;
        public string SolutionFolder;

        public Schema CurrentSchema;

        public virtual void GenerateLogicFromEntities()
        {
            CurrentSchema = Schema.Current;

            GetSolutionInfo(out SolutionFolder, out SolutionName);

            string projectFolder = GetProjectFolder();

            if (!Directory.Exists(projectFolder))
                throw new InvalidOperationException("{0} not found. Override GetProjectFolder".FormatWith(projectFolder));

            bool? overwriteFiles = null;

            foreach (var mod in GetModules())
            {
                string str = WriteFile(mod);

                string fileName = Path.Combine(projectFolder, GetFileName(mod));

                FileTools.CreateParentDirectory(fileName);

                if (!File.Exists(fileName) || SafeConsole.Ask(ref overwriteFiles, "Overwrite {0}?".FormatWith(fileName)))
                {
                    File.WriteAllText(fileName, str);
                }
            }
        }

        protected virtual string GetProjectFolder()
        {
            return Path.Combine(SolutionFolder, SolutionName + ".Logic");
        }

        protected virtual void GetSolutionInfo(out string solutionFolder, out string solutionName)
        {
            CodeGenerator.GetSolutionInfo(out solutionFolder, out solutionName);
        }

        protected virtual string GetFileName(Module t)
        {
            return t.ModuleName + "\\" + t.ModuleName + "Logic.cs";
        }

        protected virtual IEnumerable<Module> GetModules()
        {
            Dictionary<Type, bool> types = CandiateTypes().ToDictionary(a => a, Schema.Current.Tables.ContainsKey);

            return CodeGenerator.GetModules(types, this.SolutionName);
        }

        protected virtual List<Type> CandiateTypes()
        {
            var assembly = Assembly.Load(Assembly.GetEntryAssembly().GetReferencedAssemblies().Single(a => a.Name == this.SolutionName + ".Entities"));

            return assembly.GetTypes().Where(t => t.IsEntity() && !t.IsAbstract).ToList();
        }

        protected virtual string WriteFile(Module mod)
        {
            var expression = mod.Types.SelectMany(t => GetExpressions(t)).ToList();

            StringBuilder sb = new StringBuilder();
            foreach (var item in GetUsingNamespaces(mod, expression))
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine("namespace " + GetNamespace(mod));
            sb.AppendLine("{");
            sb.Append(WriteClass(mod, expression).Indent(4));
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string WriteClass(Module mod, List<ExpressionInfo> expressions)
        {
            var allExpression = mod.Types.SelectMany(t => GetExpressions(t)).ToList(); 

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static class " + mod.ModuleName + "Logic");
            sb.AppendLine("{");

            foreach (var ei in expressions)
            {
                string info = WriteExpressionMethod(ei);
                if (info != null)
                {
                    sb.Append(info.Indent(4));
                    sb.AppendLine();
                }
            }

            sb.Append(WriteStartMethod(mod, expressions).Indent(4));
            sb.AppendLine("}");
            return sb.ToString();
        }



        protected virtual string GetNamespace(Module mod)
        {
            return SolutionName + ".Logic." + mod.ModuleName;
        }

        protected virtual List<string> GetUsingNamespaces(Module mod, List<ExpressionInfo> expressions)
        {
            var result = new List<string>()
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Text",
                "System.Reflection",
                "Signum.Utilities",
                "Signum.Utilities.ExpressionTrees",
                "Signum.Entities",
                "Signum.Engine",
                "Signum.Engine.Operations",
                "Signum.Engine.Maps",
                "Signum.Engine.DynamicQuery",
            };

            result.AddRange(mod.Types.Concat(expressions.Select(e => e.FromType)).Select(t => t.Namespace).Distinct());

            return result;
        }

        protected virtual string WriteStartMethod(Module mod, List<ExpressionInfo> expressions)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)");
            sb.AppendLine("{");
            sb.AppendLine("    if (sb.NotDefined(MethodInfo.GetCurrentMethod()))");
            sb.AppendLine("    {");

            foreach (var item in mod.Types)
            {
                string include = WritetInclude(item);
                if (include != null)
                    sb.Append(include.Indent(8));
            }

            sb.AppendLine();

            foreach (var item in mod.Types)
            {
                string query = WriteQuery(item);
                if (query != null)
                {
                    sb.Append(query.Indent(8));
                    sb.AppendLine();
                }
            }

            if (expressions.Any())
            {
                foreach (var ei in expressions)
                {
                    string register = GetRegisterExpression(ei);
                    if (register != null)
                        sb.AppendLine(register.Indent(8));
                }

                sb.AppendLine();
            }

            foreach (var item in mod.Types)
            {
                string opers = WriteOperations(item);
                if (opers != null)
                {
                    sb.Append(opers.Indent(8));
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string GetRegisterExpression(ExpressionInfo ei)
        {
            return "dqm.RegisterExpression(({from} {f}) => {f}.{name}(), () => typeof({to}).{NiceName}());"
                .Replace("{from}", ei.FromType.Name)
                .Replace("{to}", ei.ToType.Name)
                .Replace("{f}", GetVariableName(ei.FromType))
                .Replace("{name}", ei.Name)
                .Replace("{NiceName}", ei.IsUnique ? "NiceName" : "NicePluralName");
        }

        protected virtual string WritetInclude(Type type)
        {
            return "sb.Include<" + type.TypeName() + ">();\r\n";
        }

        protected virtual string WriteQuery(Type type)
        {
            string typeName = type.TypeName();

            var v = GetVariableName(type);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("dqm.RegisterQuery(typeof({0}), () =>".FormatWith(typeName));
            sb.AppendLine("    from {0} in Database.Query<{1}>()".FormatWith(v, typeName));
            sb.AppendLine("    select new");
            sb.AppendLine("    {");
            sb.AppendLine("        Entity = {0},".FormatWith(v));
            sb.AppendLine("        {0}.Id,".FormatWith(v));
            foreach (var prop in GetQueryProperties(type))
	        {
                sb.AppendLine("        {0}.{1},".FormatWith(v, prop.Name));
	        }
            sb.AppendLine("    });");

            return sb.ToString();
        }

        protected internal class ExpressionInfo
        {
            public Type FromType;
            public Type ToType;
            public PropertyInfo Property;
            public string Name;
            public string ExpressionName;
            public bool IsUnique;
        }

        protected virtual List<ExpressionInfo> GetExpressions(Type toType)
        {
            var result = (from pi in Reflector.PublicInstanceDeclaredPropertiesInOrder(toType)
                          let fromType = pi.PropertyType.CleanType()
                          where fromType.IsEntity() && !fromType.IsAbstract
                          let fi = Reflector.TryFindFieldInfo(toType, pi)
                          where fi != null
                          let isUnique = fi.GetCustomAttribute<UniqueIndexAttribute>() != null
                          select new ExpressionInfo
                          {
                              ToType = toType,
                              FromType = fromType,
                              Property = pi,
                              IsUnique = isUnique,
                          }).ToList();

            foreach (var ei in result)
            {
                ei.Name = GetExpressionName(ei);
            }


            result = result.GroupBy(ei => new { ei.FromType, ei.ToType }).Where(g => g.Count() == 1).SelectMany(g => g).ToList();

            result = result.Where(ShouldWriteExpression).ToList();

            var groups = result.Select(a => a.Name).GroupCount();

            foreach (var ei in result)
            {
                if (groups[ei.Name] == 1)
                    ei.ExpressionName = ei.Name + "Expression";
                else
                    ei.ExpressionName = ei.Name + Reflector.CleanTypeName(ei.Property.PropertyType.CleanType()) + "Expresion";
            }

            return result;
        }

        protected virtual string GetExpressionName(ExpressionInfo ei)
        {
            if (ei.Property.Name == "Parent")
                return "Children";

            if(ei.IsUnique)
                return Reflector.CleanTypeName(ei.ToType);

            return NaturalLanguageTools.Pluralize(Reflector.CleanTypeName(ei.ToType).SpacePascal()).ToPascal();
        }

        protected virtual bool ShouldWriteExpression(ExpressionInfo ei)
        {
            switch (EntityKindCache.GetEntityKind(ei.FromType))
            {
                case EntityKind.Part:
                case EntityKind.String:
                case EntityKind.SystemString: return false;
                default: return true;
            }
        }

        protected virtual string WriteExpressionMethod(ExpressionInfo info)
        {
            Type from = info.Property.PropertyType.CleanType();

            string varFrom = GetVariableName(from);
            string varTo = GetVariableName(info.ToType);

            if (varTo == varFrom)
                varTo += "2";

            string filter = info.Property.PropertyType.IsLite() ? "{t} => {t}.{prop}.RefersTo({f})" : "{t} => {t}.{prop} == {f}";

            string str =  info.IsUnique?
@"static Expression<Func<{from}, {to}>> {MethodExpression} = 
    {f} => Database.Query<{to}>().SingleOrDefaultEx({filter});
[ExpressionField]
public static {to} {Method}(this {from} e)
{
    return {MethodExpression}.Evaluate(e);
}
" :
@"static Expression<Func<{from}, IQueryable<{to}>>> {MethodExpression} = 
    {f} => Database.Query<{to}>().Where({filter});
[ExpressionField]
public static IQueryable<{to}> {Method}(this {from} e)
{
    return {MethodExpression}.Evaluate(e);
}
";

            return str.Replace("{filter}", filter)
                .Replace("{from}", from.Name)
                .Replace("{to}", info.ToType.Name)
                .Replace("{t}", varTo)
                .Replace("{f}", varFrom)
                .Replace("{prop}", info.Property.Name)
                .Replace("{Method}", info.Name)
                .Replace("{MethodExpression}", info.ExpressionName);
        }

        protected virtual IEnumerable<PropertyInfo> GetQueryProperties(Type type)
        {
            return (from p in Reflector.PublicInstancePropertiesInOrder(type)
                    where Reflector.QueryableProperty(type, p)
                    where IsSimpleValueType(p.PropertyType) || p.PropertyType.IsEntity() || p.PropertyType.IsLite()
                    orderby p.Name.Contains("Name") ? 1 : 2
                    select p).Take(10);
        }

        protected virtual bool IsSimpleValueType(Type type)
        {
            var t = CurrentSchema.Settings.GetSqlDbTypePair(type.UnNullify());

            return t != null && t.UserDefinedTypeName == null && t.SqlDbType != SqlDbType.Image && t.SqlDbType != SqlDbType.VarBinary;
        }

        protected virtual string WriteOperations(Type type)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var oper in GetOperationsSymbols(type))
            {
                string operation = WriteOperation(oper);
                if (operation != null)
                {
                    sb.Append(operation);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        protected virtual string WriteOperation(IOperationSymbolContainer oper)
        {
            string type = oper.GetType().TypeName();

            if (type.Contains("ExecuteSymbolImp"))
                return WriteExecuteOperation((IEntityOperationSymbolContainer)oper);

            if (type.Contains("DeleteSymbolImp"))
                return WriteDeleteOperation((IEntityOperationSymbolContainer)oper);

            if (type.Contains("FromImp"))
                return WriteConstructFrom((IEntityOperationSymbolContainer)oper);

            if (type.Contains("FromManyImp"))
                return WriteConstructFromMany(oper);

            if (type.Contains("SimpleImp"))
                return WriteConstructSimple(oper);

            throw new InvalidOperationException();
        }

        protected virtual string WriteExecuteOperation(IEntityOperationSymbolContainer oper)
        {
            Type type = oper.GetType().GetGenericArguments().Single();

            var v = GetVariableName(type);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("new Graph<{0}>.Execute({1})".FormatWith(type.TypeName(), oper.Symbol.ToString()));
            sb.AppendLine("{");
            if (IsSave(oper))
            {
                sb.AppendLine("    AllowsNew = true,");
                sb.AppendLine("    Lite = false,");
            }
            sb.AppendLine("    Execute = ({0}, _) => {{ }}".FormatWith(v));
            sb.AppendLine("}.Register();");
            return sb.ToString();
        }

        protected virtual bool IsSave(IEntityOperationSymbolContainer oper)
        {
            return oper.ToString().Contains("Save"); ;
        }

        protected virtual string WriteDeleteOperation(IEntityOperationSymbolContainer oper)
        {
            Type type = oper.GetType().GetGenericArguments().Single();

            string v = GetVariableName(type);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("new Graph<{0}>.Delete({1})".FormatWith(type.TypeName(), oper.Symbol.ToString()));
            sb.AppendLine("{");
            sb.AppendLine("    Delete = ({0}, _) => {0}.Delete()".FormatWith(v));
            sb.AppendLine("}.Register();");
            return sb.ToString();
        }

        protected virtual string GetVariableName(Type type)
        {
            return type.Name.Substring(0, 1).ToLower();
        }

        protected virtual string WriteConstructSimple(IOperationSymbolContainer oper)
        {
            Type type = oper.GetType().GetGenericArguments().Single();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("new Graph<{0}>.Construct({1})".FormatWith(type.TypeName(), oper.Symbol.ToString()));
            sb.AppendLine("{");
            sb.AppendLine("    Construct = (_) => new {0}".FormatWith(type.TypeName()));
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine("}.Register();");
            return sb.ToString();
        }

        protected virtual string WriteConstructFrom(IEntityOperationSymbolContainer oper)
        {
            List<Type> type = oper.GetType().GetGenericArguments().ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("new Graph<{0}>.ConstructFrom<{1}>({2})".FormatWith(type[0].TypeName(), type[1].TypeName(), oper.Symbol.ToString()));
            sb.AppendLine("{");
            sb.AppendLine("    Construct = ({0}, _) => new {1}".FormatWith(GetVariableName(type[1]), type[0].TypeName()));
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine("}.Register();");
            return sb.ToString();
        }

        protected virtual string WriteConstructFromMany(IOperationSymbolContainer oper)
        {
            List<Type> type = oper.GetType().GetGenericArguments().ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("new Graph<{0}>.ConstructFromMany<{1}>({2})".FormatWith(type[0].TypeName(), type[1].TypeName(), oper.Symbol.ToString()));
            sb.AppendLine("{");
            sb.AppendLine("    Construct = ({0}s, _) => new {1}".FormatWith(GetVariableName(type[1]), type[0].TypeName()));
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine("}.Register();");
            return sb.ToString();
        }

        protected virtual IEnumerable<IOperationSymbolContainer> GetOperationsSymbols(Type type)
        {
            string name = type.FullName.RemoveSuffix("Entity") + "Operation";

            var operType = type.Assembly.GetType(name);

            if (operType == null)
                return Enumerable.Empty<IOperationSymbolContainer>();

            return (from fi in operType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    select (IOperationSymbolContainer)fi.GetValue(null)).ToList();
        }
    }
}
