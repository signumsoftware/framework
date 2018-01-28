using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Workflow
{
    //Only for split and join
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class CaseJunctionEntity : Entity
    {
        public WorkflowGatewayDirection Direction { get; set; }

        [NotNullValidator]
        public Lite<CaseActivityEntity> From { get; set; }

        [NotNullValidator]
        public Lite<CaseActivityEntity> To { get; set; }
    }

   

}
