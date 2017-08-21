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
            byte[] content = predictor.GetCsv();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.csv");
        }

        [Route("api/predictor/tsv/"), HttpPost]
        public HttpResponseMessage DownloadTsv(PredictorEntity predictor)
        {
            byte[] content = predictor.GetTsv();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.tsv");
        }

        [Route("api/predictor/tsv/metadata"), HttpPost]
        public HttpResponseMessage DownloadTsvMetadata(PredictorEntity predictor)
        {
            byte[] content = predictor.GetTsvMetadata();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.metadata.tsv");
        }
    }
}