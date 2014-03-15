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


namespace Signum.Web
{
    public static class ValueLineHelper
    {
        public static string StaticValue = "sfStaticValue";

        public static ValueLineConfigurator Configurator = new ValueLineConfigurator();

        private static MvcHtmlString InternalValueLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.Visible && (!valueLine.HideIfNull || valueLine.UntypedValue != null))
            {
                var value = InternalValue(helper, valueLine);

                return helper.FormGroup(valueLine, valueLine.Prefix, valueLine.LabelText, value);
            }

            return MvcHtmlString.Empty;
        }

        private static MvcHtmlString InternalValue(HtmlHelper helper, ValueLine valueLine)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();
            ValueLineType vltype = valueLine.ValueLineType ?? Configurator.GetDefaultValueLineType(valueLine.Type);

            valueLine.ValueHtmlProps.AddCssClass("form-control");

            sb.AddLine(Configurator.Constructor[vltype](helper, valueLine));

            if (valueLine.UnitText.HasText())
            {
                sb.AddLine(helper.Span(valueLine.Compose("unit"), valueLine.UnitText, "sf-unit-line"));
            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EnumComboBox(this HtmlHelper helper, ValueLine valueLine)
        {
            var uType = valueLine.Type.UnNullify();
            Enum value = valueLine.UntypedValue as Enum;

            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(helper.Hidden(valueLine.Prefix, valueLine.UntypedValue.ToString()));

                string str =
                    value == null ? null :
                    LocalizedAssembly.GetDescriptionOptions(uType).IsSet(DescriptionOptions.Members) ? value.NiceToString() : value.ToString();

                return result.Concat(helper.Span("", str, "form-control", valueLine.ValueHtmlProps));
            }

            StringBuilder sb = new StringBuilder();
            List<SelectListItem> items = valueLine.EnumComboItems ?? valueLine.CreateComboItems();

            if (value != null)
                items.Where(e => e.Value == value.ToString())
                    .SingleOrDefaultEx()
                    .TryDoC(s => s.Selected = true);

            return helper.DropDownList(valueLine.Prefix, items, valueLine.ValueHtmlProps);
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
                    result = result.Concat(helper.Hidden(valueLine.Prefix, value.TryToString(valueLine.Format)));
                return result.Concat(helper.Span("", value.TryToString(valueLine.Format), "form-control", valueLine.ValueHtmlProps));
            }

            return helper.DateTimePicker(valueLine.Prefix, true, value, valueLine.Format, CultureInfo.CurrentCulture, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString Hidden(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
                return helper.Span(valueLine.Prefix, valueLine.UntypedValue.TryToString() ?? "", "form-control");

            return helper.Hidden(valueLine.Prefix, valueLine.UntypedValue.TryToString() ?? "", valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString TextboxInLine(this HtmlHelper helper, ValueLine valueLine)
        {
            string value = (valueLine.UntypedValue as IFormattable).TryToString(valueLine.Format) ??
                           valueLine.UntypedValue.TryToString() ?? "";

            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(helper.Hidden(valueLine.Prefix, value));
                return result.Concat(helper.FormControlStatic(valueLine.Compose(StaticValue), value, valueLine.ValueHtmlProps));
            }

            if (!valueLine.ValueHtmlProps.ContainsKey("autocomplete"))
                valueLine.ValueHtmlProps.Add("autocomplete", "off");
            else
                valueLine.ValueHtmlProps.Remove("autocomplete");

            valueLine.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + valueLine.ValueHtmlProps.TryGetC("onblur");

            if (!valueLine.ValueHtmlProps.ContainsKey("type"))
                valueLine.ValueHtmlProps["type"] = "text";

            return helper.TextBox(valueLine.Prefix, value, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString NumericTextbox(this HtmlHelper helper, ValueLine valueLine)
        {
            if (!valueLine.ReadOnly)
                valueLine.ValueHtmlProps.Add("onkeydown", Reflector.IsDecimalNumber(valueLine.Type) ? 
                    "return SF.InputValidator.isDecimal(event);" : 
                    "return SF.InputValidator.isNumber(event);");    
            
            return helper.TextboxInLine(valueLine);
        }

        public static MvcHtmlString TextAreaInLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(helper.Hidden(valueLine.Prefix, (string)valueLine.UntypedValue));
                return result.Concat(helper.Span("", (string)valueLine.UntypedValue, "form-control", valueLine.ValueHtmlProps));
            }

            valueLine.ValueHtmlProps.Add("autocomplete", "off");
            valueLine.ValueHtmlProps["onblur"] = "this.innerHTML = this.value; " + valueLine.ValueHtmlProps.TryGetC("onblur");

            return helper.TextArea(valueLine.Prefix, (string)valueLine.UntypedValue, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString CheckBox(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
                valueLine.ValueHtmlProps.Add("disabled", "disabled");

            bool? value = (bool?)valueLine.UntypedValue;
            return helper.CheckBox(valueLine.Prefix, value ?? false, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString RadioButtons(this HtmlHelper helper, ValueLine valueLine)
        {
            bool? value = (bool?)valueLine.UntypedValue;
            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (valueLine.ReadOnly)
            {
                if (valueLine.WriteHiddenOnReadonly)
                    sb.AddLine(helper.Hidden(valueLine.Prefix, value ?? false));
                
                valueLine.ValueHtmlProps.Add("disabled", "disabled");
            }

            valueLine.ValueHtmlProps.Add("name", valueLine.Prefix);

            valueLine.ValueHtmlProps.AddCssClass("rbValueLine");

            sb.AddLine(MvcHtmlString.Create(helper.RadioButton(valueLine.Prefix, true, value == true, valueLine.ValueHtmlProps).ToHtmlString()
                .Replace("id=\"" + valueLine.Prefix + "\"", "id=\"" + valueLine.Prefix + "_True\"")));

            sb.AddLine(helper.Span(valueLine.Compose("lblRadioTrue"), valueLine.RadioButtonLabelTrue, "sf-value-line-radiolbl"));

            sb.AddLine(MvcHtmlString.Create(helper.RadioButton(valueLine.Prefix, false, value == false, valueLine.ValueHtmlProps).ToHtmlString()
              .Replace("id=\"" + valueLine.Prefix + "\"", "id=\"" + valueLine.Prefix + "_False\"")));

            sb.AddLine(helper.Span(valueLine.Compose("lblRadioFalse"), valueLine.RadioButtonLabelFalse, "sf-value-line-radiolbl"));

            return sb.ToHtml();
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

            ValueLine vl = new ValueLine(typeof(S), context.Value, context, null, context.PropertyRoute);

            Common.FireCommonTasks(vl);

            if (settingsModifier != null)
                settingsModifier(vl);

            var result = helper.InternalValueLine(vl);

            var vo = vl.ViewOverrides;
            if (vo == null)
                return result;

            return vo.OnSurroundLine(vl.PropertyRoute, helper, tc, result);
        }

        public static MvcHtmlString HiddenLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            return helper.HiddenLine(tc, property, null);
        }

        public static MvcHtmlString HiddenLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<ValueLine> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            ValueLine hl = new ValueLine(typeof(S), context.Value, context, null, context.PropertyRoute);

            Common.FireCommonTasks(hl);

            if (settingsModifier != null)
                settingsModifier(hl);

            return Hidden(helper, hl);
        }
    }

    public class ValueLineConfigurator
    {
        public int? MaxValueLineSize = 100; 

        public virtual ValueLineType GetDefaultValueLineType(Type type)
        {
            type = type.UnNullify();

            if (type.IsEnum)
                return ValueLineType.Combo;
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

        public Dictionary<ValueLineType, Func<HtmlHelper, ValueLine, MvcHtmlString>> Constructor = new Dictionary<ValueLineType, Func<HtmlHelper, ValueLine, MvcHtmlString>>()
        {
            {ValueLineType.TextBox, (helper, valueLine) => helper.TextboxInLine(valueLine)},
            {ValueLineType.TextArea, (helper, valueLine) => helper.TextAreaInLine(valueLine)},
            {ValueLineType.Boolean, (helper, valueLine) => helper.CheckBox(valueLine)},
            {ValueLineType.RadioButtons, (helper, valueLine) => helper.RadioButtons(valueLine)},
            {ValueLineType.Combo, (helper, valueLine) => helper.EnumComboBox(valueLine)},
            {ValueLineType.DateTime, (helper, valueLine) => helper.DateTimePicker(valueLine)},
            {ValueLineType.Number, (helper, valueLine) => helper.NumericTextbox(valueLine)}
        };
    }

    public enum ValueLineType
    {
        Boolean,
        RadioButtons,
        Combo,
        DateTime,
        TextBox,
        TextArea,
        Number
    };

}
