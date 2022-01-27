import * as React from 'react'
import { ErrorBoundary } from '@framework/Components';
import "./Sidebar.css"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'

interface SidebarEngineProps {
  visible: boolean,
  sideMenuExpanded: boolean;
  setSideMenuExpanded: (to: boolean) => void;
  sideMenuHide: boolean;
  fullSidebarView: boolean;
  setSideMenuHide: (to: boolean) => void;
  setExpandedSideMenu: (to: boolean) => void;
}

interface SidebarContainerProps {
  engine: SidebarEngineProps, 
  sidebarContent: React.ReactElement<any>;
  children: React.ReactNode;
}

export function SidebarContainer(p: SidebarContainerProps){
  function renderSideBar() {
    let width = p.engine.sideMenuHide ? "0px" : (p.engine.sideMenuExpanded ? (p.engine.fullSidebarView ? "100%" : "250px") : (p.engine.fullSidebarView ? "0px" : "59px"));

    return (
      <div className="sidebar sidebar-nav"
        role="navigation"
        style={{ paddingTop: "10px", width: width, minWidth: width }}>
        {p.sidebarContent}
      </div>
    );
  }

  const visible = p.engine.visible;
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

export function SidebarToggleItem(p: { engine: SidebarEngineProps}) {
  return <>{p.engine.visible && <a className="main-sidebar-button" onClick={(ev) => {
    window.dispatchEvent(new CustomEvent("sidebarMove"));
    if (!p.engine.fullSidebarView) {
      if (!p.engine.sideMenuExpanded && !p.engine.sideMenuHide) {
        p.engine.setSideMenuHide(true);
      } else {
        p.engine.setSideMenuHide(false);
        p.engine.setExpandedSideMenu(!p.engine.sideMenuExpanded);
      }
    } else {
      p.engine.setExpandedSideMenu(!p.engine.sideMenuExpanded);
    }
  }}>
    <div style={{ display: "flex", height: "100%", alignItems: "center" }}>
      <FontAwesomeIcon icon={"angle-double-left"} style={{ transition: "all 400ms", width: !p.engine.sideMenuExpanded && !p.engine.sideMenuHide && !p.engine.fullSidebarView ? "15px" : "0.1px" }} />
      <FontAwesomeIcon icon={"bars"} style={{ transition: "all 400ms", width: (p.engine.sideMenuExpanded && !p.engine.sideMenuHide) || p.engine.fullSidebarView ? "15px" : "0.1px" }} />
      <FontAwesomeIcon icon={"angle-double-right"} style={{ transition: "all 400ms", width: p.engine.sideMenuHide && !p.engine.fullSidebarView ? "15px" : "0.1px" }} />
    </div>
  </a>}</>;
}
