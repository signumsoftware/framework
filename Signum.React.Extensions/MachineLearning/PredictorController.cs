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
using Signum.Engine;
using Signum.Utilities;

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

        [Route("api/predictor/csv/{id}"), HttpGet]
        public HttpResponseMessage DownloadCsvById(int id)
        {
            var predictor = Database.Query<PredictorEntity>().SingleEx(p => p.Id == id);
            byte[] content = predictor.GetCsv();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.csv");
        }

        [Route("api/predictor/tsv/{id}"), HttpGet]
        public HttpResponseMessage DownloadTsvById(int id)
        {
            var predictor = Database.Query<PredictorEntity>().SingleEx(p => p.Id == id);
            byte[] content = predictor.GetTsv();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.tsv");
        }

        [Route("api/predictor/tsv/{id}/metadata"), HttpGet]
        public HttpResponseMessage DownloadTsvMetadataById(int id)
        {
            var predictor = Database.Query<PredictorEntity>().SingleEx(p => p.Id == id);
            byte[] content = predictor.GetTsvMetadata();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.metadata.tsv");
        }
    }
}