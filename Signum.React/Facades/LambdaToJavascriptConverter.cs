using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;

namespace Signum.React.Facades
{
    internal class LambdaToJavascriptConverter
    {
        public static string ToJavascript(LambdaExpression lambdaExpression)
        {
            if (lambdaExpression == null)
                return null;

            var body = ToJavascript(lambdaExpression.Parameters.Single(), lambdaExpression.Body);

            if (body == null)
                return null;

            return "function(e){ return " + body + "; }";
        }

        static Dictionary<string, string> replacements = new Dictionary<string, string>
        {
            { "\t", "\\t"},
            { "\n", "\\n"},
            { "\r", ""},
        };

        private static string ToJavascript(ParameterExpression param, Expression expr)
        {
            if (param == expr)
                return "e";

            if (expr is ConstantExpression ce && expr.Type == typeof(string))
            {
                var str = (string)ce.Value;

                if (!str.HasText())
                    return "\"\"";

                return "\"" + str.Replace(replacements) + "\"";
            }

            if (expr is MemberExpression me)
            {
                var a = ToJavascript(param, me.Expression);

                if (a == null)
                    return null;

                if (me.Expression.Type.IsNullable())
                {
                    if (me.Member.Name == "HasValue")
                        return a + " != null";
                    else if (me.Member.Name == "Value")
                        return a;
                }

                return a + "." + me.Member.Name.FirstLower();
            }

            if (expr is BinaryExpression be && be.NodeType == ExpressionType.Add)
            {
                var a = ToJavascriptToString(param, be.Left);
                var b = ToJavascriptToString(param, be.Right);

                if (a != null && b != null)
                    return "(" + a + " + " + b + ")";

                return null;
            }

            if (expr is MethodCallExpression mc)
            {
                if (mc.Method.Name == "ToString")
                    return ToJavascriptToString(param, mc.Object, mc.TryGetArgument("format") is ConstantExpression format ? (string)format.Value : null);

                if (mc.Method.DeclaringType == typeof(DateTime))
                {
                    switch (mc.Method.Name)
                    {
                        case "ToShortDateString": return ToJavascriptToString(param, mc.Object, "d");
                        case "ToShortTimeString": return ToJavascriptToString(param, mc.Object, "t");
                        case "ToLongDateString": return ToJavascriptToString(param, mc.Object, "D");
                        case "ToLongTimeString": return ToJavascriptToString(param, mc.Object, "T");
                    }
                }

                if (mc.Method.DeclaringType == typeof(StringExtensions) && mc.Method.Name == nameof(StringExtensions.Etc))
                {
                    var str = ToJavascriptToString(param, mc.GetArgument("str"));
                    var max = ((ConstantExpression)mc.GetArgument("max")).Value.ToString();

                    var etcString = mc.TryGetArgument("etcString");

                    if (etcString == null)
                        return $"{str}.etc({max})";
                    else
                        return $"{str}.etc({max},{ToJavascriptToString(param, etcString)})";
                }
            }

            if (expr is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
            {
                return ToJavascriptToString(param, ue.Operand);
            }

            if (expr is ConditionalExpression iff)
            {
                var t = ToJavascript(param, iff.Test);
                var a = ToJavascriptToString(param, iff.IfTrue);
                var b = ToJavascriptToString(param, iff.IfFalse);

                if (t != null && a != null && b != null)
                    return "(" + t + " ? " + a + " : " + b + ")";

                return null;
            }

            return null;
        }

        private static string ToJavascriptToString(ParameterExpression param, Expression expr, string format = null)
        {
            var r = ToJavascript(param, expr);

            if (r == null)
                return null;

            if (expr.NodeType != ExpressionType.MemberAccess)
                return r;

            if (expr.Type.IsModifiableEntity() || expr.Type.IsLite() || expr.Type.IsIEntity())
                return "getToString(" + r + ")";

            string formatFull = format == null ? null : (", '" + format + "'");

            if (expr.Type.UnNullify() == typeof(DateTime))
                return "dateToString(" + r + formatFull + ")";

            if (expr.Type.UnNullify() == typeof(TimeSpan))
                return "durationToString(" + r + formatFull + ")";

            if (ReflectionTools.IsNumber(expr.Type.UnNullify()))
                return "numberToString(" + r + formatFull + ")";

            return "valToString(" + r + ")";
        }
    }
}