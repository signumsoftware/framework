using Signum.Engine.Basics;
using Signum.Engine.Dynamic;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Signum.React.Dynamic
{
    public class DynamicController : ApiController
    {
        [Route("api/dynamic/compile"), HttpPost]
        public List<CompilationErrorTS> Compile()
        {
            Dictionary<string, CodeFile> codeFiles = DynamicLogic.GetCodeFilesDictionary();
            var result = DynamicLogic.Compile(codeFiles, inMemory: true);
            return (from ce in result.Errors.Cast<CompilerError>()
                    let fileName = Path.GetFileName(ce.FileName)
                    select (new CompilationErrorTS
                    {
                        fileName = fileName,
                        column = ce.Column,
                        line = ce.Line,
                        errorNumber = ce.ErrorNumber,
                        errorText = ce.ErrorText,
                        fileContent = codeFiles.GetOrThrow(fileName).FileContent
                    })).ToList();
        }

        public class CompilationErrorTS
        {
            public string fileName;
            public int line;
            public int column;
            public string errorNumber;
            public string errorText;
            public string fileContent;
        }

        [Route("api/dynamic/restartServer"), HttpPost]
        public void RestartServer()
        {
            System.Web.HttpRuntime.UnloadAppDomain();
        }

        [Route("api/dynamic/pingServer"), HttpPost]
        public HttpResponseMessage PingServer()
        {
            if (DynamicLogic.CodeGenError != null)
            {
                var error = new HttpError(DynamicLogic.CodeGenError, true);
                error.ExceptionMessage = DynamicTypeMessage.ServerRestartedWithErrorsInDynamicCodeFixErrorsAndRestartAgain.NiceToString() + "\r\n\r\n" + error.ExceptionMessage;
                return Request.CreateResponse<HttpError>(HttpStatusCode.InternalServerError, error);
            }

            return Request.CreateResponse(HttpStatusCode.NoContent);
        }


        [Route("api/dynamic/autocompleteType"), HttpPost]
        public List<string> AutocompleteType(AutocompleteTypeRequest request) //Not comprehensive, just useful
        {
            var types = GetTypes(request);

            var result = types.Where(a => a.StartsWith(request.query, StringComparison.InvariantCultureIgnoreCase)).OrderBy(a => a.Length).ThenBy(a => a).Take(request.limit).ToList();

            if (result.Count < request.limit)
                result.AddRange(types.Where(a => a.Contains(request.query, StringComparison.InvariantCultureIgnoreCase)).OrderBy(a => a.Length).ThenBy(a => a).Take(result.Count - request.limit).ToList());

            return result;
        }

        public class AutocompleteTypeRequest
        {
            public string query;
            public int limit;
            public bool includeBasicTypes;
            public bool includeEntities;
            public bool includeMList;
            public bool includeQueriable;
        }

        public static List<string> AditionalTypes = new List<string>
        {
            "DateTime",
            "TimeSpan",
            "Guid",
        };

        List<string> GetTypes(AutocompleteTypeRequest request)
        {
            List<string> result = new List<string>();
            if (request.includeBasicTypes)
            {
                result.AddRange(CSharpRenderer.BasicTypeNames.Values);
                result.AddRange(AditionalTypes);
            }

            if (request.includeEntities)
            {
                result.AddRange(TypeLogic.TypeToEntity.Keys.Select(a => a.Name));
            }

            if (request.includeMList)
                return Fix(result, "MList", request.query);

            if (request.includeQueriable)
                return Fix(result, "IQueryable", request.query);

            return result;
        }

        List<string> Fix(List<string> result, string token, string query)
        {
            if (query.StartsWith(token))
                return result.Select(a => token + "<" + a + ">").ToList();
            else
            {
                result.Add(token + "<");
                return result;
            }
        }
    }

   
}
