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
using Signum.Engine.Cache;
using Signum.Engine;
using Signum.Entities.Cache;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;

namespace Signum.React.Processes
{
    public class ProcessController : ApiController
    {
        [Route("api/processes/constructFromMany"), HttpPost, ValidateModelFilter]
        public EntityPackTS ConstructFromMany(OperationController.MultiOperationRequest request)
        {
            var operation = OperationController.ParseOperationAssert(request.operationKey);

            var type = request.type == null ? null : TypeLogic.GetType(request.type);

            var entity = PackageLogic.CreatePackageOperation(request.lites, operation, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/processes/view"), HttpGet]
        public ProcessLogicState View()
        {
            ProcessLogicState state = ProcessRunnerLogic.ExecutionState();

            return state;
        }

        [Route("api/processes/start"), HttpPost]
        public void Start()
        {
            ProcessPermission.ViewProcessPanel.AssertAuthorized();

            ProcessRunnerLogic.StartRunningProcesses();

            Thread.Sleep(1000);
        }

        [Route("api/processes/stop"), HttpPost]
        public void Stop()
        {
            ProcessPermission.ViewProcessPanel.AssertAuthorized();

            ProcessRunnerLogic.Stop();

            Thread.Sleep(1000);
        }
    }
}