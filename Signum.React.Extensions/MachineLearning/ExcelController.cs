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
using Signum.Entities.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Engine.Chart;

namespace Signum.React.MachineLearning
{
    public class MachineLearningController : ApiController
    {

        //[Route("api/excel/reportsFor/{queryKey}"), HttpGet]
        //public IEnumerable<Lite<ExcelReportEntity>> GetExcelReports(string queryKey)
        //{
        //    return ExcelLogic.GetExcelReports(QueryLogic.ToQueryName(queryKey));
        //}

    }
}