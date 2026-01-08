import * as React from 'react';
import { ErrorBoundary } from '@framework/Components';
import "./Sidebar.css";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
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

const DEFAULT_WIDTH = 250;
const MIN_WIDTH = 250;
const MAX_WIDTH = 600;

export function SidebarContainer(p: SidebarContainerProps): React.JSX.Element {

  const [width, setWidth] = React.useState<number>(() => {
    const stored = localStorage.getItem("sidebar-width");
    return stored ? Number(stored) : DEFAULT_WIDTH;
  });

  React.useEffect(() => {
    localStorage.setItem("sidebar-width", String(width));
  }, [width]);

  const prevMode = React.useRef<SidebarMode | null>(null);

  React.useEffect(() => {
    const wasCollapsed =
      prevMode.current === "Hidden" || prevMode.current === "Narrow";
    if (p.mode === "Wide" && wasCollapsed) {
      setWidth(DEFAULT_WIDTH);
    }
    prevMode.current = p.mode;
  }, [p.mode]);
  function renderSideBar() {
    const isResizable = p.mode === "Wide" && !p.isMobile;

    return (
      <nav
        className={classes(
          "sidebar sidebar-nav",
          p.mode.firstLower(),
          p.isMobile && "mobile"
        )}
        style={isResizable ? { width, minWidth: width } : undefined}
        role="navigation"
      >
        <a
          href="#maincontent"
          className="skip-link"
          onClick={(e) => {
            e.preventDefault();
            document.getElementById("maincontent")?.focus();
          }}
        >
          {LayoutMessage.JumpToMainContent.niceToString()}
        </a>
        {p.sidebarContent}
        {isResizable && <SidebarResizeHandle onResize={setWidth} />}
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
export function SidebarToggleItem(p: {
  isMobile: boolean;
  simpleMode?: boolean;
  mode: SidebarMode;
  setMode: (mode: SidebarMode) => void;
}): React.JSX.Element {

  return (
    <LinkButton
      title={EntityControlMessage.ToggleSideBar.niceToString()}
      className={classes(
        "main-sidebar-button",
        "nav-link",
        "main-sidebar-button-" + p.mode.toLowerCase()
      )}
      onClick={() => {
        window.dispatchEvent(new CustomEvent("sidebarMove"));
        switch (p.mode) {
          case "Hidden": p.setMode("Wide"); break;
          case "Narrow": p.setMode("Wide"); break;
          case "Wide":
          default:
            p.setMode(p.isMobile || p.simpleMode ? "Hidden" : "Narrow");
        }
      }}
    >
      <div style={{ display: "flex", height: "100%", alignItems: "center" }}>
        <FontAwesomeIcon icon={"angles-left"} style={{ width: p.mode === "Wide" ? 15 : 0 }} />
        <FontAwesomeIcon icon={"bars"} style={{ width: p.mode === "Hidden" ? 15 : 0 }} />
        {!p.simpleMode && (
          <FontAwesomeIcon icon={"angles-right"} style={{ width: p.mode === "Narrow" ? 15 : 0 }} />
        )}
      </div>
    </LinkButton>
  );
}
function SidebarResizeHandle(p: { onResize: (width: number) => void }) {
  const sidebarLeft = React.useRef(0);
  function onMouseDown(e: React.MouseEvent) {
    const sidebar = e.currentTarget.parentElement as HTMLElement;
    const rect = sidebar.getBoundingClientRect();
    sidebarLeft.current = rect.left;
    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);
    document.body.classList.add("sidebar-resizing");
    e.preventDefault();
  }
  function onMouseMove(e: MouseEvent) {
    const rawWidth = e.clientX - sidebarLeft.current;

    const nextWidth = Math.min(
      MAX_WIDTH,
      Math.max(MIN_WIDTH, rawWidth)
    );

    p.onResize(nextWidth);
  }
  function onMouseUp() {
    document.removeEventListener("mousemove", onMouseMove);
    document.removeEventListener("mouseup", onMouseUp);
    document.body.classList.remove("sidebar-resizing");
  }

  return (
    <div
      className="sidebar-resize-handle"
      onMouseDown={onMouseDown}
      aria-hidden="true"
    />
  );
}
