/// <reference path="../bpmn-js.d.ts" />
import * as Modeler from "bpmn-js/lib/Modeler"
import * as BpmnRenderer from "bpmn-js/lib/draw/BpmnRenderer"
import { WorkflowConditionEntity, WorkflowActionEntity, DecisionResult } from '../Signum.Entities.Workflow'
import { Lite, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as BpmnUtils from './BpmnUtils'

export class CustomRenderer extends BpmnRenderer {

    constructor(eventBus: BPMN.EventBus, styles: any, pathMap: any, canvas: any, priority: number) {
        super(eventBus, styles, pathMap, canvas, 1200);
    }

    getDecisionResult!: (element: BPMN.DiElement) => DecisionResult | undefined; 

    drawConnection(visuals: any, element: BPMN.DiElement) {

        var result = super.drawConnection(visuals, element);
        
        var dr = this.getDecisionResult && this.getDecisionResult(element);

        if (dr)
            result.style.setProperty('stroke', dr == "Approve" ? "#0c9c01" : "#c71a01");
            
        return result;
    }

    drawShape(visuals: any, element: BPMN.DiElement) {

        var result = super.drawShape(visuals, element);

        var strokeColor: string = "";
        var fillColor: string = "";

        if (element.type == "bpmn:StartEvent") {
            strokeColor = "#62A716";
            fillColor = "#E6FF97";
        }
        else if (element.type == "bpmn:EndEvent") {
            strokeColor = "#990000";
            fillColor = "#EEAAAA";
        }
        else if (element.type == "bpmn:IntermediateThrowEvent" || element.type == "bpmn:IntermediateCatchEvent") {
            strokeColor = "#A09B58";
            fillColor = "#FEFAEF";
        }
        else if (BpmnUtils.isTaskAnyKind(element.type)) {
            strokeColor = "#03689A";
            fillColor = "#ECEFFF";
        }
        else if (BpmnUtils.isGatewayAnyKind(element.type)) {
            strokeColor = "#ACAC28";
            fillColor = "#FFFFCC";
        }
        else if (element.type == "bpmn:TextAnnotation" || element.type == "bpmn:DataObjectReference" || element.type == "bpmn:DataStoreReference") {
            strokeColor = "#666666";
            fillColor = "#F0F0F0";
        };

        if (strokeColor.length > 0)
            result.style.setProperty('stroke', strokeColor);

        if (fillColor.length > 0)
            result.style.setProperty('fill', fillColor);

        return result;
    }
}

export var __init__ = ['customRenderer'];
export var customRenderer = ['type', CustomRenderer];