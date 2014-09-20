using System;
using System.Collections.Generic;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Web.Mvc;
using Signum.Web.Controllers;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Notes;

namespace Signum.Web.Notes
{
    public static class NoteWidgetHelper
    {
        public static int CountNotes(Lite<IdentifiableEntity> identifiable)
        { 
            return Finder.QueryCount(new CountOptions(typeof(NoteDN))
            {
                FilterOptions = { new FilterOption("Target", identifiable) }
            });
        }

        public static Widget CreateWidget(WidgetContext ctx)
        {
            var ident = (IdentifiableEntity)ctx.Entity;

            var findOptions = new FindOptions
            {
                QueryName = typeof(NoteDN),
                Create = false,
                SearchOnLoad = true,
                ShowFilters = false,
                ShowFilterButton = false,
                FilterOptions = { new FilterOption("Target", ident.ToLite()) },
                ColumnOptions = { new ColumnOption("Target") },
                ColumnOptionsMode = ColumnOptionsMode.Remove,
            }.ToJS(ctx.Prefix, "New");


            var url = RouteHelper.New().Action((NoteController ac) => ac.NotesCount());

            List<IMenuItem> items = new List<IMenuItem>()
            {
                new MenuItem(ctx.Prefix, "sfNoteView")
                {
                     CssClass = "sf-note-view",
                     OnClick = NoteClient.Module["explore"](ctx.Prefix, findOptions, url),
                     Text = NoteMessage.ViewNotes.NiceToString(),
                },

                new MenuItem(ctx.Prefix, "sfNoteCreate")
                {
                    CssClass = "sf-note-create",
                    OnClick = NoteClient.Module["createNote"](JsFunction.Event, ctx.Prefix, NoteOperation.CreateNoteFromEntity.Symbol.Key, url),
                    Text = NoteMessage.CreateNote.NiceToString()
                },
            }; 

            int count = CountNotes(ident.ToLite());

            return new Widget
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "notesWidget"),
                Title = NoteMessage.Notes.NiceToString(),
                IconClass = "glyphicon glyphicon-comment",
                Active = count > 0,
                Class = "sf-notes-toggler",
                Html = new HtmlTag("span").Class("sf-widget-count").SetInnerText(count.ToString()),
                Items = items
            };
        }
    }
}
