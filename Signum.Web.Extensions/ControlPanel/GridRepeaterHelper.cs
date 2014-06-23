#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections;
using System.Linq.Expressions;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Configuration;
using Signum.Engine;
using Signum.Entities.ControlPanel;
using Signum.Web.ControlPanel;
#endregion

namespace Signum.Web
{
    public static class GridRepeaterHelper
    {
        public static string LastEnd = "lastEnd";

        private static MvcHtmlString InternalGridRepeater<T>(this HtmlHelper helper, GridRepeater repeater)
           where T : ModifiableEntity, IGridEntity
        {
            if (!repeater.Visible || repeater.HideIfNull && repeater.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.Surround(new HtmlTag("fieldset", repeater.Prefix).Class("SF-grid-repeater-field SF-control-container")))
            {
                sb.AddLine(helper.Hidden(repeater.Compose(EntityListBaseKeys.ListPresent), ""));

                using (sb.Surround(new HtmlTag("div", repeater.Compose("hidden")).Class("hide")))
                {
                }

                using (sb.Surround(new HtmlTag("legend")))
                using (sb.Surround(new HtmlTag("div", repeater.Compose("header"))))
                {
                    sb.AddLine(new HtmlTag("span").SetInnerText(repeater.LabelText).ToHtml());

                    using (sb.Surround(new HtmlTag("span", repeater.Compose("shownButton")).Class("pull-right")))
                    {
                        sb.AddLine(EntityButtonHelper.Create(helper, repeater, btn: false));
                        sb.AddLine(EntityButtonHelper.Find(helper, repeater, btn: false));
                    }
                }

                using (sb.Surround(new HtmlTag("div").Class("row rule")))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        using (sb.Surround(new HtmlTag("div").Class("col-sm-1")))
                        using (sb.Surround(new HtmlTag("div").Class("ruleItem")))
                        {
                        }
                    }
                }

                using (sb.Surround(new HtmlTag("div").Id(repeater.Compose(EntityRepeaterKeys.ItemsContainer))))
                {
                    if (repeater.UntypedValue != null)
                    {
                        foreach (var gr in TypeContextUtilities.TypeElementContext((TypeContext<MList<T>>)repeater.Parent).GroupBy(a => a.Value.Row).OrderBy(a => a.Key))
                        {
                            using (sb.Surround(new HtmlTag("div").Class("row separator-row")))
                            {
                            }

                            using (sb.Surround(new HtmlTag("div").Class("row items-row")))
                            {
                                var lastEnd = 0;
                                foreach (var itemTC in gr.OrderBy(a => a.Value.StartColumn))
                                {
                                    helper.ViewData[LastEnd] = lastEnd;
                                    sb.Add(EntityBaseHelper.RenderContent(helper, itemTC, RenderContentMode.Content, repeater));
                                    lastEnd = itemTC.Value.StartColumn + itemTC.Value.Columns;
                                }
                            }
                        }

                        helper.ViewData.Remove("lastEnd");
                    }

                    using (sb.Surround(new HtmlTag("div").Class("row separator-row")))
                    {
                    }
                }

                sb.AddLine(repeater.ConstructorScript(ControlPanelClient.GridRepeater, "GridRepeater"));
            }

            return sb.ToHtml();
        }


        public static MvcHtmlString GridRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : ModifiableEntity, IGridEntity
        {
            return helper.GridRepeater(tc, property, null);
        }

        public static MvcHtmlString GridRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<GridRepeater> settingsModifier)
            where S : ModifiableEntity, IGridEntity
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            var vo = tc.ViewOverrides;

            if (vo != null && !vo.IsVisible(context.PropertyRoute))
                return vo.OnSurroundLine(context.PropertyRoute, helper, tc, null);

            GridRepeater repeater = new GridRepeater(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            EntityBaseHelper.ConfigureEntityBase(repeater, typeof(S).CleanType());

            Common.FireCommonTasks(repeater);

            if (settingsModifier != null)
                settingsModifier(repeater);

            var result = helper.InternalGridRepeater<S>(repeater);

            if (vo == null)
                return result;

            return vo.OnSurroundLine(repeater.PropertyRoute, helper, tc, result);
        }
    }

    public class GridRepeater : EntityRepeater
    {
        public GridRepeater(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            this.PreserveViewData = true;
        }
    }
}
