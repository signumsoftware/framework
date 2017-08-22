import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { BsStyle } from "../../../../Framework/Signum.React/Scripts/Operations";
import { Collapse } from "../../../../Framework/Signum.React/node_modules/@types/reactstrap";

export interface AccordionPanelState {
    activeIndex?: number;
}

export default class AccordionPanel extends React.Component<{}, AccordionPanelState> {

    constructor(props: any) {
        super(props);
        this.state = { activeIndex: 0 };
    }

    handleSelect = (activeIndex: number) => {
        this.setState({ activeIndex: this.state.activeIndex == activeIndex ? undefined : activeIndex });
    }

    render() {
        return (
            <div>
                {
                    React.Children.map(this.props.children,
                        (p, i) => <Collapse isOpen={this.state.activeIndex == i} key={i} >
                            {p}
                        </Collapse>)
                }
            </div>);
    }
}

