using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Text;
using Signum.Utilities;
using Signum.Web;

namespace Signum.Web
{
    public class RadioButtonLine
    {
        public string LabelText;
        public StyleContext StyleContext;
        public Dictionary<string, string> LabelFieldHtmlProps;
        public Dictionary<string, object> ValueFieldHtmlProps;
        public string LabelTrue;
        public string LabelFalse;
    }

    public static class RadioButtonLineHelper
    {
        private static string InternalRadioButtonLine(this HtmlHelper helper, string idValueField, bool? value, string trueLabel, string falseLabel, string labelText, Dictionary<string, object> rbHtmlOptions, Dictionary<string, string> labelHtmlOptions)
        {
            StringBuilder sb = new StringBuilder();

            idValueField = helper.GlobalName(idValueField);

            if (StyleContext.Current.LabelVisible)
            {
                if (labelHtmlOptions != null && labelHtmlOptions.Count > 0)
                    sb.Append(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel, labelHtmlOptions));
                else
                    sb.Append(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel));
            }
            if (rbHtmlOptions == null)
                rbHtmlOptions = new Dictionary<string, object>();

            if (StyleContext.Current.ReadOnly)
            {
                rbHtmlOptions.Add("name", idValueField);
                rbHtmlOptions.Add("class", "valueLine");
                rbHtmlOptions.Add("disabled", "disabled");
                string rb = helper.RadioButton(idValueField, true, value.HasValue && value.Value, rbHtmlOptions);
                rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_True\"");
                sb.Append(rb);
                sb.Append("&nbsp;" + trueLabel + "\n");
                rb = helper.RadioButton(idValueField, false, value.HasValue && !value.Value, rbHtmlOptions);
                rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_False\"");
                sb.Append(rb);
                sb.Append("&nbsp;" + falseLabel);
                sb.Append("\r\n");
            }
            else
            {
                if (StyleContext.Current.ShowValidationMessage)
                {
                    rbHtmlOptions.Add("name", idValueField);
                    rbHtmlOptions.Add("class", "valueLine inlineVal");//inlineVal class tells Javascript code to show Inline Error
                    sb.Append("<div id='" + idValueField + "'>");
                    string rb = helper.RadioButton(idValueField, true, value.HasValue && value.Value, rbHtmlOptions); 
                    rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_True\"");
                    sb.Append(rb);
                    sb.Append("<span class='lblRadioTrue'>" + trueLabel + "</span>\n");
                    rb = helper.RadioButton(idValueField, false, value.HasValue && !value.Value, rbHtmlOptions);
                    rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_False\"");
                    sb.Append(rb);
                    sb.Append("<span class='lblRadioFalse'>" + falseLabel + "</span>\n");
                    sb.Append("&nbsp;");
                    sb.Append("</div>");
                    sb.Append(helper.ValidationMessage(idValueField));
                    sb.Append("\n");
                }
                else
                {
                    rbHtmlOptions.Add("name", idValueField);
                    rbHtmlOptions.Add("class", "valueLine");
                    string rb = helper.RadioButton(idValueField, true, value.HasValue && value.Value, rbHtmlOptions);
                    rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_True\"");
                    sb.Append(rb);
                    sb.Append("&nbsp;" + trueLabel + "\n");
                    rb = helper.RadioButton(idValueField, false, value.HasValue && !value.Value, rbHtmlOptions);
                    rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_False\"");
                    sb.Append(rb);
                    sb.Append("&nbsp;" + falseLabel);
                    sb.Append("\r\n");
                }
            }
            if (StyleContext.Current.BreakLine)
                sb.Append("<div class=\"clearall\"></div>\n");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());

            return idValueField;
        }

        //public static string RadioButtonLine<T>(this HtmlHelper helper, TypeContext<T> tc, string propertyName, string trueLabel, string falseLabel, string labelText)
        //{
        //    T entity = tc.Value;
        //    PropertyInfo pi = typeof(T).GetProperty(propertyName);
        //    bool? value = (bool?)pi.GetValue(entity, null);

        //    string globalName = tc.Name.EndsWith("_") ? tc.Name + pi.Name : tc.Name + "_" + pi.Name;
        //    return helper.InternalRadioButtonLine<T>(globalName, value, trueLabel, falseLabel, labelText, string.Empty);
        //}

        //public static string RadioButtonLine<T>(this HtmlHelper helper, TypeContext<T> tc, string propertyName, string trueLabel, string falseLabel)
        //{
        //    T entity = tc.Value;
        //    PropertyInfo pi = typeof(T).GetProperty(propertyName);
        //    bool? value = (bool?)pi.GetValue(entity, null);

        //    string globalName = tc.Name.EndsWith("_") ? tc.Name + pi.Name : tc.Name + "_" + pi.Name;
        //    return helper.InternalRadioButtonLine<T>(globalName, value, trueLabel, falseLabel, pi.Name, string.Empty);
        //}

        //public static string RadioButtonLine<T>(this HtmlHelper helper, TypeContext<T> tc, string propertyName, string trueLabel, string falseLabel, string labelText, string onchange)
        //{
        //    T entity = tc.Value;
        //    PropertyInfo pi = typeof(T).GetProperty(propertyName);
        //    bool? value = (bool?)pi.GetValue(entity, null);
        //    string globalName = tc.Name.EndsWith("_") ? tc.Name + pi.Name : tc.Name + "_" + pi.Name;
        //    return helper.InternalRadioButtonLine<T>(globalName, value, trueLabel, falseLabel, labelText, onchange);
        //}

        public static string RadioButtonLine<T>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, bool?>> property, RadioButtonLine options)
        {
            TypeContext<bool?> context = (TypeContext<bool?>)Common.WalkExpression(tc, CastToObject(property));
            
            if (options == null)
                return helper.InternalRadioButtonLine(context.Name, context.Value, "true", "false", context.PropertyName, null, null);
            else
            {
                if (options.StyleContext != null)
                {
                    using (options.StyleContext)
                        return helper.InternalRadioButtonLine(context.Name, context.Value, options.LabelTrue ?? "true", options.LabelFalse ?? "false", options.LabelText ?? context.PropertyName, options.ValueFieldHtmlProps, options.LabelFieldHtmlProps);
                }
                else
                {
                    return helper.InternalRadioButtonLine(context.Name, context.Value, options.LabelTrue ?? "true", options.LabelFalse ?? "false", options.LabelText ?? context.PropertyName, options.ValueFieldHtmlProps, options.LabelFieldHtmlProps);
                }
            }
        }

        public static string RadioButtonLine<T>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, bool?>> property, string onclick, RadioButtonLine options)
        {
            if (options.ValueFieldHtmlProps == null)
                options.ValueFieldHtmlProps = new Dictionary<string, object>();
            options.ValueFieldHtmlProps.Add("onclick", onclick);

            return helper.RadioButtonLine(tc, property, options);
        }

        private static Expression<Func<T, object>> CastToObject<T, S>(Expression<Func<T, S>> property)
        {
            // Add the boxing operation, but get a weakly typed expression
            Expression converted = Expression.Convert(property.Body, typeof(object));
            // Use Expression.Lambda to get back to strong typing
            return Expression.Lambda<Func<T, object>>(converted, property.Parameters);
        }
    }
}
