import * as React from 'react'
import { WorkflowActivityModel, WorkflowModel, WorkflowActivityMonitorMessage } from '../Signum.Workflow'
import { WorkflowClient } from '../WorkflowClient'
import NavigatedViewer from "bpmn-js/lib/NavigatedViewer"
import searchPad from 'bpmn-js/lib/features/search'
import * as WorkflowActivityMonitorRenderer from './WorkflowActivityMonitorRenderer'
import * as BpmnUtils from './BpmnUtils'
import WorkflowActivityStatsModal from '../ActivityMonitor/WorkflowActivityStatsModal';
import { is } from '@framework/Signum.Entities';
import { WorkflowActivityMonitorConfig } from '../ActivityMonitor/WorkflowActivityMonitorPage';
import "bpmn-js/dist/assets/bpmn-font/css/bpmn-embedded.css"
import "diagram-js/assets/diagram-js.css"
import "./Bpmn.css"

export interface WorkflowActivityMonitorViewerComponentProps {
  workflowModel: WorkflowModel;
  workflowActivityMonitor: WorkflowClient.WorkflowActivityMonitor;
  workflowConfig: WorkflowActivityMonitorConfig;
  onDraw: () => void;
}

class CustomViewer extends NavigatedViewer {

}

CustomViewer.prototype._modules = CustomViewer.prototype._modules.concat([WorkflowActivityMonitorRenderer]);

export default class WorkflowActivityMonitorViewerComponent extends React.Component<WorkflowActivityMonitorViewerComponentProps> {
  viewer!: NavigatedViewer;
  divArea!: HTMLDivElement;

  handleOnModelError = (err: string): void => {
    if (err)
      throw new Error('Error rendering the model ' + err);
  }

  componentDidMount(): void {
    this.viewer = new CustomViewer({
      container: this.divArea,
      keyboard: {
        bindTo: document
      },
      height: 1000,
      additionalModules: [
        searchPad,
      ]
    });
    this.configureModules(this.props);
    this.viewer.on('element.dblclick', 1500, this.handleElementDoubleClick as (obj: BPMN.Event) => void);
    this.viewer.importXML(this.props.workflowModel.diagramXml, this.handleOnModelError);
  }

  handleElementDoubleClick = (obj: BPMN.DoubleClickEvent): void => {

    obj.preventDefault();
    obj.stopPropagation();

    var mle = this.props.workflowModel.entities.singleOrNull(mle => mle.element.bpmnElementId == obj.element.id);

    if (mle && WorkflowActivityModel.isInstance(mle.element.model)) {
      var actMod = mle.element.model;

      const stats = this.props.workflowActivityMonitor.activities.singleOrNull(a => is(a.workflowActivity, actMod.workflowActivity));
      if (stats) {
        WorkflowActivityStatsModal.show(stats, this.props.workflowConfig, actMod);
      }
    }
  }

  componentWillUnmount(): void {
    this.viewer.destroy();
  }

  componentWillReceiveProps(nextProps: WorkflowActivityMonitorViewerComponentProps): void {

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

          if (!error && this.props.workflowActivityMonitor != nextProps.workflowActivityMonitor)
            redrawAll();

        });
      } else {
        if (this.props.workflowActivityMonitor != nextProps.workflowActivityMonitor)
          redrawAll();
      }
    }
  }

  configureModules(props: WorkflowActivityMonitorViewerComponentProps): void {
    var workflowActivityMonitorRenderer = this.viewer.get<WorkflowActivityMonitorRenderer.WorkflowActivityMonitorRenderer>('workflowActivityMonitorRenderer');
    workflowActivityMonitorRenderer.viewer = this.viewer;
    workflowActivityMonitorRenderer.workflowActivityMonitor = props.workflowActivityMonitor;
    workflowActivityMonitorRenderer.workflowModel = props.workflowModel;
    workflowActivityMonitorRenderer.workflowConfig = props.workflowConfig;
  }

  handleSearchClick = (e: React.MouseEvent<HTMLButtonElement>): void => {
    e.preventDefault();
    var searchPad = this.viewer.get<any>("searchPad");
    searchPad.toggle();
  }

  resetZoom(): void {
    var zoomScroll = this.viewer.get<any>("zoomScroll");
    zoomScroll.reset();
  }

  handleZoomClick = (e: React.MouseEvent<HTMLButtonElement>): void => {
    e.preventDefault();
    this.resetZoom();
  }

  render(): React.JSX.Element {
    return (
      <div>
        <div className="btn-toolbar" style={{ marginBottom: "5px" }}>
          <button className="btn btn-primary" onClick={this.props.onDraw}>{WorkflowActivityMonitorMessage.Draw.niceToString()}</button>
          <button className="btn btn-default" onClick={this.handleZoomClick}>{WorkflowActivityMonitorMessage.ResetZoom.niceToString()}</button>
          <button className="btn btn-default" onClick={this.handleSearchClick}>{WorkflowActivityMonitorMessage.Find.niceToString()}</button>
        </div>
        <div style={{ border: "1px solid lightgray" }} ref={de => { this.divArea = de! }} />
      </div>
    );
  }
}
