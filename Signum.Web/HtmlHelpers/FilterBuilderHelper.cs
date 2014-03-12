using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Helpers;
using Signum.Engine;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using System.Web.Mvc.Html;

namespace Signum.Web
{
    public static class FilterBuilderHelper
    {
        public static MvcHtmlString NewFilter(this HtmlHelper helper, object queryName, FilterOption filterOptions, Context context, int index)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (filterOptions.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                filterOptions.Token = QueryUtils.Parse(filterOptions.ColumnName, qd, canAggregate: false);
            }

            FilterType filterType = QueryUtils.GetFilterType(filterOptions.Token.Type);
            List<FilterOperation> possibleOperations = QueryUtils.GetFilterOperations(filterType);

            var id = context.Compose("trFilter", index.ToString());

            using (sb.Surround(new HtmlTag("tr").Id(id)))
            {
                using (sb.Surround("td"))
                {
                    if (!filterOptions.Frozen)
                    {
                        var htmlAttr = new Dictionary<string, object>
                        {
                            { "data-icon", "ui-icon-close" },
                            { "data-text", false},
                            { "onclick", new JsFunction(JsFunction.FinderModule, "deleteFilter",  id) },
                        };
                        sb.AddLine(helper.Href(
                            context.Compose("btnDelete", index.ToString()),
                            SearchMessage.FilterBuilder_DeleteFilter.NiceToString(),
                            "",
                            SearchMessage.FilterBuilder_DeleteFilter.NiceToString(),
                            "sf-button",
                            htmlAttr));
                    }
                }

                using (sb.Surround(new HtmlTag("td")))
                {
                    sb.AddLine(helper.HiddenAnonymous(filterOptions.Token.FullKey()));

                    foreach (var t in filterOptions.Token.FollowC(tok => tok.Parent).Reverse())
                    {
                        sb.AddLine(new HtmlTag("span")
                            .Class("sf-filter-token ui-widget-content ui-corner-all")
                            .Attr("title", t.NiceTypeName)
                            .Attr("style", "color:" + t.TypeColor)
                            .SetInnerText(t.ToString()).ToHtml());
                    }
                }

                using (sb.Surround("td"))
                {
                    sb.AddLine(
                        helper.DropDownList(
                        context.Compose("ddlSelector", index.ToString()),
                        possibleOperations.Select(fo =>
                            new SelectListItem
                            {
                                Text = fo.NiceToString(),
                                Value = fo.ToString(),
                                Selected = fo == filterOptions.Operation
                            }),
                            (filterOptions.Frozen) ? new Dictionary<string, object> { { "disabled", "disabled" } } : null));
                }

                using (sb.Surround("td"))
                {
                    Context valueContext = new Context(context, "value_" + index.ToString());

                    if (filterOptions.Frozen && !filterOptions.Token.Type.IsLite())
                    {
                        string txtValue = (filterOptions.Value != null) ? filterOptions.Value.ToString() : "";
                        sb.AddLine(helper.TextBox(valueContext.Prefix, txtValue, new { @readonly = "readonly" }));
                    }
                    else
                        sb.AddLine(PrintValueField(helper, valueContext, filterOptions));
                }
            }

            return sb.ToHtml();
        }

        private static MvcHtmlString PrintValueField(HtmlHelper helper, Context parent, FilterOption filterOption)
        {
            var implementations = filterOption.Token.GetImplementations();

            if (filterOption.Token.Type.IsLite())
            {
                Lite<IIdentifiable> lite = (Lite<IIdentifiable>)Common.Convert(filterOption.Value, filterOption.Token.Type);
                if (lite != null && string.IsNullOrEmpty(lite.ToString()))
                    Database.FillToString(lite);

                Type cleanType = Lite.Extract(filterOption.Token.Type);
                if (EntityKindCache.IsLowPopulation(cleanType) && !cleanType.IsInterface && !implementations.Value.IsByAll)
                {
                    EntityCombo ec = new EntityCombo(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        Implementations = implementations.Value,
                    };
                    EntityBaseHelper.ConfigureEntityButtons(ec, filterOption.Token.Type.CleanType());
                    ec.FormGroupStyle = FormGroupStyle.None;
                    ec.Create = false;
                    ec.ReadOnly = filterOption.Frozen;
                    return EntityComboHelper.InternalEntityCombo(helper, ec);
                }
                else
                {
                    EntityLine el = new EntityLine(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        Implementations = implementations.Value,
                    };

                    if (el.Implementations.Value.IsByAll)
                        el.Autocomplete = false;

                    EntityBaseHelper.ConfigureEntityButtons(el, filterOption.Token.Type.CleanType());
                    el.FormGroupStyle = FormGroupStyle.None;
                    el.Create = false;
                    el.ReadOnly = filterOption.Frozen;

                    return EntityLineHelper.InternalEntityLine(helper, el);
                }
            }
            else if (filterOption.Token.Type.IsEmbeddedEntity())
            {
                EmbeddedEntity lite = (EmbeddedEntity)Common.Convert(filterOption.Value, filterOption.Token.Type);
                EntityLine el = new EntityLine(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                {
                    Implementations = null,
                };
                EntityBaseHelper.ConfigureEntityButtons(el, filterOption.Token.Type.CleanType());
                el.FormGroupStyle = FormGroupStyle.None;
                el.Create = false;
                el.ReadOnly = filterOption.Frozen;

                return EntityLineHelper.InternalEntityLine(helper, el);
            }
            else
            {
                ValueLineType vlType = ValueLineHelper.Configurator.GetDefaultValueLineType(filterOption.Token.Type);
                return ValueLineHelper.Configurator.Constructor[vlType](
                        helper, new ValueLine(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute()));
            }
            
            throw new InvalidOperationException("Invalid filter for type {0}".Formato(filterOption.Token.Type.Name));
        }

       
    }
}