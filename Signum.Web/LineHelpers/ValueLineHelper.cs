using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Web.Properties;


namespace Signum.Web
{
    public static class ValueLineHelper
    {
        public static ValueLineConfigurator Configurator = new ValueLineConfigurator();

        private static string ManualValueLine<T>(this HtmlHelper helper, string idValueField, T value, ValueLine settings)
        {
            if (!settings.Visible || settings.HideIfNull && value == null)
                return null;

            idValueField = helper.GlobalName(idValueField);

            StringBuilder sb = new StringBuilder();
            if (settings.ShowFieldDiv)
                sb.AppendLine("<div class='field'>");
            
            long? ticks = EntityBaseHelper.GetTicks(helper, idValueField, settings);
            if (ticks != null) 
                sb.AppendLine("<input type='hidden' id='{0}' name='{0}' value='{1}'/>".Formato(TypeContext.Compose(idValueField, TypeContext.Ticks), ticks.Value));

            if (StyleContext.Current.LabelVisible)
            {
                if (StyleContext.Current.ValueFirst)
                    sb.AppendLine("<div class='valueFirst'>");
                else
                    sb.AppendLine(helper.Label(idValueField + "lbl", settings.LabelText, idValueField, TypeContext.CssLineLabel, settings.LabelHtmlProps));
            }

            string valueStr = (value != null) ? value.ToString() : "";
            if (StyleContext.Current.ReadOnly)
            {
                if (value != null && typeof(T).UnNullify() == typeof(Boolean))
                {
                    settings.ValueHtmlProps.Add("disabled", "disabled");
                    sb.AppendLine(helper.CheckBox(idValueField,
                        Convert.ToBoolean(value),
                        settings));
                }
                else
                {
                    if (value != null && typeof(T).UnNullify() == typeof(DateTime) && settings.ValueLineType != null && settings.ValueLineType == ValueLineType.Date)
                        sb.AppendLine(helper.Span(idValueField, Convert.ToDateTime(value).ToString("dd/MM/yyyy"), "valueLine", typeof(T)));
                    else
                        if (typeof(T).UnNullify().BaseType == typeof(Enum) && value != null)
                            sb.AppendLine(helper.Span(idValueField, ((Enum)(object)value).NiceToString(), "valueLine", typeof(T)));
                        else
                            sb.AppendLine(helper.Span(idValueField, value, "valueLine", typeof(T)));
                }
            }
            else
            {
                ValueLineType vltype = (settings.ValueLineType.HasValue) ?
                    settings.ValueLineType.Value :
                    Configurator.GetDefaultValueLineType(typeof(T));

                if (StyleContext.Current.ShowValidationMessage)
                {
                    if (settings.ValueHtmlProps.ContainsKey("class"))
                        settings.ValueHtmlProps["class"] = "valueLine inlineVal " + settings.ValueHtmlProps["class"];
                    else
                    {
                        settings.ValueHtmlProps.Add("class", "valueLine inlineVal"); //inlineVal class tells Javascript code to show Inline Error
                    }
                    sb.AppendLine(Configurator.constructor[vltype](helper, new ValueLineData(idValueField, value, settings, typeof(T))));
                    sb.Append("&nbsp;");
                    sb.AppendLine(helper.ValidationMessage(idValueField));
                }
                else
                {
                    if (settings.ValueHtmlProps.ContainsKey("class"))
                        settings.ValueHtmlProps["class"] = "valueLine " + settings.ValueHtmlProps["class"];
                    else
                        settings.ValueHtmlProps.Add("class", "valueLine");
                    sb.AppendLine(Configurator.constructor[vltype](helper, new ValueLineData(idValueField, value, settings, typeof(T))));
                }
            }
            if (StyleContext.Current.LabelVisible && StyleContext.Current.ValueFirst)
            {
                if (settings.LabelHtmlProps != null && settings.LabelHtmlProps.Count > 0)
                    sb.AppendLine(helper.Label(idValueField + "lbl", settings.LabelText, idValueField, TypeContext.CssLineLabel, settings.LabelHtmlProps));
                else
                    sb.AppendLine(helper.Label(idValueField + "lbl", settings.LabelText, idValueField, TypeContext.CssLineLabel));
            }

            if (StyleContext.Current.LabelVisible && StyleContext.Current.ValueFirst) 
                sb.AppendLine("</div>");
            if (settings.ShowFieldDiv)
                sb.AppendLine("</div>");
            if (StyleContext.Current.BreakLine)
                sb.AppendLine("<div class='clearall'></div>");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());

            return idValueField;
        }

        public static string EnumComboBox(this HtmlHelper helper, string idValueField, Type enumType, object value, ValueLine settings) 
        {
            StringBuilder sb = new StringBuilder();
            List<SelectListItem> items = settings.EnumComboItems;
            if (items == null)
            {
                items = new List<SelectListItem>();
                items.Add(new SelectListItem() { Text = "-", Value = "" });
                items.AddRange(
                    Enum.GetValues(enumType.UnNullify())
                        .Cast<Enum>()
                        .Select(v => new SelectListItem()
                            {
                                Text = v.NiceToString(),
                                Value = v.ToString(),
                                Selected = object.Equals(value, v),
                            })
                    );
            }

            string setTicks = SetTicksFunction(helper, idValueField, settings);
            string reloadOnChangeFunction = GetReloadFunction(helper, settings);

            if (settings.ValueHtmlProps.ContainsKey("onchange"))
                settings.ValueHtmlProps["onchange"] = setTicks + settings.ValueHtmlProps["onchange"] + reloadOnChangeFunction;
            else
                settings.ValueHtmlProps.Add("onchange", setTicks + reloadOnChangeFunction);

            return helper.DropDownList(idValueField, items, settings.ValueHtmlProps);
        }

        public static string DateTimePickerTextbox(this HtmlHelper helper, string idValueField, object value, string dateFormat, ValueLine settings)
        {
            if (settings.ValueHtmlProps.ContainsKey("class"))
                settings.ValueHtmlProps["class"] += " maskedEdit";
            else
                settings.ValueHtmlProps["class"] = " maskedEdit";

            if (settings.DatePickerOptions != null && settings.DatePickerOptions.ShowAge)
                settings.ValueHtmlProps["class"] += " hasAge";

            string setTicks = SetTicksFunction(helper, idValueField, settings);
            string reloadOnChangeFunction = GetReloadFunction(helper, settings);
           
            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange || settings.ReloadFunction.HasText())
            {
                if (settings.ValueHtmlProps.ContainsKey("onblur"))
                    settings.ValueHtmlProps["onblur"] = setTicks + settings.ValueHtmlProps["onblur"] + reloadOnChangeFunction;
                else
                    settings.ValueHtmlProps.Add("onblur", setTicks + reloadOnChangeFunction);
            }

            settings.ValueHtmlProps["size"] = dateFormat.Length;
            string returnString = helper.TextBox(idValueField, value != null ? ((DateTime)value).ToString(dateFormat) : "", settings.ValueHtmlProps) +
                   "\n" +
                   helper.Calendar(idValueField, settings.DatePickerOptions);

            if (settings.DatePickerOptions != null && settings.DatePickerOptions.ShowAge)
                returnString += helper.Span(idValueField + "Age", String.Empty, "age");

            return returnString;
        }

        public static string TextboxInLine(this HtmlHelper helper, string idValueField, string valueStr, ValueLine settings)
        {
            string setTicks = SetTicksFunction(helper, idValueField, settings);
            string reloadOnChangeFunction = GetReloadFunction(helper, settings);

            settings.ValueHtmlProps.Add("autocomplete", "off");
            if (settings.ValueHtmlProps.ContainsKey("onblur"))
                settings.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + setTicks + settings.ValueHtmlProps["onblur"] + reloadOnChangeFunction;
            else
                settings.ValueHtmlProps.Add("onblur", "this.setAttribute('value', this.value); " + setTicks + reloadOnChangeFunction);

            return helper.TextBox(idValueField, valueStr, settings.ValueHtmlProps);
        }

        public static string TextAreaInLine(this HtmlHelper helper, string idValueField, string valueStr, ValueLine settings)
        {
            string setTicks = SetTicksFunction(helper, idValueField, settings);
            string reloadOnChangeFunction = GetReloadFunction(helper, settings);

            settings.ValueHtmlProps.Add("autocomplete", "off");
            if (settings.ValueHtmlProps.ContainsKey("onblur"))
                settings.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + setTicks + settings.ValueHtmlProps["onblur"] + reloadOnChangeFunction;
            else
                settings.ValueHtmlProps.Add("onblur", "this.setAttribute('value', this.value); " + setTicks + reloadOnChangeFunction);

            return helper.TextArea(idValueField, valueStr, settings.ValueHtmlProps);
        }

        public static string CheckBox(this HtmlHelper helper, string idValueField, bool? value, ValueLine settings)
        {
            string setTicks = SetTicksFunction(helper, idValueField, settings);
            string reloadOnChangeFunction = GetReloadFunction(helper, settings);

            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange || settings.ReloadFunction.HasText())
            {
                if (settings.ValueHtmlProps.ContainsKey("onclick"))
                    settings.ValueHtmlProps["onclick"] = setTicks + settings.ValueHtmlProps["onclick"] + reloadOnChangeFunction;
                else
                    settings.ValueHtmlProps.Add("onclick", setTicks + reloadOnChangeFunction);
            }

            return HtmlHelperExtenders.CheckBox(helper, idValueField, value.HasValue ? value.Value : false, !settings.ReadOnly, settings.ValueHtmlProps);
        }

        public static string ValueLine<T>(this HtmlHelper helper, T value, string idValueField, ValueLine options)
        {
            if (options == null || options.LabelText == null)
                throw new ArgumentException(Resources.LabelTextPropertyOfValueLineOptionsMustBeSpecifiedForManualValueLines);

            using (options)
                return helper.ManualValueLine(idValueField, value, options);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, property);

            ValueLine vl = new ValueLine();
            Common.FireCommonTasks(vl, context);

            return SetManualValueLineOptions<S>(helper, context, vl);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<ValueLine> settingsModifier)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, property);

            ValueLine vl = new ValueLine();
            Common.FireCommonTasks(vl, context);
            
            settingsModifier(vl);

            return SetManualValueLineOptions<S>(helper, context, vl);
        }

        private static string SetManualValueLineOptions<S>(HtmlHelper helper, TypeContext<S> context, ValueLine vl)
        {
            if (vl != null)
                using (vl)
                    return helper.ManualValueLine(context.Name, context.Value, vl);
            else
                return helper.ManualValueLine(context.Name, context.Value, vl);
        }

        private static string SetTicksFunction(HtmlHelper helper, string idValueField, ValueLine settings)
        {
            return (helper.ViewData.ContainsKey(ViewDataKeys.Reactive) || settings.ReloadOnChange || settings.ReloadFunction.HasText()) ? 
                "$('#{0}').val(new Date().getTime()); ".Formato(TypeContext.Compose(idValueField, TypeContext.Ticks)) : 
                "";
        }

        private static string GetReloadFunction(HtmlHelper helper, ValueLine settings)
        {
            return (settings.ReloadOnChange || settings.ReloadFunction.HasText()) ?
                settings.ReloadFunction ?? "ReloadEntity('{0}','{1}'); ".Formato("Signum/ReloadEntity", helper.ParentPrefix()) :
                "";
        }
    }

    public class ValueLine : BaseLine 
    { 
        public readonly Dictionary<string, object> ValueHtmlProps = new Dictionary<string, object>(0);
        public ValueLineType? ValueLineType;
        public List<SelectListItem> EnumComboItems;
        public DatePickerOptions DatePickerOptions;

        public ValueLine() { }

        public ValueLine(Dictionary<string, object> valueHtmlProps)
        {
            ValueHtmlProps = valueHtmlProps;
        }

        public override void SetReadOnly()
        {
            ReadOnly = true;
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
                        return ValueLineType.DecimalNumber;
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

        public Dictionary<ValueLineType, Func<HtmlHelper, ValueLineData, string>> constructor = new Dictionary<ValueLineType, Func<HtmlHelper, ValueLineData, string>>()
        {
            {ValueLineType.TextBox, (helper, valueLineData) => helper.TextboxInLine(valueLineData.IdValueField, (string)valueLineData.Value, valueLineData.ValueLineSettings)},
            {ValueLineType.TextArea, (helper, valueLineData) => helper.TextAreaInLine(valueLineData.IdValueField, (string)valueLineData.Value, valueLineData.ValueLineSettings)},
            {ValueLineType.Boolean, (helper, valueLineData) => helper.CheckBox(valueLineData.IdValueField, (bool?)valueLineData.Value, valueLineData.ValueLineSettings)},
            {ValueLineType.Combo, (helper, valueLineData) => helper.EnumComboBox(valueLineData.IdValueField, valueLineData.EnumType, valueLineData.Value, valueLineData.ValueLineSettings)},
            {ValueLineType.DateTime, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, "dd/MM/yyyy HH:mm:ss", valueLineData.ValueLineSettings)},
            {ValueLineType.Date, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, "dd/MM/yyyy", valueLineData.ValueLineSettings)},
            {ValueLineType.Number, (helper, valueLineData) => 
                {
                    valueLineData.ValueLineSettings.ValueHtmlProps.Add("onkeydown", "return validator.number(event);");
                    return helper.TextboxInLine(valueLineData.IdValueField, valueLineData.Value!=null ? valueLineData.Value.ToString() : "", valueLineData.ValueLineSettings);
                }
            },
            {ValueLineType.DecimalNumber, (helper, valueLineData) => 
                {
                    valueLineData.ValueLineSettings.ValueHtmlProps.Add("onkeydown", "return validator.decimalNumber(event);");
                    return helper.TextboxInLine(valueLineData.IdValueField, valueLineData.Value!=null ? valueLineData.Value.ToString() : "", valueLineData.ValueLineSettings);
                }
            }
        };
    }

    public class ValueLineData
    { 
        public string IdValueField { get; private set; }
        public object Value { get; private set; }
        public ValueLine ValueLineSettings { get; private set; }
        public Type EnumType { get; private set; }

        public ValueLineData(string idValueField, object value, ValueLine valueLineSettings)
        {
            IdValueField = idValueField;
            Value = value;
            ValueLineSettings = valueLineSettings;
        }

        public ValueLineData(string idValueField, object value, ValueLine valueLineSettings, Type enumType)
        {
            IdValueField = idValueField;
            Value = value;
            EnumType = enumType;
            ValueLineSettings = valueLineSettings;
        }
    }

    public enum ValueLineType
    {
        Boolean,
        Combo,
        DateTime,
        Date,
        TextBox,
        TextArea,
        Number,
        DecimalNumber,
    };

}
