import * as React from 'react'
import { NavItem } from 'react-bootstrap'
import JoyrideComponent from "./JoyrideComponent";

export interface JoyrideNavItemProps {
    getJoyrideComponent: () => JoyrideComponent;
}

export interface JoyrideNavItemState {

}

export default class JoyrideNavItem extends React.Component<JoyrideNavItemProps, JoyrideNavItemState> {

    constructor(props: JoyrideNavItemProps) {
        super(props);
        this.state = { };
    }

    onClick = () => {
        const joyrideComponent = this.props.getJoyrideComponent();

        if (joyrideComponent && joyrideComponent.joyride)
            joyrideComponent.joyride.reset(true);
    }
    
    render() {
        return (
            <NavItem id="help-nav-item" onClick={this.onClick}>
                <i className="fa fa-question-circle-o" aria-hidden="true"></i>
            </NavItem>
        );
    }
}