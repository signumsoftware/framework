using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using $custommessage$.Entities;

namespace $custommessage$.Logic
{
    public static class MyEntityLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<MyEntityDN>();

                dqm[typeof(MyEntityDN)] = (from e in Database.Query<MyEntityDN>()
                                           select new
                                           {
                                                 Entity = e.ToLite(),
                                                 e.Id,
                                                 e.Name
                                           }).ToDynamic();  
            }
        }
    }
}
