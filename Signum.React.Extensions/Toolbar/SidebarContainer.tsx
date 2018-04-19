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
            <div className="sidebar-container">
                {visible && this.renderSideBar()}
                <div className="container-fluid" style={{ paddingTop: "10px" }}>
                    {this.props.children}
                </div>
            </div>
        );
    }

    renderSideBar() {
        return (
            <div className="navbar-light bg-light sidebar sidebar-nav" role="navigation" style={{ paddingTop: "10px" }}>
                {this.props.sidebarContent}
            </div>
        );
    }
}