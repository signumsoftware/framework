
namespace Signum.Workflow;

public class WorkflowConfigurationEmbedded : EmbeddedEntity
{
    [Unit("sec")]
    public int ScriptRunnerPeriod { get; set; } = 5 * 60; //5 minutes

    [Unit("hrs")]
    public double? AvoidExecutingScriptsOlderThan { get; set; }

    public int ChunkSizeRunningScripts { get; set; } = 100;
}
