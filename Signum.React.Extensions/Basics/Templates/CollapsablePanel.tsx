import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'

export interface CollapsablePanelProps {
    type?: "primary" | "info" | "success" | "warning" | "danger";
    header?: React.ReactNode;
    body?: React.ReactNode;
    defaultOpen?: boolean;
    collapsable?: boolean;
}

export interface CollapsablePanelState {
    open: boolean,
    isRTL: boolean;
}

export default class CollapsablePanel extends React.Component<CollapsablePanelProps, CollapsablePanelState> {

    constructor(props: any) {
        super(props);
        this.state = { open: this.props.defaultOpen == true, isRTL: document.body.classList.contains("rtl-mode") };
    }

    render() {
        return (
            <div className={classes("panel", this.props.type ? "panel-" + this.props.type : "panel-default")}>
                <div className="panel-heading">
                    {this.props.header}
                    {(this.props.collapsable == undefined || this.props.collapsable == true) &&
                        <span
                            className={classes(this.state.isRTL ? "pull-left" : "pull-right", "glyphicon", this.state.open ? "glyphicon-chevron-up" : "glyphicon-chevron-down")}
                            style={{ cursor: "pointer" }}
                            onClick={() => this.setState({ open: !this.state.open })}>
                        </span>}
                </div>
                {this.state.open &&
                    <div className="panel-body">
                        {this.props.body}
                    </div>}
            </div>
        );
    }
}