using Signum.Cache;
using Signum.Dynamic.SqlMigrations;
using Signum.Dynamic;
using Signum.Engine.Sync;
using Signum.Eval;
using System.Data;

namespace Signum.Dynamic.Types;

public static class DynamicTypeLogic
{

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicTypeEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.TypeName,
                e.BaseType,
            });



        DynamicTypeGraph.Register();
        DynamicLogic.GetCodeFiles += GetCodeFiles;
        DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
        EvalLogic.RegisteredDynamicTypes.Add(typeof(DynamicTypeEntity));
    }

    public class DynamicTypeGraph : Graph<DynamicTypeEntity>
    {
        public static void Register()
        {
            new Construct(DynamicTypeOperation.Create)
            {
                Construct = (_) => new DynamicTypeEntity { BaseType = DynamicBaseType.Entity },
            }.Register();

            new ConstructFrom<DynamicTypeEntity>(DynamicTypeOperation.Clone)
            {
                Construct = (e, _) =>
                {

                    var def = e.GetDefinition();
                    var result = new DynamicTypeEntity { TypeName = null!, BaseType = e.BaseType };
                    result.SetDefinition(def);
                    return result;
                },
            }.Register();

            new Execute(DynamicTypeOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (e, _) =>
                {
                    var newDef = e.GetDefinition();
                    var duplicatePropertyNames = newDef.Properties
                            .GroupToDictionary(a => a.Name.ToLower())
                            .Where(a => a.Value.Count() > 1)
                            .ToList();

                    if (duplicatePropertyNames.Any())
                        throw new InvalidOperationException(ValidationMessage._0HasSomeRepeatedElements1.NiceToString(e.TypeName, duplicatePropertyNames.Select(a => a.Key.FirstUpper()).Comma(", ")));

                    if (!e.IsNew)
                    {
                        var old = e.ToLite().RetrieveAndRemember();
                        if (e.TypeName != old.TypeName)
                            DynamicSqlMigrationLogic.AddDynamicRename(TypeNameKey, old.TypeName, e.TypeName);

                        if (e.BaseType == DynamicBaseType.ModelEntity)
                            return;

                        var oldDef = old.GetDefinition();
                        var newName = GetTableName(e, newDef);
                        var oldName = GetTableName(old, oldDef);

                        if (newName != oldName)
                            DynamicSqlMigrationLogic.AddDynamicRename(Replacements.KeyTables, oldName, newName);

                        var pairs = newDef.Properties
                            .Join(oldDef.Properties, n => n.UID, o => o.UID, (n, o) => new { n, o })
                            .Where(a => a.n.Type == a.o.Type); 
                        {
                            string ColName(DynamicProperty dp) => dp.ColumnName ?? dp.Name;

                            string replacementKey = e.BaseType != DynamicBaseType.Entity || old.BaseType != DynamicBaseType.Entity ? UnknownColumnKey : Replacements.KeyColumnsForTable(oldName);
                            foreach (var a in pairs.Where(a => ColName(a.n!) != ColName(a.o!)))
                            {
                                DynamicSqlMigrationLogic.AddDynamicRename(replacementKey, ColName(a.o!), ColName(a.n!));
                            }
                        }

                        {
                            string replacementKey = e.BaseType != DynamicBaseType.Entity || old.BaseType != DynamicBaseType.Entity ? UnknownPropertyKey : PropertyRouteLogic.PropertiesFor.FormatWith(old.TypeName);
                            foreach (var a in pairs.Where(a => a.n!.Name != a.o!.Name))
                            {
                                DynamicSqlMigrationLogic.AddDynamicRename(replacementKey, a.o!.Name, a.n!.Name);
                            }

                        }
                    }
                },
            }.Register();

            new Delete(DynamicTypeOperation.Delete)
            {
                Delete = (e, _) =>
                {
                    e.Delete();
                }
            }.Register();
        }
    }

    public const string UnknownPropertyKey = "UnknownProperty";
    public const string UnknownColumnKey = "UnknownColumn";
    public const string TypeNameKey = "TypeName";

    public static Func<DynamicTypeEntity, DynamicTypeDefinition, string> GetTableName = (dt, def) => def.TableName ?? "codegen." + dt.TypeName;

    public static string GetPropertyType(DynamicProperty property)
    {
        var generator = new DynamicTypeCodeGenerator(DynamicLogic.CodeGenNamespace, null!, DynamicBaseType.Entity, null!, new HashSet<string>());

        return generator.GetPropertyType(property);
    }

    internal static List<DynamicTypeEntity> GetTypes()
    {
        var cacheOldDisabled = CacheLogic.GloballyDisabled;
        CacheLogic.GloballyDisabled = true;
        try
        {
            if (!Administrator.ExistsTable<DynamicTypeEntity>())
                return new List<DynamicTypeEntity>();

            return ExecutionMode.Global().Using(a => Database.Query<DynamicTypeEntity>().ToList());
        }
        finally
        {
            CacheLogic.GloballyDisabled = cacheOldDisabled;
        }
    }

    public static void WriteDynamicStarter(StringBuilder sb, int indent)
    {

        var types = GetTypes();
        foreach (var item in types)
            sb.AppendLine($"{item}Logic.Start(sb);".Indent(indent));
    }

    public static Func<Dictionary<string, Dictionary<string, string>>>? GetAlreadyTranslatedExpressions;
    public static Func<Dictionary<string, Dictionary<string, FormatUnit>>>? GetFormattedExpressions;

    public static List<CodeFile> GetCodeFiles()
    {
        List<DynamicTypeEntity> types = GetTypes();
        var alreadyTranslatedExpressions = GetAlreadyTranslatedExpressions?.Invoke();
        var formattedExpressions = GetFormattedExpressions?.Invoke();

        var result = new List<CodeFile>();

        var entities = types.Select(dt =>
        {
            var def = dt.GetDefinition();

            var dcg = new DynamicTypeCodeGenerator(DynamicLogic.CodeGenNamespace, dt.TypeName, dt.BaseType, def, EvalLogic.Namespaces);

            var content = dcg.GetFileCode();
            return new CodeFile(dt.TypeName + ".cs", content);
        }).ToList();
        result.AddRange(entities);

        var logics = types.Select(dt =>
        {
            var def = dt.GetDefinition();

            var dlg = new DynamicTypeLogicGenerator(DynamicLogic.CodeGenNamespace, dt.TypeName, dt.BaseType, def, EvalLogic.Namespaces)
            {
                AlreadyTranslated = alreadyTranslatedExpressions?.TryGetC(dt.TypeName + "Entity"),
                Formatted = formattedExpressions?.TryGetC(dt.TypeName + "Entity"),
            };

            var content = dlg.GetFileCode();
            return new CodeFile(dt.TypeName + "Logic.cs", content);
        }).ToList();
        result.AddRange(logics);

        var bs = new DynamicBeforeSchemaGenerator(DynamicLogic.CodeGenNamespace, types.Select(a => a.GetDefinition().CustomBeforeSchema).NotNull().ToList(), EvalLogic.Namespaces);
        result.Add(new CodeFile("CodeGenBeforeSchema.cs", bs.GetFileCode()));

        return result;
    }
}

public class DynamicTypeCodeGenerator
{
    public HashSet<string> Usings { get; private set; }
    public string Namespace { get; private set; }
    public string TypeName { get; private set; }
    public DynamicBaseType BaseType { get; private set; }
    public DynamicTypeDefinition Def { get; private set; }
    public bool IsTreeEntity { get; private set; }

    public DynamicTypeCodeGenerator(string @namespace, string typeName, DynamicBaseType baseType, DynamicTypeDefinition def, HashSet<string> usings)
    {
        Usings = usings;
        Namespace = @namespace;
        TypeName = typeName;
        BaseType = baseType;
        Def = def;
        IsTreeEntity = def != null && def.CustomInheritance != null && def.CustomInheritance.Code.Contains("TreeEntity");
    }

    public string GetFileCode()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in Usings)
            sb.AppendLine("using {0};".FormatWith(item));

        sb.AppendLine();
        sb.AppendLine("namespace " + Namespace);
        sb.AppendLine("{");
        sb.Append(GetEntityCode().Indent(4));

        if (BaseType == DynamicBaseType.Entity)
        {
            var ops = GetEntityOperation();
            if (ops != null)
            {
                sb.AppendLine();
                sb.Append(ops.Indent(4));
            }
        }

        if (Def!.CustomTypes != null)
            sb.AppendLine(Def.CustomTypes.Code);

        sb.AppendLine("}");

        return sb.ToString();
    }

    public string GetEntityCode()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var gr in GetEntityAttributes().Chunk(a => a.Length, 100))
        {
            sb.AppendLine("[" + gr.ToString(", ") + "]");
        }
        sb.AppendLine($"public class {GetTypeNameWithSuffix()} : {GetEntityBaseClass(BaseType)}");
        sb.AppendLine("{");

        if (BaseType == DynamicBaseType.MixinEntity)
            sb.AppendLine($"{GetTypeNameWithSuffix()}(ModifiableEntity mainEntity, MixinEntity next): base(mainEntity, next) {{ }}".Indent(4));

        foreach (var prop in Def.Properties)
        {
            string field = WriteProperty(prop);

            if (field != null)
            {
                sb.Append(field.Indent(4));
                sb.AppendLine();
            }
        }

        if (Def.CustomEntityMembers != null)
            sb.AppendLine(Def.CustomEntityMembers.Code.Indent(4));

        string? toString = GetToString();
        if (toString != null)
        {
            sb.Append(toString.Indent(4));
            sb.AppendLine();
        }

        sb.AppendLine("}");
        sb.AppendLine();

        return sb.ToString();
    }

    public string? GetEntityOperation()
    {
        if (IsTreeEntity)
            return null;

        if (Def.OperationCreate == null &&
             Def.OperationSave == null &&
             Def.OperationDelete == null &&
             Def.OperationClone == null)
            return null;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[AutoInit]"); //Only for ReflectionServer
        sb.AppendLine($"public static class {TypeName}Operation");
        sb.AppendLine("{");

        if (Def.OperationCreate != null)
            sb.AppendLine($"    public static readonly ConstructSymbol<{TypeName}Entity>.Simple Create = OperationSymbol.Construct<{TypeName}Entity>.Simple(typeof({TypeName}Operation), \"Create\");");

        var requiresSaveOperation = Def.EntityKind != null && EntityKindAttribute.CalculateRequiresSaveOperation(Def.EntityKind.Value);
        if (Def.OperationSave != null && !requiresSaveOperation)
            throw new InvalidOperationException($"DynamicType '{TypeName}' defines Save but has EntityKind = '{Def.EntityKind}'");
        else if (Def.OperationSave == null && requiresSaveOperation)
            throw new InvalidOperationException($"DynamicType '{TypeName}' does not define Save but has EntityKind = '{Def.EntityKind}'");

        if (Def.OperationSave != null)
            sb.AppendLine($"    public static readonly ExecuteSymbol<{TypeName}Entity> Save = OperationSymbol.Execute<{TypeName}Entity>(typeof({TypeName}Operation), \"Save\");");

        if (Def.OperationDelete != null)
            sb.AppendLine($"    public static readonly DeleteSymbol<{TypeName}Entity> Delete = OperationSymbol.Delete<{TypeName}Entity>(typeof({TypeName}Operation), \"Delete\");");

        if (Def.OperationClone != null)
            sb.AppendLine($"    public static readonly ConstructSymbol<{TypeName}Entity>.From<{TypeName}Entity> Clone = OperationSymbol.Construct<{TypeName}Entity>.From<{TypeName}Entity>(typeof({TypeName}Operation), \"Clone\");");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GetEntityBaseClass(DynamicBaseType baseType)
    {
        if (Def.CustomInheritance != null)
            return Def.CustomInheritance.Code;

        return baseType.ToString();
    }

    protected virtual string? GetToString()
    {
        if (Def.ToStringExpression == null)
            return null;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"static Expression<Func<{GetTypeNameWithSuffix()}, string>> ToStringExpression = e => {Def.ToStringExpression};");
        sb.AppendLine("[ExpressionField(\"ToStringExpression\")]");
        sb.AppendLine("public override string ToString()");
        sb.AppendLine("{");
        sb.AppendLine("    return ToStringExpression.Evaluate(this);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public virtual string GetTypeNameWithSuffix()
    {
        return TypeName + (BaseType == DynamicBaseType.MixinEntity ? "Mixin" :
            BaseType == DynamicBaseType.EmbeddedEntity ? "Embedded" :
            BaseType == DynamicBaseType.ModelEntity ? "Model" : "Entity");
    }

    private List<string> GetEntityAttributes()
    {
        List<string> atts = new List<string> { };

        if (BaseType == DynamicBaseType.Entity)
        {
            atts.Add("EntityKind(EntityKind." + Def.EntityKind!.Value + ", EntityData." + Def.EntityData!.Value + ")");

            if (Def.TableName.HasText())
            {
                var parts = ParseTableName(Def.TableName);
                atts.Add("TableName(" + parts + ")");
            }

            if (Def.PrimaryKey != null)
            {
                var name = Def.PrimaryKey.Name ?? "ID";
                var type = Def.PrimaryKey.Type ?? "int";
                var identity = Def.PrimaryKey.Identity;

                atts.Add($"PrimaryKey(typeof({type}), {Literal(name)}, Identity = {identity.ToString().ToLower()})");
            }

            if (Def.Ticks != null)
            {
                var hasTicks = Def.Ticks.HasTicks;
                var name = Def.Ticks.Name ?? "Ticks";
                var type = Def.Ticks.Type ?? "int";

                if (!hasTicks)
                    atts.Add("TicksColumn(false)");
                else
                    atts.Add($"TicksColumn(true, Name = {Literal(name)}, Type = typeof({type}))");
            }
        }

        return atts;
    }

    string Literal(object obj)
    {
        return CSharpRenderer.Value(obj);
    }

    protected virtual string WriteProperty(DynamicProperty property)
    {
        string type = GetPropertyType(property);

        StringBuilder sb = new StringBuilder();

        string? inititalizer = property.IsMList != null ? $" = new {type}()" : null;
        string fieldName = GetFieldName(property);

        WriteAttributeTag(sb, GetFieldAttributes(property));
        sb.AppendLine($"{type} {fieldName}{inititalizer};");
        WriteAttributeTag(sb, GetPropertyAttributes(property));
        sb.AppendLine($"public {type} {property.Name}");
        sb.AppendLine("{");
        sb.AppendLine($"    get {{ return this.Get({fieldName}); }}");
        sb.AppendLine($"    set {{ this.Set(ref {fieldName}, value); }}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetFieldName(DynamicProperty property)
    {
        var fn = property.Name.FirstLower();

        return CSharpRenderer.EscapeIdentifier(fn);
    }

    private IEnumerable<string> GetPropertyAttributes(DynamicProperty property)
    {
        var atts = property.Validators.EmptyIfNull().Select(v => GetValidatorAttribute(v)).ToList();

        if (property.IsNullable == IsNullable.OnlyInMemory && !property.Validators.EmptyIfNull().Any(v => v.Type == "NotNull"))
            atts.Add(GetValidatorAttribute(new DynamicValidator.NotNull { Type = "NotNull" }));

        if (property.Unit != null)
            atts.Add($"Unit(\"{property.Unit}\")");

        if (property.Format != null)
            atts.Add($"Format(\"{property.Format}\")");

        if (property.NotifyChanges == true)
        {
            if (property.IsMList != null ||
                property.Type.EndsWith("Embedded") ||
                property.Type.EndsWith("Entity") && property.IsLite != true)
                atts.Add("BindParent");
        }

        if (property.CustomPropertyAttributes.HasText())
            atts.Add(property.CustomPropertyAttributes);

        return atts;
    }

    private string GetValidatorAttribute(DynamicValidator v)
    {
        var name = v.Type + "Validator";

        var extra = v.ExtraArguments();

        if (extra == null)
            return name;

        return $"{name}({extra})";
    }

    protected virtual void WriteAttributeTag(StringBuilder sb, IEnumerable<string> attributes)
    {
        foreach (var gr in attributes.Chunk(a => a.Length, 100))
        {
            sb.AppendLine("[" + gr.ToString(", ") + "]");
        }
    }

    private List<string> GetFieldAttributes(DynamicProperty property)
    {
        List<string> atts = new List<string>();
        if (property.IsNullable == IsNullable.OnlyInMemory)
            atts.Add("ForceNotNullable");

        if (property.Size != null || property.Scale != null || property.ColumnType.HasText())
        {
            var props = new[]
            {
                property.Size != null ? "Size = " + Literal(property.Size) : null,
                property.Scale != null ? "Scale = " + Literal(property.Scale) : null,
                property.ColumnType.HasText() ?  "SqlDbType = " + Literal(Enum.TryParse(property.ColumnType, out SqlDbType dbType) ? dbType : SqlDbType.Udt) : null,
                property.ColumnType.HasText() && !Enum.TryParse<SqlDbType>(property.ColumnType, out var _) ?  "UserDefinedTypeName = " + Literal(property.ColumnType) : null,

            }.NotNull().ToString(", ");

            atts.Add($"DbType({props})");
        }

        if (property.ColumnName.HasText())
            atts.Add("ColumnName(" + Literal(property.ColumnName) + ")");

        switch (property.UniqueIndex)
        {
            case UniqueIndex.No: break;
            case UniqueIndex.Yes: atts.Add("UniqueIndex"); break;
            case UniqueIndex.YesAllowNull: atts.Add("UniqueIndex"); break;
        }

        if (property.IsMList != null)
        {

            var mlist = property.IsMList;
            if (mlist.PreserveOrder)
                atts.Add("PreserveOrder" + (mlist.OrderName.HasText() ? "(" + Literal(mlist.OrderName) + ")" : ""));

            if (mlist.TableName.HasText())
            {
                var parts = ParseTableName(mlist.TableName);
                atts.Add("TableName(" + parts + ")");
            }

            if (mlist.BackReferenceName.HasText())
                atts.Add($"BackReferenceColumnName({Literal(mlist.BackReferenceName)})");
        }

        if (property.CustomFieldAttributes.HasText())
            atts.Add(property.CustomFieldAttributes);


        return atts;
    }

    private string ParseTableName(string value)
    {
        var isPostgres = Schema.Current.Settings.IsPostgres;
        var objName = ObjectName.Parse(value, isPostgres);

        return new List<string?>
        {
            Literal(objName.Name),
            !objName.Schema.IsDefault() ? "SchemaName =" + Literal(objName.Schema.Name) : null,
            objName.Schema.Database != null ? "DatabaseName =" + Literal(objName.Schema.Database.Name) : null,
            objName.Schema.Database?.Server != null ? "ServerName =" + Literal(objName.Schema.Database.Server.Name) : null,
        }.NotNull().ToString(", ");
    }

    public virtual string GetPropertyType(DynamicProperty property)
    {
        if (string.IsNullOrEmpty(property.Type))
            return "";

        string result = SimplifyType(property.Type);

        if (property.IsLite == true)
            result = "Lite<" + result + ">";

        if (property.IsMList != null)
            return "MList<" + result + ">";

        var isNullable = property.IsNullable == IsNullable.Yes ||
            property.IsNullable == IsNullable.OnlyInMemory;

        return result + (isNullable ? "?" : "");
    }

    private bool IsValueType(DynamicProperty property)
    {
        var t = TryResolveType(property.Type);
        if (t != null)
            return t.IsValueType;

        var tn = property.Type;
        if (tn.EndsWith("Embedded") || tn.EndsWith("Entity") || tn.EndsWith("Mixin") || tn.EndsWith("Symbol"))
        {
            return false;
        }
        else
        {
            return true; // Assume Enum
        }
    }

    private string SimplifyType(string type)
    {
        var ns = type.TryBeforeLast(".");

        if (ns == null)
            return type;

        if (Namespace == ns || Usings.Contains(ns))
            return type.AfterLast(".");

        return type;
    }

    public Type? TryResolveType(string typeName)
    {
        switch (typeName)
        {
            case "bool": return typeof(bool);
            case "byte": return typeof(byte);
            case "char": return typeof(char);
            case "decimal": return typeof(decimal);
            case "double": return typeof(double);
            case "short": return typeof(short);
            case "int": return typeof(int);
            case "long": return typeof(long);
            case "sbyte": return typeof(sbyte);
            case "float": return typeof(float);
            case "string": return typeof(string);
            case "ushort": return typeof(ushort);
            case "uint": return typeof(uint);
            case "ulong": return typeof(ulong);
        }


        var result = Type.GetType("System." + typeName);

        if (result != null)
            return result;

        var type = TypeLogic.TryGetType(typeName);
        if (type != null)
            return EnumEntity.Extract(type) ?? type;

        return null;
    }
}

public class DynamicTypeLogicGenerator
{
    public HashSet<string> Usings { get; private set; }
    public string Namespace { get; private set; }
    public string TypeName { get; private set; }
    public DynamicBaseType BaseType { get; private set; }
    public DynamicTypeDefinition Def { get; private set; }
    public bool IsTreeEntity { get; private set; }

    public Dictionary<string, string>? AlreadyTranslated { get; set; }
    public Dictionary<string, FormatUnit>? Formatted { get; set; }

    public DynamicTypeLogicGenerator(string @namespace, string typeName, DynamicBaseType baseType, DynamicTypeDefinition def, HashSet<string> usings)
    {
        Usings = usings;
        Namespace = @namespace;
        TypeName = typeName;
        BaseType = baseType;
        Def = def;
        IsTreeEntity = def != null && def.CustomInheritance != null && def.CustomInheritance.Code.Contains("TreeEntity");
    }

    public string GetFileCode()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in Usings)
            sb.AppendLine("using {0};".FormatWith(item));

        sb.AppendLine();
        sb.AppendLine($"namespace {Namespace};");

        var complexFields = Def.QueryFields.EmptyIfNull().Select(a => GetComplexQueryField(a)).NotNull().ToList();
        var complexNotTranslated = complexFields.Where(a => AlreadyTranslated?.TryGetC(a) == null).ToList();
        if (complexNotTranslated.Any())
        {
            sb.AppendLine($"public enum CodeGenQuery{TypeName}Message");
            sb.AppendLine($"{{");
            foreach (var item in complexNotTranslated)
                sb.AppendLine($"    " + item + ",");
            sb.AppendLine($"}}");
        }

        sb.AppendLine($"public static class {TypeName}Logic");
        sb.AppendLine($"{{");
        sb.AppendLine($"    public static void Start(SchemaBuilder sb)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))");
        sb.AppendLine($"            return;");

        if (BaseType == DynamicBaseType.Entity)
        {
            sb.AppendLine(GetInclude().Indent(8));
        }

        if (Def.CustomStartCode != null)
            sb.AppendLine(Def.CustomStartCode.Code.Indent(8));

        if (BaseType == DynamicBaseType.Entity)
        {
            if (complexFields.HasItems())
                sb.AppendLine(RegisterComplexQuery(complexFields).Indent(8));

            var complexOperations = RegisterComplexOperations();
            if (complexOperations != null)
                sb.AppendLine(complexOperations.Indent(8));
        }

        sb.AppendLine($"    }}");

        if (Def.CustomLogicMembers != null)
            sb.AppendLine(Def.CustomLogicMembers.Code.Indent(4));

        sb.AppendLine($"}}");

        return sb.ToString();
    }

    private string GetInclude()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"var fi = sb.Include<{TypeName}Entity>()");

        if (!IsTreeEntity)
        {
            if (Def.OperationSave != null && string.IsNullOrWhiteSpace(Def.OperationSave.Execute.Trim()))
                sb.AppendLine($"    .WithSave({TypeName}Operation.Save)");

            if (Def.OperationDelete != null && string.IsNullOrWhiteSpace(Def.OperationDelete.Delete.Trim()))
                sb.AppendLine($"    .WithDelete({TypeName}Operation.Delete)");
        }

        var mcui = Def.MultiColumnUniqueIndex;
        if (mcui != null)
            sb.AppendLine($"    .WithUniqueIndex(e => new {{{mcui.Fields.ToString(", ")}}}{(mcui.Where.HasText() ? ", e => " + mcui.Where : "")})");

        var queryFields = Def.QueryFields.EmptyIfNull();

        if (queryFields.EmptyIfNull().Any() && queryFields.All(a => GetComplexQueryField(a) == null))
        {
            var lines = new[] { "Entity = e" }.Concat(queryFields);

            sb.AppendLine($@"    .WithQuery(() => e => new 
{{ 
{lines.ToString(",\n").Indent(8)}
}})");
        }

        sb.Insert(sb.Length - 2, ';');
        return sb.ToString();
    }

    public static string? GetComplexQueryField(string field)
    {
        var fieldName = field.TryBefore("=")?.Trim();

        if (fieldName == null)
            return null;

        if (!IdentifierValidatorAttribute.International.IsMatch(fieldName))
            return null;

        var lastProperty = field.After("=").TryAfterLast(".")?.Trim();

        if (lastProperty == null || fieldName != lastProperty)
            return fieldName;

        return null;
    }

    public string RegisterComplexQuery(List<string> complexQueryFields)
    {
        StringBuilder sb = new StringBuilder();

        var lines = new[] { "Entity = e" }.Concat(Def.QueryFields);

        sb.AppendLine($@"QueryLogic.Queries.Register(typeof({TypeName}Entity), () => DynamicQueryCore.Auto(
from e in Database.Query<{TypeName}Entity>()
select new
{{
{lines.ToString(",\n").Indent(8)}
}})
{complexQueryFields.Select(f => $".ColumnDisplayName(a => a.{f}, {AlreadyTranslated?.TryGetC(f) ?? $"CodeGenQuery{TypeName}Message.{f}"})").ToString("\n").Indent(4)}
{complexQueryFields.Where(f => Formatted?.TryGetS(f) != null).Select(f =>
        {
            var fu = Formatted?.TryGetS(f);

            var formatText = fu != null && fu.Value.Format.HasText() ? $"c.Format = \"{fu.Value.Format}\";" : "";
            var unitText = fu != null && fu.Value.Unit.HasText() ? $"c.Unit = \"{fu.Value.Unit}\";" : "";

            return $".Column(a => a.{f}, c => {{ {formatText} {unitText} }})";
        }).ToString("\n").Indent(4)}
);");

        sb.AppendLine();
        return sb.ToString();
    }

    private string RegisterComplexOperations()
    {
        StringBuilder sb = new StringBuilder();
        var operationConstruct = Def.OperationCreate?.Construct.Trim();
        if (operationConstruct.HasText())
        {
            sb.AppendLine();

            if (IsTreeEntity)
            {
                sb.AppendLine("Graph<{0}Entity>.Construct.Untyped(TreeOperation.CreateRoot).Do(g => ".FormatWith(TypeName));
                sb.AppendLine("    g.Construct = (args) => {\n" + operationConstruct.Indent(8) + "\n}");
                sb.AppendLine(").Register(replace: true);");
            }
            else
            {
                sb.AppendLine("new Graph<{0}Entity>.Construct({0}Operation.Create)".FormatWith(TypeName));
                sb.AppendLine("{");
                sb.AppendLine("    Construct = (args) => {\n" + operationConstruct.Indent(8) + "\n}");
                sb.AppendLine("}.Register();");
            }
        }

        var operationExecute = Def.OperationSave?.Execute.Trim();
        var operationCanExecute = Def.OperationSave?.CanExecute?.Trim();
        if (!string.IsNullOrWhiteSpace(operationExecute) || !string.IsNullOrWhiteSpace(operationCanExecute))
        {
            sb.AppendLine();
            sb.AppendLine("new Graph<{0}Entity>.Execute({1}Operation.Save)".FormatWith(TypeName, IsTreeEntity ? "Tree" : TypeName));
            sb.AppendLine("{");

            if (!string.IsNullOrWhiteSpace(operationCanExecute))
                sb.AppendLine($"    CanExecute = e => {operationCanExecute},");

            sb.AppendLine("    CanBeNew = true,");
            sb.AppendLine("    CanBeModified = true,");
            sb.AppendLine("    Execute = (e, args) => {\n" + operationExecute?.Indent(8) + "\n}");
            sb.AppendLine("}." + (IsTreeEntity ? "Register(replace: true)" : "Register()") + ";");
        }

        var operationDelete = Def.OperationDelete?.Delete.Trim();
        var operationCanDelete = Def.OperationDelete?.CanDelete?.Trim();
        if (!string.IsNullOrWhiteSpace(operationDelete) || !string.IsNullOrEmpty(operationCanDelete))
        {
            sb.AppendLine();
            sb.AppendLine("new Graph<{0}Entity>.Delete({1}Operation.Delete)".FormatWith(TypeName, IsTreeEntity ? "Tree" : TypeName));
            sb.AppendLine("{");

            if (!string.IsNullOrWhiteSpace(operationCanDelete))
                sb.AppendLine($"    CanDelete = e => {operationCanDelete},");

            sb.AppendLine("    Delete = (e, args) => {\n" + operationDelete.DefaultText("e.Delete();").Indent(8) + "\n}");
            sb.AppendLine("}." + (IsTreeEntity ? "Register(replace: true)" : "Register()") + ";");
        }

        var operationClone = Def.OperationClone?.Construct.Trim();
        var operationCanClone = Def.OperationClone?.CanConstruct?.Trim();
        if (!string.IsNullOrWhiteSpace(operationClone) || !string.IsNullOrEmpty(operationCanClone))
        {
            sb.AppendLine();
            sb.AppendLine("new Graph<{0}Entity>.ConstructFrom<{0}Entity>({1}Operation.Clone)".FormatWith(TypeName, IsTreeEntity ? "Tree" : TypeName));
            sb.AppendLine("{");

            if (!string.IsNullOrWhiteSpace(operationCanClone))
                sb.AppendLine($"    CanConstruct = e => {operationCanClone},");

            sb.AppendLine("    Construct = (e, args) => {\n" + operationClone.DefaultText($"return new {TypeName}Entity();").Indent(8) + "\n}");
            sb.AppendLine("}." + (IsTreeEntity ? "Register(replace: true)" : "Register()") + ";");
        }

        return sb.ToString();
    }
}

public struct FormatUnit
{
    public string? Format { get; set; }
    public string? Unit { get; set; }

    public FormatUnit(string? format, string? unit)
    {
        Format = format;
        Unit = unit;
    }
}

public class DynamicBeforeSchemaGenerator
{
    public HashSet<string> Usings { get; private set; }
    public string Namespace { get; private set; }
    public List<DynamicTypeCustomCode> BeforeSchema { get; private set; }

    public DynamicBeforeSchemaGenerator(string @namespace, List<DynamicTypeCustomCode> beforeSchema, HashSet<string> usings)
    {
        Usings = usings;
        Namespace = @namespace;
        BeforeSchema = beforeSchema;
    }

    public string GetFileCode()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in Usings)
            sb.AppendLine("using {0};".FormatWith(item));

        sb.AppendLine("[assembly: AssemblySchemaName(\"codegen\")]");
        sb.AppendLine();
        sb.AppendLine($"namespace {Namespace}");
        sb.AppendLine($"{{");
        sb.AppendLine($"    public static class CodeGenBeforeSchemaLogic");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public static void Start(SchemaBuilder sb)");
        sb.AppendLine($"        {{");

        if (BeforeSchema != null && BeforeSchema.Count > 0)
            BeforeSchema.ForEach(bs => sb.AppendLine(bs.Code.Indent(12)));

        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");

        return sb.ToString();
    }
}
