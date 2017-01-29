/// <reference path="../bpmn-js.d.ts" />
import Modeler = require("bpmn-js/lib/Modeler");
import BpmnRenderer = require("bpmn-js/lib/draw/BpmnRenderer");
import { WorkflowConditionEntity, WorkflowActionEntity, DecisionResult } from '../Signum.Entities.Workflow'
import { Lite, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'

export class CustomRenderer extends BpmnRenderer {

    constructor(eventBus: BPMN.EventBus, styles: any, pathMap: any, canvas: any, priority: number) {
        super(eventBus, styles, pathMap, canvas, 1200);
    }

    getDecisionResult : (element: BPMN.DiElement) => DecisionResult | undefined; 

    drawConnection(visuals: any, element: BPMN.DiElement) {
    
        var result = super.drawConnection(visuals, element);
       
        if (element.type == 'bpmn:SequenceFlow') {

            var dr = this.getDecisionResult(element);

            if(dr)
                result.style.setProperty('stroke', dr == "Approve" ? "#0c9c01" : "#c71a01");
        }

        return result;
    }
}


export var __init__ = ['customRenderer'];
export var customRenderer = ['type', CustomRenderer];