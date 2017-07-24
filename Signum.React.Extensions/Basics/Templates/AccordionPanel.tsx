import * as React from 'react'
import { Panel, PanelGroup } from "react-bootstrap";
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { BsStyle } from "../../../../Framework/Signum.React/Scripts/Operations";

export interface PanelInfo {
    type: BsStyle;
    header: React.ReactNode;
    body: React.ReactNode;
}

export interface AccordionPanelProps {
    panels: PanelInfo[];
}

export interface AccordionPanelState {
    activeIndex?: number;
}

export default class AccordionPanel extends React.Component<AccordionPanelProps, AccordionPanelState> {

    constructor(props: AccordionPanelProps) {
        super(props);
        this.state = { activeIndex: 0 };
    }

    handleSelect = (activeIndex: number) => {
        this.setState({ activeIndex });
    }

    render() {
        return (
            <div>
                <PanelGroup activeKey={this.state.activeIndex} onSelect={this.handleSelect as any} accordion>
                    {
                        this.props.panels.map((p, i) =>
                            <Panel header={p.header} eventKey={i} key={i} bsStyle={p.type}>
                                {p.body}
                            </Panel>)
                    }
                </PanelGroup>
            </div>);
    }
}

