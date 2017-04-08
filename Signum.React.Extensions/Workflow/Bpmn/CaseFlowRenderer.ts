/// <reference path="../bpmn-js.d.ts" />
import Modeler = require("bpmn-js/lib/Modeler");
import BpmnRenderer = require("bpmn-js/lib/draw/BpmnRenderer");
import * as moment from 'moment'
import { WorkflowConditionEntity, WorkflowActionEntity, DecisionResult, CaseActivityEntity, CaseNotificationEntity, DoneType, WorkflowActivityEntity, CaseFlowColor } from '../Signum.Entities.Workflow'
import { Lite, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { CustomRenderer } from './CustomRenderer'
import { Color, Gradient } from '../../Basics/Color'
import { CaseFlow, CaseConnectionStats, CaseActivityStats  } from '../WorkflowClient'
import * as BpmnUtils from './BpmnUtils'
require("moment-duration-format");

export class CaseFlowRenderer extends CustomRenderer {

    constructor(eventBus: BPMN.EventBus, styles: any, pathMap: any, canvas: any, priority: number) {
        super(eventBus, styles, pathMap, canvas, 1200);
    }

    caseFlow: CaseFlow;

    drawConnection(visuals: any, element: BPMN.DiElement) {

        var result = super.drawConnection(visuals, element);

        var stats = this.caseFlow.Connections[element.id];

        if (!stats)
            result.style.setProperty('stroke', "lightgray");

        return result;
    }    

    caseFlowColor?: CaseFlowColor;
    maxDuration: number;

    gradient = new Gradient([
        { value: 0, color: Color.parse("rgb(117, 202, 112)")},
        { value: 0.5, color: Color.parse("rgb(251, 214, 95)") },
        { value: 1, color: Color.parse("rgb(251, 114, 95)") },
    ]);
    
    drawShape(visuals: any, element: BPMN.DiElement) {
        
        var result = super.drawShape(visuals, element);

        if (BpmnUtils.isLabel(element.type)) {
            if (!this.caseFlow.AllNodes.contains(element.businessObject.id) &&
                !this.caseFlow.Connections[element.businessObject.id])
                result.style.setProperty('fill', "gray");
        }
        else if (BpmnUtils.isStartEvent(element.type) ||
            BpmnUtils.isEndEvent(element.type) ||
            BpmnUtils.isGatewayAnyKind(element.type)) {

            if (!this.caseFlow.AllNodes.contains(element.id)) {
                result.style.setProperty('stroke', "lightgray");
                result.style.setProperty('fill', "#eee");

                //debugger;
            }
        }
        else if (BpmnUtils.isTaskAnyKind(element.type)) {

            var stats = this.caseFlow.Activities[element.id];
            if (!stats) {
                result.style.setProperty('stroke', "lightgray");
                result.style.setProperty('fill', "#eee");
            } else {
                var compare =
                    this.caseFlowColor == "AverageDuration" ? (stats[0].AverageDuration == undefined ? undefined : stats[0].AverageDuration! * 2) :
                        this.caseFlowColor == "EstimatedDuration" ? (stats[0].EstimatedDuration == undefined ? undefined : stats[0].EstimatedDuration! * 2) :
                            this.caseFlowColor == "CaseMaxDuration" ? this.maxDuration : undefined;

                var sumDuration = stats.map(a => a.Duration || 0).sum();          

                if (compare != null && sumDuration > 0) {
                    var color = this.gradient.getColor(sumDuration / compare);

                    result.style.setProperty('stroke', color.lerp(0.5, Color.Black).toString());
                    result.style.setProperty('fill', color.toString());
                }

                var gParent = ((result.parentNode as SVGGElement).parentNode as SVGGElement);
                var title = Array.toArray(gParent.childNodes).filter((a: SVGElement) => a.nodeName == "title").firstOrNull() || gParent.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "title"));
                title.textContent = stats.map(a => getTitle(a)).join("\n");

                //var ggParent = gParent.parentNode as SVGGElement;
                //var path = Array.toArray(ggParent.childNodes).filter((a: SVGElement) => a.nodeName == "path").firstOrNull() as SVGPathElement || ggParent.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "path"));
                //debugger;
                //path.setAttribute("d", `m  ${element.x},${element.y}L${element.x + element.width},${element.y + element.height}`);
                //path.style.setProperty("fill", "none");
                //path.style.setProperty("stroke-width", "2px");
                //path.style.setProperty("stroke", "black");
                //path.style.setProperty("stroke-linejoin", "round");
                //path.style.setProperty("marker-end", "url(#sequenceflow-end-white-black)");
            }
        }
      
        return result;
    }
}

function getTitle(stats: CaseActivityStats) {
    var result = `${stats.WorkflowActivity.toStr} (${CaseNotificationEntity.nicePluralName()} ${stats.Notifications})
${CaseActivityEntity.nicePropertyName(a => a.startDate)}: ${moment(stats.StartOn).format("L LT")} (${moment(stats.StartOn).fromNow()})`;

    if (stats.DoneDate != null)
        result += `
${CaseActivityEntity.nicePropertyName(a => a.doneDate)}: ${moment(stats.DoneDate).format("L LT")} (${moment(stats.DoneDate).fromNow()})
${CaseActivityEntity.nicePropertyName(a => a.doneBy)}: ${stats.DoneBy && stats.DoneBy.toStr} (${DoneType.niceName(stats.DoneType!)})
${CaseActivityEntity.nicePropertyName(a => a.duration)}: ${formatDuration(stats.Duration)}`;

    result += `
${CaseFlowColor.niceName("AverageDuration")}: ${formatDuration(stats.AverageDuration)}
${CaseFlowColor.niceName("EstimatedDuration")}: ${formatDuration(stats.EstimatedDuration)}
`;

    return result;
}


function formatDuration(minutes: number | undefined) {

    if (minutes == undefined)
        return "";

    return moment.duration(minutes, "minutes").format("d[d] h[h] m[m] s[s]");
}

export var __init__ = ['caseFlowRenderer'];
export var caseFlowRenderer = ['type', CaseFlowRenderer];