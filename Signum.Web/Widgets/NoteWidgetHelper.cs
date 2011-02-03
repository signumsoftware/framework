using System;
using System.Collections.Generic;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Web
{
    public static class NoteWidgetHelper
    {
        public static Func<IdentifiableEntity, INoteDN> CreateNote { get; set; }
        public static object NotesQuery { get; set; }
        public static string NotesQueryColumn { get; set; }
        public static Type Type { get; set; }

        public static WidgetItem CreateWidget(IdentifiableEntity identifiable)
        {
            if (identifiable == null || identifiable.IsNew || identifiable is INoteDN)
                return null;

            int count = Navigator.QueryCount(new CountOptions(NotesQuery)
            {
                FilterOptions = { new FilterOption(NotesQueryColumn, identifiable) }
            });

            JsFindOptions foptions = new JsFindOptions
            {
                FindOptions = new FindOptions
                {
                    QueryName = NotesQuery,
                    Create = false,
                    SearchOnLoad = true,
                    FilterMode = FilterMode.AlwaysHidden,
                    FilterOptions = { new FilterOption( NotesQueryColumn, identifiable.ToLite()) }
                }
            };

            JsViewOptions voptions = new JsViewOptions
            {
                Type = Type.Name,
                ControllerUrl = RouteHelper.New().Action("CreateNote", "Widgets"),
                OnOkClosed = new JsFunction() { "RefreshNotes('{0}')".Formato(RouteHelper.New().Action("RefreshNotes", "Widgets")) }
            };

            return new WidgetItem
            {
                Content = new HtmlTag("div").Class("widget notes").InnerHtml(
                        new HtmlTag("href")
                            .Class("view")
                            .Attr("onclick", "javascript:new SF.FindNavigator({0}).openFinder();".Formato(foptions.ToJS()))
                            .SetInnerText(Properties.Resources.ViewNotes).ToHtml(),
                        new HtmlTag("href")
                            .Class("create")
                            .Attr("onclick", "javascript:SF.relatedEntityCreate({0});".Formato(voptions.ToJS()))
                            .SetInnerText(Properties.Resources.CreateNote).ToHtml()
                        ).ToHtml(),


                Label = new HtmlTag("a", "Notes").InnerHtml(
                    Properties.Resources.Notes.EncodeHtml(),
                    new HtmlTag("span").Class("count").Class(count == 0 ? "disabled" : "").SetInnerText(count.ToString()).ToHtml()).ToHtml(),
                
                Id = "Notes",
                Show = true,
            };
        }
    }
}
