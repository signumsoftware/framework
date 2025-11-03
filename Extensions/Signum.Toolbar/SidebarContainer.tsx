import * as React from 'react'
import { ErrorBoundary } from '@framework/Components';
import "./Sidebar.css"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals';
import { EntityControlMessage } from '@framework/Signum.Entities';
import { LinkButton } from '../../Signum/React/Basics/LinkButton';
import { LayoutMessage } from './Signum.Toolbar';

export type SidebarMode = "Wide" | "Narrow" | "Hidden";   

interface SidebarContainerProps {
  mode: SidebarMode;
  isMobile: boolean;
  sidebarContent?: React.ReactElement<any>;
  children: React.ReactNode;
}

export function SidebarContainer(p: SidebarContainerProps): React.JSX.Element{
  function renderSideBar() {
    return (
      <nav
        className={classes("sidebar sidebar-nav", p.mode.firstLower(), p.isMobile && "mobile")}
        role="navigation">
        <a
          href="#maincontent"
          className="skip-link"
          onClick={(e) => {
            e.preventDefault();
            const el = document.getElementById("maincontent");
            if (el) {
              el.focus();
            }
          }}
        >{LayoutMessage.JumpToMainContent.niceToString()}</a>
        {p.sidebarContent}
      </nav>
    );
  }

  return (
    <div className="sidebar-container">
      {p.sidebarContent && renderSideBar()}
      <div className="sf-page-container">
        <ErrorBoundary>
          {p.children}
        </ErrorBoundary>
      </div>
    </div>
  );
}

export function SidebarToggleItem(p: { isMobile: boolean, simpleMode?: boolean, mode: SidebarMode, setMode: (mode: SidebarMode) => void }): React.JSX.Element {
  return (
    <LinkButton title={EntityControlMessage.ToggleSideBar.niceToString()} className={classes("main-sidebar-button", "nav-link", "main-sidebar-button-" + p.mode.toLowerCase())} onClick={(ev) => {
      window.dispatchEvent(new CustomEvent("sidebarMove"));
      switch (p.mode) {
        case "Hidden": p.setMode("Wide"); break;
        case "Narrow": p.setMode("Wide"); break;
        case "Wide":
        default: p.setMode(p.isMobile || p.simpleMode ? "Hidden" : "Narrow"); break;
      }
    }}>
      <div style={{ display: "flex", height: "100%", alignItems: "center" }}>
        <FontAwesomeIcon icon={"angles-left"} style={{ transition: "all 400ms", width: p.mode == "Wide" ? "15px" : "0.1px" }} title={EntityControlMessage.ToggleSideBar.niceToString()} />
        <FontAwesomeIcon icon={"bars"} style={{ transition: "all 400ms", width: p.mode == "Hidden" ? "15px" : "0.1px" }} title={EntityControlMessage.ToggleSideBar.niceToString()} />
        {!p.simpleMode && <FontAwesomeIcon icon={"angles-right"} style={{ transition: "all 400ms", width: p.mode == "Narrow" ? "15px" : "0.1px" }} title={EntityControlMessage.ToggleSideBar.niceToString()} />}
      </div>
    </LinkButton>
  );
}
