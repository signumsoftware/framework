using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Web.Basic
{
    public static class QueryClient
    {
        public static string ViewPrefix = "~/basic/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(QueryClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<QueryDN>(EntityType.SystemString) 
                    {  
                        MappingMain = new EntityMapping<QueryDN>(true).GetValue,
                        MappingLine = new EntityMapping<QueryDN>(true).GetValue
                    }
                });
            }
        }
    }
}
