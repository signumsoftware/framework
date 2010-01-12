using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Basics;

namespace Signum.Web
{
    //public delegate List<NoteItem> GetNotesDelegate(HtmlHelper helper, object entity, string partialViewName, string prefix);

    public static class NoteWidgetHelper
    {
        //public static event GetNotesDelegate GetNotes;
        public static Func<IdentifiableEntity, INoteDN> CreateNote { get; set; }
        public static object NotesQuery { get; set; }
        public static string NotesQueryColumn { get; set; }
        

        public static WidgetItem CreateWidget(IdentifiableEntity identifiable)
        {
            if (identifiable == null || identifiable.IsNew || identifiable is INoteDN)
                return null;

            int count = Navigator.QueryCount(new QueryOptions(NotesQuery)
            {
                FilterOptions = new List<FilterOptions>
                {
                    new FilterOptions(NotesQueryColumn, identifiable) 
                }
            });


            JsFindOptions foptions = new JsFindOptions
            {
                FindOptions = new FindOptions
                {
                    QueryName = NotesQuery,
                    Create = false,
                    SearchOnLoad = true,
                    FilterMode = FilterMode.Hidden,
                    FilterOptions = new List<FilterOptions>
                    {
                        new FilterOptions( NotesQueryColumn, identifiable.ToLite())
                    }
                }
            };

            JsViewOptions voptions = new JsViewOptions
            {
                Type = null, //Navigator.TypesToURLNames[typeof(NotaDN)],
                ControllerUrl = "Widgets/CreateNote",
                OnOkSuccess = "function(){ RefreshNotes('Widgets/RefreshNotes','NotaDN'); }"
            };

            return new WidgetItem
            {
                Content = 
@"<div class='widget notes'>
  <a class='view' onclick=""javascript:OpenFinder({0});>{1}</a>
  <a class='create' onclick=""javascript:RelatedEntityCreate({2});"">{3}</a>
</div>".Formato(foptions.ToJS(), "Ver notas", voptions.ToJS(), "Crear nota"),
                Label = "<a id='Notes' onclick=\"javascript:OpenFinder({0});\">{1}<span class='notes {2}'>{3}</span></a>".Formato(foptions.ToJS(), Properties.Resources.Notes, count, count == 0 ? "disabled" : ""),
                Id = "Notes",
                Show = true,
            };
        }
    }
}
