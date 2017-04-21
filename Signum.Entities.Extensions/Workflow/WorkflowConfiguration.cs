using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Workflow
{
    [Serializable]
    public class WorkflowConfigurationEmbedded : EmbeddedEntity
    {
        [Unit("sec")]
        public int ScriptRunnerPeriod { get; set; } = 5 * 60; //5 minutes

        [Unit("hs")]
        public double? AvoidExecutingScriptsOlderThan { get; set; }

        public int ChunkSizeRunningScripts { get; set; } = 100;
    }
}
