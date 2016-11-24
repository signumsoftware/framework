using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Signum.React.Filters;
using System.Threading;
using System.Web.Http;

namespace Signum.React.Processes
{
    public class PrintController : ApiController
    {
        [Route("api/printing/constructFromMany"), HttpPost, ValidateModelFilter]
        public EntityPackTS ConstructFromMany(OperationController.MultiOperationRequest request)
        {
            var type = request.type == null ? null : TypeLogic.GetType(request.type);

            var entity = PackageLogic.CreatePackageOperation(request.lites, request.operarionSymbol, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/printing/view"), HttpGet]
        public ProcessLogicState View()
        {
            ProcessLogicState state = ProcessRunnerLogic.ExecutionState();

            return state;
        }

        [Route("api/printing/start"), HttpPost]
        public void Start()
        {
            ProcessPermission.ViewProcessPanel.AssertAuthorized();

            ProcessRunnerLogic.StartRunningProcesses();

            Thread.Sleep(1000);
        }

        //[Route("api/printing/stop"), HttpPost]
        //public void Stop()
        //{
            //ProcessPermission.ViewProcessPanel.AssertAuthorized();

            //ProcessRunnerLogic.Stop();

            //Thread.Sleep(1000);
        //}
    }
}