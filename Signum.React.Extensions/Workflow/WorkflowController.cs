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
using Signum.React.Filters;
using static Signum.React.ApiControllers.OperationController;
using Signum.Entities.Reflection;

namespace Signum.React.Workflow
{
    public class WorkflowController : ApiController
    {
        [Route("api/workflow/fetchForViewing/{caseActivityId}"), HttpGet]
        public EntityPackWorkflow GetEntity(string caseActivityId)
        {
            var lite = Lite.ParsePrimaryKey<CaseActivityEntity>(caseActivityId);

            var activity = CaseActivityLogic.RetrieveForViewing(lite);
            using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = activity }))
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
        public WorkflowModelAndIssues GetWorkflowModel(string workflowId)
        {
            var id = PrimaryKey.Parse(workflowId, typeof(WorkflowEntity));
            var wf = Database.Retrieve<WorkflowEntity>(id);
            var model = WorkflowLogic.GetWorkflowModel(wf);
            var wb = new WorkflowBuilder(wf);
            List<WorkflowIssue> issues = new List<WorkflowIssue>();
            wb.ValidateGraph(issues);
            return new WorkflowModelAndIssues
            {
                model = model,
                issues = issues,
            };
        }

        public class WorkflowModelAndIssues
        {
            public WorkflowModel model;
            public List<WorkflowIssue> issues;
        }

        [Route("api/workflow/previewChanges/{workflowId}"), HttpPost]
        public PreviewResult PreviewChanges(string workflowId, WorkflowModel model)
        {
            var id = PrimaryKey.Parse(workflowId, typeof(WorkflowEntity));
            var wf = Database.Retrieve<WorkflowEntity>(id);
            return WorkflowLogic.PreviewChanges(wf, model);
        }


        [Route("api/workflow/save"), HttpPost, ValidateModelFilter]
        public EntityPackWithIssues SaveWorkflow(EntityOperationRequest request)
        {
            WorkflowEntity entity;
            List<WorkflowIssue> issuesContainer = new List<WorkflowIssue>();
            try
            {
                entity = ((WorkflowEntity)request.entity).Execute(WorkflowOperation.Save, request.args.And(issuesContainer).ToArray());
            }
            catch (IntegrityCheckException ex)
            {
                GraphExplorer.SetValidationErrors(GraphExplorer.FromRoot(request.entity), ex);
                this.Validate(request, "request");
                this.ModelState.AddModelError("workflowIssues", JsonConvert.SerializeObject(issuesContainer, GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings));
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, this.ModelState));
            }

            return new EntityPackWithIssues { entityPack = SignumServer.GetEntityPack(entity), issues = issuesContainer };
        }

        public class EntityPackWithIssues
        {
            public EntityPackTS entityPack;
            public List<WorkflowIssue> issues;
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
                    validationResult = evaluator.EvaluateUntyped(request.exampleEntity, new WorkflowTransitionContext(null, null, null))
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
            WorkflowPanelPermission.ViewWorkflowPanel.AssertAuthorized();

            WorkflowScriptRunnerState state = WorkflowScriptRunner.ExecutionState();

            return state;
        }

        [Route("api/workflow/scriptRunner/start"), HttpPost]
        public void Start()
        {
            WorkflowPanelPermission.ViewWorkflowPanel.AssertAuthorized();

            WorkflowScriptRunner.StartRunningScripts(0);

            Thread.Sleep(1000);
        }

        [Route("api/workflow/scriptRunner/stop"), HttpPost]
        public void Stop()
        {
            WorkflowPanelPermission.ViewWorkflowPanel.AssertAuthorized();

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

        [Route("api/workflow/nextConnections"), HttpPost]
        public List<Lite<IWorkflowNodeEntity>> GetNextJumps(NextConnectionsRequest request)
        {
            return request.workflowActivity.RetrieveAndForget()
                .NextConnectionsFromCache(request.connectionType)
                .Select(a => a.To.ToLite())
                .ToList();
        }
    }

    public class NextConnectionsRequest
    {
        public Lite<WorkflowActivityEntity> workflowActivity;
        public ConnectionType connectionType;

    }

    public class WorkflowActivityMonitorRequestTS
    {
        public Lite<WorkflowEntity> workflow;
        public List<FilterTS> filters;
        public List<ColumnTS> columns;

        public WorkflowActivityMonitorRequest ToRequest()
        {
            var qd = QueryLogic.Queries.QueryDescription(typeof(CaseActivityEntity));
            return new WorkflowActivityMonitorRequest
            {
                Workflow = workflow,
                Filters = filters.Select(f => f.ToFilter(qd, true)).ToList(),
                Columns = columns.Select(c => c.ToColumn(qd, true)).ToList(),
            };
        }
    }
}
