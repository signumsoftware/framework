/// <reference path="../bpmn-js.d.ts" />
import BpmnRenderer from "bpmn-js/lib/draw/BpmnRenderer"
import { BootstrapStyle } from "../../Basics/Signum.Entities.Basics";
import { ConnectionType } from '../Signum.Entities.Workflow'
import * as BpmnUtils from './BpmnUtils'

const bootstrapStyleToColor: { [style: string /* BootstrapStyle*/]: string } = {
  "Light": "#f8f9fa",
  "Dark": "#343a40",
  "Primary": "#007bff",
  "Secondary": "#6c757d",
  "Success": "#28a745",
  "Info": "#17a2b8",
  "Warning": "#ffc107",
  "Danger": "#dc3545",
};

export class CustomRenderer extends BpmnRenderer {
  static $inject = ['config.bpmnRenderer', 'eventBus', 'styles', 'pathMap', 'canvas', 'textRenderer'];
  constructor(config: any, eventBus: BPMN.EventBus, styles: any, pathMap: any, canvas: any, textRenderer: any, priority: number) {
    super(config, eventBus, styles, pathMap, canvas, textRenderer, 1200);
  }

  getConnectionType!: (element: BPMN.Connection) => ConnectionType | undefined;
  getDecisionStyle!: (element: BPMN.Connection) => BootstrapStyle | undefined;

  drawConnection(visuals: any, element: BPMN.Connection) {
    var result = super.drawConnection(visuals, element);
    var ct = this.getConnectionType(element);
    var ds = this.getDecisionStyle(element);

    if (ct && ct != "Normal")
      result.style.setProperty('stroke',
        ct == "Jump" ? "blue" :
          ct == "ScriptException" ? "magenta" :
            ct == "Decision" && ds ? (bootstrapStyleToColor[ds] ?? "black") :
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
    }
    else if (element.type == "bpmn:Lane") {
      strokeColor = "#CCCCCC";
      fillColor = "#FFFFFF";
    }
    else if (element.type == "bpmn:Participant") {
      strokeColor = "#CCCCCC";
      fillColor = "#FFFFFF";
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


