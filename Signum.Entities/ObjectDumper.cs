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

namespace Signum.Entities
{
    public static class ObjectDumper
    {
        public static string Dump(this object o)
        {
            var objects = new HashSet<Object>();
            var res = new StringBuilder();
            Dump(o, 0, res, objects);
            string r = res.TryToString();
            return "{0};"; //.Formato(r.HasText() ? r.Left(r.Length - 3) : "null");
        }

        static string Indent(this string t, int level)
        {
            return t.PadLeft(t.Length + (level * 3));
        }

        static void Dump(object o, int level, StringBuilder res, HashSet<object> objects)
        {
            if (o == null)
            {
                res.Append("null");
                return;
            }

            Type t = o.GetType();

            if (IsDelegate(t))
            {
                res.Append("DELEGATE");
                return;
            }

            if (IsBasicType(t))
            {
                WritePropertyOrField(t, null, o, level, res, objects);
                return;
            }

            res.Append(CSharpRenderer.TypeName(t));

            if (objects.Contains(o))
            {
                res.Append("{0} ".Formato(o.ToString() + "{...} // CICLO"));
                return;
            }

            objects.Add(o);

            if (o is IdentifiableEntity || o is Lite)
            {
                var id = o is IdentifiableEntity ? (o as IdentifiableEntity).IdOrNull : (o as Lite).IdOrNull;
                res.Append(id.HasValue ? "({0})".Formato(id.Value) : "");
                string comment = " // " + o.ToString();
                if (o is Lite && ((Lite)o).UntypedEntityOrNull == null)
                {
                    res.Append("{}" + comment);
                    return;
                }
                else
                    res.Append(comment);
            }

            if (o is IEnumerable && !Any((o as IEnumerable)))
            {
                res.Append("{}");
                return;
            }

            res.AppendLine("{".Indent(level));
            level += 1;

            if (o is IEnumerable)
            {
                foreach (var item in (o as IEnumerable))
                {
                    res.Append("{".Indent(level));
                    WritePropertyOrField(item.GetType(), null, item, level, res, objects);
                    res.Append("},".Indent(level)).AppendLine();
                }
            }
            else if (t.IsAnonymous())
                foreach (var prop in t.GetProperties(BindingFlags.Instance |
                                                     BindingFlags.Public))
                {
                    WritePropertyOrField(prop.PropertyType, prop.Name, prop.GetValue(o, null), level, res, objects);
                    res.Append(",").AppendLine();
                }
            else
                foreach (var field in Reflector.InstanceFieldsInOrder(t))
                {
                    WritePropertyOrField(field.FieldType, field.Name, field.GetValue(o), level, res, objects);
                    res.Append(",").AppendLine();
                }

            res.AppendLine("}".Indent(level - 1));
            return;
        }

        private static bool Any(IEnumerable ie)
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

        private static void WritePropertyOrField(Type type, string name, object obj, int level, StringBuilder res, HashSet<object> objects)
        {
            if (IsDelegate(type))
                return;
            if (IsBasicType(type))
                res.Append("{0} = {1},".Formato(name, writeValue(obj)).Indent(level));
            else
            {
                res.Append("{0}new = ".Formato(name.HasText() ? name + " = " : null).Indent(level));
                Dump(obj, level, res, objects);
            }
        }

        private static bool IsDelegate(Type type)
        {
            return typeof(Delegate).IsAssignableFrom(type);
        }

        static bool IsBasicType(Type type)
        {
            var unType = type.UnNullify();
            return CSharpRenderer.IsBasicType(unType) || unType == typeof(DateTime);
        }

        static string writeValue(object item)
        {
            string value = item.TryToString() ?? "null";
            string startDelimiter = null;
            string endDelimiter = null;

            if (item != null)
            {
                if (item is string)
                {
                    startDelimiter = endDelimiter = "\"";
                    if (value.Contains("\\"))
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
                    value = "DateTime.Parse(\"{0}\")".Formato(value);
                }
            }

            return "{0}{1}{2}".Formato(startDelimiter, value, endDelimiter);
        }
    }
}
