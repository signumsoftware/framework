import * as React from 'react'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'

import "./Sidebar.css"

interface SidebarContainerProps {
    sidebarVisible: boolean | undefined;
    sidebarContent: React.ReactElement<any>;
}

export default class SidebarContainer extends React.Component<SidebarContainerProps> {

    render() {
        const visible = this.props.sidebarVisible;
        return (
            <div>
                {visible && this.renderSideBar()}
                <div className={visible ? "sidebar-container" : "container-fluid"}>
                    {this.props.children}
                </div>
            </div>
        );
    }

    renderSideBar() {
        return (
            <div className="navbar-default sidebar" role="navigation">
                <div className="sidebar-nav">
                    {this.props.sidebarContent} 
                </div>
            </div>
        );
    }
}