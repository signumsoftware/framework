using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;


namespace Signum.Web
{
    public static class ValueLineHelper
    {
        public static ValueLineConfigurator Configurator = new ValueLineConfigurator();

        private static string ManualValueLine<T>(this HtmlHelper helper, string idValueField, T value, ValueLine settings)
        {
            if (!settings.Visible)
                return null;

            StringBuilder sb = new StringBuilder();
            if (settings.ShowFieldDiv)
                sb.Append("<div class='field'>");
            idValueField = helper.GlobalName(idValueField);

            if (StyleContext.Current.LabelVisible && StyleContext.Current.ValueFirst) sb.Append("<div class='valueFirst'>");
            if (StyleContext.Current.LabelVisible && !StyleContext.Current.ValueFirst)
            {
                if (settings.LabelHtmlProps != null && settings.LabelHtmlProps.Count > 0)
                    sb.Append(helper.Label(idValueField + "lbl", settings.LabelText, idValueField, TypeContext.CssLineLabel, settings.LabelHtmlProps));
                else
                    sb.Append(helper.Label(idValueField + "lbl", settings.LabelText, idValueField, TypeContext.CssLineLabel));
            }
            string valueStr = (value != null) ? value.ToString() : "";
            if (StyleContext.Current.ReadOnly)
            {
                if (value != null && typeof(T).UnNullify() == typeof(DateTime) && settings.ValueLineType != null && settings.ValueLineType == ValueLineType.Date)
                    sb.Append(helper.Span(idValueField, Convert.ToDateTime(value).ToString("dd/MM/yyyy"), "valueLine", typeof(T)));
                else
                    sb.Append(helper.Span(idValueField, value, "valueLine", typeof(T)));
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
                    sb.Append(Configurator.constructor[vltype](helper, new ValueLineData(idValueField, value, settings.ValueHtmlProps, settings.DatePickerOptions, typeof(T), settings.EnumComboItems)));
                    sb.Append("\n");
                    sb.Append("&nbsp;");
                    sb.Append(helper.ValidationMessage(idValueField));
                    sb.Append("\n");
                }
                else
                {
                    if (settings.ValueHtmlProps.ContainsKey("class"))
                        settings.ValueHtmlProps["class"] = "valueLine inlineVal " + settings.ValueHtmlProps["class"];
                    else
                        settings.ValueHtmlProps.Add("class", "valueLine");
                    sb.Append(Configurator.constructor[vltype](helper, new ValueLineData(idValueField, value, settings.ValueHtmlProps, settings.DatePickerOptions, typeof(T), settings.EnumComboItems)));
                    sb.Append("\r\n");
                }
            }
            if (StyleContext.Current.LabelVisible && StyleContext.Current.ValueFirst)
            {
                if (settings.LabelHtmlProps != null && settings.LabelHtmlProps.Count > 0)
                    sb.Append(helper.Label(idValueField + "lbl", settings.LabelText, idValueField, TypeContext.CssLineLabel, settings.LabelHtmlProps));
                else
                    sb.Append(helper.Label(idValueField + "lbl", settings.LabelText, idValueField, TypeContext.CssLineLabel));
            }

            if (StyleContext.Current.LabelVisible && StyleContext.Current.ValueFirst) sb.Append("</div>");

            if (settings.ShowFieldDiv)
                sb.Append("</div>");
            if (StyleContext.Current.BreakLine)
                sb.Append("<div class='clearall'></div>\n");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());

            return idValueField;
        }

        public static string EnumComboBox(this HtmlHelper helper, string idValueField, Type enumType, object value, Dictionary<string, object> htmlProperties, List<SelectListItem> enumComboItems) 
        {
            StringBuilder sb = new StringBuilder();
            List<SelectListItem> items = enumComboItems;
            if (items == null)
            {
                items = new List<SelectListItem>();
                items.Add(new SelectListItem() { Text = "-", Value = "", Selected = true });
                items.AddRange(
                    Enum.GetValues(enumType.UnNullify())
                        .Cast<Enum>()
                        .Select(v => new SelectListItem()
                            {
                                Text = EnumExtensions.NiceToString(v),
                                Value = v.ToString(),
                                Selected = object.Equals(value, v),
                            })
                    );
            }
            return helper.DropDownList(idValueField, items, htmlProperties);
        }

        public static string DateTimePickerTextbox(this HtmlHelper helper, string idValueField, object value, string dateFormat, Dictionary<string, object> htmlProperties, DatePickerOptions datepickerOptions)
        {
            if (htmlProperties.ContainsKey("class"))
                htmlProperties["class"] += " maskedEdit";
            else
                htmlProperties["class"] = " maskedEdit";

            if (datepickerOptions != null && datepickerOptions.ShowAge)
                htmlProperties["class"] += " hasAge";

            htmlProperties["size"] = dateFormat.Length;
            string returnString = helper.TextBox(idValueField, value != null ? ((DateTime)value).ToString(dateFormat) : "",  htmlProperties)+
                   "\n" + 
                   helper.Calendar(idValueField, datepickerOptions);

            if (datepickerOptions != null && datepickerOptions.ShowAge)
                returnString += helper.Span(idValueField + "Age", String.Empty, "age");

            return returnString;
        }

        public static string TextboxInLine(this HtmlHelper helper, string idValueField, string valueStr, Dictionary<string, object> htmlProperties)
        {
            htmlProperties.Add("autocomplete", "off");
            if (htmlProperties.ContainsKey("onblur"))
                htmlProperties["onblur"] = "this.setAttribute('value', this.value); " + htmlProperties["onblur"];
            else
                htmlProperties.Add("onblur", "this.setAttribute('value', this.value);");

            return helper.TextBox(idValueField, valueStr, htmlProperties);
        }

        public static string CheckBox(this HtmlHelper helper, string idValueField, bool? value, Dictionary<string, object> htmlProperties)
        { 
            return System.Web.Mvc.Html.InputExtensions.CheckBox(helper, idValueField, value.HasValue ? value.Value : false, htmlProperties) + "\n";
        }

        public static string ValueLine<T>(this HtmlHelper helper, T value, string idValueField, ValueLine options)
        {
            if (options == null || options.LabelText == null)
                throw new ArgumentException("LabelText property of ValueLineOptions must be specified for Manual Value Lines");

            if (options.StyleContext != null)
            {
                using (options.StyleContext)
                    return helper.ManualValueLine(idValueField, value, options);
            }
            else
                return helper.ManualValueLine(idValueField, value, options);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, property);

            ValueLine vl = new ValueLine();
            Common.FireCommonTasks(vl, typeof(T), context);

            return SetManualValueLineOptions<S>(helper, context, vl);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<ValueLine> settingsModifier)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, property);

            ValueLine vl = new ValueLine();
            Common.FireCommonTasks(vl, typeof(T), context);
            
            settingsModifier(vl);

            return SetManualValueLineOptions<S>(helper, context, vl);
        }

        private static string SetManualValueLineOptions<S>(HtmlHelper helper, TypeContext<S> context, ValueLine vl)
        {
            if (vl != null && vl.StyleContext != null)
            {
                using (vl.StyleContext)
                    return helper.ManualValueLine(context.Name, context.Value, vl);
            }
            else
                return helper.ManualValueLine(context.Name, context.Value, vl);
        }
    }

    public class ValueLine : BaseLine 
    { 
        public readonly Dictionary<string, object> ValueHtmlProps = new Dictionary<string, object>(0);
        public ValueLineType? ValueLineType;
        public List<SelectListItem> EnumComboItems;
        public DatePickerOptions DatePickerOptions;
        public bool ShowFieldDiv = false;

        public override void SetReadOnly()
        {
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
            {ValueLineType.TextBox, (helper, valueLineData) => helper.TextboxInLine(valueLineData.IdValueField, (string)valueLineData.Value, valueLineData.HtmlProperties)},
            {ValueLineType.Boolean, (helper, valueLineData) => helper.CheckBox(valueLineData.IdValueField, (bool?)valueLineData.Value, valueLineData.HtmlProperties)},
            {ValueLineType.Combo, (helper, valueLineData) => helper.EnumComboBox(valueLineData.IdValueField, valueLineData.EnumType, valueLineData.Value, valueLineData.HtmlProperties, valueLineData.EnumComboItems)},
            {ValueLineType.DateTime, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, "dd/MM/yyyy HH:mm:ss", valueLineData.HtmlProperties, valueLineData.DatePickerOptions)},
            {ValueLineType.Date, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, "dd/MM/yyyy", valueLineData.HtmlProperties, valueLineData.DatePickerOptions)},
            {ValueLineType.Number, (helper, valueLineData) => 
                {
                    valueLineData.HtmlProperties.Add("onkeydown", "return validator.number(event)");
                    return helper.TextboxInLine(valueLineData.IdValueField, valueLineData.Value!=null ? valueLineData.Value.ToString() : "", valueLineData.HtmlProperties);
                }
            },
            {ValueLineType.DecimalNumber, (helper, valueLineData) => 
                {
                    valueLineData.HtmlProperties.Add("onkeydown", "return validator.decimalNumber(event)");
                    return helper.TextboxInLine(valueLineData.IdValueField, valueLineData.Value!=null ? valueLineData.Value.ToString() : "", valueLineData.HtmlProperties);
                }
            }
        };

        private static string numberKeyCode =
            "(event.keyCode >= 48 && event.keyCode <= 57) || " +  //0-9
            "(event.keyCode >= 96 && event.keyCode <= 105) ";  //NumPad 0-9

        private static string standardKeyCode = 
            "(event.keyCode == 8) || " +   //BackSpace
            "(event.keyCode == 9) || " +   //Tab
            "(event.keyCode == 12) || " +  //Clear
            "(event.keyCode == 27) || " +  //Escape
            "(event.keyCode == 37) || " +  //Left
            "(event.keyCode == 39) || " +  //Right
            "(event.keyCode == 46) || " +  //Delete
            "(event.keyCode == 36) || " +  //Home
            "(event.keyCode == 35) ";     //End

        private static string decimalKeyCode = 
            "(event.keyCode == 110) || " + //NumPad Decimal
            "(event.keyCode == 190) || " + //.
            "(event.keyCode == 188) ";     //, 
        
        private static string negativeKeyCode = 
            "(event.keyCode == 109) || " + //NumPad -
            "(event.keyCode == 189) ";     //-

        private static string onKeyDownNumber = "return (" + numberKeyCode + " || " + standardKeyCode + " || " + negativeKeyCode + " );";
        private static string onKeyDownDecimalNumber = "return (" + numberKeyCode + " || " + standardKeyCode + " || " + negativeKeyCode + " || " + decimalKeyCode + " );";
    }

    public class ValueLineData
    { 
        public string IdValueField { get; private set; }
        public object Value { get; private set; }
        public Dictionary<string, object> HtmlProperties { get; private set; }
        
        public Type EnumType { get; private set; }
        public List<SelectListItem> EnumComboItems { get; private set; }

        public DatePickerOptions DatePickerOptions;

        public ValueLineData(string idValueField, object value, Dictionary<string, object> htmlProperties, DatePickerOptions datePickerOptions)
        {
            IdValueField = idValueField;
            Value = value;
            HtmlProperties = htmlProperties;
            DatePickerOptions = datePickerOptions;
        }

        public ValueLineData(string idValueField, object value, Dictionary<string, object> htmlProperties, DatePickerOptions datePickerOptions, Type enumType, List<SelectListItem> enumComboItems)
        {
            IdValueField = idValueField;
            Value = value;
            HtmlProperties = htmlProperties;
            DatePickerOptions = datePickerOptions;
            EnumType = enumType;
            EnumComboItems = enumComboItems;
        }
    }

    public enum ValueLineType
    {
        Boolean,
        Combo,
        DateTime,
        Date,
        TextBox,
        Number,
        DecimalNumber,
    };

}
