using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using Signum;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Entities.DynamicQuery;
using $custommessage$.Entities;

namespace $custommessage$.Web
{
    public static class Queries
    {
        static DynamicQueryManager dqm = new DynamicQueryManager();

        public static DynamicQueryManager DynamicQueryManager
        {
            get { return dqm; }
        }

        public static void Initialize()
        {
            dqm[typeof(MyEntityDN)] = from e in Database.Query<MyEntityDN>()
                                     select new
                                     {
                                         Entity = e.ToLazy(),
                                         e.Id,
                                         e.Name
                                     };
        }
    }
}
