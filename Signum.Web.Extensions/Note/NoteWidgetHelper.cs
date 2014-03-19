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
            return Navigator.QueryCount(new CountOptions(typeof(NoteDN))
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
                FilterMode = FilterMode.AlwaysHidden,
                FilterOptions = { new FilterOption("Target", ident.ToLite()) },
                ColumnOptions = { new ColumnOption("Target") },
                ColumnOptionsMode = ColumnOptionsMode.Remove,
            }.ToJS(ctx.Prefix, "New");


            List<IMenuItem> items = new List<IMenuItem>()
            {
                new MenuItem
                {
                     CssClass = "sf-note-view",
                     OnClick = new JsFunction(NoteClient.Module, "explore", ctx.Prefix, findOptions),
                     Text = NoteMessage.ViewNotes.NiceToString(),
                },

                new MenuItem
                {
                    CssClass = "sf-note-create",
                    OnClick = new JsFunction(NoteClient.Module, "createNote", ctx.Prefix, OperationDN.UniqueKey(NoteOperation.CreateNoteFromEntity)),
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
                Class = "sf-notes-toggler" + (count > 0 ? " sf-widget-toggler-active" : null),
                Html = new HtmlTag("span").Class("sf-widget-count").SetInnerText(count.ToString()),
                Items = items
            };
        }
    }
}
