import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import JoyrideComponent from "./JoyrideComponent";
import { NavItem } from '@framework/Components';

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
                <a className="nav-link">
                    <FontAwesomeIcon icon={"question-circle"}/>
                </a>
            </NavItem>
        );
    }
}