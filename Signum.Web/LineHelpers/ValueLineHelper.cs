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


namespace Signum.Web
{
    public static class ValueLineHelper
    {
        public static ValueLineConfigurator Configurator = new ValueLineConfigurator();

        private static MvcHtmlString InternalValueLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (!valueLine.Visible || (valueLine.HideIfNull && valueLine.UntypedValue == null))
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            if (valueLine.OnlyValue)
            {
                InternalValueLineValue(helper, valueLine, sb);
            }
            else
            {
                using (sb.Surround(new HtmlTag("div").Class("sf-field")))
                using (valueLine.LabelVisible && valueLine.ValueFirst ? sb.Surround(new HtmlTag("div").Class("sf-value-first")) : null)
                {
                    if (!valueLine.ValueFirst)
                        InternalValueLineLabel(helper, valueLine, sb);

                    using (sb.Surround(new HtmlTag("div").Class("sf-value-container")))
                        InternalValueLineValue(helper, valueLine, sb);

                    if (valueLine.ValueFirst)
                        InternalValueLineLabel(helper, valueLine, sb);
                }
            }

            return sb.ToHtml();
        }

        private static void InternalValueLineLabel(HtmlHelper helper, ValueLine valueLine, HtmlStringBuilder sb)
        {
            if (valueLine.LabelVisible)
                sb.AddLine(helper.Label(valueLine.Compose("lbl"), valueLine.LabelText, valueLine.ControlID, "sf-label-line", valueLine.LabelHtmlProps));
        }

        private static void InternalValueLineValue(HtmlHelper helper, ValueLine valueLine, HtmlStringBuilder sb)
        {
            ValueLineType vltype = valueLine.ValueLineType ?? Configurator.GetDefaultValueLineType(valueLine.Type);

            valueLine.ValueHtmlProps.AddCssClass("sf-value-line");

            if (valueLine.ShowValidationMessage)
                valueLine.ValueHtmlProps.AddCssClass("inlineVal"); //inlineVal class tells Javascript code to show Inline Error

            sb.AddLine(Configurator.Constructor[vltype](helper, valueLine));

            if (valueLine.UnitText.HasText())
            {
                sb.AddLine(helper.Span(valueLine.Compose("unit"), valueLine.UnitText, "sf-unit-line"));
            }

            if (valueLine.ShowValidationMessage)
            {
                sb.AddLine(helper.ValidationMessage(valueLine.ControlID));
            }
        }

        public static MvcHtmlString EnumComboBox(this HtmlHelper helper, ValueLine valueLine)
        {
            Enum value = valueLine.UntypedValue as Enum;

            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(helper.Hidden(valueLine.ControlID, valueLine.UntypedValue.ToString()));
                return result.Concat(helper.Span("", value != null ? value.NiceToString() : valueLine.UntypedValue.TryToString(), "sf-value-line", valueLine.ValueHtmlProps));
            }

            StringBuilder sb = new StringBuilder();
            List<SelectListItem> items = valueLine.EnumComboItems;
            if (items == null)
            {
                items = new List<SelectListItem>();

                if (valueLine.UntypedValue == null ||
                    valueLine.Type.IsNullable() && (valueLine.PropertyRoute == null || !Validator.TryGetPropertyValidator(valueLine.PropertyRoute).Validators.OfType<NotNullValidatorAttribute>().Any()))
                {
                    items.Add(new SelectListItem() { Text = "-", Value = "" });
                }

                items.AddRange(
                    Enum.GetValues(valueLine.Type.UnNullify())
                        .Cast<Enum>()
                        .Select(v => new SelectListItem()
                            {
                                Text = v.NiceToString(),
                                Value = v.ToString(),
                                Selected = object.Equals(value, v),
                            })
                    );
            }
            else
                if (value != null)
                    items.Where(e => e.Value == value.ToString()).SingleEx(()=>"Not value present in ValueLine", ()=> "More than one values present in ValueLine").Selected = true;

            return helper.DropDownList(valueLine.ControlID, items, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString DateTimePickerTextbox(this HtmlHelper helper, ValueLine valueLine)
        {
            DateTime? value = (DateTime?)valueLine.UntypedValue;

            if (valueLine.DatePickerOptions == null)
                valueLine.DatePickerOptions = new DatePickerOptions();

            if (value.HasValue)
                value = value.Value.ToUserInterface();

            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(helper.Hidden(valueLine.ControlID, value.TryToString(valueLine.Format)));
                return result.Concat(helper.Span("", value.TryToString(valueLine.Format), "sf-value-line", valueLine.ValueHtmlProps));
            }

            valueLine.ValueHtmlProps.AddCssClass("maskedEdit");

            valueLine.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + valueLine.ValueHtmlProps.TryGetC("onblur");        
            
            string jsDataFormat = DatePickerOptions.JsDateFormat(valueLine.Format ?? "g");

            valueLine.ValueHtmlProps["size"] = jsDataFormat.Length + 1;   //time is often rendered with two digits as hours, but format is represented as "H"

            if (valueLine.DatePickerOptions.Format == null)
                valueLine.DatePickerOptions.Format = jsDataFormat;

            bool isDefaultDatepicker = valueLine.DatePickerOptions.IsDefault();
            if (isDefaultDatepicker) //if default, datepicker will be created when processing html in javascript 
            {
                valueLine.ValueHtmlProps.AddCssClass("sf-datepicker");
                valueLine.ValueHtmlProps["data-format"] =  valueLine.DatePickerOptions.Format;
            }

            if (!valueLine.ValueHtmlProps.ContainsKey("placeholder") && jsDataFormat.HasText())
                valueLine.ValueHtmlProps["placeholder"] = jsDataFormat.ToLower();

            MvcHtmlString returnString = helper.TextBox(valueLine.ControlID, value.TryToString(valueLine.Format), valueLine.ValueHtmlProps);
            
            if (!isDefaultDatepicker)
                returnString = returnString.Concat(helper.Calendar(valueLine.ControlID, valueLine.DatePickerOptions));

            return returnString;
        }

        public static MvcHtmlString Hidden(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
                return helper.Span(valueLine.ControlID, valueLine.UntypedValue.TryToString() ?? "", "sf-value-line");

            return HtmlHelperExtenders.InputType("hidden", valueLine.ControlID, valueLine.UntypedValue.TryToString() ?? "", valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString TextboxInLine(this HtmlHelper helper, ValueLine valueLine)
        {
            string value = (valueLine.UntypedValue as IFormattable).TryToString(valueLine.Format) ??
                           valueLine.UntypedValue.TryToString() ?? "";

            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(helper.Hidden(valueLine.ControlID, value));
                return result.Concat(helper.Span("", value, "sf-value-line", valueLine.ValueHtmlProps));
            }

            if (!valueLine.ValueHtmlProps.ContainsKey("autocomplete"))
                valueLine.ValueHtmlProps.Add("autocomplete", "off");
            else
                valueLine.ValueHtmlProps.Remove("autocomplete");

            valueLine.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + valueLine.ValueHtmlProps.TryGetC("onblur");

            valueLine.ValueHtmlProps["type"] = "text";

            return helper.TextBox(valueLine.ControlID, value, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString NumericTextbox(this HtmlHelper helper, ValueLine valueLine)
        {
            if (!valueLine.ReadOnly)
                valueLine.ValueHtmlProps.Add("onkeydown", Reflector.IsDecimalNumber(valueLine.Type) ? "return SF.InputValidator.isDecimal(event);" : "return SF.InputValidator.isNumber(event);");    
            
            return helper.TextboxInLine(valueLine);
        }

        public static MvcHtmlString TextAreaInLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
            {
                MvcHtmlString result = MvcHtmlString.Empty;
                if (valueLine.WriteHiddenOnReadonly)
                    result = result.Concat(helper.Hidden(valueLine.ControlID, (string)valueLine.UntypedValue));
                return result.Concat(helper.Span("", (string)valueLine.UntypedValue, "sf-value-line", valueLine.ValueHtmlProps));
            }

            valueLine.ValueHtmlProps.Add("autocomplete", "off");
            valueLine.ValueHtmlProps["onblur"] = "this.innerHTML = this.value; " + valueLine.ValueHtmlProps.TryGetC("onblur");

            return helper.TextArea(valueLine.ControlID, (string)valueLine.UntypedValue, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString CheckBox(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
                valueLine.ValueHtmlProps.Add("disabled", "disabled");

            bool? value = (bool?)valueLine.UntypedValue;
            return HtmlHelperExtenders.CheckBox(helper, valueLine.ControlID, value.HasValue ? value.Value : false, !valueLine.ReadOnly, valueLine.ValueHtmlProps);
        }

        public static MvcHtmlString RadioButtons(this HtmlHelper helper, ValueLine valueLine)
        {
            bool? value = (bool?)valueLine.UntypedValue;
            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (valueLine.ReadOnly)
            {
                if (valueLine.WriteHiddenOnReadonly)
                    sb.AddLine(helper.Hidden(valueLine.ControlID, value ?? false));
                
                valueLine.ValueHtmlProps.Add("disabled", "disabled");
            }

            valueLine.ValueHtmlProps.Add("name", valueLine.ControlID);

            valueLine.ValueHtmlProps.AddCssClass("rbValueLine");

            sb.AddLine(MvcHtmlString.Create(helper.RadioButton(valueLine.ControlID, true, value == true, valueLine.ValueHtmlProps).ToHtmlString()
                .Replace("id=\"" + valueLine.ControlID + "\"", "id=\"" + valueLine.ControlID + "_True\"")));

            sb.AddLine(helper.Span(valueLine.Compose("lblRadioTrue"), valueLine.RadioButtonLabelTrue, "sf-value-line-radiolbl"));

            sb.AddLine(MvcHtmlString.Create(helper.RadioButton(valueLine.ControlID, false, value == false, valueLine.ValueHtmlProps).ToHtmlString()
              .Replace("id=\"" + valueLine.ControlID + "\"", "id=\"" + valueLine.ControlID + "_False\"")));

            sb.AddLine(helper.Span(valueLine.Compose("lblRadioFalse"), valueLine.RadioButtonLabelFalse, "sf-value-line-radiolbl"));

            return sb.ToHtml();
        }

        public static MvcHtmlString ValueLine<T>(this HtmlHelper helper, ValueLine valueLine)
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

            return InternalValueLine(helper, vl);
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
            {ValueLineType.DateTime, (helper, valueLine) => helper.DateTimePickerTextbox(valueLine)},
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
