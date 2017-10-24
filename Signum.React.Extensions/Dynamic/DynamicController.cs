using Signum.Engine.Basics;
using Signum.Engine.Dynamic;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Scheduler;
using Signum.Entities;
using Signum.Entities.Dynamic;
using Signum.React.Facades;
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
            SystemEventLogLogic.Log("DynamicController.Compile");
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
            SystemEventLogLogic.Log("DynamicController.RestartServer");
            System.Web.HttpRuntime.UnloadAppDomain();
        }

        [Route("api/dynamic/startErrors"), HttpGet]
        public List<HttpError> GetStartErrors()
        {
            return StartParameters.IgnoredCodeErrors.EmptyIfNull()
                .PreAnd(DynamicLogic.CodeGenError).NotNull()
                .Select(e => new HttpError(e, true))
                .ToList();
        }
    }
}
