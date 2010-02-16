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
                ControllerUrl = "Widgets/CreateNote",
                OnCancelled = "function(){ RefreshNotes('Widgets/RefreshNotes'); }"
            };

            return new WidgetItem
            {
                Content =
                    @"<div class='widget notes'>
                      <a class='view' onclick=""javascript:OpenFinder({0});"">{1}</a>
                      <a class='create' onclick=""javascript:RelatedEntityCreate({2});"">{3}</a>
                    </div>".Formato(foptions.ToJS(), Properties.Resources.ViewNotes, voptions.ToJS(), Properties.Resources.CreateNote),
                Label = "<a id='{0}'>{0}<span class='count {1}'>{2}</span></a>".Formato(Properties.Resources.Notes, count == 0 ? "disabled" : "", count),
                Id = "Notes",
                Show = true,
            };
        }
    }
}
