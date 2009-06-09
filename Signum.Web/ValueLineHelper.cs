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

        private static string ManualValueLine<T>(this HtmlHelper helper, string idValueField, T value, string labelText, Dictionary<string, object> valueFieldHtmlProps)
        {
            StringBuilder sb = new StringBuilder();

            idValueField = helper.GlobalName(idValueField);

            if (StyleContext.Current.LabelVisible)
            {
                sb.Append("<div>");
                sb.Append(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel));
                //sb.Append(helper.Span(idValueField + "lbl", labelText, TypeContext.CssLineLabel));
            }
            string valueStr = (value != null) ? value.ToString() : "";
            if (StyleContext.Current.ReadOnly)
                sb.Append(helper.Span(idValueField, valueStr, "valueLine"));
            else
            {
                ValueLineType vlType = Configurator.GetDefaultValueLineType(typeof(T));
                if (valueFieldHtmlProps == null)
                    valueFieldHtmlProps = new Dictionary<string, object>();
                if (StyleContext.Current.ShowValidationMessage)
                {
                    valueFieldHtmlProps.Add("class", "valueLine inlineVal"); //inlineVal class tells Javascript code to show Inline Error
                    sb.Append(Configurator.constructor[vlType](helper, new ValueLineData(idValueField, value, valueFieldHtmlProps, typeof(T)))); 
                    sb.Append("\n");
                    sb.Append("&nbsp;");
                    sb.Append(helper.ValidationMessage(idValueField));
                    sb.Append("\n");
                }
                else
                {
                    valueFieldHtmlProps.Add("class", "valueLine");
                    sb.Append(Configurator.constructor[vlType](helper, new ValueLineData(idValueField, value, valueFieldHtmlProps, typeof(T)))); 
                    sb.Append("\r\n");
                }
            }
            if (StyleContext.Current.LabelVisible)
                sb.Append("</div>");
            if (StyleContext.Current.BreakLine)
                sb.Append("<div class=\"clearall\"></div>\n");

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
                            Text = v.ToString().Replace("_", " "), 
                            Value = v.ToString(), 
                            Selected = object.Equals(value,v),
                        })
                );
            
            return helper.DropDownList(idValueField, items, htmlProperties);
        }

        public static string DateTimePickerTextbox(this HtmlHelper helper, string idValueField, object value, Dictionary<string, object> htmlProperties)
        {
            return helper.TextBox(idValueField, value != null ? value.ToString() : "", htmlProperties) + 
                   "\n" + 
                   helper.Calendar(idValueField);
        }

        public static string TextboxInLine(this HtmlHelper helper, string idValueField, string valueStr, Dictionary<string, object> htmlProperties)
        {
            htmlProperties.Add("autocomplete", "off");
            htmlProperties.Add("onblur", "this.setAttribute('value', this.value);");
            return helper.TextBox(idValueField, valueStr, htmlProperties);
        }

        public static string CheckBox(this HtmlHelper helper, string idValueField, bool? value, Dictionary<string, object> htmlProperties)
        { 
            return System.Web.Mvc.Html.InputExtensions.CheckBox(helper, idValueField, value.HasValue ? value.Value : false, htmlProperties) + "\n";
        }

        public static string ValueLine<T>(this HtmlHelper helper, string labelText, T value, string idValueField, StyleContext styleContext)
        {
            using (styleContext)
                return helper.ManualValueLine(idValueField, value, labelText, null); 
        }

        public static string ValueLine<T>(this HtmlHelper helper, string labelText, T value, string idValueField, StyleContext styleContext, Dictionary<string, object> valueFieldHtmlProps)
        {
            using (styleContext)
                return helper.ManualValueLine(idValueField, value, labelText, valueFieldHtmlProps);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            return helper.ManualValueLine(context.Name, context.Value, context.PropertyName, null);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Dictionary<string, object> valueFieldHtmlProps)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            return helper.ManualValueLine(context.Name, context.Value, context.PropertyName, valueFieldHtmlProps);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, string labelText)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            return helper.ManualValueLine(context.Name, context.Value, labelText, null);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, string labelText, Dictionary<string, object> valueFieldHtmlProps)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            return helper.ManualValueLine(context.Name, context.Value, labelText, valueFieldHtmlProps);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, StyleContext styleContext)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            using (styleContext)
                return helper.ManualValueLine(context.Name, context.Value, context.PropertyName, null);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, StyleContext styleContext, Dictionary<string, object> valueFieldHtmlProps)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            using (styleContext)
                return helper.ManualValueLine(context.Name, context.Value, context.PropertyName, valueFieldHtmlProps);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, string labelText, StyleContext styleContext)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            using (styleContext)
                return helper.ManualValueLine(context.Name, context.Value, labelText, null);
        }

        public static string ValueLine<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, string labelText, StyleContext styleContext, Dictionary<string, object> valueFieldHtmlProps)
        {
            Type t = typeof(S);
            TypeContext<S> context = (TypeContext<S>)Common.WalkExpression(tc, CastToObject(property));

            using (styleContext)
                return helper.ManualValueLine(context.Name, context.Value, labelText, valueFieldHtmlProps);
        }

        private static Expression<Func<T, object>> CastToObject<T, S>(Expression<Func<T, S>> property)
        {
            // Add the boxing operation, but get a weakly typed expression
            Expression converted = Expression.Convert(property.Body, typeof(object));
            // Use Expression.Lambda to get back to strong typing
            return Expression.Lambda<Func<T, object>>(converted, property.Parameters);
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
                        return ValueLineType.Calendar;
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
            {ValueLineType.Calendar, (helper, valueLineData) => helper.DateTimePickerTextbox(valueLineData.IdValueField, valueLineData.Value, valueLineData.HtmlProperties)},
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

        private static string onKeyDownNumber = "return (" + numberKeyCode + " || " + standardKeyCode + " || " + negativeKeyCode + " )";
        private static string onKeyDownDecimalNumber = "return (" + numberKeyCode + " || " + standardKeyCode + " || " + negativeKeyCode + " || " + decimalKeyCode + " )";
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
        Calendar,
        TextBox,
        Number,
        DecimalNumber,
    };

}
