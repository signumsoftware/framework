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
        public static MvcHtmlString NewFilter(this HtmlHelper helper, FilterOption filterOptions, Context context, int index)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            FilterType filterType = QueryUtils.GetFilterType(filterOptions.Token.Type);
            List<FilterOperation> possibleOperations = QueryUtils.GetFilterOperations(filterType);

            var id = context.Compose("trFilter", index.ToString());

            using (sb.SurroundLine(new HtmlTag("tr").Id(id)))
            {
                using (sb.SurroundLine("td"))
                {
                    if (!filterOptions.Frozen)
                    {
                        sb.AddLine(new HtmlTag("a", context.Compose("btnDelete", index.ToString()))
                        .Attr("title",  SearchMessage.DeleteFilter.NiceToString())
                        .Attr("onclick", JsModule.Finder["deleteFilter"](id).ToString())
                        .Class("sf-line-button sf-remove")
                        .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-remove")));
                    }
                }

                using (sb.SurroundLine(new HtmlTag("td")))
                {
                    sb.AddLine(new HtmlTag("input")
                        .Attr("type", "hidden")
                        .Attr("value", filterOptions.Token.FullKey())
                        .ToHtmlSelf());
                        
                    foreach (var t in filterOptions.Token.Follow(tok => tok.Parent).Reverse())
                    {
                        sb.AddLine(new HtmlTag("span")
                            .Class("sf-filter-token")
                            .Attr("title", t.NiceTypeName)
                            .Attr("style", "color:" + t.TypeColor)
                            .SetInnerText(t.ToString()).ToHtml());
                    }
                }

                using (sb.SurroundLine("td"))
                {
                    var dic = new Dictionary<string, object> { { "class", "form-control" } };
                    if (filterOptions.Frozen)
                        dic.Add("disabled", "disabled");

                    sb.AddLine(
                        helper.SafeDropDownList(
                        context.Compose("ddlSelector", index.ToString()),
                        possibleOperations.Select(fo =>
                            new SelectListItem
                            {
                                Text = fo.NiceToString(),
                                Value = fo.ToString(),
                                Selected = fo == filterOptions.Operation
                            }),
                            dic));
                }

                using (sb.SurroundLine("td"))
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
                Lite<IEntity> lite = (Lite<IEntity>)Common.Convert(filterOption.Value, filterOption.Token.Type);
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
                    Autocomplete = false,
                };
                EntityBaseHelper.ConfigureEntityButtons(el, filterOption.Token.Type.CleanType());
                el.FormGroupStyle = FormGroupStyle.None;
                el.Create = false;
                el.ReadOnly = filterOption.Frozen;

                return EntityLineHelper.InternalEntityLine(helper, el);
            }
            else
            {
                var vl = new ValueLine(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute())
                {
                    FormGroupStyle = FormGroupStyle.None,
                    ReadOnly = filterOption.Frozen,
                    Format = filterOption.Token.Format,
                    UnitText = filterOption.Token.Unit,
                }; 

                if (filterOption.Token.Type.UnNullify().IsEnum)
                {
                    vl.EnumComboItems = ValueLine.CreateComboItems(EnumEntity.GetValues(vl.Type.UnNullify()), vl.UntypedValue == null || vl.Type.IsNullable());
                }

                return ValueLineHelper.ValueLine(helper, vl);
            }
            
            throw new InvalidOperationException("Invalid filter for type {0}".FormatWith(filterOption.Token.Type.Name));
        }

       
    }
}