import * as React from 'react'
import { Tabs, Tab } from 'reactstrap'


 export function isPool(elementType: string): boolean {
    return (elementType == "bpmn:Participant");
}

export function isLane(elementType: string): boolean {
    return (elementType == "bpmn:Lane");
}

export function isEvent(elementType: string): boolean {
    return isStartEvent(elementType) || isEndEvent(elementType);
}

export function isStartEvent(elementType: string): boolean {
    return (elementType == "bpmn:StartEvent")
}

export function isTimerStartEvent(definitionType: string): boolean {
    return (definitionType == "bpmn:TimerEventDefinition");
}

export function isConditionalStartEvent(definitionType: string): boolean {
    return (definitionType == "bpmn:ConditionalEventDefinition");
}

export function isEndEvent(elementType: string): boolean {
    return (elementType == "bpmn:EndEvent");
}

export function isIntermediateThrowEvent(elementType: string): boolean {
    return (elementType == "bpmn:IntermediateThrowEvent");
}

export function isTaskAnyKind(elementType: string): boolean {
    return isTask(elementType) || isUserTask(elementType) || isCallActivity(elementType) || isScriptTask(elementType);
}

export function isTask(elementType: string): boolean {
    return (elementType == "bpmn:Task");
}

export function isUserTask(elementType: string): boolean {
    return (elementType == "bpmn:UserTask");
}

export function isCallActivity(elementType: string): boolean {
    return (elementType == "bpmn:CallActivity");
}

export function isScriptTask(elementType: string): boolean {
    return (elementType == "bpmn:ScriptTask");
}

export function isGatewayAnyKind(elementType: string): boolean {
    return isExclusiveGateway(elementType) || isInclusiveGateway(elementType) || isParallelGateway(elementType);
}

export function isExclusiveGateway(elementType: string): boolean {
    return (elementType == "bpmn:ExclusiveGateway");
}

export function isInclusiveGateway(elementType: string): boolean {
    return (elementType == "bpmn:InclusiveGateway");
}

export function isParallelGateway(elementType: string): boolean {
    return (elementType == "bpmn:ParallelGateway");
}

export function isConnection(elementType: string): boolean {
    return (isSequenceFlowConnection(elementType) || isMessageFlowConnection(elementType));
}

export function isSequenceFlowConnection(elementType: string): boolean {
    return (elementType == "bpmn:SequenceFlow");
}

export function isMessageFlowConnection(elementType: string): boolean {
    return (elementType == "bpmn:MessageFlow");
}

export function isLabel(elementType: string): boolean {
    return (elementType == "label");
}

export function isTextAnnotation(elementType: string): boolean {
    return (elementType == "bpmn:TextAnnotation");
}

export function isDataObjectReference(elementType: string): boolean {
    return (elementType == "bpmn:DataObjectReference");
}

export function isDataStoreReference(elementType: string): boolean {
    return (elementType == "bpmn:DataStoreReference");
}
