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

        private static string ManualValueLine<T>(this HtmlHelper helper, string idValueField, T value, string labelText, Dictionary<string, object> ValueHtmlProps, ValueLineType? valueLineType, Dictionary<string, object> LabelHtmlProps)
        {
            StringBuilder sb = new StringBuilder();

            idValueField = helper.GlobalName(idValueField);

           // if (StyleContext.Current.LabelVisible) sb.Append("<div>");
            if (StyleContext.Current.LabelVisible && StyleContext.Current.ValueFirst) sb.Append("<div class='valueFirst'>");
            if (StyleContext.Current.LabelVisible && !StyleContext.Current.ValueFirst)
            {
                if (LabelHtmlProps != null && LabelHtmlProps.Count > 0)
                    sb.Append(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel, LabelHtmlProps));
                else
                    sb.Append(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel));
            }
            string valueStr = (value != null) ? value.ToString() : "";
            if (StyleContext.Current.ReadOnly)
                sb.Append(helper.Span(idValueField, value, "valueLine", typeof(T)));
            else
            {
                ValueLineType vltype = (valueLineType.HasValue) ? 
                    valueLineType.Value : 
                    Configurator.GetDefaultValueLineType(typeof(T));

                if (ValueHtmlProps == null)
                    ValueHtmlProps = new Dictionary<string, object>();

                if (StyleContext.Current.ShowValidationMessage)
                {
                    if (ValueHtmlProps.ContainsKey("class"))
                        ValueHtmlProps["class"] = "valueLine inlineVal " + ValueHtmlProps["class"];
                    else
                        ValueHtmlProps.Add("class", "valueLine inlineVal"); //inlineVal class tells Javascript code to show Inline Error
                    sb.Append(Configurator.constructor[vltype](helper, new ValueLineData(idValueField, value, ValueHtmlProps, typeof(T)))); 
                    sb.Append("\n");
                    sb.Append("&nbsp;");
                    sb.Append(helper.ValidationMessage(idValueField));
                    sb.Append("\n");
                }
                else
                {
                    if (ValueHtmlProps.ContainsKey("class"))
                        ValueHtmlProps["class"] = "valueLine inlineVal " + ValueHtmlProps["class"];
                    else
                        ValueHtmlProps.Add("class", "valueLine");
                    sb.Append(Configurator.constructor[vltype](helper, new ValueLineData(idValueField, value, ValueHtmlProps, typeof(T)))); 
                    sb.Append("\r\n");
                }
            }
            if (StyleContext.Current.LabelVisible && StyleContext.Current.ValueFirst)
            {
                if (LabelHtmlProps != null && LabelHtmlProps.Count > 0)
                    sb.Append(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel, LabelHtmlProps));
                else
                    sb.Append(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel));
            }
           // if (StyleContext.Current.LabelVisible)
            //    sb.Append("</div>");
            if (StyleContext.Current.LabelVisible && StyleContext.Current.ValueFirst) sb.Append("</div>");
            if (StyleContext.Current.BreakLine)
                sb.Append("<div class='clearall'></div>\n");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());

            return idValueField;
        }

        public static string EnumComboBox(this HtmlHelper helper, string idValueField, Type enumType, object value, Dictionary<string, object> htmlProperties) 
        {
            StringBuilder sb = new StringBuilder();
            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem() { Text = "-", Value = "", Selected = true });
            items.AddRange(
                Enum.GetValues(enumType.UnNullify())
                    .Cast<object>()
                    .Select(v => new SelectListItem()
                        {
                            Text = EnumExtensions.NiceToString(v),
                            Value = v.ToString(), 
                            Selected = object.Equals(value,v),
                        })
                );
            
            return helper.DropDownList(idValueField, items, htmlProperties);
        }

        public static string DateTimePickerTextbox(this HtmlHelper helper, string idValueField, object value, string dateFormat, Dictionary<string, object> htmlProperties)
        {
            return helper.TextBox(idValueField, value != null ? ((DateTime)value).ToString(dateFormat) : "", htmlProperties) + 
                   "\n" + 
                   helper.Calendar(idValueField);
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
                    return helper.ManualValueLine(idValueField, value, options.LabelText, options.ValueHtmlProps, options.ValueLineType, options.LabelHtmlProps);
            }
            else
            {
                return helper.ManualValueLine(idValueField, value, options.LabelText, options.ValueHtmlProps, options.ValueLineType, options.LabelHtmlProps);
            }
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            ValueLine vl = new ValueLine();
            Common.FireCommonTasks(vl, typeof(T), context);

            return SetManualValueLineOptions<S>(helper, context, vl);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<ValueLine> settingsModifier)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            ValueLine vl = new ValueLine();
            Common.FireCommonTasks(vl, typeof(T), context);
            
            settingsModifier(vl);

            return SetManualValueLineOptions<S>(helper, context, vl);
        }

        private static string SetManualValueLineOptions<S>(HtmlHelper helper, TypeContext<S> context, ValueLine vl)
        {
            if (vl == null)
                return helper.ManualValueLine(context.Name, context.Value, context.FriendlyName, null, null, null);
            else
            {
                if (vl.StyleContext != null)
                {
                    using (vl.StyleContext)
                        return helper.ManualValueLine(context.Name, context.Value, vl.LabelText ?? context.FriendlyName, vl.ValueHtmlProps, vl.ValueLineType, vl.LabelHtmlProps);
                }
                else
                {
                    return helper.ManualValueLine(context.Name, context.Value, vl.LabelText ?? context.FriendlyName, vl.ValueHtmlProps, vl.ValueLineType, vl.LabelHtmlProps);
                }
            }
        }

        private static Expression<Func<T, object>> CastToObject<T, S>(Expression<Func<T, S>> property)
        {
            // Add the boxing operation, but get a weakly typed expression
            Expression converted = Expression.Convert(property.Body, typeof(object));
            // Use Expression.Lambda to get back to strong typing
            return Expression.Lambda<Func<T, object>>(converted, property.Parameters);
        }
    }

    public class ValueLine
    { 
        public string LabelText;
        public StyleContext StyleContext;
        public readonly Dictionary<string, object> LabelHtmlProps = new Dictionary<string,object>(0);
        public readonly Dictionary<string, object> ValueHtmlProps = new Dictionary<string, object>(0);
        public ValueLineType? ValueLineType;
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
            {ValueLineType.Combo, (helper, valueLineData) => helper.EnumComboBox(valueLineData.IdValueField, valueLineData.EnumType, valueLineData.Value, valueLineData.HtmlProperties)},
            {ValueLineType.DateTime, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, "dd/MM/yyyy hh:mm:ss", valueLineData.HtmlProperties)},
            {ValueLineType.Date, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, "dd/MM/yyyy", valueLineData.HtmlProperties)},
            {ValueLineType.Number, (helper, valueLineData) => 
                {
                    valueLineData.HtmlProperties.Add("onkeydown", onKeyDownNumber);
                    return helper.TextboxInLine(valueLineData.IdValueField, valueLineData.Value!=null ? valueLineData.Value.ToString() : "", valueLineData.HtmlProperties);
                }
            },
            {ValueLineType.DecimalNumber, (helper, valueLineData) => 
                {
                    valueLineData.HtmlProperties.Add("onkeydown", onKeyDownDecimalNumber);
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

        public ValueLineData(string idValueField, object value, Dictionary<string, object> htmlProperties)
        {
            IdValueField = idValueField;
            Value = value;
            HtmlProperties = htmlProperties;
        }

        public ValueLineData(string idValueField, object value, Dictionary<string, object> htmlProperties, Type enumType)
        {
            IdValueField = idValueField;
            Value = value;
            HtmlProperties = htmlProperties;
            EnumType = enumType;
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
