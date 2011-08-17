//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using Signum.Entities.ControlPanel;
//using Signum.Utilities;
//using Signum.Entities;
//using System.Web.Mvc;
//using Signum.Entities.Reports;
//using Signum.Web.UserQueries;
//using Signum.Entities.DynamicQuery;

//namespace Signum.Web.ControlPanel
//{
//    [Serializable]
//    public abstract class PanelPartRenderer
//    {
//        public static Dictionary<Type, Func<PanelPartRenderer>> RegisteredRenderers = new Dictionary<Type, Func<PanelPartRenderer>>()
//        {
//            {typeof(UserQueryDN), ()=> new SearchControlPartRenderer()},
//            {typeof(CountSearchControlPartDN), ()=>new CountSearchControlPartRenderer()},
//            {typeof(LinkListPartDN), ()=>new LinkListPartRenderer()},
//        };

//        public abstract MvcHtmlString RenderPart(HtmlHelper helper, PanelPart part);

//        public static Func<HtmlHelper, PanelPart, MvcHtmlString> Render = (helper, panelPart) =>
//            {
//                var rederer = RegisteredRenderers.GetOrThrow(panelPart.Content.GetType(), "Not renderer registered in PanelPartRenderer for {0}")().RenderPart(helper, panelPart);

//                return new HtmlTag("fieldset").InnerHtml(
//                        new HtmlTag("legend").SetInnerText(panelPart.Title).ToHtml().Concat(rederer)).ToHtml();
//            };
//    }

//    [Serializable]
//    public class SearchControlPartRenderer : PanelPartRenderer
//    {
//        public SearchControlPartRenderer() { }

//        public override MvcHtmlString RenderPart(HtmlHelper helper, PanelPart part)
//        {
//            UserQueryDN uq = ((UserQueryDN)part.Content);
//            object queryName = Navigator.Manager.QuerySettings.Keys.First(k => QueryUtils.GetQueryUniqueKey(k) == uq.Query.Key);
//            FindOptions fo = new FindOptions(queryName)
//            {
//                FilterMode = FilterMode.OnlyResults
//            };

//            return helper.SearchControl(uq,fo,
//                new Context(null, "r{0}c{1}".Formato(part.Row, part.Column)));
//        }
//    }

//    [Serializable]
//    public class CountSearchControlPartRenderer : PanelPartRenderer
//    {
//        public CountSearchControlPartRenderer() { }

//        public override MvcHtmlString RenderPart(HtmlHelper helper, PanelPart part)
//        {
//            var uqList = ((CountSearchControlPartDN)part.Content).UserQueries;

//            HtmlStringBuilder sb = new HtmlStringBuilder();
//            foreach (CountUserQueryElement uq in uqList)
//            {
//                object queryName = Navigator.Manager.QuerySettings.Keys.First(k => QueryUtils.GetQueryUniqueKey(k) == uq.UserQuery.Query.Key);
//                FindOptions fo = new FindOptions(queryName)
//                {
//                    FilterMode = FilterMode.Hidden
//                };


//                sb.Add(helper.Span("lblr{0}c{1}".Formato(part.Row, part.Column), uq.Label, "sf-label-line"));

//                sb.Add(helper.CountSearchControl(uq.UserQuery, fo, "r{0}c{1}".Formato(part.Row, part.Column)));

//                sb.Add(helper.Div("divr{0}c{1}".Formato(part.Row, part.Column), null, "clearall"));
//            }

//            return sb.ToHtml();
//        }
//    }

//    [Serializable]
//    public class LinkListPartRenderer : PanelPartRenderer
//    {
//        public LinkListPartRenderer() { }

//        public override MvcHtmlString RenderPart(HtmlHelper helper, PanelPart part)
//        {
//            var linkList = ((LinkListPartDN)part.Content).Links;

//            HtmlStringBuilder sb = new HtmlStringBuilder();
//            using (sb.Surround(new HtmlTag("ul", "ulr{0}c{1}".Formato(part.Row, part.Column))))
//                foreach (LinkElement link in linkList)
//                {
//                    using (sb.Surround(new HtmlTag("li")))
//                        sb.Add(helper.Href(link.Link, link.Label));
//                }

//            return sb.ToHtml();
//        }
//    }
//}
