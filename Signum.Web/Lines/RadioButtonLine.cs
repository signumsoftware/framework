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
        public Dictionary<string, object> LabelHtmlProps;
        public Dictionary<string, object> ValueHtmlProps;
        public string LabelTrue;
        public string LabelFalse;
    }

    public static class RadioButtonLineHelper
    {
        private static string InternalRadioButtonLine(this HtmlHelper helper, string idValueField, bool? value, string trueLabel, string falseLabel, string labelText, Dictionary<string, object> rbHtmlOptions, Dictionary<string, object> labelHtmlOptions)
        {
            StringBuilder sb = new StringBuilder();

            idValueField = helper.GlobalName(idValueField);

            if (StyleContext.Current.LabelVisible)
            {
                if (labelHtmlOptions != null && labelHtmlOptions.Count > 0)
                    sb.AppendLine(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel, labelHtmlOptions));
                else
                    sb.AppendLine(helper.Label(idValueField + "lbl", labelText, idValueField, TypeContext.CssLineLabel));
            }
            if (rbHtmlOptions == null)
                rbHtmlOptions = new Dictionary<string, object>();

            if (StyleContext.Current.ReadOnly)
            {
                rbHtmlOptions.Add("name", idValueField);
                rbHtmlOptions.Add("class", "rbValueLine");
                rbHtmlOptions.Add("disabled", "disabled");
                string rb = helper.RadioButton(idValueField, true, value.HasValue && value.Value, rbHtmlOptions);
                rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_True\"");
                sb.AppendLine(rb);
                sb.AppendLine("<span class='lblRadioTrue'>" + trueLabel + "</span>");
                rb = helper.RadioButton(idValueField, false, value.HasValue && !value.Value, rbHtmlOptions);
                rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_False\"");
                sb.AppendLine(rb);
                sb.AppendLine("<span class='lblRadioFalse'>" + falseLabel + "</span>");
            }
            else
            {
                if (StyleContext.Current.ShowValidationMessage)
                {
                    rbHtmlOptions.Add("name", idValueField);
                    rbHtmlOptions.Add("class", "rbValueLine inlineVal");//inlineVal class tells Javascript code to show Inline Error
                    sb.AppendLine("<div id='{0}' class='valueLine'>".Formato(idValueField));
                    string rb = helper.RadioButton(idValueField, true, value.HasValue && value.Value, rbHtmlOptions); 
                    rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_True\"");
                    sb.AppendLine(rb);
                    sb.AppendLine("<span class='lblRadioTrue'>" + trueLabel + "</span>");
                    rb = helper.RadioButton(idValueField, false, value.HasValue && !value.Value, rbHtmlOptions);
                    rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_False\"");
                    sb.AppendLine(rb);
                    sb.AppendLine("<span class='lblRadioFalse'>" + falseLabel + "</span>");
                    sb.AppendLine("</div>");
                    sb.AppendLine(helper.ValidationMessage(idValueField));
                }
                else
                {
                    rbHtmlOptions.Add("name", idValueField);
                    rbHtmlOptions.Add("class", "rbValueLine");
                    string rb = helper.RadioButton(idValueField, true, value.HasValue && value.Value, rbHtmlOptions);
                    rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_True\"");
                    sb.AppendLine(rb);
                    sb.AppendLine("<span class='lblRadioTrue'>" + trueLabel + "</span>");
                    rb = helper.RadioButton(idValueField, false, value.HasValue && !value.Value, rbHtmlOptions);
                    rb = rb.Replace("id=\"" + idValueField + "\"", "id=\"" + idValueField + "_False\"");
                    sb.AppendLine(rb);
                    sb.AppendLine("<span class='lblRadioFalse'>" + falseLabel + "</span>");
                }
            }
            if (StyleContext.Current.BreakLine)
                sb.AppendLine("<div class='clearall'></div>");

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());

            return idValueField;
        }

        public static string RadioButtonLine<T>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, bool?>> property, RadioButtonLine options)
        {
            TypeContext<bool?> context = (TypeContext<bool?>)Common.WalkExpression(tc, property);
            
            if (options == null)
                return helper.InternalRadioButtonLine(context.Name, context.Value, "true", "false", context.FriendlyName, null, null);
            else
            {
                if (options.StyleContext != null)
                {
                    using (options.StyleContext)
                        return helper.InternalRadioButtonLine(context.Name, context.Value, options.LabelTrue ?? "true", options.LabelFalse ?? "false", options.LabelText ?? context.FriendlyName, options.ValueHtmlProps, options.LabelHtmlProps);
                }
                else
                {
                    return helper.InternalRadioButtonLine(context.Name, context.Value, options.LabelTrue ?? "true", options.LabelFalse ?? "false", options.LabelText ?? context.FriendlyName, options.ValueHtmlProps, options.LabelHtmlProps);
                }
            }
        }

        public static string RadioButtonLine<T>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, bool?>> property, string onclick, RadioButtonLine options)
        {
            if (options.ValueHtmlProps == null)
                options.ValueHtmlProps = new Dictionary<string, object>();
            options.ValueHtmlProps.Add("onclick", onclick);

            return helper.RadioButtonLine(tc, property, options);
        }
    }
}
