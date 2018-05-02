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

