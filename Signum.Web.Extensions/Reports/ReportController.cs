#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Web.Properties;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using Signum.Entities.Reports;
#endregion

namespace Signum.Web.Controllers
{
    [HandleException, AuthenticationRequired]
    public class ReportController : Controller
    {
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult  ToExcel(FindOptions findOptions, string prefix,int? sfTop)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            var request = new QueryRequest
            { 
                QueryName =  findOptions.QueryName,
                Filters =  findOptions.FilterOptions.Select(fo => fo.ToFilter()).ToList(),
                Orders =  findOptions.OrderOptions.Select(fo => fo.ToOrder()).ToList(),
                Limit = sfTop  
            };

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery( request);
            byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(queryResult);
            
            return File(binaryFile, SignumController.GetMimeType(".xlsx"), Navigator.Manager.QuerySettings[findOptions.QueryName].UrlName + ".xlsx");
        }
    }
}
