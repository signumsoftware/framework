using Signum.Engine.Maps;
using Signum.Entities.Workflow;
using System;

namespace Signum.Engine.Workflow
{

    public static class WorkflowLogicStarter
    {
        public static void Start(SchemaBuilder sb, Func<WorkflowConfigurationEmbedded> getConfiguration)
        {
            WorkflowLogic.Start(sb, getConfiguration);
            CaseActivityLogic.Start(sb);
            WorkflowEventTaskLogic.Start(sb);
        }
    }
}
