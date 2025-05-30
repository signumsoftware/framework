/// <reference path="../bpmn-js.ts" />
import NavigatedViewer from "bpmn-js/lib/NavigatedViewer"
import { DateTime, Duration } from 'luxon'
import { CaseActivityEntity, CaseNotificationEntity, DoneType, CaseFlowColor } from '../Signum.Workflow'
import { CustomRenderer } from './CustomRenderer'
import { WorkflowClient } from '../WorkflowClient'
import * as BpmnUtils from './BpmnUtils'
import { calculatePoint } from "../../Signum.Map/Utils"
import { getToString } from "@framework/Signum.Entities"
import { Color, Gradient } from "@framework/Basics/Color"
import { Rectangle } from "../../Signum.Map/Schema/ClientColorProvider"

export class CaseFlowRenderer extends CustomRenderer {
  static $inject: string[] = ['config.bpmnRenderer', 'eventBus', 'styles', 'pathMap', 'canvas', 'textRenderer'];
  constructor(config: any, eventBus: BPMN.EventBus, styles: any, pathMap: any, canvas: any, textRenderer: any, priority: number) {
    super(config, eventBus, styles, pathMap, canvas, textRenderer, 1200);
  }

  caseFlow!: WorkflowClient.CaseFlow;
  maxDuration!: number;
  viewer!: NavigatedViewer;
  caseFlowColor?: CaseFlowColor;

  drawConnection(visuals: any, element: BPMN.Connection): SVGElement {

    const path = super.drawConnection(visuals, element);

    const stats = this.caseFlow.connections[element.id];

    if (!stats)
      path.style.setProperty('stroke', "lightgray");
    else {
      const pathGroup = (path.parentNode as SVGGElement).parentNode as SVGGElement;
      const title = (Array.toArray(pathGroup.childNodes) as SVGElement[]).filter(a => a.nodeName == "title").firstOrNull() || pathGroup.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "title"));
      title.textContent = stats.filter(con => con.doneDate != null).map(con => `${DoneType.niceToString(con.doneType)} (${getToString(con.doneBy)} ${DateTime.fromISO(con.doneDate).toRelative()})`).join("\n");
    }

    return path;
  }

  gradient: Gradient = new Gradient([
    { value: 0, color: Color.parse("rgb(117, 202, 112)") },
    { value: 0.5, color: Color.parse("rgb(251, 214, 95)") },
    { value: 1, color: Color.parse("rgb(251, 114, 95)") },
  ]);

  drawShape(visuals: any, element: BPMN.DiElement): SVGElement {

    const result = super.drawShape(visuals, element);

    if (element.type == "label") {
      if (!this.caseFlow.allNodes.contains(element.businessObject.id) &&
        !this.caseFlow.connections[element.businessObject.id])
        result.style.setProperty('fill', "gray");
    }
    else if (element.type == "bpmn:StartEvent" ||
      element.type == "bpmn:EndEvent" ||
      BpmnUtils.isGatewayAnyKind(element.type)) {

      if (!this.caseFlow.allNodes.contains(element.id)) {
        result.style.setProperty('stroke', "lightgray");
        result.style.setProperty('fill', "#eee");
      }
    }
    else if (BpmnUtils.isTaskAnyKind(element.type)) {

      const stats = this.caseFlow.activities[element.id];
      if (!stats) {
        result.style.setProperty('stroke', "lightgray");
        result.style.setProperty('fill', "#eee");
      } else {
        const compare =
          this.caseFlowColor == "AverageDuration" ? (stats[0].averageDuration == undefined ? undefined : stats[0].averageDuration! * 2) :
            this.caseFlowColor == "EstimatedDuration" ? (stats[0].estimatedDuration == undefined ? undefined : stats[0].estimatedDuration! * 2) :
              this.caseFlowColor == "CaseMaxDuration" ? this.maxDuration : undefined;

        const sumDuration = stats.map(a => a.duration || 0).sum();

        if (compare != null && sumDuration > 0) {
          const color = this.gradient.getColor(sumDuration / compare);

          result.style.setProperty('stroke', color.lerp(0.5, Color.Black).toString());
          result.style.setProperty('fill', color.toString());
        }

        const gParent = ((result.parentNode as SVGGElement).parentNode as SVGGElement);
        const title = (Array.toArray(gParent.childNodes) as SVGElement[]).filter(a => a.nodeName == "title").firstOrNull() || gParent.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "title"));
        title.textContent = stats.map((a, i) => i == 0 || i == stats.length - 1 ? getTitle(a) :
          i == 1 ? `(…${CaseActivityEntity.niceCount(stats.length - 2)})` : "").filter(a => a).join("\n\n");

        const ggParent = gParent.parentNode as SVGGElement;

        const pathGroups = (Array.toArray(ggParent.childNodes) as SVGPathElement[]).filter(a => a.nodeName == "g" && a.className == "jump-group");
        const jumps = this.caseFlow.jumps.filter(j => j.fromBpmnElementId == element.id);

        const toCenteredRectangle = (bounds: BPMN.BoundsElement) => ({
          x: bounds.x + bounds.width / 2,
          y: bounds.y + bounds.height / 2,
          width: bounds.width,
          height: bounds.height
        }) as Rectangle;

        pathGroups.slice(jumps.length).forEach(path => (path.parentNode as SVGGElement).removeChild(path));

        if (jumps.length) {
          const moddleElements = ((this.viewer as any)._definitions.diagrams[0].plane.planeElement as BPMN.ModdleElement[]);

          const fromModdle = moddleElements.filter(a => a.id == (element.id + "_di")).single();
          const fromRec: Rectangle = toCenteredRectangle(fromModdle.bounds);

          jumps.forEach((jump, i) => {

            const pathGroup = pathGroups[i] || ggParent.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "g"));
            pathGroup.classList.add("jump-group");
            const path = Array.toArray(pathGroup.childNodes).filter(a => a.nodeName == "path").singleOrNull() as SVGPathElement || pathGroup.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "path"));
            const toModdle = moddleElements.filter(a => a.id == (jump.toBpmnElementId + "_di")).single();

            if (toModdle.id != fromModdle.id) {
              const toRec: Rectangle = toCenteredRectangle(toModdle.bounds);

              const fromPoint = calculatePoint(fromRec, toRec);
              const toPoint = calculatePoint(toRec, fromRec);

              const curveness = 0.2;
              const controlPoint = {
                x: (fromPoint.x! + toPoint.x!) / 2 + (toPoint.y! - fromPoint.y!) * curveness,
                y: (fromPoint.y! + toPoint.y!) / 2 - (toPoint.x! - fromPoint.x!) * curveness,
              };

              path.setAttribute("d", `M${fromPoint.x} ${fromPoint.y} Q ${controlPoint.x} ${controlPoint.y} ${toPoint.x} ${toPoint.y}`);
            } else {
              const unit = 30;

              const corner = { x: fromRec.x! + fromRec.width / 2, y: fromRec.y! - fromRec.height / 2 };

              const fromPoint = { x: corner.x, y: corner.y + unit };
              const fromCPoint = { x: corner.x + unit * 2, y: corner.y + unit / 2 };
              const toCPoint = { x: corner.x - unit / 2, y: corner.y - unit * 2 };
              const toPoint = { x: corner.x - unit, y: corner.y };
              path.setAttribute("d", `M${fromPoint.x} ${fromPoint.y} C ${fromCPoint.x} ${fromCPoint.y} ${toCPoint.x} ${toCPoint.y} ${toPoint.x} ${toPoint.y}`);

            }
            path.style.setProperty("fill", "transparent");
            path.style.setProperty("stroke-width", "2px");
            path.style.setProperty("stroke", getDoneColor(jump.doneType));
            path.style.setProperty("stroke-linejoin", "round");
            path.style.setProperty("stroke-dasharray", "5 5");
            path.style.setProperty("marker-end", "url(#sequenceflow-end-white-black)");

            const title = (Array.toArray(pathGroup.childNodes) as SVGElement[]).filter(a => a.nodeName == "title").firstOrNull() ||
              pathGroup.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "title"));

            title.textContent = `${DoneType.niceToString(jump.doneType)} (${getToString(jump.doneBy)} ${DateTime.fromISO(jump.doneDate).toRelative()})`;
          });
        }
      }
    }

    return result;
  }
}

function getDoneColor(doneType: DoneType) {
  switch (doneType) {
    case "Jump": return "blue";
    case "Timeout": return "gold";
    case "ScriptSuccess": return "green";
    case "ScriptFailure": return "violet";
    case "Next": return "#ff7504";
    default: return "magenta";
  }
}

function getTitle(stats: WorkflowClient.CaseActivityStats) {
  let result = `${getToString(stats.workflowActivity)} (${CaseNotificationEntity.nicePluralName()} ${stats.notifications})
${CaseActivityEntity.nicePropertyName(a => a.startDate)}: ${DateTime.fromISO(stats.startDate).toFormat("FFF")} (${DateTime.fromISO(stats.startDate).toRelative()})`;

  if (stats.doneDate != null)
    result += `
${CaseActivityEntity.nicePropertyName(a => a.doneDate)}: ${DateTime.fromISO(stats.doneDate).toFormat("FFF")} (${DateTime.fromISO(stats.doneDate).toRelative()})
${CaseActivityEntity.nicePropertyName(a => a.doneBy)}: ${stats.doneBy && getToString(stats.doneBy)} (${DoneType.niceToString(stats.doneType!)})
${CaseActivityEntity.nicePropertyName(a => a.duration)}: ${formatMinutes(stats.duration)}`;

  result += `
${CaseFlowColor.niceToString("AverageDuration")}: ${formatMinutes(stats.averageDuration)}
${CaseFlowColor.niceToString("EstimatedDuration")}: ${formatMinutes(stats.estimatedDuration)}`;

  return result;
}


function formatMinutes(minutes: number | undefined) {

  if (minutes == undefined)
    return "";

  return WorkflowClient.formatDuration(Duration.fromObject({ minutes }).shiftTo("days", "hours", "minutes"));
}

export const __init__: string[] = ['caseFlowRenderer'];
export const caseFlowRenderer: (string | typeof CaseFlowRenderer)[] = ['type', CaseFlowRenderer];
