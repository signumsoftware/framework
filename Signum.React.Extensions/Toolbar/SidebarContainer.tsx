import * as React from 'react'
import { ErrorBoundary } from '@framework/Components';
import "./Sidebar.css"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '../../Signum.React/Scripts/Globals';

export type SidebarMode = "Wide" | "Narrow" | "Hidden";   

interface SidebarContainerProps {
  mode: SidebarMode;
  isMobile: boolean;
  sidebarContent?: React.ReactElement<any>;
  children: React.ReactNode;
}

export function SidebarContainer(p: SidebarContainerProps){
  function renderSideBar() {
    return (
      <nav
        className={classes("sidebar sidebar-nav", p.mode.firstLower(), p.isMobile && "mobile")}
        role="navigation">
        {p.sidebarContent}
      </nav>
    );
  }

  return (
    <div className="sidebar-container">
      {p.sidebarContent && renderSideBar()}
      <div className="container-fluid sf-page-container">
        <ErrorBoundary>
          {p.children}
        </ErrorBoundary>
      </div>
    </div>
  );
}

export function SidebarToggleItem(p: { isMobile: boolean; mode: SidebarMode, setMode: (mode: SidebarMode) => void }) {
  return (
    <a className="main-sidebar-button nav-link" onClick={(ev) => {
      window.dispatchEvent(new CustomEvent("sidebarMove"));
      switch (p.mode) {
        case "Hidden": p.setMode("Wide"); break;
        case "Narrow": p.setMode("Wide"); break;
        case "Wide":
        default: p.setMode(p.isMobile ? "Hidden" : "Narrow"); break;
      }
    }}>
      <div style={{ display: "flex", height: "100%", alignItems: "center" }}>
        <FontAwesomeIcon icon={"angle-double-left"} style={{ transition: "all 400ms", width: p.mode == "Wide" ? "15px" : "0.1px" }} />
        <FontAwesomeIcon icon={"bars"} style={{ transition: "all 400ms", width: p.mode == "Hidden" ? "15px" : "0.1px" }} />
        <FontAwesomeIcon icon={"angle-double-right"} style={{ transition: "all 400ms", width: p.mode == "Narrow" ? "15px" : "0.1px" }} />
      </div>
    </a>
  );
}
