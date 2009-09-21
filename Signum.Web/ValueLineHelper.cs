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

            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                sb.Append("<input type='hidden' id='{0}' name='{0}' value='0' />".Formato(idValueField + TypeContext.Separator + TypeContext.Ticks));

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
                    sb.Append(Configurator.constructor[vltype](helper, new ValueLineData(idValueField, value, settings, typeof(T))));
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
                    sb.Append(Configurator.constructor[vltype](helper, new ValueLineData(idValueField, value, settings, typeof(T))));
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

        public static string EnumComboBox(this HtmlHelper helper, string idValueField, Type enumType, object value, ValueLine settings) 
        {
            StringBuilder sb = new StringBuilder();
            List<SelectListItem> items = settings.EnumComboItems;
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

            string reloadOnChangeFunction = "''";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                reloadOnChangeFunction = settings.ReloadOnChangeFunction ?? "ReloadEntity('{0}','{1}'); ".Formato("Signum.aspx/ReloadEntity", helper.ParentPrefix());
            if (reloadOnChangeFunction != "''")
            {
                if (settings.ValueHtmlProps.ContainsKey("onblur"))
                    settings.ValueHtmlProps["onblur"] = reloadOnChangeFunction + settings.ValueHtmlProps["onblur"];
                else
                    settings.ValueHtmlProps.Add("onblur", reloadOnChangeFunction);
            }

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

            string reloadOnChangeFunction = "''";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                reloadOnChangeFunction = settings.ReloadOnChangeFunction ?? "ReloadEntity('{0}','{1}'); ".Formato("Signum.aspx/ReloadEntity", helper.ParentPrefix());
            if (reloadOnChangeFunction != "''")
            {
                if (settings.ValueHtmlProps.ContainsKey("onblur"))
                    settings.ValueHtmlProps["onblur"] = reloadOnChangeFunction + settings.ValueHtmlProps["onblur"];
                else
                    settings.ValueHtmlProps.Add("onblur", reloadOnChangeFunction);
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
            string reloadOnChangeFunction = "";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
            {
                reloadOnChangeFunction = "$('#{0}').val(new Date().getTime()); ".Formato(idValueField + TypeContext.Separator + TypeContext.Ticks) +
                                         (settings.ReloadOnChangeFunction ?? "ReloadEntity('{0}','{1}'); ".Formato("Signum.aspx/ReloadEntity", helper.ParentPrefix()));
            }
            
            settings.ValueHtmlProps.Add("autocomplete", "off");
            if (settings.ValueHtmlProps.ContainsKey("onblur"))
                settings.ValueHtmlProps["onblur"] = "this.setAttribute('value', this.value); " + reloadOnChangeFunction + settings.ValueHtmlProps["onblur"];
            else
                settings.ValueHtmlProps.Add("onblur", "this.setAttribute('value', this.value); " + reloadOnChangeFunction);

            return helper.TextBox(idValueField, valueStr, settings.ValueHtmlProps);
        }

        public static string CheckBox(this HtmlHelper helper, string idValueField, bool? value, ValueLine settings)
        {
            string reloadOnChangeFunction = "''";
            if (settings.ReloadOnChange || settings.ReloadOnChangeFunction.HasText())
                reloadOnChangeFunction = settings.ReloadOnChangeFunction ?? "ReloadEntity('{0}','{1}'); ".Formato("Signum.aspx/ReloadEntity", helper.ParentPrefix());
            if (reloadOnChangeFunction != "''")
            {
                if (settings.ValueHtmlProps.ContainsKey("onblur"))
                    settings.ValueHtmlProps["onblur"] = reloadOnChangeFunction + settings.ValueHtmlProps["onblur"];
                else
                    settings.ValueHtmlProps.Add("onblur", reloadOnChangeFunction);
            }

            return System.Web.Mvc.Html.InputExtensions.CheckBox(helper, idValueField, value.HasValue ? value.Value : false, settings.ValueHtmlProps) + "\n";
        }

        public static string ValueLine<T>(this HtmlHelper helper, T value, string idValueField, ValueLine options)
        {
            if (options == null || options.LabelText == null)
                throw new ArgumentException("LabelText property of ValueLineOptions must be specified for Manual Value Lines");

            using (options)
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
            if (vl != null)
                using (vl)
                    return helper.ManualValueLine(context.Name, context.Value, vl);
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
            {ValueLineType.TextBox, (helper, valueLineData) => helper.TextboxInLine(valueLineData.IdValueField, (string)valueLineData.Value, valueLineData.ValueLineSettings)},
            {ValueLineType.Boolean, (helper, valueLineData) => helper.CheckBox(valueLineData.IdValueField, (bool?)valueLineData.Value, valueLineData.ValueLineSettings)},
            {ValueLineType.Combo, (helper, valueLineData) => helper.EnumComboBox(valueLineData.IdValueField, valueLineData.EnumType, valueLineData.Value, valueLineData.ValueLineSettings)},
            {ValueLineType.DateTime, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, "dd/MM/yyyy HH:mm:ss", valueLineData.ValueLineSettings)},
            {ValueLineType.Date, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, "dd/MM/yyyy", valueLineData.ValueLineSettings)},
            {ValueLineType.Number, (helper, valueLineData) => 
                {
                    valueLineData.ValueLineSettings.ValueHtmlProps.Add("onkeydown", "return validator.number(event)");
                    return helper.TextboxInLine(valueLineData.IdValueField, valueLineData.Value!=null ? valueLineData.Value.ToString() : "", valueLineData.ValueLineSettings);
                }
            },
            {ValueLineType.DecimalNumber, (helper, valueLineData) => 
                {
                    valueLineData.ValueLineSettings.ValueHtmlProps.Add("onkeydown", "return validator.decimalNumber(event)");
                    return helper.TextboxInLine(valueLineData.IdValueField, valueLineData.Value!=null ? valueLineData.Value.ToString() : "", valueLineData.ValueLineSettings);
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
        Number,
        DecimalNumber,
    };

}
