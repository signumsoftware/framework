using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
    public static class ObjectDumper
    {
        public static string Dump(this object o)
        {
            var res = Dump(o, 0, new StringBuilder(), null);
            string r = res.TryToString();
            return "{0};".Formato(r.HasText() ? r.Left(r.Length - 1) : "null");
        }

        static string Indent(this string t, int level)
        {
            return t.PadLeft(t.Length + (level * 3));
        }

        static StringBuilder Dump(object o, int level, StringBuilder res, string propertyName)
        {
            if (o == null)
                return null;

            Type t = o.GetType();

            int? Id = null;

            string initializerHeader = "{0} new ".Formato(propertyName.HasText() ? propertyName + " = " : null).Indent(level);

            initializerHeader += CSharpRenderer.TypeName(t);

            if (o is IdentifiableEntity)
            {
                var ie = o as IdentifiableEntity;
                Id = ie.IdOrNull;
                initializerHeader += " " + (Id.HasValue ? " ({0})".Formato(Id.Value) : "");
                string comment = " //" + ie.ToString();
                if (o is Lite && ((Lite)o).UntypedEntityOrNull == null)
                {
                    initializerHeader += "{}," + comment;
                    res.AppendLine(initializerHeader);
                    return res;
                }
                else
                    initializerHeader += comment;
            }

            res.AppendLine(initializerHeader);

            res.AppendLine("{".Indent(level));
            level += 1;

            if (o is IEnumerable)
            {
                foreach (var item in (o as IEnumerable))
                {
                    res.Append("{");
                    if (CSharpRenderer.IsBasicType(item.GetType()))
                        res.AppendLine("{0},".Formato(writeValue(item)).Indent(level));
                    else
                        Dump(item, level, res, null);
                    res.Append("}");
                }
            }
            else
                if (o is Lite && ((Lite)o).UntypedEntityOrNull != null)
                {
                    Dump((o as Lite).UntypedEntityOrNull, level, res, "UntypedEntityOrNull");
                }
                else
                    foreach (var field in t.GetFields())
                    {
                        if (CSharpRenderer.IsBasicType(field.FieldType))
                            res.AppendLine("{0} = {1},".Formato(field.Name, writeValue(field.GetValue(o))).Indent(level));
                        else
                            Dump(field.GetValue(o), level, res, field.Name);
                    }

            res.AppendLine("},".Indent(level - 1));
            return res;
        }

        static string writeValue(object item)
        {
            string value = item.TryToString() ?? "null";
            string startDelimiter = null;
            string endDelimiter = null;

            if (item != null)
            {
                if (item is string)
                    startDelimiter = endDelimiter = "\"";

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
            }

            return "{0}{1}{2}".Formato(startDelimiter, value, endDelimiter);
        }
    }
}
