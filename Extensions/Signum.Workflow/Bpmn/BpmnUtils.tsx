import { WorkflowActivityModel, WorkflowConnectionModel, WorkflowEntitiesDictionary } from "../Signum.Workflow";

export function isEvent(elementType: BPMN.ElementType): boolean {
  return elementType == "bpmn:StartEvent" ||
    elementType == "bpmn:EndEvent";
}

export function isTaskAnyKind(elementType: BPMN.ElementType): boolean {
  return elementType == "bpmn:Task" ||
    elementType == "bpmn:UserTask" ||
    elementType == "bpmn:CallActivity" ||
    elementType == "bpmn:ScriptTask";
}

export function isGatewayAnyKind(elementType: BPMN.ElementType): boolean {
  return elementType == "bpmn:ExclusiveGateway" ||
    elementType == "bpmn:InclusiveGateway" ||
    elementType == "bpmn:ParallelGateway";
}

export function isConnection(elementType: BPMN.ElementType): boolean {
  return elementType == "bpmn:SequenceFlow" ||
    elementType == "bpmn:MessageFlow";
}


export function findDecisionStyle(con: BPMN.Connection, entities: WorkflowEntitiesDictionary) {
  var mod = entities[con.id] as (WorkflowConnectionModel | undefined);
  if (mod && mod.type == "Decision") {

    const gateway = (con.businessObject as BPMN.ConnectionModdleElemnet).sourceRef;

    const activities = gateway.incoming!
      .filter(c => c.sourceRef.$type == "bpmn:Task" || c.sourceRef.$type == "bpmn:UserTask")
      .map(c => entities[c.sourceRef.id] as WorkflowActivityModel);

    const doe = activities
      .flatMap(a => a.decisionOptions)
      .firstOrNull(dco => dco.element.name == mod!.decisionOptionName);

    return doe?.element.style;
  }
  return undefined;
}
