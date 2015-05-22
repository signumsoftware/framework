using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Utilities;
using Signum.Web.Omnibox;
using Signum.Engine.ViewLog;
using Signum.Entities.ViewLog;

namespace Signum.Web.ViewLog
{
    public class ViewLogClient
    {
        public static string ViewPrefix = "~/ViewLog/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.Manager.RetrievingForView += Manager_RetrievingForView;
                
                Navigator.RegisterArea(typeof(ViewLogClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ViewLogEntity>{ PartialViewName = _ => ViewPrefix.FormatWith("ViewLog") },             
                });              
            }
        }

        static IDisposable Manager_RetrievingForView(Lite<Entity> lite)
        {
            return ViewLogLogic.LogView(lite, "WebRetrieve");
        }

      
    }
}