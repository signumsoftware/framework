namespace Signum.Workflow;


public static class WorkflowLogicStarter
{
    public static void Start(SchemaBuilder sb, Func<WorkflowConfigurationEmbedded> getConfiguration)
    {
        WorkflowLogic.Start(sb, getConfiguration);
        CaseActivityLogic.Start(sb);
        WorkflowEventTaskLogic.Start(sb);
    }
}
