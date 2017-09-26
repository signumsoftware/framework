import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { BsStyle } from "../../../../Framework/Signum.React/Scripts/Operations";

export interface CollapsablePanelProps {
    type?: BsStyle;
    header?: React.ReactNode;
    body?: React.ReactNode;
    defaultOpen?: boolean;
    collapsable?: boolean;
}

export interface CollapsablePanelState {
    open: boolean,
    loaded: boolean;
    isRTL: boolean;
}

export default class CollapsablePanel extends React.Component<CollapsablePanelProps, CollapsablePanelState> {

    constructor(props: CollapsablePanelProps) {
        super(props);
        this.state = {
            open: this.props.defaultOpen == true,
            isRTL: document.body.classList.contains("rtl-mode"),
            loaded: this.props.defaultOpen == true,
        };
    }

    changeState = () => {
        this.setState({
            open: !this.state.open,
            loaded: true,
        });
    }

    render() {
        const collapsable = !this.props.collapsable || this.props.collapsable == true;

        return (
            <div className={classes("panel", this.props.type ? "panel-" + this.props.type : "panel-default")}>
                <div className="panel-heading" style={collapsable ? { cursor: "pointer" } : undefined} onClick={this.changeState}>
                    {this.props.header}
                    {collapsable &&
                        <span
                            className={classes(this.state.isRTL ? "pull-left" : "pull-right", "glyphicon", this.state.open ? "glyphicon-chevron-up" : "glyphicon-chevron-down")}
                            onClick={this.changeState}>
                        </span>}
                </div>
                {this.state.loaded ?
                    <div className="panel-body" style={{ display: this.state.open ? "block" : "none" }}>
                        {this.props.body}
                    </div> : undefined}
            </div>
        );
    }
}