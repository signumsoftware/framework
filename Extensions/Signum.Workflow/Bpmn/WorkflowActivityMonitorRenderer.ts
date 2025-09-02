/// <reference path="../bpmn-js.ts" />
import { Duration } from 'luxon'
import { WorkflowModel, WorkflowActivityModel } from '../Signum.Workflow'
import { Color, Gradient } from '@framework/Basics/Color'
import { CustomRenderer } from './CustomRenderer'
import { WorkflowClient } from '../WorkflowClient'
import * as BpmnUtils from './BpmnUtils'
import NavigatedViewer from "bpmn-js/lib/NavigatedViewer"
import { WorkflowActivityMonitorConfig } from "../ActivityMonitor/WorkflowActivityMonitorPage";
import { QueryToken } from "@framework/QueryToken";
import { getToString, is } from "@framework/Signum.Entities";

export class WorkflowActivityMonitorRenderer extends CustomRenderer {
  workflowActivityMonitor!: WorkflowClient.WorkflowActivityMonitor;
  workflowConfig!: WorkflowActivityMonitorConfig;
  workflowModel!: WorkflowModel;

  viewer!: NavigatedViewer;

  gradient: Gradient = new Gradient([
    { value: 0, color: Color.parse("rgb(117, 202, 112)") },
    { value: 0.5, color: Color.parse("rgb(251, 214, 95)") },
    { value: 1, color: Color.parse("rgb(251, 114, 95)") },
  ]);

  drawShape(visuals: any, element: BPMN.DiElement): SVGElement {
    const result = super.drawShape(visuals, element);

    if (BpmnUtils.isTaskAnyKind(element.type)) {
      var mle = this.workflowModel.entities.singleOrNull(mle => mle.element.bpmnElementId == element.id);
      var actMod = mle && (mle.element.model as WorkflowActivityModel);

      const stats = actMod && this.workflowActivityMonitor.activities.singleOrNull(ac => is(ac.workflowActivity, actMod!.workflowActivity));

      if (!stats) {
        result.style.setProperty('stroke', "lightgray");
        result.style.setProperty('fill', "#eee");
      }
      else if (this.workflowConfig.columns.length == 0) {
        var max = Math.max(1, this.workflowActivityMonitor.activities.max(a => a.caseActivityCount) || 0);
        const color = this.gradient.getColor(stats.caseActivityCount / max);
        result.style.setProperty('stroke', color.lerp(0.5, Color.Black).toString());
        result.style.setProperty('fill', color.toString());

      } else {
        var max = Math.max(0.01, this.workflowActivityMonitor.activities.max(a => a.customValues[0]) || 0);
        const color = this.gradient.getColor((stats.customValues[0] || 0) / max);
        result.style.setProperty('stroke', color.lerp(0.5, Color.Black).toString());
        result.style.setProperty('fill', color.toString());
      }
      const gParent = ((result.parentNode as SVGGElement).parentNode as SVGGElement);
      const title = (Array.toArray(gParent.childNodes) as SVGElement[]).filter(a => a.nodeName == "title").firstOrNull() ||
        gParent.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "title"));

      title.textContent = stats == null ? "" : getTitle(stats, this.workflowConfig);
    }

    return result;
  }
}

function getTitle(stats: WorkflowClient.WorkflowActivityStats, config: WorkflowActivityMonitorConfig) {
  let result = `${getToString(stats.workflowActivity)} (${stats.caseActivityCount})`;

  if (config.columns.length) {
    result += "\n" + config.columns.map((col, i) =>
      `${col.displayName || col.token!.niceName}: ${formatMinutes(stats.customValues[i], col.token!)}`).join("\n");
  }
  return result;
}


function formatMinutes(minutes: number | undefined, token: QueryToken) {

  if (minutes == undefined)
    return "";

  return WorkflowClient.formatDuration(Duration.fromObject({ minutes }));
}

export const __init__: string[] = ['workflowActivityMonitorRenderer'];
export const workflowActivityMonitorRenderer: (string | typeof WorkflowActivityMonitorRenderer)[] = ['type', WorkflowActivityMonitorRenderer];
