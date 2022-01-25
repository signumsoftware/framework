import * as React from 'react'
import { ErrorBoundary } from '@framework/Components';
import "./Sidebar.css"

interface SidebarContainerProps {
  sidebarVisible: boolean | undefined;
  sidebarContent: React.ReactElement<any>;
  children: React.ReactNode;
  sideMenuExpanded: boolean | undefined;
  setSideMenuExpanded: (to: boolean) => void | undefined;
  sideMenuHide: boolean | undefined;
  fullSidebarView: boolean | undefined;
}

export default function SidebarContainer(p : SidebarContainerProps){

  function renderSideBar() {
    return (
      <div className="sidebar sidebar-nav"
        role="navigation"
        style={{ paddingTop: "10px", width: p.sideMenuHide ? "0px" : (p.sideMenuExpanded ? (p.fullSidebarView ? "100%" : "250px") : (p.fullSidebarView ? "0px" : "59px")), minWidth: p.sideMenuHide ? "0px" : (p.sideMenuExpanded ? (p.fullSidebarView ? "100%" : "250px") : (p.fullSidebarView ? "0px" : "59px")) }}>
        {p.sidebarContent}
      </div>
    );
  }
  const visible = p.sidebarVisible;
  return (
    <div className="sidebar-container">
      {visible && renderSideBar()}
      <div className="container-fluid sf-page-container" style={{ paddingTop: "10px" }}>
        <ErrorBoundary>
          {p.children}
        </ErrorBoundary>
      </div>
    </div>
  );
}
