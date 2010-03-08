using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using Signum;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Entities.DynamicQuery;
using $custommessage$.Entities;

namespace $custommessage$.Logic
{
    public static class Queries
    {
        public static void Initialize(DynamicQueryManager dqm)
        {
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
