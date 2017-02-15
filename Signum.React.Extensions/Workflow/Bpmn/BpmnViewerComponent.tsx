import * as React from 'react'
import Viewer = require("bpmn-js/lib/Viewer");

export interface BpmnViewerComponentProps {
    diagramXML?: string
}

export default class BpmnViewerComponent extends React.Component<BpmnViewerComponentProps, void> {

    viewer: Viewer;
    divArea: HTMLDivElement; 

    handleOnModelError = (err: string) => {
        if (err) {
            throw new Error('Error rendering the model ' + err);
        };
    }

    constructor(props: any) {
        super(props);
    }

    componentDidMount() {
        this.viewer = new Viewer({ container: this.divArea });
        if (this.props.diagramXML && this.props.diagramXML.trim() != "")
            this.viewer.importXML(this.props.diagramXML, this.handleOnModelError);
    }

    componentWillUnmount() {
        this.viewer.destroy();
    }

    componentWillReceiveProps(nextProps: BpmnViewerComponentProps) {
        if (this.viewer) {
            if (nextProps.diagramXML !== undefined && this.props.diagramXML !== nextProps.diagramXML) {
                this.viewer.importXML(nextProps.diagramXML, this.handleOnModelError);
            }
        }
    }

    render() {
        return (<div ref={ de => this.divArea = de } />);

    }
}
