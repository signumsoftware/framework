using Signum.API;
using Signum.Eval.TypeHelp;

namespace Signum.Workflow;


public static class WorkflowLogicStarter
{
    public static void Start(SchemaBuilder sb, Func<WorkflowConfigurationEmbedded> getConfiguration)
    {
        TypeHelpLogic.Start(sb);
        WorkflowLogic.Start(sb, getConfiguration);
        CaseActivityLogic.Start(sb);
        WorkflowEventTaskLogic.Start(sb);
    }
}
