using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;
using Signum.Utilities.ExpressionTrees;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.Basics;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;

namespace Signum.Entities
{
    public enum ShowIgnoredFields
    {
        Yes,
        //VirtualMList
        OnlyQueryables,
        No,
    }

    public static class ObjectDumper
    {
        public static HashSet<Type> IgnoreTypes = new HashSet<Type> { typeof(ExceptionEntity) };

        public static string Dump(this object o, ShowIgnoredFields showIgnoredFields = ShowIgnoredFields.OnlyQueryables, bool showByteArrays = false)
        {
            using (HeavyProfiler.LogNoStackTrace("Dump"))
            {
                var od = new DumpVisitor(showIgnoredFields, showByteArrays);
                od.DumpObject(o);
                return od.Sb?.ToString();
            }
        }

        static string Indent(this string t, int level)
        {
            return t.PadLeft(t.Length + (level * 3));
        }

        class DumpVisitor
        {
            HashSet<object> objects = new HashSet<Object>(ReferenceEqualityComparer<object>.Default);
            public StringBuilder Sb = new StringBuilder();
            int level = 0;
            ShowIgnoredFields showIgnoredFields;
            bool showByteArrays;

            public DumpVisitor(ShowIgnoredFields showIgnoredFields, bool showByteArrays)
            {
                this.showIgnoredFields = showIgnoredFields;
                this.showByteArrays = showByteArrays;
            }

            public void DumpObject(object o)
            {
                if (o == null)
                {
                    Sb.Append("null");
                    return;
                }

                if (o is Type)
                {
                    Sb.Append("typeof(");
                    Sb.Append(CSharpRenderer.TypeName((Type)o));
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
                    Sb.Append(DumpValue(o));
                    return;
                }

                Sb.Append("new ");

                Sb.Append(CSharpRenderer.CleanIdentifiers(CSharpRenderer.TypeName(t)));

                if (IgnoreTypes.Contains(t))
                {
                    Sb.Append("{ " + o.ToString() + " }");
                    return;
                }

                if (objects.Contains(o))
                {
                    if (o is Entity)
                    {
                        var ident = o as Entity;
                        var ent = o as Entity;

                        Sb.Append("({0}{1})".FormatWith(
                            ident.IsNew ? "IsNew": ident.IdOrNull.ToString(),
                            ent == null ? null : ", ticks: " + ent.ticks
                            ));
                    }
                    if (o is Lite<Entity>)
                    {
                        var id =  ((Lite<Entity>)o).IdOrNull;
                        Sb.Append(id.HasValue ? "({0})".FormatWith(id.Value) : "");
                    }
                    Sb.Append(" /* [CICLE] {0} */".FormatWith(SafeToString(o)));
                    return;
                }

                objects.Add(o);

                if (o is Entity)
                {
                    var ent = (Entity)o;
                    Sb.Append("({0}{1})".FormatWith(
                        ent.IsNew ? "IsNew" : ent.IdOrNull.ToString(),
                        ent.ticks == 0 ? null : ", ticks: " + ent.ticks
                        ));

                    string toString = SafeToString(o);

                    Sb.Append(" /* {0} */".FormatWith(toString));
                }

                if (o is Lite<Entity>)
                {
                    var l = o as Lite<Entity>;
                    Sb.Append("({0}, \"{1}\")".FormatWith((l.IdOrNull.HasValue ? l.Id.ToString() : "null"), l.ToString()));
                    if (((Lite<Entity>)o).EntityOrNull == null)
                        return;
                }

                if (o is IEnumerable && !Any((o as IEnumerable)))
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
                    Sb.AppendLine("ToString = {0},".FormatWith(SafeToString(o)).Indent(level));
                else if (o is Exception)
                {
                    var ex = o as Exception;
                    DumpPropertyOrField(typeof(string), "Message", ex.Message);
                    DumpPropertyOrField(typeof(string), "StackTrace", ex.StackTrace);
                    DumpPropertyOrField(typeof(Exception), "InnerException", ex.InnerException);
                    DumpPropertyOrField(typeof(IDictionary), "Data", ex.Data);
                }
                else if (o is IEnumerable)
                {
                    if (o is IDictionary)
                    {
                        foreach (DictionaryEntry item in (o as IDictionary))
                        {
                            Sb.Append("{".Indent(level));
                            DumpObject(item.Key);
                            Sb.Append(", ");
                            DumpObject(item.Value);
                            Sb.AppendLine("},");
                        }
                    }
                    else
                    {
                        foreach (var item in (o as IEnumerable))
                        {
                            Sb.Append("".Indent(level));
                            DumpObject(item);
                            Sb.AppendLine(",");
                        }
                    }
                }
                else if (!typeof(ModifiableEntity).IsAssignableFrom(t))
                    foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        DumpPropertyOrField(prop.PropertyType, prop.Name, prop.GetValue(o, null));
                    }
                else
                    foreach (var field in Reflector.InstanceFieldsInOrder(t).OrderBy(IsMixinField))
                    {
                        if (IsIdOrTicks(field))
                            continue;

                        if (IsMixinField(field))
                        {
                            var val = field.GetValue(o);

                            if (val == null)
                                continue;

                            DumpPropertyOrField(field.FieldType, GetFieldName(field), val);
                        }

                        var skip = this.showIgnoredFields == ShowIgnoredFields.Yes ? false :
                            this.showIgnoredFields == ShowIgnoredFields.OnlyQueryables ? IsIgnored(field) && Reflector.TryFindPropertyInfo(field)?.HasAttribute<QueryablePropertyAttribute>() != true:
                            this.showIgnoredFields == ShowIgnoredFields.No ? IsIgnored(field) :
                            throw new InvalidOperationException("Unexpected ShowIgnoredFields");

                        if (!skip)
                        {
                            DumpPropertyOrField(field.FieldType, GetFieldName(field), field.GetValue(o));
                        }
                    }

                level -= 1;
                Sb.Append("}".Indent(level));
                return;
            }

            private static bool IsIgnored(FieldInfo field)
            {
                return (field.HasAttribute<IgnoreAttribute>() || Reflector.TryFindPropertyInfo(field)?.HasAttribute<IgnoreAttribute>() == true);
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
                    toString = o.ToString();
                }
                catch (Exception e)
                {
                    toString = "ToString thrown " + e.GetType().Name + ":" + e.Message.Etc(100);
                }
                return toString;
            }

            private bool IsMixinField(FieldInfo field)
            {
                return field.Name == "mixin" && field.DeclaringType == typeof(Entity) ||
                    field.Name == "next" && field.DeclaringType == typeof(MixinEntity);
            }

            private bool IsIdOrTicks(FieldInfo field)
            {
                return field.Name == "id" && field.DeclaringType == typeof(Entity) ||
                    field.Name == "ticks" && field.DeclaringType == typeof(Entity);
            }

            private bool Any(IEnumerable ie)
            {
                if (ie is IList)
                    return (ie as IList).Count > 0;

                if (ie is Array)
                    return (ie as Array).Length > 0;

                foreach (var item in ie)
                {
                    return true;
                }

                return false;
            }

            private void DumpPropertyOrField(Type type, string name, object obj)
            {
                Sb.AppendFormat("{0} = ".Indent(level), name);
                DumpObject(obj);
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
                string startDelimiter = null;
                string endDelimiter = null;

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

                    if (item is DateTime)
                    {
                        value = "DateTime.Parse(\"{0}\")".FormatWith(value);
                    }
                }

                return "{0}{1}{2}".FormatWith(startDelimiter, value, endDelimiter);
            }
        };

    }
}
