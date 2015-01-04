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
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<QueryEntity> 
                    {  
                        MappingMain = new EntityMapping<QueryEntity>(true).GetValue,
                        MappingLine = new EntityMapping<QueryEntity>(true).GetValue
                    }
                });
            }
        }
    }
}
