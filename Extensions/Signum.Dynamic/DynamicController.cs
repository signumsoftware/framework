using Signum.Utilities.DataStructures;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Signum.Dynamic.Controllers;
using Signum.API.Filters;
using Signum.Eval;

namespace Signum.Dynamic;

[ValidateModelFilter]
public class DynamicController : ControllerBase
{
    IHostApplicationLifetime lifeTime;
    public DynamicController(IHostApplicationLifetime lifeTime)
    {
        this.lifeTime = lifeTime;
    }

    [HttpPost("api/dynamic/compile")]
    public List<CompilationErrorTS> Compile(bool inMemory)
    {
        EvalPanelPermission.ViewDynamicPanel.AssertAuthorized();

        SystemEventLogLogic.Log("DynamicController.Compile");
        var compileResult = new List<DynamicLogic.CompilationResult>();

        if (!inMemory)
            DynamicLogic.CleanCodeGenFolder();

        Dictionary<string, CodeFile> codeFiles = DynamicLogic.GetCodeFilesDictionary();
        compileResult.Add(DynamicLogic.Compile(codeFiles, inMemory: inMemory, assemblyName: DynamicLogic.CodeGenAssembly, needsCodeGenAssembly: false));

        if (DynamicApiLogic.IsStarted)
        {
            Dictionary<string, CodeFile> apiFiles = DynamicApiLogic.GetCodeFiles().ToDictionaryEx(a => a.FileName, "CodeGenController C# code file");
            compileResult.Add(DynamicLogic.Compile(apiFiles, inMemory: inMemory, assemblyName: DynamicLogic.CodeGenControllerAssembly, needsCodeGenAssembly: true));
            codeFiles.AddRange(apiFiles);
        }

        return (from cr in compileResult
                from ce in cr.Errors
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

    [HttpPost("api/dynamic/restartServer")]
    public void RestartServer()
    {
        DynamicPanelPermission.RestartApplication.AssertAuthorized();


        SystemEventLogLogic.Log("DynamicController.RestartServer");
        DynamicLogic.OnApplicationServerRestarted?.Invoke();
        lifeTime.StopApplication();
    }

    [HttpGet("api/dynamic/startErrors")]
    public List<HttpError> GetStartErrors()
    {
        EvalPanelPermission.ViewDynamicPanel.AssertAuthorized();


        return new Sequence<Exception?>
        {
            DynamicLogic.CodeGenError,
            StartParameters.IgnoredCodeErrors.EmptyIfNull(),
            StartParameters.IgnoredDatabaseMismatches.EmptyIfNull(),
        }
        .NotNull()
        .Select(e => new HttpError(e))
        .ToList();
    }

  

    [HttpPost("api/dynamic/getPanelInformation")]
    public DynamicPanelInformation GetPanelInformation()
    {
        EvalPanelPermission.ViewDynamicPanel.AssertAuthorized();

        return new DynamicPanelInformation
        {
            lastDynamicCompilationDateTime = DynamicLogic.GetLastCodeGenAssemblyFileInfo()?.CreationTime,
            loadedCodeGenAssemblyDateTime = DynamicLogic.GetLoadedCodeGenAssemblyFileInfo()?.CreationTime,
            loadedCodeGenControllerAssemblyDateTime = DynamicLogic.GetLoadedCodeGenControllerAssemblyFileInfo()?.CreationTime,
            lastDynamicChangeDateTime = Database.Query<OperationLogEntity>()
                    .Where(a => EvalLogic.RegisteredDynamicTypes.Contains(a.Target!.EntityType))
                    .Max(a => a.End),
        };
    } 
}

public class DynamicPanelInformation
{
    public DateTime? lastDynamicCompilationDateTime;
    public DateTime? loadedCodeGenAssemblyDateTime;
    public DateTime? loadedCodeGenControllerAssemblyDateTime;
    public DateTime? lastDynamicChangeDateTime;
}
