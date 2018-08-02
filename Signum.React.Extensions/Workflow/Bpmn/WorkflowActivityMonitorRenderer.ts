/// <reference path="../bpmn-js.d.ts" />
import Modeler from "bpmn-js/lib/Modeler"
import BpmnRenderer from "bpmn-js/lib/draw/BpmnRenderer"
import * as moment from 'moment'
import {
    WorkflowConditionEntity, WorkflowActionEntity, CaseActivityEntity,
    CaseNotificationEntity, DoneType, WorkflowActivityEntity, CaseFlowColor, WorkflowModel,
    WorkflowActivityModel
} from '../Signum.Entities.Workflow'
import { Lite, liteKey } from '@framework/Signum.Entities'
import { Color, Gradient } from '../../Basics/Color'
import { CustomRenderer } from './CustomRenderer'
import { WorkflowActivityStats, WorkflowActivityMonitor } from '../WorkflowClient'
import * as BpmnUtils from './BpmnUtils'
import { calculatePoint, Rectangle } from "../../Map/Utils"
import NavigatedViewer from "bpmn-js/lib/NavigatedViewer"
import "moment-duration-format"
import { WorkflowActivityMonitorConfig } from "../ActivityMonitor/WorkflowActivityMonitorPage";
import { QueryToken } from "@framework/FindOptions";
import { is } from "@framework/Signum.Entities";

export class WorkflowActivityMonitorRenderer extends CustomRenderer {

    workflowActivityMonitor!: WorkflowActivityMonitor;
    workflowConfig!: WorkflowActivityMonitorConfig;
    workflowModel!: WorkflowModel;

    viewer!: NavigatedViewer;

    gradient = new Gradient([
        { value: 0, color: Color.parse("rgb(117, 202, 112)")},
        { value: 0.5, color: Color.parse("rgb(251, 214, 95)") },
        { value: 1, color: Color.parse("rgb(251, 114, 95)") },
    ]);
    
    drawShape(visuals: any, element: BPMN.DiElement) {
        
        const result = super.drawShape(visuals, element);

        if (BpmnUtils.isTaskAnyKind(element.type)) {

            var mle = this.workflowModel.entities.singleOrNull(mle => mle.element.bpmnElementId == element.id);

            var actMod = mle && (mle.element.model as WorkflowActivityModel);

            const stats = actMod && this.workflowActivityMonitor.Activities.singleOrNull(ac => is(ac.WorkflowActivity, actMod!.workflowActivity));
            
            if (!stats) {
                result.style.setProperty('stroke', "lightgray");
                result.style.setProperty('fill', "#eee");
            }
            else if (this.workflowConfig.columns.length == 0) {
                var max = Math.max(1, this.workflowActivityMonitor.Activities.max(a => a.CaseActivityCount));
                const color = this.gradient.getColor(stats.CaseActivityCount / max);
                result.style.setProperty('stroke', color.lerp(0.5, Color.Black).toString());
                result.style.setProperty('fill', color.toString());

            } else {
                var max = Math.max(0.01, this.workflowActivityMonitor.Activities.max(a => a.CustomValues[0]));
                const color = this.gradient.getColor((stats.CustomValues[0] || 0) / max);
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

function getTitle(stats: WorkflowActivityStats, config: WorkflowActivityMonitorConfig) {
    let result = `${stats.WorkflowActivity.toStr} (${stats.CaseActivityCount})`;

    if (config.columns.length) {
        result += "\n" + config.columns.map((col, i) =>
            `${col.displayName || col.token!.niceName}: ${formatDuration(stats.CustomValues[i], col.token!)}`).join("\n");
    }
    return result;
}


function formatDuration(minutes: number | undefined, token: QueryToken) {

    if (minutes == undefined)
        return "";

    return moment.duration(minutes, "minutes").format("d[d] h[h] m[m] s[s]");
}

export const __init__ = ['workflowActivityMonitorRenderer'];
export const workflowActivityMonitorRenderer = ['type', WorkflowActivityMonitorRenderer];