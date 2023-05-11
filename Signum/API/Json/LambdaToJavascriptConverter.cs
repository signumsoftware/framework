using Signum.Utilities.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Signum.API.Json;

public class LambdaToJavascriptConverter
{
    public static string? ToJavascript(LambdaExpression lambdaExpression, bool assert)
    {
        if (lambdaExpression == null)
            return null;

        var newLambda = (LambdaExpression)ExpressionCleaner.Clean(lambdaExpression)!;

        var body = ToJavascript(newLambda.Parameters.Single(), newLambda.Body);

        if (body == null)
        {
            if (assert)
                throw new InvalidOperationException("Unable to convert to Javascript:" + lambdaExpression.ToString());

            return null;
        }

        return "return " + body;
    }

    static Dictionary<string, string> replacements = new Dictionary<string, string>
    {
        { "\t", "\\t"},
        { "\n", "\\n"},
        { "\r", ""},
    };

    static Regex FormatRegex = new Regex(@"{(?<index>\d+):(?<format>[^}]+)}");

    private static string? ToJavascript(ParameterExpression param, Expression expr)
    {
        if (param == expr)
            return "e";

        if (expr is ConstantExpression ce)
        {
            if (ce.Value == null)
            {
                return "null";
            }

            if (expr.Type == typeof(string))
            {
                var str = (string)ce.Value!;

                if (!str.HasText())
                    return "\"\"";

                return "\"" + str.Replace(replacements) + "\"";
            }

            if (ReflectionTools.IsNumber(ce.Type) && ce.Value != null)
                return ((IFormattable)ce.Value).ToString(null, CultureInfo.InvariantCulture);
        }

        if (expr is MemberExpression me)
        {
            var a = ToJavascript(param, me.Expression!);

            if (a != null)
            {
                if (me.Expression!.Type.IsNullable())
                {
                    if (me.Member.Name == "HasValue")
                        return a + " != null";
                    else if (me.Member.Name == "Value")
                        return a;
                }

                if (me.Expression.Type.IsEntity())
                {
                    if (me.Member.Name == "IdOrNull")
                        return a + "." + "id";
                }

                return a + "." + me.Member.Name.FirstLower();
            }
        }

        if (expr is ConditionalExpression cond)
        {
            var test = ToJavascript(param, cond.Test);
            var ifTrue = ToJavascript(param, cond.IfTrue);
            var ifFalse = ToJavascript(param, cond.IfFalse);

            if (test != null && ifTrue != null && ifFalse != null)
                return "(" + test + " ? " + ifTrue + " : " + ifFalse + ")";
        }

        if (expr is BinaryExpression be)
        {
            if (be.NodeType == ExpressionType.Add && be.Type == typeof(string))
            {

                var a = ToJavascriptToString(param, be.Left);
                var b = ToJavascriptToString(param, be.Right);

                if (a != null && b != null)
                    return "(" + a + " + " + b + ")";
            }
            else if (be.NodeType == ExpressionType.Add)
            {

                var a = ToJavascript(param, be.Left);
                var b = ToJavascript(param, be.Right);

                if (a != null && b != null)
                    return "(" + a + " + " + b + ")";
            }
            else
            {
                var a = ToJavascript(param, be.Left);
                var b = ToJavascript(param, be.Right);

                var op = ToJsOperator(be.NodeType);

                if (a != null && op != null && b != null)
                    return a + op + b;
            }
        }

        if (expr is MethodCallExpression mc)
        {
            if (mc.Method.Name == "ToString" && mc.Object != null)
                return ToJavascriptToString(param, mc.Object!, mc.TryGetArgument("format") is ConstantExpression format ? (string)format.Value! : null);

            if (mc.Method.Name == "GetType" && mc.Object != null && (mc.Object.Type.IsIEntity() || mc.Object.Type.IsModelEntity()))
            {
                var obj = ToJavascript(param, mc.Object!);
                if (obj != null)
                    return "fd.getTypeInfo(" + obj + ")";
            }

            if (mc.Method.Name == "ToLite" && mc.Arguments[0].Type.IsIEntity())
            {
                var obj = ToJavascript(param, mc.Arguments[0]);
                if (obj != null)
                    return "fd.toLite(" + obj + ")";
            }

            if (mc.Method.Name == nameof(Entity.Mixin))
            {
                var obj = ToJavascript(param, mc.Object!);
                if (obj != null)
                {
                    var mixinType = mc.Method.GetGenericArguments().SingleEx();
                    return $"{obj}.mixins['{Reflector.CleanTypeName(mixinType)}']";

                }
            }

            if (mc.Method.Name == nameof(string.Format) && mc.Method.DeclaringType == typeof(string) ||
                mc.Method.Name == nameof(StringExtensions.FormatWith) && mc.Method.DeclaringType == typeof(StringExtensions))
            {
                var format = (mc.Object ?? mc.GetArgument("format"));

                var argFormats = new Dictionary<int, string>();
                if (format is ConstantExpression c)
                {
                    var newFormat = FormatRegex.Replace((string)c.Value!, m =>
                    {
                        var index = int.Parse(m.Groups["index"].Value);
                        argFormats.Add(index, m.Groups["format"].Value);

                        return "{" + index + "}";
                    });

                    format = Expression.Constant(newFormat, typeof(string));
                }

                var args = mc.TryGetArgument("args")?.Let(a => ((NewArrayExpression)a).Expressions) ??
                    new[] { mc.TryGetArgument("arg0"), mc.TryGetArgument("arg1"), mc.TryGetArgument("arg2"), mc.TryGetArgument("arg3") }.NotNull().ToReadOnly();

                var strFormat = ToJavascriptToString(param, format);
                var arguments = args.Select((a, i) => ToJavascriptToString(param, a, argFormats.TryGetC(i))).ToList();

                if (strFormat == null || arguments.Any(a => a == null))
                    return null;

                return $"{strFormat}.formatWith(" + arguments.ToString(", ") + ")";
            }

            if (mc.Method.IsExtensionMethod() && mc.Arguments.Only()?.Type == typeof(Type))
            {
                var obj = ToJavascript(param, mc.Arguments.SingleEx());
                if (obj != null)
                {
                    if (mc.Method.Name == nameof(DescriptionManager.NiceName))
                        return obj + ".niceName";


                    if (mc.Method.Name == nameof(DescriptionManager.NicePluralName))
                        return obj + ".nicePluralName";

                    if (mc.Method.Name == nameof(Reflector.NewNiceName))
                        return "fd.newNiceName(" + obj + ")";


                }
            }

            if (mc.Method.Name == nameof(Symbol.NiceToString))
            {
                if (mc.Object != null && (typeof(Symbol).IsAssignableFrom(mc.Object.Type) || typeof(SemiSymbol).IsAssignableFrom(mc.Object.Type)))
                {
                    var obj = ToJavascript(param, mc.Object!);
                    if (obj != null)
                        return "fd.getToString(" + obj + ")";
                }
            }

            if (mc.Method.Name == nameof(DescriptionManager.NiceToString))
            {
                var arg = mc.Arguments.Only();
                if (arg != null)
                {
                    if (arg is UnaryExpression u && u.NodeType == ExpressionType.Convert && u.Type == typeof(Enum))
                        arg = u.Operand;

                    if (arg.Type.IsEnum)
                    {
                        var obj = ToJavascript(param, arg);
                        if (obj != null)
                            return $"(fd.getTypeInfo(\"{arg.Type.Name}\").members[{obj}]?.niceName ?? \"\")";
                    }
                }
            }

            if (mc.Method.DeclaringType == typeof(DateTime))
            {
                switch (mc.Method.Name)
                {
                    case nameof(DateTime.ToShortDateString): return ToJavascriptToString(param, mc.Object!, "d");
                    case nameof(DateTime.ToShortTimeString): return ToJavascriptToString(param, mc.Object!, "t");
                    case nameof(DateTime.ToLongDateString): return ToJavascriptToString(param, mc.Object!, "D");
                    case nameof(DateTime.ToLongTimeString): return ToJavascriptToString(param, mc.Object!, "T");
                }
            }

            if (mc.Method.DeclaringType == typeof(DateOnly))
            {
                switch (mc.Method.Name)
                {
                    case nameof(DateOnly.ToShortDateString): return ToJavascriptToString(param, mc.Object!, "d");
                    case nameof(DateOnly.ToLongDateString): return ToJavascriptToString(param, mc.Object!, "D");
                    case nameof(DateOnly.ToString): return ToJavascriptToString(param, mc.Object!, "d");
                }
            }

            if (mc.Method.DeclaringType == typeof(StringExtensions) && mc.Method.Name == nameof(StringExtensions.Etc))
            {
                var str = ToJavascriptToString(param, mc.GetArgument("str"));
                var max = ((ConstantExpression)mc.GetArgument("max")).Value!.ToString();

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



        if (expr is MemberInitExpression mie &&
            mie.NewExpression.Arguments.Count == 0 &&
            mie.Bindings.All(b => b is MemberAssignment))
        {
            var fields = mie.Bindings.Cast<MemberAssignment>()
                .ToString(ma => ma.Member.Name.FirstLower() + ": " + ToJavascript(param, ma.Expression) + ",", "\n");

            var t = mie.Type;

            var typeName = TypeLogic.TryGetCleanName(t) ?? t.Name;

            return $"fd.New(\"{typeName}\", {{\n{fields}\n}})";
        }

        return null;
    }

    private static string? ToJsOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.Coalesce => "??",
            _ => null
        };
    }

    private static string? ToJavascriptToString(ParameterExpression param, Expression expr, string? format = null)
    {
        if (expr.NodeType == ExpressionType.Convert && expr.Type == typeof(object))
            expr = ((UnaryExpression)expr).Operand;

        if (expr is ConstantExpression ce && ce.Value == null)
        {
            return "\"\"";
        }

        if (expr is ConditionalExpression iff)
        {
            var t = ToJavascript(param, iff.Test);
            var a = ToJavascriptToString(param, iff.IfTrue, format);
            var b = ToJavascriptToString(param, iff.IfFalse, format);

            if (t != null && a != null && b != null)
                return "(" + t + " ? " + a + " : " + b + ")";
        }

        if (expr is BinaryExpression be && be.NodeType == ExpressionType.Coalesce)
        {
            var t = ToJavascript(param, be.Left);
            var a = ToJavascriptToString(param, be.Left, format);
            var b = ToJavascriptToString(param, be.Right, format);

            if (t != null && a != null && b != null)
                return "(" + t + " != null ? " + a + " : " + b + ")";
        }

        var r = ToJavascript(param, expr);

        if (r == null)
            return null;

        if (expr.NodeType != ExpressionType.MemberAccess)
            return r;

        if (expr.Type.IsModifiableEntity() || expr.Type.IsLite() || expr.Type.IsIEntity())
            return "fd.getToString(" + r + ")";

        string? formatFull = format == null ? null : (", '" + format + "'");

        if (expr.Type.UnNullify() == typeof(DateTime))
            return "fd.dateToString(" + r + ", 'DateTime'" + formatFull + ")";

        if (expr.Type.UnNullify() == typeof(DateOnly))
            return "fd.dateToString(" + r + ", 'DateOnly'" + formatFull + ")";

        if (expr.Type.UnNullify() == typeof(TimeSpan)) /*deprecate?*/
            return "fd.timeToString(" + r + formatFull + ")";

        if (expr.Type.UnNullify() == typeof(TimeOnly))
            return "fd.timeToString(" + r + formatFull + ")";

        if (ReflectionTools.IsIntegerNumber(expr.Type.UnNullify()))
            return "fd.numberToString(" + r + (formatFull ?? ", 'D'") + ")";

        if (ReflectionTools.IsDecimalNumber(expr.Type.UnNullify()))
            return "fd.numberToString(" + r + (formatFull ?? ", 'N'") + ")";

        return "fd.valToString(" + r + ")";
    }
}
