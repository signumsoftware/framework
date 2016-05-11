using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Engine.Cache;
using Signum.Engine;
using Signum.Entities.Cache;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;
using System.Web;
using Signum.React.Files;
using System.IO;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Excel;
using Signum.Entities.DynamicQuery;

namespace Signum.React.Excel
{
    public class ExcelController : ApiController
    {
        [Route("api/excel/plain"), HttpPost]
        public HttpResponseMessage ToPlainExcel(QueryRequestTS request)
        {
            var queryRequest = request.ToQueryRequest();

            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(queryRequest);
            byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(queryResult, QueryUtils.GetNiceName(queryRequest.QueryName));

            return FilesController.GetHttpReponseMessage(new MemoryStream(binaryFile), request.queryKey + ".xlsx");            
        }
    }
}