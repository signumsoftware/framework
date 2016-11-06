using Signum.Engine.Dynamic;
using Signum.Entities.Dynamic;
using Signum.Utilities;
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
            Dictionary<string, CodeFile> codeFiles;
            var result = DynamicLogic.Compile(out codeFiles);
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
}
