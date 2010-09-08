using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities.ControlPanel;
using Signum.Utilities;
using Signum.Entities;
using System.Web.Mvc;
using Signum.Entities.Reports;
using Signum.Web.Queries;

namespace Signum.Web.ControlPanel
{
    [Serializable]
    public abstract class PanelPartRenderer
    {
        public abstract void RenderPart(HtmlHelper helper, PanelPart part);

        public static Action<HtmlHelper, PanelPart> Render = (helper, panelPart) => DefaultRender(helper, panelPart);
        public static void DefaultRender(HtmlHelper helper, PanelPart part)
        {
            if (part.Content is UserQueryDN)
                new SearchControlPartRenderer().RenderPart(helper, part);

            if (part.Content is CountSearchControlPartDN)
                new CountSearchControlPartRenderer().RenderPart(helper, part);

            if (part.Content is LinkListPartDN)
                new LinkListPartRenderer().RenderPart(helper, part);
        }
    }

    [Serializable]
    public class SearchControlPartRenderer : PanelPartRenderer
    {
        public SearchControlPartRenderer() { }

        public override void RenderPart(HtmlHelper helper, PanelPart part)
        {
            UserQueryDN uq = ((UserQueryDN)part.Content);
            FindOptions fo = new FindOptions(Navigator.ResolveQueryFromToStr(uq.Query.Key))
            {
                FilterMode = FilterMode.OnlyResults
            };

            helper.SearchControl(
                uq,
                fo,
                new Context(null, "r{0}c{1}".Formato(part.Row, part.Column)));
        }
    }

    [Serializable]
    public class CountSearchControlPartRenderer : PanelPartRenderer
    {
        public CountSearchControlPartRenderer() { }

        public override void RenderPart(HtmlHelper helper, PanelPart part)
        {
            var uqList = ((CountSearchControlPartDN)part.Content).UserQueries;

            foreach (CountUserQueryElement uq in uqList)
            { 
                FindOptions fo = new FindOptions(Navigator.ResolveQueryFromToStr(uq.UserQuery.Query.Key))
                {
                    FilterMode = FilterMode.OnlyResults
                };

                helper.Write(
                    helper.Span(
                        "lblr{0}c{1}".Formato(part.Row, part.Column),
                        uq.Label,
                        "labelLine"));

                helper.Write(
                    helper.CountSearchControl(
                        uq.UserQuery,
                        fo,
                        "r{0}c{1}".Formato(part.Row, part.Column)));

                helper.Write(
                    helper.Div(
                        "divr{0}c{1}".Formato(part.Row, part.Column),
                        "",
                        "clearall"));
            }
        }
    }

    [Serializable]
    public class LinkListPartRenderer : PanelPartRenderer
    {
        public LinkListPartRenderer() { }

        public override void RenderPart(HtmlHelper helper, PanelPart part)
        {
            var linkList = ((LinkListPartDN)part.Content).Links;

            foreach (LinkElement link in linkList)
            {
                helper.Write(
                    helper.Href(link.Link, link.Label));

                helper.Write(
                    helper.Div(
                        "divr{0}c{1}".Formato(part.Row, part.Column),
                        "",
                        "clearall"));
            }
        }
    }
}
