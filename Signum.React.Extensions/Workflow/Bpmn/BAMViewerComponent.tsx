import * as React from 'react'
import { DropdownButton, MenuItem } from 'react-bootstrap'
import {
    WorkflowEntitiesDictionary, WorkflowActivityModel, WorkflowActivityType, WorkflowPoolModel,
    WorkflowLaneModel, WorkflowConnectionModel, WorkflowEventModel, WorkflowEntity,
    IWorkflowNodeEntity, WorkflowMessage, WorkflowActivityEntity, WorkflowActivityMessage, WorkflowModel, WorkflowBAMMessage
} from '../Signum.Entities.Workflow'
import { WorkflowBAM, API } from '../WorkflowClient'
import { JavascriptMessage, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as NavigatedViewer from "bpmn-js/lib/NavigatedViewer"
import * as BAMRenderer from './BAMRenderer'
import * as searchPad from 'bpmn-js/lib/features/search'
import * as BpmnUtils from './BpmnUtils'
import WorkflowBAMActivityStatsModal from '../BAM/WorkflowBAMActivityStatsModal';
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal';

import "bpmn-js/assets/bpmn-font/css/bpmn-embedded.css"
import "diagram-js/assets/diagram-js.css"
import "./Bpmn.css"
import { is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';
import { WorkflowBAMConfig } from '../BAM/WorkflowBAMPage';

export interface BAMViewerComponentProps {
    workflowModel: WorkflowModel;
    workflowBAM: WorkflowBAM;
    workflowConfig: WorkflowBAMConfig;
    onDraw: () => void;
}

class CustomViewer extends NavigatedViewer {

}

CustomViewer.prototype._modules = 
    CustomViewer.prototype._modules.concat([BAMRenderer]);

export default class BAMViewerComponent extends React.Component<BAMViewerComponentProps> {

    viewer: NavigatedViewer;
    divArea: HTMLDivElement;

    handleOnModelError = (err: string) => {
        if (err)
            throw new Error('Error rendering the model ' + err)
        else 
            this.resetZoom();
    }

    componentDidMount() {
        this.viewer = new CustomViewer({
            container: this.divArea,
            keyboard: {
                bindTo: document
            },
            height: 500,
            additionalModules: [
                searchPad,
            ]
        });
        this.configureModules(this.props);
        this.viewer.on('element.dblclick', 1500, this.handleElementDoubleClick as (obj: BPMN.Event) => void);
        this.viewer.importXML(this.props.workflowModel.diagramXml, this.handleOnModelError);
    }

    handleElementDoubleClick = (obj: BPMN.DoubleClickEvent) => {

        obj.preventDefault();
        obj.stopPropagation();

        var mle = this.props.workflowModel.entities.singleOrNull(mle => mle.element.bpmnElementId == obj.element.id);

        if (mle && WorkflowActivityModel.isInstance(mle.element.model)) {
            var actMod = mle.element.model;

            const stats = this.props.workflowBAM.Activities.singleOrNull(a => is(a.WorkflowActivity, actMod.workflowActivity));
            if (stats) {
                WorkflowBAMActivityStatsModal.show(stats, this.props.workflowConfig, actMod);
            }
        }
    }

    componentWillUnmount() {
        this.viewer.destroy();
    }

    componentWillReceiveProps(nextProps: BAMViewerComponentProps) {
        
        if (this.viewer) {

            var redrawAll = () => {
                this.configureModules(nextProps);

                var reg = this.viewer.get<BPMN.ElementRegistry>("elementRegistry");
                var gFactory = this.viewer.get<BPMN.GraphicsFactory>("graphicsFactory");
                reg.getAll().forEach(a => {

                    const type = BpmnUtils.isConnection(a.type) ? "connection" : "shape";
                    const gfx = reg.getGraphics(a);
                    gFactory.update(type, a, gfx);
                });
            }

            if (this.props.workflowModel.diagramXml !== nextProps.workflowModel.diagramXml) {
                this.viewer.importXML(nextProps.workflowModel.diagramXml, (error, warnings) => {
                    this.handleOnModelError(error);

                    if (!error && this.props.workflowBAM != nextProps.workflowBAM)
                        redrawAll();

                });
            } else {
                if (this.props.workflowBAM != nextProps.workflowBAM)
                    redrawAll();
            }
        }
    }

    configureModules(props: BAMViewerComponentProps) {
        var bamRenderer = this.viewer.get<BAMRenderer.BAMRenderer>('bamRenderer');
        bamRenderer.viewer = this.viewer;
        bamRenderer.workflowBAM = props.workflowBAM;
        bamRenderer.workflowModel = props.workflowModel;
        bamRenderer.workflowConfig = props.workflowConfig;
    }

    handleSearchClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();
        var searchPad = this.viewer.get<any>("searchPad");
        searchPad.toggle();
    }

    resetZoom() {
        var zoomScroll = this.viewer.get<any>("zoomScroll");
        zoomScroll.reset();
    }

    handleZoomClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();
        this.resetZoom();
    }

    render() {
        return (
            <div>
                <div className="btn-toolbar" style={{ marginBottom: "20px" }}>
                    <button className="btn btn-primary" onClick={this.props.onDraw}>{WorkflowBAMMessage.Draw.niceToString()}</button>
                    <button className="btn btn-default" onClick={this.handleZoomClick}>{WorkflowBAMMessage.ResetZoom.niceToString()}</button>
                    <button className="btn btn-default" onClick={this.handleSearchClick}>{WorkflowBAMMessage.Find.niceToString()}</button>
                </div>
                <div ref={de => this.divArea = de!} />
            </div>
        );
    }
}
