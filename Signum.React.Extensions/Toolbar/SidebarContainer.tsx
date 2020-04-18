import * as React from 'react'
import { ErrorBoundary } from '@framework/Components';
import "./Sidebar.css"

interface SidebarContainerProps {
  sidebarVisible: boolean | undefined;
  sidebarContent: React.ReactElement<any>;
  children: React.ReactNode;
}

export default function SidebarContainer(p : SidebarContainerProps){

  function renderSideBar() {
    return (
      <div className="navbar-light bg-light sidebar sidebar-nav" role="navigation" style={{ paddingTop: "10px" }}>
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
