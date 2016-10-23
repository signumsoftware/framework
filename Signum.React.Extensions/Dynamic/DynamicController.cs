using Signum.Engine.Dynamic;
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

        [Route("api/dynamic/restartApplication"), HttpPost]
        public void RestartApplication()
        {
            System.Web.HttpRuntime.UnloadAppDomain();
        }

        [Route("api/dynamic/pingApplication"), HttpPost]
        public bool PingApplication()
        {
            return true;
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
