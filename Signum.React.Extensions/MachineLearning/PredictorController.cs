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
using Signum.Entities.MachineLearning;
using Signum.Engine.MachineLearning;

namespace Signum.React.MachineLearning
{
    public class PredictorController : ApiController
    {
        [Route("api/predictor/csv/"), HttpPost]
        public HttpResponseMessage DownloadCsv(PredictorEntity predictor)
        {
            byte[] file = predictor.GetCsv();

            return FilesController.GetHttpReponseMessage(new MemoryStream(file), $"{predictor.Name}.csv");
        }

        [Route("api/predictor/tsv/"), HttpPost]
        public HttpResponseMessage DownloadTsv(PredictorEntity predictor)
        {
            byte[] file = predictor.GetCsv(separator: "\t");

            return FilesController.GetHttpReponseMessage(new MemoryStream(file), $"{predictor.Name}.tsv");
        }

        [Route("api/predictor/tsv/metadata"), HttpPost]
        public HttpResponseMessage DownloadTsvMetadata(PredictorEntity predictor)
        {
            byte[] file = new byte[0]; //ExcelLogic.ExecuteExcelReport(request.excelReport, request.queryRequest.ToQueryRequest());

            return FilesController.GetHttpReponseMessage(new MemoryStream(file), $"{predictor.Name}.metadata.tsv");
        }
    }
}