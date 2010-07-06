#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Engine;
using System.Configuration;
using Signum.Web.Properties;
using System.Web;
using Signum.Engine.DynamicQuery;
#endregion

namespace Signum.Web
{
    public static class EntityComboHelper
    {
        internal static string InternalEntityCombo(this HtmlHelper helper, EntityCombo entityCombo)
        {
            if (!entityCombo.Visible || entityCombo.HideIfNull && entityCombo.UntypedValue == null)
                return "";

            if (!entityCombo.Type.IsIIdentifiable() && !entityCombo.Type.IsLite())
                throw new InvalidOperationException(Resources.EntityComboCanOnlyBeDoneForAnIdentifiableOrALiteNotFor0.Formato(entityCombo.Type.CleanType()));

            StringBuilder sb = new StringBuilder();

            if (entityCombo.ShowFieldDiv)
                sb.AppendLine("<div class='field'>");

            sb.AppendLine(EntityBaseHelper.BaseLineLabel(helper, entityCombo, entityCombo.Compose(EntityComboKeys.Combo)));

            sb.AppendLine("<div class=\"value-container\">");

            sb.AppendLine(EntityBaseHelper.WriteImplementations(helper, entityCombo));

            sb.AppendLine(helper.HiddenEntityInfo(entityCombo));

            if (EntityBaseHelper.RequiresLoadAll(helper, entityCombo))
                sb.AppendLine(EntityBaseHelper.RenderTypeContext(helper, (TypeContext)entityCombo.Parent, RenderMode.PopupInDiv, entityCombo.PartialViewName, entityCombo.ReloadOnChange));
            else if (entityCombo.UntypedValue != null)
                sb.AppendLine(helper.Div(entityCombo.Compose(EntityBaseKeys.Entity), "", "", new Dictionary<string, object> { { "style", "display:none" } }));

            if (entityCombo.ReadOnly)
                sb.AppendLine(helper.Span(entityCombo.ControlID, entityCombo.UntypedValue.TryToString(), "valueLine"));
            else
            {
                List<SelectListItem> items = new List<SelectListItem>();
                items.Add(new SelectListItem() { Text = "-", Value = "" });
                if (entityCombo.Preload)
                {
                    int? id = entityCombo.IdOrNull;

                    List<Lite> data = entityCombo.Data ?? AutoCompleteUtils.RetriveAllLite(entityCombo.Type.CleanType(), entityCombo.Implementations);

                    items.AddRange(
                        data.Select(lite => new SelectListItem()
                            {
                                Text = lite.ToString(),
                                Value = entityCombo.Implementations != null ? lite.RuntimeType.Name + ";" + lite.Id.ToString() : lite.Id.ToString(),
                                Selected = lite.IdOrNull == entityCombo.IdOrNull
                            })
                        );
                }

                entityCombo.ComboHtmlProperties.AddCssClass("valueLine");

                if (entityCombo.ComboHtmlProperties.ContainsKey("onchange"))
                    throw new InvalidOperationException("EntityCombo cannot have onchange html property, use onEntityChanged instead");

                entityCombo.ComboHtmlProperties.Add("onchange", "EComboOnChanged({0});".Formato(entityCombo.ToJS()));

                if (entityCombo.Size > 0)
                {
                    entityCombo.ComboHtmlProperties.AddCssClass("entityList");
                    entityCombo.ComboHtmlProperties.Add("size", Math.Min(entityCombo.Size, items.Count - 1));
                }            
                
                sb.AppendLine(helper.DropDownList(
                        entityCombo.Compose(EntityComboKeys.Combo),
                        items,
                        entityCombo.ComboHtmlProperties).ToHtmlString());
            }

            sb.AppendLine(EntityBaseHelper.WriteViewButton(helper, entityCombo));
            sb.AppendLine(EntityBaseHelper.WriteCreateButton(helper, entityCombo));

            sb.AppendLine("</div>");

            if (entityCombo.ShowFieldDiv)
                sb.AppendLine("</div>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, entityCombo));

            return sb.ToString();
        }

        public static void EntityCombo<T,S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            helper.EntityCombo<T, S>(tc, property, null);
        }

        public static void EntityCombo<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityCombo> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            EntityCombo ec = new EntityCombo(typeof(S), context.Value, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(ec, ec.CleanRuntimeType ?? ec.Type.CleanType());

            Common.FireCommonTasks(ec);

            if (settingsModifier != null)
                settingsModifier(ec);

            helper.Write(helper.InternalEntityCombo(ec));
        }

        public static string RenderOption(this SelectListItem item)
        {
            TagBuilder builder = new TagBuilder("option")
            {
                InnerHtml = HttpUtility.HtmlEncode(item.Text),
            };

            if (item.Value != null)
                builder.Attributes["value"] = item.Value;

            if (item.Selected)
                builder.Attributes["selected"] = "selected";

            return builder.ToString(TagRenderMode.Normal);
        }

        public static SelectListItem ToSelectListItem(this Lite lite, bool selected)
        {
            return new SelectListItem { Text = lite.ToStr, Value = lite.Id.ToString(), Selected = selected };
        }

        public static string ToOptions<T>(this IEnumerable<Lite<T>> lites, Lite<T> selectedElement) where T : class, IIdentifiable
        {
            List<SelectListItem> list = new List<SelectListItem>();

            if (selectedElement == null)
                list.Add(new SelectListItem { Text = "-", Value = "" });

            list.AddRange(lites.Select(l => l.ToSelectListItem(l.Is(selectedElement))));
     
            return list.ToString(RenderOption, "\r\n");
        }
    }
}
