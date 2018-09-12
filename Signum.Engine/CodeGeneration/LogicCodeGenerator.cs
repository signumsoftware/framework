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

        protected bool? overwriteFiles = null;

        public virtual void GenerateLogicFromEntities()
        {
            CurrentSchema = Schema.Current;

            GetSolutionInfo(out SolutionFolder, out SolutionName);

            string projectFolder = GetProjectFolder();

            if (!Directory.Exists(projectFolder))
                throw new InvalidOperationException("{0} not found. Override GetProjectFolder".FormatWith(projectFolder));
            
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
            Dictionary<Type, bool> types = CandidateTypes().ToDictionary(a => a, Schema.Current.Tables.ContainsKey);

            return CodeGenerator.GetModules(types, this.SolutionName);
        }

        protected virtual List<Type> CandidateTypes()
        {
            var assembly = Assembly.Load(Assembly.GetEntryAssembly().GetReferencedAssemblies().Single(a => a.Name == this.SolutionName + ".Entities"));

            return assembly.GetTypes().Where(t => t.IsEntity() && !t.IsAbstract && !typeof(MixinEntity).IsAssignableFrom(t)).ToList();
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
            sb.Append(WriteLogicClass(mod, expression).Indent(4));
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string WriteLogicClass(Module mod, List<ExpressionInfo> expressions)
        {
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
            var allExpressions = expressions.ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static void Start(SchemaBuilder sb)");
            sb.AppendLine("{");
            sb.AppendLine("    if (sb.NotDefined(MethodInfo.GetCurrentMethod()))");
            sb.AppendLine("    {");

            foreach (var item in mod.Types)
            {
                string include = WriteInclude(item, allExpressions);
                if (include != null)
                {
                    sb.Append(include.Indent(8));
                    sb.AppendLine();
                }

                string query = WriteQuery(item);
                if (query != null)
                {
                    sb.Append(query.Indent(8));
                    sb.AppendLine();
                }

                string opers = WriteOperations(item);
                if (opers != null)
                {
                    sb.Append(opers.Indent(8));
                    sb.AppendLine();
                }
            }
            
            if (allExpressions.Any())
            {
                foreach (var ei in allExpressions)
                {
                    string register = GetRegisterExpression(ei);
                    if (register != null)
                        sb.AppendLine(register.Indent(8));
                }

                sb.AppendLine();
            }
            

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        protected virtual string GetRegisterExpression(ExpressionInfo ei)
        {
            return "QueryLogic.Expressions.Register(({from} {f}) => {f}.{name}(), () => typeof({to}).{NiceName}());"
                .Replace("{from}", ei.FromType.Name)
                .Replace("{to}", ei.ToType.Name)
                .Replace("{f}", GetVariableName(ei.FromType))
                .Replace("{name}", ei.Name)
                .Replace("{NiceName}", ei.IsUnique ? "NiceName" : "NicePluralName");
        }

        protected virtual string WriteInclude(Type type, List<ExpressionInfo> expression)
        {
            var ops = GetOperationsSymbols(type);
            var save = ops.SingleOrDefaultEx(o => GetOperationType(o) == OperationType.Execute && IsSave(o));
            var delete = ops.SingleOrDefaultEx(o => GetOperationType(o) == OperationType.Delete);
            var p = ShouldWriteSimpleQuery(type) ? GetVariableName(type) : null;

            var simpleExpressions = expression.Extract(exp => IsSimpleExpression(exp, type));

            return new[]
            {
                "sb.Include<" + type.TypeName() + ">()",
                GetWithVirtualMLists(type),
                save != null && ShouldWriteSimpleOperations(save) ? ("   .WithSave(" + save.Symbol.ToString() + ")") : null,
                delete != null && ShouldWriteSimpleOperations(delete) ? ("   .WithDelete(" + delete.Symbol.ToString() + ")") : null,
                simpleExpressions.HasItems() ? simpleExpressions.ToString(e => $"   .WithExpressionFrom(({e.FromType.Name} {GetVariableName(e.FromType)}) => {GetVariableName(e.FromType)}.{e.Name}())", "\r\n") : null,
                p == null ? null : $"   .WithQuery(() => {p} => {WriteQueryConstructor(type, p)})"
            }.NotNull().ToString("\r\n") + ";";
        }

        private bool IsSimpleExpression(ExpressionInfo exp, Type type)
        {
            return !exp.IsUnique && type == exp.ToType;
        }

        protected virtual string WriteQuery(Type type)
        {
            if (ShouldWriteSimpleQuery(type))
                return null;

            string typeName = type.TypeName();

            var v = GetVariableName(type);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("QueryLogic.Queries.Register(typeof({0}), () =>".FormatWith(typeName));
            sb.AppendLine("    from {0} in Database.Query<{1}>()".FormatWith(v, typeName));
            sb.AppendLine("    select " + WriteQueryConstructor(type, v) + ");");
            return sb.ToString();
        }

        private string WriteQueryConstructor(Type type, string v)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("new");
            sb.AppendLine("    {");
            sb.AppendLine("        Entity = {0},".FormatWith(v));
            sb.AppendLine("        {0}.Id,".FormatWith(v));
            foreach (var prop in GetQueryProperties(type))
            {
                sb.AppendLine("        {0}.{1},".FormatWith(v, prop.Name));
            }
            sb.Append("    }");
            return sb.ToString();

        }

        protected virtual bool ShouldWriteSimpleQuery(Type type)
        {
            return true;
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

            string filter = info.Property.PropertyType.IsLite() ? "{t} => {t}.{prop}.Is({f})" : "{t} => {t}.{prop} == {f}";

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

        protected virtual string GetWithVirtualMLists(Type type)
        {
            return (from p in Reflector.PublicInstancePropertiesInOrder(type)
                    let bp = GetVirtualMListBackReference(p)
                    where bp != null
                    select GetWithVirtualMList(type, p, bp)).ToString("\r\n").DefaultText(null);
        }

        protected virtual string GetWithVirtualMList(Type type, PropertyInfo p, PropertyInfo bp)
        {
            var p1 = GetVariableName(type);
            var p2 = GetVariableName(p.PropertyType.ElementType());
            if (p1 == p2)
                p2 += "2";

            var cast = p.DeclaringType == bp.PropertyType.CleanType() ? "" : $"(Lite<{p.DeclaringType.Name}>)";

            return $"   .WithVirtualMList({p1} => {p1}.{p.Name}, {p2} => {cast}{p2}.{bp.Name})";
        }

        protected virtual PropertyInfo GetVirtualMListBackReference(PropertyInfo pi)
        {
            if (!pi.PropertyType.IsMList())
                return null;

            if (!pi.PropertyType.ElementType().IsEntity())
                return null;

            if (!pi.HasAttribute<IgnoreAttribute>())
                return null;

            var t = pi.PropertyType.ElementType();

            var backProperty = Reflector.PublicInstancePropertiesInOrder(t).SingleOrDefaultEx(bp => IsVirtualMListBackReference(bp, pi.DeclaringType));

            return backProperty;
        }

        protected virtual bool IsVirtualMListBackReference(PropertyInfo pi, Type targetType)
        {
            if (!pi.PropertyType.IsLite())
                return false;

            if (pi.PropertyType.CleanType() == targetType)
                return true;
            
            if (pi.GetCustomAttribute<ImplementedByAttribute>()?.ImplementedTypes.Contains(targetType) == true)
                return true;

            return false;
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

            switch (GetOperationType(oper))
            {
                case OperationType.Execute:
                    if (IsSave(oper) && ShouldWriteSimpleOperations(oper))
                        return null;
                    return WriteExecuteOperation(oper);
                case OperationType.Delete:
                    return WriteDeleteOperation(oper);
                case OperationType.Constructor:
                    return WriteConstructSimple(oper);
                case OperationType.ConstructorFrom:
                    return WriteConstructFrom(oper);
                case OperationType.ConstructorFromMany:
                    return WriteConstructFromMany(oper);
                default:
                    throw new InvalidOperationException();
            }
        }

        private OperationType GetOperationType(IOperationSymbolContainer oper)
        {
            string type = oper.GetType().TypeName();

            if (type.Contains("ExecuteSymbolImp"))
                return OperationType.Execute;

            if (type.Contains("DeleteSymbolImp"))
                return OperationType.Delete;

            if (type.Contains("SimpleImp"))
                return OperationType.Constructor;

            if (type.Contains("FromImp"))
                return OperationType.ConstructorFrom;

            if (type.Contains("FromManyImp"))
                return OperationType.ConstructorFromMany;
;
            throw new InvalidOperationException();
        }

        protected virtual string WriteExecuteOperation(IOperationSymbolContainer oper)
        {   
            Type type = oper.GetType().GetGenericArguments().Single();

            var v = GetVariableName(type);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("new Graph<{0}>.Execute({1})".FormatWith(type.TypeName(), oper.Symbol.ToString()));
            sb.AppendLine("{");
            if (IsSave(oper))
            {
                sb.AppendLine("    CanBeNew = true,");
                sb.AppendLine("    CanBeModified = true,");
            }
            sb.AppendLine("    Execute = ({0}, _) => {{ }}".FormatWith(v));
            sb.AppendLine("}.Register();");
            return sb.ToString();
        }

        private bool ShouldWriteSimpleOperations(IOperationSymbolContainer oper)
        {
            return true;
        }

        protected virtual bool IsSave(IOperationSymbolContainer oper)
        {
            return oper.ToString().Contains("Save"); ;
        }

        protected virtual string WriteDeleteOperation(IOperationSymbolContainer oper)
        {
            if (ShouldWriteSimpleOperations(oper))
                return null;

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

        protected virtual string WriteConstructFrom(IOperationSymbolContainer oper)
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
