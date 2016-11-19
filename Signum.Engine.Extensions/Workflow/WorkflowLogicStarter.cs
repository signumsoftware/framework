using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Logic.Workflow
{

    public static class WorkflowLogicStarter
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            WorkflowLogic.Start(sb, dqm);
            CaseLogic.Start(sb, dqm);
        }
    }
}
