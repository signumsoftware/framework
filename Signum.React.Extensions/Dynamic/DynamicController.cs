using Signum.Engine.Dynamic;
using Signum.Engine.Scheduler;
using Signum.Entities.Reflection;
using Signum.React.ApiControllers;
using Signum.Utilities.DataStructures;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Signum.React.Filters;
using System.ComponentModel.DataAnnotations;
using Signum.Entities.Dynamic;
using Signum.Entities.Basics;
using Microsoft.Extensions.Hosting;
using Signum.Engine.Authorization;

namespace Signum.React.Dynamic;

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
        DynamicPanelPermission.ViewDynamicPanel.AssertAuthorized();

        SystemEventLogLogic.Log("DynamicController.Compile");
        var compileResult = new List<DynamicLogic.CompilationResult>();

        if (!inMemory)
            DynamicLogic.CleanCodeGenFolder();

        Dictionary<string, CodeFile> codeFiles = DynamicLogic.GetCodeFilesDictionary();
        compileResult.Add(DynamicLogic.Compile(codeFiles, inMemory: inMemory, assemblyName: DynamicCode.CodeGenAssembly, needsCodeGenAssembly: false));

        if (DynamicApiLogic.IsStarted)
        {
            Dictionary<string, CodeFile> apiFiles = DynamicApiLogic.GetCodeFiles().ToDictionaryEx(a => a.FileName, "CodeGenController C# code file");
            compileResult.Add(DynamicLogic.Compile(apiFiles, inMemory: inMemory, assemblyName: DynamicCode.CodeGenControllerAssembly, needsCodeGenAssembly: true));
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
        DynamicCode.OnApplicationServerRestarted?.Invoke();
        lifeTime.StopApplication();
    }

    [HttpGet("api/dynamic/startErrors")]
    public List<HttpError> GetStartErrors()
    {
        DynamicPanelPermission.ViewDynamicPanel.AssertAuthorized();


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

    [HttpPost("api/dynamic/evalErrors")]
    public async Task<List<EvalEntityError>> GetEvalErrors([Required, FromBody]QueryEntitiesRequestTS request)
    {
        DynamicPanelPermission.ViewDynamicPanel.AssertAuthorized();

        var allEntities = await QueryLogic.Queries.GetEntitiesLite(request.ToQueryEntitiesRequest()).Select(a => a.Entity).ToListAsync();

        return allEntities.Select(entity =>
        {
            GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(entity));

            return new EvalEntityError
            {
                lite = entity.ToLite(),
                error = entity.FullIntegrityCheck().EmptyIfNull().Select(a => a.Value).SelectMany(a => a.Errors.Values).ToString("\n")
            };
        })
        .Where(ee => ee.error.HasText())
        .ToList();
    }

    [HttpPost("api/dynamic/getPanelInformation")]
    public DynamicPanelInformation GetPanelInformation()
    {
        DynamicPanelPermission.ViewDynamicPanel.AssertAuthorized();

        return new DynamicPanelInformation
        {
            lastDynamicCompilationDateTime = DynamicLogic.GetLastCodeGenAssemblyFileInfo()?.CreationTime,
            loadedCodeGenAssemblyDateTime = DynamicLogic.GetLoadedCodeGenAssemblyFileInfo()?.CreationTime,
            loadedCodeGenControllerAssemblyDateTime = DynamicLogic.GetLoadedCodeGenControllerAssemblyFileInfo()?.CreationTime,
            lastDynamicChangeDateTime = Database.Query<OperationLogEntity>()
                    .Where(a => DynamicCode.RegisteredDynamicTypes.Contains(a.Target!.EntityType))
                    .Max(a => a.End),
        };
    } 
}

public class EvalEntityError
{
    public Lite<Entity> lite;
    public string error;
}

public class DynamicPanelInformation
{
    public DateTime? lastDynamicCompilationDateTime;
    public DateTime? loadedCodeGenAssemblyDateTime;
    public DateTime? loadedCodeGenControllerAssemblyDateTime;
    public DateTime? lastDynamicChangeDateTime;
}
