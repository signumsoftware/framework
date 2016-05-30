using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using System.Reflection;
using System.IO;
using System.Web;
using System.Web.Routing;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;
using System.Web.Script.Serialization;
using System.Web.WebPages;
using Signum.Engine;
using Signum.Web.Controllers;
using Newtonsoft.Json;

namespace Signum.Web
{
    public static class HtmlHelperExtenders
    {
        public static MvcHtmlString ValidationSummaryAjax(this HtmlHelper html)
        {
            return new HtmlTag("div", "sfGlobalValidationSummary").ToHtml();
        }

        public static MvcHtmlString ValidationSummaryAjax(this HtmlHelper html, Context context)
        {
            return new HtmlTag("div", context.Compose("sfGlobalValidationSummary")).ToHtml();
        }

        public static MvcHtmlString ValudationSummaryStatic(this HtmlHelper html)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("div", "sfGlobalValidationSummary")))
            {
                if (html.ViewData.ModelState.Any(x => x.Value.Errors.Any()))
                {
                    using (sb.SurroundLine(new HtmlTag("ul").Class("validaton-summary alert alert-danger")))
                    {
                        foreach (var str in html.ViewData.ModelState.SelectMany(a => a.Value.Errors))
                        {
                            sb.Add(new HtmlTag("li").SetInnerText(str.ErrorMessage));
                        }
                    }
                }
            }
            return sb.ToHtml();
        }

        public static MvcHtmlString FormGroupStatic(this HtmlHelper html, Context context, string controlId, string label, string text)
        {
            var span = html.FormControlStatic(context, controlId, text);
            return FormGroup(html, context, controlId, label, span);
        }

        public static MvcHtmlString FormControlStatic(this HtmlHelper html, Context context, string controlId, string text, IDictionary<string, object> htmlProps = null)
        {   
            return new HtmlTag("p").Id(controlId).SetInnerText(text)
                .Class(context.FormControlStaticAsFormControlReadonly ? "form-control readonly" :  "form-control-static")
                .Attrs(htmlProps).ToHtml();
        }

        public static MvcHtmlString FormGroup(this HtmlHelper html, Context context, string controlId, string label, Func<object, HelperResult> value)
        {
            return html.FormGroup(context, controlId, label.FormatHtml(), value);
        }

        public static MvcHtmlString FormGroup(this HtmlHelper html, Context context, string controlId, MvcHtmlString label, Func<object, HelperResult> value)
        {
            StringWriter writer = new StringWriter();
            value(null).WriteTo(writer);
            return FormGroup(html, context, controlId, label, MvcHtmlString.Create(writer.ToString()));
        }

        public static MvcHtmlString FormGroup(this HtmlHelper html, Context context, string controlId, string label, MvcHtmlString value)
        {
            return html.FormGroup(context, controlId, label.FormatHtml(), value);
        }

        public static MvcHtmlString FormGroup(this HtmlHelper html, Context context, string controlId, MvcHtmlString label, MvcHtmlString value)
        {
            if (context.FormGroupStyle == FormGroupStyle.None)
                return value;

            var form = context is LineBase ? ((LineBase)context).FormGroupHtmlProps : null;

            var formSize = context.FormGroupSizeCss;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("div").Class("form-group").Class(formSize).Attrs(form)))
            {
                var lbl = new HtmlTag("label").Attr("for", controlId).InnerHtml(label);

                if (context.FormGroupStyle == FormGroupStyle.SrOnly)
                    lbl.Class("sr-only");
                else if (context.FormGroupStyle == FormGroupStyle.LabelColumns)
                    lbl.Class("control-label").Class(context.LabelColumns.ToString());

                lbl.Attrs(context is LineBase ? ((LineBase)context).LabelHtmlProps : null);

                if (context.FormGroupStyle != FormGroupStyle.BasicDown)
                    sb.AddLine(lbl);

                using (context.FormGroupStyle == FormGroupStyle.LabelColumns ? sb.SurroundLine(new HtmlTag("div").Class(context.ValueColumns.ToString())) : null)
                {
                    sb.AddLine(value);
                }

                if (context.FormGroupStyle == FormGroupStyle.BasicDown)
                    sb.AddLine(lbl);
            }

            return sb.ToHtml();
        }
        

        public static MvcHtmlString CheckBox(this HtmlHelper html, string name, bool isChecked, bool enabled, IDictionary<string, object> htmlAttributes = null)
        {
            if (htmlAttributes == null)
                htmlAttributes = new Dictionary<string, object>();

            if (enabled)
                return html.CheckBox(name, isChecked, htmlAttributes); //MVC CheckBox only applied disabled=disabled to the checkbox, not to the hidden value
            else
            {
                HtmlTag checkbox = new HtmlTag("input")
                    .Id(name)
                    .Attrs(new
                    {
                        type = "checkbox",
                        name = name,
                        value = (isChecked ? "true" : "false"),
                        disabled = "disabled"
                    })
                    .Attrs(htmlAttributes);

                if (isChecked)
                    checkbox.Attr("checked", "checked");

                HtmlTag hidden = new HtmlTag("input")
                        .Attrs(new
                        {
                            type = "hidden",
                            name = name,
                            value = (isChecked ? "true" : "false"),
                            disabled = "disabled"
                        });

                return checkbox.ToHtmlSelf().Concat(hidden.ToHtmlSelf());
            }
        }

        public static TabContainer Tabs(this HtmlHelper html, TypeContext ctx, string containerId = "tabs")
        {
            return new TabContainer(html, ctx, containerId);
        }


        /// <summary>
        /// Returns a "label" label that is used to show the name of a field in a form
        /// </summary>
        /// <param name="html"></param>
        /// <param name="id">The id of the label</param>
        /// <param name="innerText">The text of the label, which will be shown</param>
        /// <param name="idField">The id of the field that the label is describing</param>
        /// <param name="cssClass">The class that will be appended to the label</param>
        /// <returns>An HTML string representing a "label" label</returns>
        public static MvcHtmlString Label(this HtmlHelper html, string id, string innerText, string idField, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("label", id)
                                    .Attr("for", idField)
                                    .Attrs(htmlAttributes)
                                    .Class(cssClass)
                                    .SetInnerText(innerText)
                                    .ToHtml();
        }

        public static MvcHtmlString Label(this HtmlHelper html, string id, string innerText, string idField, string cssClass)
        {
            return html.Label(id, innerText, idField, cssClass, null);
        }

        public static MvcHtmlString Span(this HtmlHelper html, string id, string innerText, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("span", id)
                        .Attrs(htmlAttributes)
                        .Class(cssClass)
                        .SetInnerText(innerText)
                        .ToHtml();
        }

        public static MvcHtmlString Span(this HtmlHelper html, string id, string value, string cssClass)
        {
            return Span(html, id, value, cssClass, null);
        }

        public static MvcHtmlString Span(this HtmlHelper html, string id, string value)
        {
            return Span(html, id, value, null, null);
        }

        public static MvcHtmlString Href(this HtmlHelper html, string url, string text)
        {
            return new HtmlTag("a")
                        .Attr("href", url)
                        .SetInnerText(text)
                        .ToHtml();
        }

        public static MvcHtmlString Href(this HtmlHelper html, string id, string innerText, string url, string title, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            HtmlTag href = new HtmlTag("a", id)
                        .Attrs(htmlAttributes)
                        .Class(cssClass)
                        .SetInnerText(innerText);

            if (url.HasText())
                href.Attr("href", url);

            if (title.HasText())
                href.Attr("title", title);

            return href.ToHtml();
        }

        public static MvcHtmlString Div(this HtmlHelper html, string id, MvcHtmlString innerHTML, string cssClass)
        {
            return html.Div(id, innerHTML, cssClass, null);
        }

        public static MvcHtmlString Div(this HtmlHelper html, string id, MvcHtmlString innerHTML, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("div", id)
                .Attrs(htmlAttributes)
                .Class(cssClass)
                .InnerHtml(innerHTML)
                .ToHtml();
        }

        public static MvcHtmlString Button(this HtmlHelper html, string id, string value, string onclick, string cssClass)
        {
            return html.Button(id, value, onclick, cssClass, null);
        }

        public static MvcHtmlString Button(this HtmlHelper html, string id, string value, string onclick, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("input", id)
                .Attrs(new { type = "button", value = value, onclick = onclick })
                .Attrs(htmlAttributes)
                .Class(cssClass)
                .ToHtmlSelf();
        }

        public static string PropertyNiceName<R>(this HtmlHelper html, Expression<Func<R>> property)
        {
            return ReflectionTools.BasePropertyInfo(property).NiceName();
        }

        public static string PropertyNiceName<T, R>(this HtmlHelper html, Expression<Func<T, R>> property)
        {
            return ReflectionTools.BasePropertyInfo(property).NiceName();
        }

        public static UrlHelper UrlHelper(this HtmlHelper html)
        {
            return new UrlHelper(html.ViewContext.RequestContext);
        }

        public static MvcHtmlString FormatHtml(this HtmlHelper html, string text, params object[] values)
        {
            return text.FormatHtml(values);
        }

        public static MvcHtmlString FormatHtml(this string text, params object[] values)
        {
            var encoded = HttpUtility.HtmlEncode(text);

            if (values == null || values.Length == 0)
                return new MvcHtmlString(encoded);

            return new MvcHtmlString(string.Format(encoded,
                values.Select(a => a is IHtmlString ? ((IHtmlString)a).ToHtmlString() : HttpUtility.HtmlEncode(a)).ToArray()));
        }

        public static MvcHtmlString Json(this HtmlHelper html, object value, JsonSerializerSettings settings = null)
        {
            if (settings == null)
                settings = new JsonSerializerSettings() { Converters = { new LiteJavaScriptConverter() } };

            return new MvcHtmlString(JsonConvert.SerializeObject(value, settings));
        }

        public static MvcHtmlString Lambda(this HtmlHelper html, Func<object, HelperResult> lambda)
        {
            return MvcHtmlString.Create(lambda(null).ToHtmlString());
        }

        public static MvcHtmlString SafeDropDownList(this HtmlHelper html, string idAndName, IEnumerable<SelectListItem> elements, object htmlAttributes)
        {
            return html.SafeDropDownList(idAndName, elements, new RouteValueDictionary(htmlAttributes));
        }

        //http://stackoverflow.com/questions/979095/asp-net-mvc-dropdownlist-pre-selected-item-ignored
        public static MvcHtmlString SafeDropDownList(this HtmlHelper html, string idAndName, IEnumerable<SelectListItem> elements, IDictionary<string, object> htmlAttributes = null)
        {
            var select = new HtmlTag("select").IdName(idAndName).Attrs(htmlAttributes);
            
            HtmlStringBuilder sb = new HtmlStringBuilder();
            foreach (var se in elements)
	        {
                var option = new HtmlTag("option").Attr("value", se.Value).SetInnerText(se.Text);
 
                if(se.Selected)
                    option.Attr("selected", "selected");

                sb.Add(option.ToHtml());
	        }

            select.InnerHtml(sb.ToHtml());

            return select.ToHtml();
        }
    }
}

