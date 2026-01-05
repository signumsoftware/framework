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
  const sidebarShellRef = React.useRef<HTMLDivElement | null>(null);

  const sidebarWidthStorageKey = "signum-toolbar-sidebar-width";

  const [sidebarWidth, setSidebarWidth] = React.useState<number | null>(() => {
    const str = localStorage.getItem(sidebarWidthStorageKey);
    if (str == null)
      return null;

    const n = Number(str);
    return Number.isFinite(n) ? n : null;
  });

  const minSidebarWidthRef = React.useRef<number | null>(null);
  const maxSidebarWidthRef = React.useRef<number | null>(null);

  const isResizable = !p.isMobile && p.mode === "Wide";

  React.useLayoutEffect(() => {
    if (!isResizable)
      return;

    const nav = sidebarShellRef.current?.querySelector<HTMLElement>(".sidebar.sidebar-nav");
    if (nav) {
      nav.style.width = "100%";
      nav.style.minWidth = "0";
    }
  }, [isResizable]);

  React.useLayoutEffect(() => {
    if (!isResizable)
      return;

    if (minSidebarWidthRef.current != null)
      return;

    const el = sidebarShellRef.current;
    if (!el)
      return;

    const current = Math.round(el.getBoundingClientRect().width);
    minSidebarWidthRef.current = current;
    maxSidebarWidthRef.current = current * 2;

    if (sidebarWidth == null) {
      setSidebarWidth(current);
      localStorage.setItem(sidebarWidthStorageKey, current.toString());
    }
  }, [isResizable, sidebarWidth]);

  function clampSidebarWidth(width: number): number {
    const min = minSidebarWidthRef.current ?? width;
    const max = maxSidebarWidthRef.current ?? width;
    return Math.max(min, Math.min(max, width));
  }

  function handleResizePointerDown(e: React.PointerEvent<HTMLDivElement>) {
    if (!isResizable)
      return;

    const el = sidebarShellRef.current;
    if (!el)
      return;

    e.preventDefault();
    e.stopPropagation();

    const startX = e.clientX;
    const startWidth = Math.round(el.getBoundingClientRect().width);
    const pointerId = e.pointerId;

    (e.currentTarget as HTMLDivElement).setPointerCapture(pointerId);
    document.body.classList.add("sf-sidebar-resizing");

    function onMove(ev: PointerEvent) {
      if (ev.pointerId !== pointerId)
        return;

      const newWidth = clampSidebarWidth(startWidth + (ev.clientX - startX));
      setSidebarWidth(newWidth);
      localStorage.setItem(sidebarWidthStorageKey, newWidth.toString());
    }

    function cleanup(ev?: PointerEvent) {
      if (ev && ev.pointerId !== pointerId)
        return;

      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", cleanup);
      window.removeEventListener("pointercancel", cleanup);
      document.body.classList.remove("sf-sidebar-resizing");
    }

    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", cleanup);
    window.addEventListener("pointercancel", cleanup);
  }

  function renderSideBar() {
    return (
      <div className={classes("sf-sidebar-shell", isResizable && "resizable")} ref={sidebarShellRef} style={isResizable && sidebarWidth != null ? { width: sidebarWidth, minWidth: sidebarWidth } : undefined}>
        {isResizable && <div className="sf-sidebar-resize-handle" onPointerDown={handleResizePointerDown} role="separator" aria-orientation="vertical" aria-label="Resize sidebar" />}
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
      </div>
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
