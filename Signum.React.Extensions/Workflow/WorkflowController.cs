using Signum.Entities.Workflow;
using Signum.Engine.Workflow;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.React.Facades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;

namespace Signum.React.Workflow
{
    public class WorkflowController : ApiController
    {
        [Route("api/workflow/fetchForViewing/{caseActivityId}"), HttpGet]
        public EntityPackWorkflow GetEntity(string caseActivityId)
        {
            var lite = Lite.ParsePrimaryKey<CaseActivityEntity>(caseActivityId);

            var activity = CaseLogic.RetrieveForViewing(lite);

            return new EntityPackWorkflow
            {
                activity = activity,
                canExecuteActivity = OperationLogic.ServiceCanExecute(activity).ToDictionary(a => a.Key.Key, a => a.Value),
                canExecuteMainEntity = OperationLogic.ServiceCanExecute((Entity)activity.Case.MainEntity).ToDictionary(a => a.Key.Key, a => a.Value),
            };
        }

        public class EntityPackWorkflow
        {
            public Entity activity { get; set; }
            public Dictionary<string, string> canExecuteActivity { get; set; }
            public Dictionary<string, string> canExecuteMainEntity { get; set; }
        }

        [Route("api/workflow/starts"), HttpGet]
        public List<Lite<WorkflowEntity>> Starts()
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

        [Route("api/workflow/condition/test"), HttpPost]
        public WorkflowConditionTestResponse Test(WorkflowConditionTestRequest request)
        {
            IWorkflowEvaluator evaluator;
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
                    validationResult = evaluator.EvaluateUntyped(request.exampleEntity, new WorkflowEvaluationContext { CaseActivity = null, DecisionResult = request.decisionResult })
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
    }
}
