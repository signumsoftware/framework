/// <reference path="../bpmn-js.d.ts" />
import BpmnRenderer from "bpmn-js/lib/draw/BpmnRenderer"
import { BootstrapStyle } from "../../Basics/Signum.Entities.Basics";
import { ConnectionType } from '../Signum.Entities.Workflow'
import * as BpmnUtils from './BpmnUtils'

function BootstrapStyleToColor(style: BootstrapStyle): string {
  switch (style) {
    case "Light": return "#f8f9fa";
    case "Dark": return "#343a40";
    case "Primary": return "#007bff";
    case "Secondary": return "#6c757d";
    case "Success": return "#28a745";
    case "Info": return "#17a2b8";
    case "Warning": return "#ffc107";
    case "Danger": return "#dc3545";
    default: return "black";
  }
}

export class CustomRenderer extends BpmnRenderer {
  static $inject = ['config.bpmnRenderer', 'eventBus', 'styles', 'pathMap', 'canvas', 'textRenderer'];
  constructor(config: any, eventBus: BPMN.EventBus, styles: any, pathMap: any, canvas: any, textRenderer: any, priority: number) {
    super(config, eventBus, styles, pathMap, canvas, textRenderer, 1200);
  }

  getConnectionType!: (element: BPMN.DiElement) => ConnectionType | undefined;
  getDecisionStyle!: (element: BPMN.DiElement) => BootstrapStyle| undefined;

  drawConnection(visuals: any, element: BPMN.DiElement) {
    var result = super.drawConnection(visuals, element);
    var ct = this.getConnectionType && this.getConnectionType(element);
    var cs = this.getDecisionStyle && this.getDecisionStyle(element);

    if (ct && ct != "Normal")
      result.style.setProperty('stroke',
          ct == "Jump" ? "blue" :
          ct == "ScriptException" ? "magenta" :
            ct == "Decision" && cs ? BootstrapStyleToColor(cs) :
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
