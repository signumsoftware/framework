/// <reference path="../bpmn-js.d.ts" />
import Modeler from "bpmn-js/lib/Modeler"
import BpmnRenderer from "bpmn-js/lib/draw/BpmnRenderer"
import { WorkflowConditionEntity, WorkflowActionEntity, ConnectionType } from '../Signum.Entities.Workflow'
import { Lite, liteKey } from '@framework/Signum.Entities'
import * as BpmnUtils from './BpmnUtils'

export class CustomRenderer extends BpmnRenderer {

    static $inject = ['config.bpmnRenderer', 'eventBus', 'styles', 'pathMap', 'canvas', 'textRenderer'];
    constructor(config: any, eventBus: BPMN.EventBus, styles: any, pathMap: any, canvas: any, textRenderer: any, priority: number) {
        super(config, eventBus, styles, pathMap, canvas, textRenderer, 1200);
    }

    getConnectionType!: (element: BPMN.DiElement) => ConnectionType | undefined; 

    drawConnection(visuals: any, element: BPMN.DiElement) {

        var result = super.drawConnection(visuals, element);

        var ct = this.getConnectionType && this.getConnectionType(element);

        if (ct && ct != "Normal")
            result.style.setProperty('stroke',
                ct == "Approve" ? "#0c9c01" :
                    ct == "Decline" ? "#c71a01" :
                        ct == "Jump" ? "blue" :
                            ct == "ScriptException" ? "magenta" :
                                "gray");
            
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