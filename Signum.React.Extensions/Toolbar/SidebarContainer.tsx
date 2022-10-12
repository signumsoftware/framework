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

export function SidebarToggleItem(p: { isMobile: boolean, simpleMode?: boolean, mode: SidebarMode, setMode: (mode: SidebarMode) => void }) {
  return (
    <a className={classes("main-sidebar-button", "nav-link", "main-sidebar-button-" + p.mode.toLowerCase())} onClick={(ev) => {
      window.dispatchEvent(new CustomEvent("sidebarMove"));
      switch (p.mode) {
        case "Hidden": p.setMode("Wide"); break;
        case "Narrow": p.setMode("Wide"); break;
        case "Wide":
        default: p.setMode(p.isMobile || p.simpleMode ? "Hidden" : "Narrow"); break;
      }
    }}>
      <div style={{ display: "flex", height: "100%", alignItems: "center" }}>
        <FontAwesomeIcon icon={"angles-left"} style={{ transition: "all 400ms", width: p.mode == "Wide" ? "15px" : "0.1px" }} />
        <FontAwesomeIcon icon={"bars"} style={{ transition: "all 400ms", width: p.mode == "Hidden" ? "15px" : "0.1px" }} />
        {!p.simpleMode && <FontAwesomeIcon icon={"angles-right"} style={{ transition: "all 400ms", width: p.mode == "Narrow" ? "15px" : "0.1px" }} />}
      </div>
    </a>
  );
}
