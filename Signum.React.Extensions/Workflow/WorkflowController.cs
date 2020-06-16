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
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Engine.Authorization;
using Newtonsoft.Json;
using Signum.Utilities;
using Signum.React.ApiControllers;
using Microsoft.AspNetCore.Mvc;
using Signum.React.Filters;
using static Signum.React.ApiControllers.OperationController;
using Signum.Entities.Reflection;
using System.ComponentModel.DataAnnotations;

namespace Signum.React.Workflow
{
    [ValidateModelFilter]
    public class WorkflowController : Controller
    {
        [HttpGet("api/workflow/fetchForViewing/{caseActivityId}")]
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
                    Extension = ep.extension,
                };
            }
        }

        [HttpGet("api/workflow/tags/{caseId}")]
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
            public Dictionary<string, object?> Extension { get; set; } = new Dictionary<string, object?>();
        }

        [HttpGet("api/workflow/starts")]
        public List<WorkflowEntity> Starts()
        {
            return WorkflowLogic.GetAllowedStarts();
        }

        [HttpGet("api/workflow/workflowModel/{workflowId}")]
        public WorkflowModelAndIssues GetWorkflowModel(string workflowId)
        {
            var id = PrimaryKey.Parse(workflowId, typeof(WorkflowEntity));
            var wf = Database.Retrieve<WorkflowEntity>(id);
            var model = WorkflowLogic.GetWorkflowModel(wf);
            var wb = new WorkflowBuilder(wf);
            List<WorkflowIssue> issues = new List<WorkflowIssue>();
            wb.ValidateGraph(issues);
            return new WorkflowModelAndIssues(model, issues);
        }

        public class WorkflowModelAndIssues
        {
            public WorkflowModel model;
            public List<WorkflowIssue> issues;

            public WorkflowModelAndIssues(WorkflowModel model, List<WorkflowIssue> issues)
            {
                this.model = model;
                this.issues = issues;
            }
        }

        [HttpPost("api/workflow/previewChanges/{workflowId}")]
        public PreviewResult PreviewChanges(string workflowId, [Required, FromBody]WorkflowModel model)
        {
            var id = PrimaryKey.Parse(workflowId, typeof(WorkflowEntity));
            var wf = Database.Retrieve<WorkflowEntity>(id);
            return WorkflowLogic.PreviewChanges(wf, model);
        }


        [HttpPost("api/workflow/save"), ValidateModelFilter]
        public ActionResult<EntityPackWithIssues> SaveWorkflow([Required, FromBody]EntityOperationRequest request)
        {
            WorkflowEntity entity;
            List<WorkflowIssue> issuesContainer = new List<WorkflowIssue>();
            try
            {
                entity = ((WorkflowEntity)request.entity).Execute(WorkflowOperation.Save, (request.args.EmptyIfNull()).And(issuesContainer).ToArray());
            }
            catch (IntegrityCheckException ex)
            {
                GraphExplorer.SetValidationErrors(GraphExplorer.FromRoot(request.entity), ex);
                this.TryValidateModel(request, "request");
                this.ModelState.AddModelError("workflowIssues", JsonConvert.SerializeObject(issuesContainer, SignumServer.JsonSerializerSettings));
                return BadRequest(this.ModelState);
            }

            return new EntityPackWithIssues(SignumServer.GetEntityPack(entity), issuesContainer);
        }

        public class EntityPackWithIssues
        {
            public EntityPackTS entityPack;
            public List<WorkflowIssue> issues;

            public EntityPackWithIssues(EntityPackTS entityPack, List<WorkflowIssue> issues)
            {
                this.entityPack = entityPack;
                this.issues = issues;
            }
        }

        [HttpGet("api/workflow/findMainEntityType")]
        public List<Lite<TypeEntity>> FindMainEntityType(string subString, int count)
        {
            var list = TypeLogic.TypeToEntity
                .Where(kvp => typeof(ICaseMainEntity).IsAssignableFrom(kvp.Key))
                .Select(kvp => kvp.Value.ToLite());

            return AutocompleteUtils.Autocomplete(list, subString, count);
        }

        [HttpPost("api/workflow/findNode")]
        public List<Lite<IWorkflowNodeEntity>> FindNode([Required, FromBody]WorkflowFindNodeRequest request)
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

        [HttpPost("api/workflow/condition/test")]
        public WorkflowConditionTestResponse Test([Required, FromBody]WorkflowConditionTestRequest request)
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

        [HttpGet("api/workflow/scriptRunner/view")]
        public WorkflowScriptRunnerState ViewScriptRunner()
        {
            WorkflowPermission.ViewWorkflowPanel.AssertAuthorized();

            WorkflowScriptRunnerState state = WorkflowScriptRunner.ExecutionState();

            return state;
        }

        [HttpPost("api/workflow/scriptRunner/start")]
        public void StartScriptRunner()
        {
            WorkflowPermission.ViewWorkflowPanel.AssertAuthorized();

            WorkflowScriptRunner.StartRunningScripts(0);

            Thread.Sleep(1000);
        }

        [HttpPost("api/workflow/scriptRunner/stop")]
        public void StopScriptRunner()
        {
            WorkflowPermission.ViewWorkflowPanel.AssertAuthorized();

            WorkflowScriptRunner.Stop();

            Thread.Sleep(1000);
        }

        [HttpGet("api/workflow/caseflow/{caseId}")]
        public CaseFlow GetCaseFlow(string caseId)
        {
            var lite = Lite.ParsePrimaryKey<CaseEntity>(caseId);

            return CaseFlowLogic.GetCaseFlow(lite.RetrieveAndRemember());
        }

        [HttpPost("api/workflow/activityMonitor")]
        public WorkflowActivityMonitor GetWorkflowActivityMonitor([Required, FromBody]WorkflowActivityMonitorRequestTS request)
        {
            return WorkflowActivityMonitorLogic.GetWorkflowActivityMonitor(request.ToRequest());
        }

        [HttpPost("api/workflow/nextConnections")]
        public List<Lite<IWorkflowNodeEntity>> GetNextJumps([Required, FromBody]NextConnectionsRequest request)
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
