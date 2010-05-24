using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Web.Properties;
using Signum.Entities.Reflection;
using Signum.Entities;


namespace Signum.Web
{
    public static class ValueLineHelper
    {
        public static ValueLineConfigurator Configurator = new ValueLineConfigurator();

        private static string InternalValueLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (!valueLine.Visible || (valueLine.HideIfNull && valueLine.UntypedValue == null))
                return null;

            StringBuilder sb = new StringBuilder();
            if (valueLine.ShowFieldDiv)
                sb.AppendLine("<div class='field'>");

            long? ticks = EntityInfoHelper.GetTicks(helper, valueLine);
            if (ticks != null)
                sb.AppendLine(helper.Hidden(valueLine.Compose(TypeContext.Ticks), ticks.Value).ToHtmlString());

            if (valueLine.LabelVisible)
            {
                if (valueLine.ValueFirst)
                    sb.AppendLine("<div class='valueFirst'>");
                else
                    sb.AppendLine(helper.Label(valueLine.Compose("lbl"), valueLine.LabelText, valueLine.ControlID, TypeContext.CssLineLabel, valueLine.LabelHtmlProps));
            }

            ValueLineType vltype = valueLine.ValueLineType ?? Configurator.GetDefaultValueLineType(valueLine.Type);

            valueLine.ValueHtmlProps.AddCssClass("valueLine");

            if (valueLine.ShowValidationMessage)
                valueLine.ValueHtmlProps.AddCssClass("inlineVal"); //inlineVal class tells Javascript code to show Inline Error
            
            sb.AppendLine(Configurator.Constructor[vltype](helper, valueLine));

            if (valueLine.ShowValidationMessage)
            {
                sb.Append("&nbsp;");
                sb.AppendLine(helper.ValidationMessage(valueLine.ControlID).TryCC(hs => hs.ToHtmlString()));
            }

            if (valueLine.LabelVisible && valueLine.ValueFirst)
            {
                if (valueLine.LabelHtmlProps != null && valueLine.LabelHtmlProps.Count > 0)
                    sb.AppendLine(helper.Label(valueLine.Compose("lbl"), valueLine.LabelText, valueLine.ControlID, TypeContext.CssLineLabel, valueLine.LabelHtmlProps));
                else
                    sb.AppendLine(helper.Label(valueLine.Compose("lbl"), valueLine.LabelText, valueLine.ControlID, TypeContext.CssLineLabel));
            }

            if (valueLine.LabelVisible && valueLine.ValueFirst)
                sb.AppendLine("</div>");
            if (valueLine.ShowFieldDiv)
                sb.AppendLine("</div>");

            if (valueLine.BreakLine)
                sb.AppendLine(helper.Div("", "", "clearall"));

            helper.Write(sb.ToString());

            return valueLine.ControlID;
        }

        public static string EnumComboBox(this HtmlHelper helper, ValueLine valueLine) 
        {
            Enum value = (Enum)valueLine.UntypedValue;

            if (valueLine.ReadOnly)
                return helper.Span(valueLine.ControlID, value != null ? value.NiceToString() : "", "valueLine");
            
            StringBuilder sb = new StringBuilder();
            List<SelectListItem> items = valueLine.EnumComboItems;
            if (items == null)
            {
                items = new List<SelectListItem>();
                items.Add(new SelectListItem() { Text = "-", Value = "" });
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

            string setTicks = SetTicksFunction(helper, valueLine);
            string reloadOnChangeFunction = GetReloadFunction(helper, valueLine);

            if (valueLine.ValueHtmlProps.ContainsKey("onchange"))
                valueLine.ValueHtmlProps["onchange"] = setTicks + valueLine.ValueHtmlProps["onchange"] + reloadOnChangeFunction;
            else
                valueLine.ValueHtmlProps.Add("onchange", setTicks + reloadOnChangeFunction);

            return helper.DropDownList(valueLine.ControlID, items, valueLine.ValueHtmlProps).ToHtmlString();
        }

        public static string DateTimePickerTextbox(this HtmlHelper helper, ValueLine valueLine)
        {
            DateTime? value = (DateTime?)valueLine.UntypedValue;

            if (value.HasValue)
                value = value.Value.ToUserInterface();

            if (valueLine.ReadOnly)
            {
                return helper.Span(valueLine.ControlID, value.TryToString(valueLine.Format), "valueLine");
            }
    
            valueLine.ValueHtmlProps.AddCssClass("maskedEdit");
            
            if (valueLine.DatePickerOptions != null && valueLine.DatePickerOptions.ShowAge)
                valueLine.ValueHtmlProps.AddCssClass("hasAge");

            string setTicks = SetTicksFunction(helper, valueLine);
            string reloadOnChangeFunction = GetReloadFunction(helper, valueLine);

            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || valueLine.ReloadOnChange || valueLine.ReloadFunction.HasText())
            {
                if (valueLine.ValueHtmlProps.ContainsKey("onblur"))
                    valueLine.ValueHtmlProps["onblur"] = setTicks + valueLine.ValueHtmlProps["onblur"] + reloadOnChangeFunction;
                else
                    valueLine.ValueHtmlProps.Add("onblur", setTicks + reloadOnChangeFunction);
            }

            valueLine.ValueHtmlProps["size"] = (valueLine.Format == "d") ? 10 : 20;
            
            string returnString = helper.TextBox(valueLine.ControlID, value.TryToString(valueLine.Format), valueLine.ValueHtmlProps) +
                   "\n" +
                   helper.Calendar(valueLine.ControlID, valueLine.DatePickerOptions);

            if (valueLine.DatePickerOptions != null && valueLine.DatePickerOptions.ShowAge)
                returnString += helper.Span(valueLine.ControlID + "Age", String.Empty, "age");

            return returnString;
        }

        public static string TextboxInLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
                return helper.Span(valueLine.ControlID, valueLine.UntypedValue.TryToString() ?? "", "valueLine");

            string setTicks = SetTicksFunction(helper, valueLine);
            string reloadOnChangeFunction = GetReloadFunction(helper, valueLine);

            if (!valueLine.ValueHtmlProps.ContainsKey("autocomplete"))
                valueLine.ValueHtmlProps.Add("autocomplete", "off");
            else
                valueLine.ValueHtmlProps.Remove("autocomplete");
            
            if (valueLine.ValueHtmlProps.ContainsKey("onblur"))
                valueLine.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + setTicks + valueLine.ValueHtmlProps["onblur"] + reloadOnChangeFunction;
            else
                valueLine.ValueHtmlProps.Add("onblur", "this.setAttribute('value', this.value); " + setTicks + reloadOnChangeFunction);

            return helper.TextBox(valueLine.ControlID, valueLine.UntypedValue.TryToString() ?? "", valueLine.ValueHtmlProps).ToHtmlString();
        }

        public static string NumericTextbox(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
                return helper.Span(valueLine.ControlID, valueLine.UntypedValue.TryToString() ?? "", "valueLine");

            valueLine.ValueHtmlProps.Add("onkeydown", Reflector.IsDecimalNumber(valueLine.Type) ? "return validator.decimalNumber(event);" : "return validator.number(event);");

            return helper.TextboxInLine(valueLine);
        }

        public static string TextAreaInLine(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
                return helper.Span(valueLine.ControlID, (string)valueLine.UntypedValue, "valueLine");

            string setTicks = SetTicksFunction(helper, valueLine);
            string reloadOnChangeFunction = GetReloadFunction(helper, valueLine);

            valueLine.ValueHtmlProps.Add("autocomplete", "off");
            if (valueLine.ValueHtmlProps.ContainsKey("onblur"))
                valueLine.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + setTicks + valueLine.ValueHtmlProps["onblur"] + reloadOnChangeFunction;
            else
                valueLine.ValueHtmlProps.Add("onblur", "this.setAttribute('value', this.value); " + setTicks + reloadOnChangeFunction);

            return helper.TextArea(valueLine.ControlID, (string)valueLine.UntypedValue, valueLine.ValueHtmlProps).ToHtmlString();
        }

        public static string CheckBox(this HtmlHelper helper, ValueLine valueLine)
        {
            if (valueLine.ReadOnly)
                valueLine.ValueHtmlProps.Add("disabled", "disabled");
            else
            {
                string setTicks = SetTicksFunction(helper, valueLine);
                string reloadOnChangeFunction = GetReloadFunction(helper, valueLine);

                if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || valueLine.ReloadOnChange || valueLine.ReloadFunction.HasText())
                {
                    if (valueLine.ValueHtmlProps.ContainsKey("onclick"))
                        valueLine.ValueHtmlProps["onclick"] = setTicks + valueLine.ValueHtmlProps["onclick"] + reloadOnChangeFunction;
                    else
                        valueLine.ValueHtmlProps.Add("onclick", setTicks + reloadOnChangeFunction);
                }
            }
            
            bool? value = (bool?)valueLine.UntypedValue;
            return HtmlHelperExtenders.CheckBox(helper, valueLine.ControlID, value.HasValue ? value.Value : false, !valueLine.ReadOnly, valueLine.ValueHtmlProps);
        }

        public static string RadioButtons(this HtmlHelper helper, ValueLine valueLine)
        {
            bool? value = (bool?)valueLine.UntypedValue;
            StringBuilder sb = new StringBuilder();

            if (valueLine.ReadOnly)
                valueLine.ValueHtmlProps.Add("disabled", "disabled");
            
            valueLine.ValueHtmlProps.Add("name", valueLine.ControlID);

            valueLine.ValueHtmlProps.AddCssClass("rbValueLine");

            string rb = helper.RadioButton(valueLine.ControlID, true, value == true, valueLine.ValueHtmlProps).ToHtmlString();
            rb = rb.Replace("id=\"" + valueLine.ControlID + "\"", "id=\"" + valueLine.ControlID + "_True\"");

            sb.AppendLine(rb);
            sb.AppendLine(helper.Span("", valueLine.RadioButtonLabelTrue, "lblRadioTrue"));
            rb = helper.RadioButton(valueLine.ControlID, false, value == false, valueLine.ValueHtmlProps).ToHtmlString();
            rb = rb.Replace("id=\"" + valueLine.ControlID + "\"", "id=\"" + valueLine.ControlID + "_False\"");
            sb.AppendLine(rb);

            sb.AppendLine(helper.Span("", valueLine.RadioButtonLabelFalse, "lblRadioFalse"));
            
            return sb.ToString();
        }

        public static string ValueLine<T>(this HtmlHelper helper, ValueLine valueLine)
        {
            return helper.InternalValueLine(valueLine);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            return helper.ValueLine(tc, property, null);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<ValueLine> settingsModifier)
        {
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, property);

            ValueLine vl = new ValueLine(typeof(S), context.Value, context, null, context.PropertyRoute);
            
            Common.FireCommonTasks(vl);
            
            if (settingsModifier != null)
                settingsModifier(vl);

            return InternalValueLine(helper, vl);
        }

        private static string SetTicksFunction(HtmlHelper helper, ValueLine valueLine)
        {
            return (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || valueLine.ReloadOnChange || valueLine.ReloadFunction.HasText()) ? 
                "$('#{0}').val(new Date().getTime()); ".Formato(valueLine.Compose(TypeContext.Ticks)) : 
                "";
        }

        private static string GetReloadFunction(HtmlHelper helper, ValueLine valueLine)
        {
            return (valueLine.ReloadOnChange || valueLine.ReloadFunction.HasText()) ?
                valueLine.ReloadFunction ?? "ReloadEntity('{0}','{1}'); ".Formato("Signum/ReloadEntity", helper.WindowPrefix()) :
                "";
        }
    }

    public class ValueLineConfigurator
    {
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

        public Dictionary<ValueLineType, Func<HtmlHelper, ValueLine, string>> Constructor = new Dictionary<ValueLineType, Func<HtmlHelper, ValueLine, string>>()
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
