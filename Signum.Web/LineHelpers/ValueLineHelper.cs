using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities.Reflection;
using Signum.Entities;
using System.Globalization;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Web
{
    public static class ValueLineHelper
    {
        public static ValueLineConfigurator Configurator = new ValueLineConfigurator();

        private static MvcHtmlString InternalValueLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (!valueLine.Visible || (valueLine.HideIfNull && valueLine.UntypedValue == null))
                return MvcHtmlString.Empty;

            if (valueLine.PlaceholderLabels && !valueLine.ValueHtmlProps.ContainsKey("placeholder"))
                valueLine.ValueHtmlProps["placeholder"] = valueLine.LabelText;

            var value = InternalValue(helper, valueLine);

            if (valueLine.InlineCheckbox)
                return new HtmlTag("label").InnerHtml("{0} {1}".FormatHtml(value, valueLine.LabelText)).ToHtml();

            return helper.FormGroup(valueLine, valueLine.Prefix, valueLine.LabelHtml ?? valueLine.LabelText.FormatHtml(), value);
        }

        private static MvcHtmlString InternalValue(HtmlHelper helper, ValueLine valueLine)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();
            ValueLineType vltype = valueLine.ValueLineType ?? Configurator.GetDefaultValueLineType(valueLine.Type);

            using (valueLine.UnitText == null ? null : sb.SurroundLine(new HtmlTag("div").Class("input-group")))
            {
                sb.AddLine(Configurator.Helper[vltype](helper, valueLine));

                if (valueLine.UnitText.HasText())
                    sb.AddLine(helper.Span(null, valueLine.UnitText, "input-group-addon"));
            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EnumComboBox(this HtmlHelper helper, ValueLine valueLine)
        {
            var uType = valueLine.Type.UnNullify();
            Enum value = valueLine.UntypedValue as Enum;

            return InternalComboBox(helper, valueLine, uType, value);
        }

        public static MvcHtmlString BooleanEnumComboBox(this HtmlHelper helper, ValueLine valueLine)
        {
            var uType = valueLine.Type.UnNullify();
            if (uType != typeof(bool))
                throw new InvalidOperationException("valueLine Type is not a boolean");

            BooleanEnum? value = ToBooleanEnum((bool?)valueLine.UntypedValue);

            return InternalComboBox(helper, valueLine, typeof(BooleanEnum), value);
        }

        private static BooleanEnum? ToBooleanEnum(bool? value)
        {
            return value == null ? (BooleanEnum?)null :
                value.Value ? BooleanEnum.True :
                BooleanEnum.False;
        }

        static MvcHtmlString HiddenWithoutId(string name, string value)
        {
            return new HtmlTag("input")
                .Attr("type", "hidden")
                .Attr("name", name)
                .Attr("value", value)
                .ToHtmlSelf();
        }

        private static MvcHtmlString InternalComboBox(HtmlHelper helper, ValueLine valueLine, Type uType, Enum value)
        {
            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(HiddenWithoutId(valueLine.Prefix, valueLine.UntypedValue.ToString()));

                string str = value == null ? null :
                    LocalizedAssembly.GetDescriptionOptions(uType).IsSet(DescriptionOptions.Members) ? value.NiceToString() : value.ToString();

                return result.Concat(helper.FormControlStatic(valueLine, valueLine.Prefix, str, valueLine.ValueHtmlProps));
            }

            StringBuilder sb = new StringBuilder();
            List<SelectListItem> items = valueLine.EnumComboItems ?? valueLine.CreateComboItems();

            if (value != null)
                items.Where(e => e.Value == value.ToString())
                    .SingleOrDefaultEx()?.Do(s => s.Selected = true);

            valueLine.ValueHtmlProps.AddCssClass("form-control");
            return helper.SafeDropDownList(valueLine.Prefix, items, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString DateTimePicker(this HtmlHelper helper, ValueLine valueLine)
        {
            DateTime? value = (DateTime?)valueLine.UntypedValue;

            if (value.HasValue)
                value = value.Value.ToUserInterface();

            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(HiddenWithoutId(valueLine.Prefix, value?.ToString(valueLine.Format)));

                return result.Concat(helper.FormControlStatic(valueLine, valueLine.Prefix, value?.ToString(valueLine.Format), valueLine.ValueHtmlProps));
            }

            valueLine.ValueHtmlProps.AddCssClass("form-control");
            return helper.DateTimePicker(valueLine.Prefix, true, value, valueLine.Format, CultureInfo.CurrentCulture, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString TimeSpanPicker(this HtmlHelper helper, ValueLine valueLine)
        {
            TimeSpan? value = (TimeSpan?)valueLine.UntypedValue;

            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(HiddenWithoutId(valueLine.Prefix, value?.ToString(valueLine.Format)));

                return result.Concat(helper.FormControlStatic(valueLine, valueLine.Prefix, value?.ToString(valueLine.Format), valueLine.ValueHtmlProps));
            }

            var dateFormatAttr = valueLine.PropertyRoute.PropertyInfo.GetCustomAttribute<TimeSpanDateFormatAttribute>();
            if (dateFormatAttr != null)
                return helper.TimePicker(valueLine.Prefix, true, value, dateFormatAttr.Format, CultureInfo.CurrentCulture, valueLine.ValueHtmlProps);
            else
            {
                valueLine.ValueHtmlProps.AddCssClass("form-control");
                return helper.TextBox(valueLine.Prefix, value == null ? "" : value.Value.ToString(valueLine.Format, CultureInfo.CurrentCulture), valueLine.ValueHtmlProps);
            }
        }

        public static MvcHtmlString TextboxInLine(this HtmlHelper helper, ValueLine valueLine)
        {
            string value = (valueLine.UntypedValue as IFormattable)?.ToString(valueLine.Format, CultureInfo.CurrentCulture) ??
                           valueLine.UntypedValue?.ToString() ?? "";

            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(HiddenWithoutId(valueLine.Prefix, value));

                if (valueLine.UnitText.HasText())
                    return result.Concat(new HtmlTag("p").Id(valueLine.Prefix).SetInnerText(value).Class("form-control").Attrs(valueLine.ValueHtmlProps).ToHtml());
                else
                    return result.Concat(helper.FormControlStatic(valueLine, valueLine.Prefix, value, valueLine.ValueHtmlProps));
            }

            if (!valueLine.ValueHtmlProps.ContainsKey("autocomplete"))
                valueLine.ValueHtmlProps.Add("autocomplete", "off");
            else
                valueLine.ValueHtmlProps.Remove("autocomplete");



            valueLine.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + (valueLine.ValueHtmlProps.ContainsKey("onblur") ? valueLine.ValueHtmlProps["onblur"] : null);

            if (!valueLine.ValueHtmlProps.ContainsKey("type"))
                valueLine.ValueHtmlProps["type"] = "text";

            valueLine.ValueHtmlProps.AddCssClass("form-control");
            return helper.TextBox(valueLine.Prefix, value, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString NumericTextbox(this HtmlHelper helper, ValueLine valueLine)
        {
            if (!valueLine.ReadOnly)
                valueLine.ValueHtmlProps.Add("onkeydown", ReflectionTools.IsDecimalNumber(valueLine.Type) ?
                    "return SF.InputValidator.isDecimal(event);" :
                    "return SF.InputValidator.isNumber(event);");

            valueLine.ValueHtmlProps.AddCssClass("numeric");

            return helper.TextboxInLine(valueLine);
        }

        public static MvcHtmlString ColorTextbox(this HtmlHelper helper, ValueLine valueLine)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.SurroundLine(new HtmlTag("div").Class("input-group")))
            {
                valueLine.ValueHtmlProps.AddCssClass("form-control");

                ColorEmbedded color = (ColorEmbedded)valueLine.UntypedValue;

                sb.AddLine(helper.TextBox(valueLine.Prefix, color == null ? "" : color.RGBHex(), valueLine.ValueHtmlProps));

                sb.AddLine(new HtmlTag("span").Class("input-group-addon").InnerHtml(new HtmlTag("i")));
            }

            sb.AddLine(new HtmlTag("script").InnerHtml(MvcHtmlString.Create(
@" $(function(){
        $('#" + valueLine.Prefix + @"').parent().colorpicker()" + (valueLine.ReadOnly ? ".colorpicker('disable')" : null) + @";
   });")));

            return sb.ToHtml();
        }

        public static MvcHtmlString TextAreaInLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(HiddenWithoutId(valueLine.Prefix, (string)valueLine.UntypedValue));

                if (valueLine.FormControlStaticAsFormControlReadonly)
                    valueLine.ValueHtmlProps.AddCssClass("readonly-textarea");

                return result.Concat(helper.FormControlStatic(valueLine, valueLine.Prefix, (string)valueLine.UntypedValue, valueLine.ValueHtmlProps));
            }

            valueLine.ValueHtmlProps.Add("autocomplete", "off");
            valueLine.ValueHtmlProps["onblur"] = "this.innerHTML = this.value; " + (valueLine.ValueHtmlProps.ContainsKey("onblur") ? valueLine.ValueHtmlProps["onblur"] : null);
            valueLine.ValueHtmlProps.AddCssClass("form-control");
            return helper.TextArea(valueLine.Prefix, (string)valueLine.UntypedValue, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString CheckBox(this HtmlHelper helper, ValueLine valueLine)
        {
            bool? value = (bool?)valueLine.UntypedValue;
            if (!valueLine.InlineCheckbox)
                valueLine.ValueHtmlProps.AddCssClass("form-control");
            return helper.CheckBox(valueLine.Prefix, value ?? false, !valueLine.ReadOnly, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString ValueLine(this HtmlHelper helper, ValueLine valueLine)
        {
            return helper.InternalValueLine(valueLine);
        }

        public static MvcHtmlString ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            return helper.ValueLine(tc, property, null);
        }

        public static MvcHtmlString ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<ValueLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            ValueLine vl = new ValueLine(typeof(S), context.Value, context, null, context.PropertyRoute);

            Common.FireCommonTasks(vl);

            if (settingsModifier != null)
                settingsModifier(vl);

            var result = helper.InternalValueLine(vl);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(vl.PropertyRoute, helper, tc, result);
        }

        public static MvcHtmlString HiddenLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            return helper.HiddenLine(tc, property, null);
        }

        public static MvcHtmlString HiddenLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<HiddenLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            HiddenLine hl = new HiddenLine(typeof(S), context.Value, context, null, context.PropertyRoute);

            Common.FireCommonTasks(hl);

            if (settingsModifier != null)
                settingsModifier(hl);

            return Hidden(helper, hl);
        }

        public static MvcHtmlString Hidden(this HtmlHelper helper, HiddenLine hiddenLine)
        {
            return helper.Hidden(hiddenLine.Prefix, hiddenLine.UntypedValue?.ToString() ?? "", hiddenLine.ValueHtmlProps);
        }
    }

    public class ValueLineConfigurator
    {
        public int? MaxValueLineSize = 100;

        public virtual ValueLineType GetDefaultValueLineType(Type type)
        {
            if (type == typeof(bool?))
                return ValueLineType.Enum;

            type = type.UnNullify();

            if (type.IsEnum)
                return ValueLineType.Enum;
            else if (type == typeof(ColorEmbedded))
                return ValueLineType.Color;
            else if (type == typeof(TimeSpan))
                return ValueLineType.TimeSpan;
            else
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.DateTime:
                        return ValueLineType.DateTime;
                    case TypeCode.Boolean:
                        return ValueLineType.Boolean;
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                    case TypeCode.Single:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return ValueLineType.Number;
                    case TypeCode.Empty:
                    case TypeCode.Object:
                    case TypeCode.Char:
                    case TypeCode.String:
                    default:
                        return ValueLineType.TextBox;
                }
            }

        }

        public Dictionary<ValueLineType, Func<HtmlHelper, ValueLine, MvcHtmlString>> Helper = new Dictionary<ValueLineType, Func<HtmlHelper, ValueLine, MvcHtmlString>>()
        {
            {ValueLineType.TextBox, (helper, valueLine) => helper.TextboxInLine(valueLine)},
            {ValueLineType.TextArea, (helper, valueLine) => helper.TextAreaInLine(valueLine)},
            {ValueLineType.Boolean, (helper, valueLine) => helper.CheckBox(valueLine)},
            {ValueLineType.Enum, (helper, valueLine) =>  valueLine.Type.UnNullify() == typeof(bool) ? helper.BooleanEnumComboBox(valueLine):  helper.EnumComboBox(valueLine)},
            {ValueLineType.DateTime, (helper, valueLine) => helper.DateTimePicker(valueLine)},
            {ValueLineType.TimeSpan, (helper, valueLine) => helper.TimeSpanPicker(valueLine)},
            {ValueLineType.Number, (helper, valueLine) => helper.NumericTextbox(valueLine)},
            {ValueLineType.Color, (helper, valueLine) => helper.ColorTextbox(valueLine)}
        };
    }

    public enum ValueLineType
    {
        Boolean,
        Enum,
        DateTime,
        TimeSpan,
        TextBox,
        TextArea,
        Number,
        Color
    };

}
