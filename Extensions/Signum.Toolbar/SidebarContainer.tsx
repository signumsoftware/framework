import * as React from 'react'
import { ErrorBoundary } from '@framework/Components';
import "./Sidebar.css"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals';
import { EntityControlMessage } from '@framework/Signum.Entities';
import { LayoutMessage } from './Signum.Toolbar';
import { LinkButton } from '../../Signum/React/Basics/LinkButton';

export type SidebarMode = "Wide" | "Narrow" | "Hidden";

interface SidebarContainerProps {
  mode: SidebarMode;
  isMobile: boolean;
  sidebarContent?: React.ReactElement<any>;
  children: React.ReactNode;
}

export function SidebarContainer(p: SidebarContainerProps): React.JSX.Element {
  const sidebarRef = React.useRef<HTMLDivElement>(null);
  const isResizing = React.useRef(false);

  React.useEffect(() => {
    // Whenever sidebar mode becomes Wide on desktop, reset width to 250px
    if (p.mode === "Wide" && !p.isMobile) {
      document.documentElement.style.setProperty("--sidebar-width", `250px`);
    }
  }, [p.mode, p.isMobile]);
  React.useEffect(() => {
    function onMouseMove(e: MouseEvent) {
      if (!isResizing.current || !sidebarRef.current) return;
      const MIN_WIDTH = 250;
      const MAX_WIDTH = 600;
      const newWidth = Math.min(Math.max(e.clientX, MIN_WIDTH), MAX_WIDTH);
      document.documentElement.style.setProperty(
        "--sidebar-width",
        `${newWidth}px`
      );
    }

    function onMouseUp() {
      isResizing.current = false;
      document.body.classList.remove("sidebar-resizing");
    }

    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("mouseup", onMouseUp);

    return () => {
      window.removeEventListener("mousemove", onMouseMove);
      window.removeEventListener("mouseup", onMouseUp);
    };
  }, []);

  function startResize() {
    isResizing.current = true;
    document.body.classList.add("sidebar-resizing");
  }

  function renderSideBar() {
    return (
      <nav
        ref={sidebarRef}
        className={classes(
          "sidebar sidebar-nav",
          p.mode.firstLower(),
          p.isMobile && "mobile"
        )}
        role="navigation"
      >
        {p.sidebarContent}

        {/* Resize handle (desktop + wide only) */}
        {!p.isMobile && p.mode === "Wide" && (
          <div
            className="sidebar-resizer"
            onMouseDown={startResize}
          />
        )}
      </nav>
    );
  }

  return (
    <div className="sidebar-container">
      {p.sidebarContent && renderSideBar()}
      <div className="sf-page-container">
        <ErrorBoundary>{p.children}</ErrorBoundary>
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
