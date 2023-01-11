using System.Collections;
using Signum.Entities.Reflection;
using Signum.Entities.Basics;
using Signum.Utilities.DataStructures;

namespace Signum.Entities;

public enum ShowIgnoredFields
{
    Yes,
    //VirtualMList
    OnlyQueryables,
    No,
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class AvoidDumpAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class AvoidDumpEntityAttribute : Attribute
{

}

public static class ObjectDumper
{
    public static HashSet<Type> IgnoreTypes = new HashSet<Type> { typeof(ExceptionEntity) };

    public static string Dump(object o, ShowIgnoredFields showIgnoredFields = ShowIgnoredFields.OnlyQueryables, bool showByteArrays = false)
    {
        using (HeavyProfiler.LogNoStackTrace("Dump"))
        {
            var od = new DumpVisitor(showIgnoredFields, showByteArrays);
            od.DumpObject(o);
            return od.Sb.ToString();
        }
    }

    static string Indent(this string t, int level)
    {
        return t.PadLeft(t.Length + (level * 3));
    }

    class DumpVisitor
    {
        HashSet<object> objects = new HashSet<object>(ReferenceEqualityComparer<object>.Default);
        public StringBuilder Sb = new StringBuilder();
        int level = 0;
        readonly ShowIgnoredFields showIgnoredFields;
        readonly bool showByteArrays;

        public DumpVisitor(ShowIgnoredFields showIgnoredFields, bool showByteArrays)
        {
            this.showIgnoredFields = showIgnoredFields;
            this.showByteArrays = showByteArrays;
        }

        public void DumpObject(object? o, bool avoidDumpEntity = false)
        {
            if (o == null)
            {
                Sb.Append("null");
                return;
            }

            if (o is Type type)
            {
                Sb.Append("typeof(");
                Sb.Append(CSharpRenderer.TypeName(type));
                Sb.Append(")");
                return;
            }

            Type t = o.GetType();

            if (IsDelegate(t))
            {
                Sb.Append("[DELEGATE]");
                return;
            }

            if (IsBasicType(t) || t.IsValueType)
            {
                Sb.Append(DumpValue(o!));
                return;
            }

            Sb.Append("new ");

            Sb.Append(CSharpRenderer.CleanIdentifiers(CSharpRenderer.TypeName(t)));

            if (IgnoreTypes.Contains(t))
            {
                Sb.Append("{ " + o!.ToString() + " }");
                return;
            }

            if (objects.Contains(o!))
            {
                if (o is Entity ent)
                {
                    Sb.Append("({0}{1})".FormatWith(
                        ent.IsNew ? "IsNew": ent.IdOrNull.ToString(),
                        ent == null ? null : ", ticks: " + ent.ticks
                        ));
                }
                if (o is Lite<Entity>)
                {
                    var id = ((Lite<Entity>)o).IdOrNull;
                    Sb.Append(id.HasValue ? "({0})".FormatWith(id.Value) : "");
                }
                Sb.Append(" /* [ALREADY] {0} */".FormatWith(SafeToString(o!)));
                return;
            }

            objects.Add(o!);

            if (o is Entity e)
            {
                Sb.Append("({0}{1})".FormatWith(
                    e.IsNew ? "IsNew" : e.IdOrNull.ToString(),
                    e.ticks == 0 ? null : ", ticks: " + e.ticks
                    ));

                string toString = SafeToString(o);

                Sb.Append(" /* {0} {1} */".FormatWith(toString, (avoidDumpEntity ? "[DUMP AS LITE]" : "")));

                if (avoidDumpEntity)
                    return;
            }

            if (o is Lite<Entity> l)
            {
                Sb.Append("({0}, \"{1}\")".FormatWith((l.IdOrNull.HasValue ? l.Id.ToString() : "null"), l.ToString()));
                if (((Lite<Entity>)o).EntityOrNull != null && !avoidDumpEntity)
                {
                    Sb.AppendLine().AppendLine("{".Indent(level));
                    level += 1;
                    var prop = o.GetType().GetProperty(nameof(Lite<Entity>.Entity))!;
                    DumpPropertyOrField(prop.PropertyType, prop.Name, prop.GetValue(o, null)!);
                    level -= 1;
                    Sb.Append("}".Indent(level));
                }
                return;
            }

            if (o is IEnumerable ie && !Any(ie))
            {
                Sb.Append("{}");
                return;
            }

            if (o is byte[] && !showByteArrays)
            {
                Sb.Append("{...}");
                return;
            }

            Sb.AppendLine().AppendLine("{".Indent(level));
            level += 1;

            if (t.Namespace.HasText() && t.Namespace.StartsWith("System.Reflection"))
                Sb.AppendLine("ToString = {0},".FormatWith(SafeToString(o!)).Indent(level));
            else if (o is Exception ex)
            {
                DumpPropertyOrField(typeof(string), "Message", ex.Message);
                DumpPropertyOrField(typeof(string), "StackTrace", ex.StackTrace);
                DumpPropertyOrField(typeof(Exception), "InnerException", ex.InnerException);
                DumpPropertyOrField(typeof(IDictionary), "Data", ex.Data);
            }
            else if (o is IEnumerable)
            {
                if (o is IDictionary dic)
                {
                    foreach (DictionaryEntry? item in dic)
                    {
                        Sb.Append("{".Indent(level));
                        DumpObject(item!.Value.Key);
                        Sb.Append(", ");
                        DumpObject(item!.Value.Value);
                        Sb.AppendLine("},");
                    }
                }
                else
                {
                    foreach (var item in (o as IEnumerable)!)
                    {
                        Sb.Append("".Indent(level));
                        DumpObject(item, avoidDumpEntity);
                        Sb.AppendLine(",");
                    }
                }
            }
            else if (!typeof(ModifiableEntity).IsAssignableFrom(t))
                foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    var hasAvoidDumpEntityAttr = prop.HasAttribute<AvoidDumpEntityAttribute>();
                    DumpPropertyOrField(prop.PropertyType, prop.Name, prop.GetValue(o, null), hasAvoidDumpEntityAttr);
                }
            else 
                foreach (var field in Reflector.InstanceFieldsInOrder(t).OrderBy(IsMixinField))
                {
                    if (IsIdOrTicks(field))
                        continue;

                    var hasAvoidDumpEntityAttr = field.HasAttribute<AvoidDumpEntityAttribute>() || Reflector.TryFindPropertyInfo(field)?.HasAttribute<AvoidDumpEntityAttribute>() == true;

                    if (IsMixinField(field))
                    {
                        var val = field.GetValue(o);

                        if (val == null)
                            continue;

                        DumpPropertyOrField(field.FieldType, GetFieldName(field), val, hasAvoidDumpEntityAttr);
                    }

                    var skip = this.showIgnoredFields == ShowIgnoredFields.Yes ? false :
                        this.showIgnoredFields == ShowIgnoredFields.OnlyQueryables ? IsIgnored(field) && Reflector.TryFindPropertyInfo(field)?.HasAttribute<QueryablePropertyAttribute>() != true :
                        this.showIgnoredFields == ShowIgnoredFields.No ? IsIgnored(field) :
                        throw new InvalidOperationException("Unexpected ShowIgnoredFields");

                    if (!skip)
                    {
                        DumpPropertyOrField(field.FieldType, GetFieldName(field), field.GetValue(o), hasAvoidDumpEntityAttr);
                    }
                }

            level -= 1;
            Sb.Append("}".Indent(level));
            return;
        }

        private static bool IsIgnored(FieldInfo field)
        {
            return (field.HasAttribute<IgnoreAttribute>() || Reflector.TryFindPropertyInfo(field)?.HasAttribute<IgnoreAttribute>() == true) ||
                (field.HasAttribute<AvoidDumpAttribute>() || Reflector.TryFindPropertyInfo(field)?.HasAttribute<AvoidDumpAttribute>() == true);
        }

        private static string GetFieldName(FieldInfo field)
        {
            if (field.Name.StartsWith("<"))
                return field.Name.Between('<', '>');

            return field.Name;
        }

        private static string SafeToString(object o)
        {
            string toString;
            try
            {
                toString = o.ToString()!;
            }
            catch (Exception e)
            {
                toString = "ToString thrown " + e.GetType().Name + ":" + e.Message.Etc(100);
            }
            return toString;
        }

        private bool IsMixinField(FieldInfo field)
        {
            return  field.Name == "mixin" && field.DeclaringType == typeof(ModifiableEntity) ||
                field.Name == "next" && field.DeclaringType == typeof(MixinEntity);
        }

        private bool IsIdOrTicks(FieldInfo field)
        {
            return field.Name == "id" && field.DeclaringType == typeof(Entity) ||
                field.Name == "ticks" && field.DeclaringType == typeof(Entity);
        }

        private bool Any(IEnumerable ie)
        {
            if (ie is IList l)
                return l.Count > 0;

            if (ie is Array a)
                return a.Length > 0;

            foreach (var item in ie!)
            {
                return true;
            }

            return false;
        }

        private void DumpPropertyOrField(Type type, string name, object? obj, bool avoidDumpEntity = false)
        {
            Sb.AppendFormat("{0} = ".Indent(level), name);
            DumpObject(obj, avoidDumpEntity);
            Sb.AppendLine(",");
        }

        private bool IsDelegate(Type type)
        {
            return typeof(Delegate).IsAssignableFrom(type);
        }

        bool IsBasicType(Type type)
        {
            var unType = type.UnNullify();
            return CSharpRenderer.IsBasicType(unType) || unType == typeof(DateTime);
        }

        string DumpValue(object item)
        {
            string value = item?.ToString() ?? "null";
            string? startDelimiter = null;
            string? endDelimiter = null;

            if (item != null)
            {
                if (item is string)
                {
                    startDelimiter = endDelimiter = "\"";
                    value = value.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
                }

                if (item is decimal || item is double || item is float)
                    value = value.Replace(',', '.');

                if (item is decimal)
                    endDelimiter = "M";

                if (item is float)
                    endDelimiter = "F";

                if (item is Enum)
                    startDelimiter = item.GetType().Name + ".";

                if (item is bool)
                    value = value.ToLower();

                if (item is DateTime dt)
                {
                    value = "DateTime.Parse(\"{0}\")".FormatWith(dt.ToString("O"));
                }
            }

            return "{0}{1}{2}".FormatWith(startDelimiter, value, endDelimiter);
        }
    };

}
