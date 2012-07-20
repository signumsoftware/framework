using System;
using System.Collections.Generic;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Web.Mvc;
using Signum.Web.Properties;
using Signum.Web.Controllers;
using Signum.Entities.DynamicQuery;

namespace Signum.Web
{
    public static class NoteWidgetHelper
    {
        public static Type NoteType { get; set; }
        public static Func<IdentifiableEntity, INoteDN> CreateNote { get; set; }
        public static string NotesQueryColumn { get; set; }

        static object notesQuery; 
        public static object NotesQuery 
        {
            get { return notesQuery ?? NoteType; }
            set { notesQuery = value; }
        }

        public static int CountNotes(IdentifiableEntity identifiable)
        { 
            return Navigator.QueryCount(new CountOptions(NotesQuery)
            {
                FilterOptions = { new FilterOption(NotesQueryColumn, identifiable) }
            });
        }

        public static string JsOnNoteCreated(string prefix, string successMessage)
        {
            return "SF.Widgets.onNoteCreated('{0}','{1}','{2}')".Formato(
                RouteHelper.New().Action<WidgetsController>(wc => wc.NotesCount()), 
                prefix,
                successMessage);
        }

        public static WidgetItem CreateWidget(IdentifiableEntity identifiable, string partialViewName, string prefix)
        {
            if (identifiable == null || identifiable.IsNew || identifiable is INoteDN)
                return null;

            JsFindOptions foptions = new JsFindOptions
            {
                Prefix = TypeContextUtilities.Compose(prefix, "New"),
                FindOptions = new FindOptions
                {
                    QueryName = NotesQuery,
                    Create = false,
                    SearchOnLoad = true,
                    FilterMode = FilterMode.AlwaysHidden,
                    FilterOptions = { new FilterOption( NotesQueryColumn, identifiable.ToLite()) },
                    ColumnOptions = { new ColumnOption(NotesQueryColumn) },
                    ColumnOptionsMode = ColumnOptionsMode.Remove,
                }
            };

            JsViewOptions voptions = new JsViewOptions
            {
                Type = NoteType.Name,
                Prefix = prefix,
                ControllerUrl = RouteHelper.New().Action("CreateNote", "Widgets"),
                RequestExtraJsonData = "function(){{ return {{ {0}: new SF.RuntimeInfo('{1}').find().val() }}; }}".Formato(EntityBaseKeys.RuntimeInfo, prefix),
                OnOkClosed = new JsFunction() { JsOnNoteCreated(prefix, Resources.NoteCreated) }
            };

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-menu-button sf-widget-content sf-notes")))
            {
                using (content.Surround(new HtmlTag("li").Class("sf-note")))
                {
                    content.AddLine(new HtmlTag("a")
                        .Class("sf-note-view")
                        .Attr("onclick", JsFindNavigator.openFinder(foptions).ToJS())
                        .InnerHtml(Resources.ViewNotes.EncodeHtml())
                        .ToHtml());
                }

                using (content.Surround(new HtmlTag("li").Class("sf-note")))
                {
                    content.AddLine(new HtmlTag("a")
                       .Class("sf-note-create")
                       .Attr("onclick", new JsViewNavigator(voptions).createSave(RouteHelper.New().SignumAction("TrySavePartial")).ToJS())
                       .InnerHtml(Resources.CreateNote.EncodeHtml())
                       .ToHtml());
                }
            }

            int count = CountNotes(identifiable);

            HtmlStringBuilder label = new HtmlStringBuilder();
            
            var toggler = new HtmlTag("a")
                .Class("sf-widget-toggler sf-notes-toggler").Class(count > 0 ? "sf-widget-toggler-active" : null)
                .Attr("title", Resources.Notes);
            
            using (label.Surround(toggler))
            {
                label.Add(new HtmlTag("span")
                    .Class("ui-icon ui-icon-pin-w")
                    .InnerHtml(Resources.Notes.EncodeHtml())
                    .ToHtml());
                                
                label.Add(new HtmlTag("span")
                    .Class("sf-widget-count")
                    .SetInnerText(count.ToString())
                    .ToHtml());
            }

            return new WidgetItem
            {
                Id = TypeContextUtilities.Compose(prefix, "notesWidget"),
                Label = label.ToHtml(),
                Content = content.ToHtml()
            };
        }
    }
}
