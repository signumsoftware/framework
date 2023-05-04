
namespace Signum.Workflow;

//Only for split and join
[EntityKind(EntityKind.System, EntityData.Transactional)]
public class CaseJunctionEntity : Entity
{
    public WorkflowGatewayDirection Direction { get; set; }

    
    public Lite<CaseActivityEntity> From { get; set; }

    
    public Lite<CaseActivityEntity> To { get; set; }
}



