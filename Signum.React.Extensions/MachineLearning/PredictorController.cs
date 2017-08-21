using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.React.Files;
using System.IO;
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
            byte[] file = predictor.GetTsvMetadata();

            return FilesController.GetHttpReponseMessage(new MemoryStream(file), $"{predictor.Name}.metadata.tsv");
        }
    }
}