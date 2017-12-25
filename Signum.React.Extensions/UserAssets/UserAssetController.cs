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
using Signum.React.ApiControllers;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Engine.Basics;
using Signum.Entities.UserAssets;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.React.Files;
using Signum.Engine.UserAssets;
using System.IO;

namespace Signum.React.UserAssets
{ 
    public class UserAssetController : ApiController
    {
        [Route("api/userAssets/parseFilters"), HttpPost]
        public List<FilterTS> ParseFilters(ParseFiltersRequest request)
        {
            var queryName = QueryLogic.ToQueryName(request.queryKey);
            var qd = DynamicQueryManager.Current.QueryDescription(queryName);
            var options = SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (request.canAggregate ? SubTokensOptions.CanAggregate : 0);

            using (request.entity != null ? CurrentEntityConverter.SetCurrentEntity(request.entity.Retrieve()) : null)
            {
                var result = request.filters
                        .Select(f => new FilterTS
                        {
                            token = f.tokenString,
                            operation = f.operation,
                            value = FilterValueConverter.Parse(f.valueString, QueryUtils.Parse(f.tokenString, qd, options).Type, f.operation.IsList(), allowSmart: true)
                        })
                        .ToList();

                return result;
            }
        }

        public class ParseFiltersRequest
        {
            public string queryKey;
            public bool canAggregate;
            public List<ParseFilterRequest> filters;
            public Lite<Entity> entity;
        }

        public class ParseFilterRequest
        {
            public string tokenString;
            public FilterOperation operation;
            public string valueString;
        }

        [Route("api/userAssets/export"), HttpPost]
        public HttpResponseMessage Export(Lite<IUserAssetEntity> lite)
        {
            var bytes = UserAssetsExporter.ToXml(lite.Retrieve());
            
            return FilesController.GetHttpReponseMessage(new MemoryStream(bytes), "{0}{1}.xml".FormatWith(lite.EntityType.Name, lite.Id));
        }

        [Route("api/userAssets/importPreview"), HttpPost]
        public UserAssetPreviewModel ImportPreview(FileUpload file)
        {
            return UserAssetsImporter.Preview(file.content);
        }

        [Route("api/userAssets/import"), HttpPost]
        public void Import(FileUploadWithModel file)
        {
            UserAssetsImporter.Import(file.file.content, file.model);
        }

        public class FileUpload
        {
            public string fileName;
            public byte[] content;
        }

        public class FileUploadWithModel
        {
            public FileUpload file;
            public UserAssetPreviewModel model;
        }
    }
}