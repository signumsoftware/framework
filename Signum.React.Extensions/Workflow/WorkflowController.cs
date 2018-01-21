using Signum.Entities.Workflow;
using Signum.Engine.Workflow;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.React.Facades;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Engine.Authorization;
using Newtonsoft.Json;
using Signum.Utilities;
using Signum.React.ApiControllers;

namespace Signum.React.Workflow
{
    public class WorkflowController : ApiController
    {
        [Route("api/workflow/fetchForViewing/{caseActivityId}"), HttpGet]
        public EntityPackWorkflow GetEntity(string caseActivityId)
        {
            var lite = Lite.ParsePrimaryKey<CaseActivityEntity>(caseActivityId);

            var activity = CaseActivityLogic.RetrieveForViewing(lite);
            using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = activity, WorkflowActivity = activity.WorkflowActivity }))
            {
                var ep = SignumServer.GetEntityPack((Entity)activity.Case.MainEntity);

                return new EntityPackWorkflow
                {
                    activity = activity,
                    canExecuteActivity = OperationLogic.ServiceCanExecute(activity).ToDictionary(a => a.Key.Key, a => a.Value),
                    canExecuteMainEntity = ep.canExecute,
                    Extension = ep.Extension,
                };
            }
        }

        [Route("api/workflow/tags/{caseId}"), HttpGet]
        public List<CaseTagTypeEntity> GetTags(string caseId)
        {
            var lite = Lite.ParsePrimaryKey<CaseEntity>(caseId);

            return Database.Query<CaseTagEntity>().Where(a => a.Case == lite).Select(a => a.TagType).ToList();
        }

        public class EntityPackWorkflow
        {
            public CaseActivityEntity activity { get; set; }
            public Dictionary<string, string> canExecuteActivity { get; set; }
            public Dictionary<string, string> canExecuteMainEntity { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();
        }

        [Route("api/workflow/starts"), HttpGet]
        public List<WorkflowEntity> Starts()
        {
            return WorkflowLogic.GetAllowedStarts();
        }

        [Route("api/workflow/workflowModel/{workflowId}"), HttpGet]
        public WorkflowModel GetWorkflowModel(string workflowId)
        {
            var id = PrimaryKey.Parse(workflowId, typeof(WorkflowEntity));
            var wf = Database.Retrieve<WorkflowEntity>(id);
            var res = WorkflowLogic.GetWorkflowModel(wf);
            return res;
        }

        [Route("api/workflow/previewChanges/{workflowId}"), HttpPost]
        public PreviewResult PreviewChanges(string workflowId, WorkflowModel model)
        {
            var id = PrimaryKey.Parse(workflowId, typeof(WorkflowEntity));
            var wf = Database.Retrieve<WorkflowEntity>(id);
            return WorkflowLogic.PreviewChanges(wf, model);
        }

        [Route("api/workflow/findMainEntityType"), HttpGet]
        public List<Lite<TypeEntity>> FindMainEntityType(string subString, int count)
        {
            var list = TypeLogic.TypeToEntity
                .Where(kvp => typeof(ICaseMainEntity).IsAssignableFrom(kvp.Key))
                .Select(kvp => kvp.Value.ToLite());

            return AutocompleteUtils.Autocomplete(list, subString, count);
        }

        [Route("api/workflow/findNode"), HttpPost]
        public List<Lite<IWorkflowNodeEntity>> FindNode(WorkflowFindNodeRequest request)
        {
            var workflow = Lite.Create<WorkflowEntity>(request.workflowId);

            return WorkflowLogic.AutocompleteNodes(workflow, request.subString, request.count, request.excludes);
        }

        public class WorkflowFindNodeRequest
        {
            public int workflowId;
            public string subString;
            public int count;
            public List<Lite<IWorkflowNodeEntity>> excludes;
        }

        [Route("api/workflow/condition/test"), HttpPost]
        public WorkflowConditionTestResponse Test(WorkflowConditionTestRequest request)
        {
            IWorkflowConditionEvaluator evaluator;
            try
            {
                evaluator = request.workflowCondition.Eval.Algorithm;
            }
            catch (Exception e)
            {
                return new WorkflowConditionTestResponse
                {
                    compileError = e.Message
                };
            }

            try
            {
                return new WorkflowConditionTestResponse
                {
                    validationResult = evaluator.EvaluateUntyped(request.exampleEntity, new WorkflowTransitionContext(null, null, null, request.decisionResult))
                };
            }
            catch (Exception e)
            {
                return new WorkflowConditionTestResponse
                {
                    validationException = e.Message
                };
            }
        }

        public class WorkflowConditionTestRequest
        {
            public WorkflowConditionEntity workflowCondition;
            public ICaseMainEntity exampleEntity;
            public DecisionResult decisionResult;
        }

        public class WorkflowConditionTestResponse
        {
            public string compileError;
            public string validationException;
            public bool validationResult;
        }

        [Route("api/workflow/scriptRunner/view"), HttpGet]
        public WorkflowScriptRunnerState View()
        {
            WorkflowScriptRunnerPanelPermission.ViewWorkflowScriptRunnerPanel.AssertAuthorized();

            WorkflowScriptRunnerState state = WorkflowScriptRunner.ExecutionState();

            return state;
        }

        [Route("api/workflow/scriptRunner/start"), HttpPost]
        public void Start()
        {
            WorkflowScriptRunnerPanelPermission.ViewWorkflowScriptRunnerPanel.AssertAuthorized();

            WorkflowScriptRunner.StartRunningScripts(0);

            Thread.Sleep(1000);
        }

        [Route("api/workflow/scriptRunner/stop"), HttpPost]
        public void Stop()
        {
            WorkflowScriptRunnerPanelPermission.ViewWorkflowScriptRunnerPanel.AssertAuthorized();

            WorkflowScriptRunner.Stop();

            Thread.Sleep(1000);
        }

        [Route("api/workflow/caseflow/{caseId}"), HttpGet]
        public CaseFlow GetCaseFlow(string caseId)
        {
            var lite = Lite.ParsePrimaryKey<CaseEntity>(caseId);

            return CaseFlowLogic.GetCaseFlow(lite.Retrieve());
        }

        [Route("api/workflow/activityMonitor"), HttpPost]
        public WorkflowActivityMonitor GetWorkflowActivityMonitor(WorkflowActivityMonitorRequestTS request)
        {
            return WorkflowActivityMonitorLogic.GetWorkflowActivityMonitor(request.ToRequest());
        }
    }

    public class WorkflowActivityMonitorRequestTS
    {
        public Lite<WorkflowEntity> workflow;
        public List<FilterTS> filters;
        public List<ColumnTS> columns;

        public WorkflowActivityMonitorRequest ToRequest()
        {
            var qd = DynamicQueryManager.Current.QueryDescription(typeof(CaseActivityEntity));
            return new WorkflowActivityMonitorRequest
            {
                Workflow = workflow,
                Filters = filters.Select(f => f.ToFilter(qd, true)).ToList(),
                Columns = columns.Select(c => c.ToColumn(qd, true)).ToList(),
            };
        }
    }
}
