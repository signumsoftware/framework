import * as React from 'react'
import * as History from 'history'
import * as AppContext from '@framework/AppContext'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarClient";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import { Nav } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI, useUpdatedRef, useHistoryListen } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { parseIcon } from '../../Basics/Templates/IconTypeahead'
import * as SidebarContainer from '../SidebarContainer'

interface ToolbarEngineProps {
  sideMenuExpanded: boolean | undefined;
  setSideMenuExpanded: (to: boolean) => void | undefined;
  sideMenuHide: boolean | undefined;
  fullSidebarView: boolean | undefined;
}

function isCompatibleWithUrl(r: ToolbarClient.ToolbarResponse<any>, location: History.Location, query: any): boolean {
  if (r.url)
    return (location.pathname + location.search).startsWith(AppContext.toAbsoluteUrl(r.url));

  if (!r.content)
    return false;

  var config = ToolbarClient.configs[r.content.EntityType];
  if (!config)
    return false;

  return config.isCompatibleWithUrl(r, location, query);
}

function inferActive(r: ToolbarClient.ToolbarResponse<any>, location: History.Location, query: any): ToolbarClient.ToolbarResponse<any> | null {
  if (r.elements)
    return r.elements.map(e => inferActive(e, location, query)).notNull().onlyOrNull();

  if (isCompatibleWithUrl(r, location, query))
    return r;

  return null;
}

export default function ToolbarRenderer(p: { engine: ToolbarEngineProps, appTitle: React.ReactNode, logoExpanded: React.ReactNode, logoMini?: React.ReactNode, children: React.ReactNode }): React.ReactElement | null {
  const response = useAPI(() => ToolbarClient.API.getCurrentToolbar(), []);
  const responseRef = useUpdatedRef(response);

  const [refresh, setRefresh] = React.useState(false);
  const [active, setActive] = React.useState<ToolbarClient.ToolbarResponse<any> | null>(null);
  const activeRef = useUpdatedRef(active);

  function changeActive(location: History.Location) {
    var query = QueryString.parse(location.search);
    if (responseRef.current) {
      if (activeRef.current && isCompatibleWithUrl(activeRef.current, location, query)) {
        return;
      }

      var newActive = inferActive(responseRef.current, location, query);
      setActive(newActive);
    }
  }

  const randomKey = () => ("c" + (Math.random() * 1250));

  useHistoryListen((location: History.Location, action: History.Action) => {
    changeActive(location);
  }, response != null);

  React.useEffect(() => changeActive(AppContext.history.location), [response]);

  return <div className={"sidebar-inner " + (p.engine.sideMenuExpanded ? "" : " sidebar-collapsed")} style={{ paddingTop: "0px" }}>
    <div style={{ display: "flex", transition: "all 200ms", alignItems: "center", padding: p.engine.sideMenuExpanded === true ? "5px 25px 16px" : "0px 13px 10px" }}>
      <div className={"sidebar-brand-icon" + (p.engine.sideMenuExpanded ? "" : " sidebar-brand-icon-mini")}>{p.engine.sideMenuExpanded ? p.logoExpanded : (p.logoMini || p.logoExpanded)}</div>
      <h5 className={"sidebar-app-title"}>{p.appTitle}</h5>
    </div>

    <div className={"close-sidebar"} onClick={() => { if (p.engine.setSideMenuExpanded) p.engine.setSideMenuExpanded(false) }}><FontAwesomeIcon icon={"angle-double-left"} /></div>

    <div onClick={(ev) => { if ((ev.target as any).className != "nav-item-dropdown-elem") sidebarItemAction() }}>
      {p.children}
      {response && response.elements && response.elements.map((res: ToolbarClient.ToolbarResponse<any>, i: number) => withKey(renderNavItem(res, false, () => setTimeout(() => setRefresh(!refresh), 500)), i))}
    </div>
  </div>;

  function renderNavItem(res: ToolbarClient.ToolbarResponse<any>, additionalPaddingDropdown?: boolean, onRefresh?: () => void, key?: string) {
    let activeCheck = isCompatibleWithUrl(res, AppContext.history.location, QueryString.parse(AppContext.history.location.search));

    switch (res.type) {
      case "Divider":
        return <hr style={{ margin: "10px 0 5px 0px" }}></hr>;
      case "Header":
      case "Item":
        if (res.elements && res.elements.length) {
          var title = res.label || res.content!.toStr;
          var icon = getIcon(res);

          return <CustomSidebarDropdown parentTitle={title} icon={icon} key={randomKey()} sideMenuExpanded={p.engine.sideMenuExpanded}>
            {res.elements && res.elements.map(sr => renderNavItem(sr, true, onRefresh, "e" + (Math.random() * 1250)))}
          </CustomSidebarDropdown>;
        }

        if (res.url) {
          return (
            <Nav.Item key={key ?? randomKey()}>
              <Nav.Link
                title={res.label}
                style={{ paddingLeft: p.engine.sideMenuExpanded === true ? "25px" : "13px" }}
                onClick={(e: React.MouseEvent<any>) => AppContext.pushOrOpenInTab(res.url!, e)}
                onAuxClick={(e: React.MouseEvent<any>) => AppContext.pushOrOpenInTab(res.url!, e)}
                active={activeCheck || res == active}>
                {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}<span>{res.label}</span>
                {!p.engine.sideMenuExpanded && !p.engine.fullSidebarView && <div className={"nav-item-float"}>{res.label}</div>}
              </Nav.Link>
            </Nav.Item>
          );
        }

        if (res.content) {
          var config = ToolbarClient.configs[res.content!.EntityType];
          if (!config)
            return <Nav.Item style={{ color: "red" }}>{res.content!.EntityType + "ToolbarConfig not registered"}</Nav.Item>;

          return (
            <Nav.Item key={key ?? randomKey()}>
              <Nav.Link
                title={res.label}
                style={{ paddingLeft: p.engine.sideMenuExpanded === true ? "25px" : "13px" }}
                onClick={(e: React.MouseEvent<any>) => config.handleNavigateClick(e, res)}
                onAuxClick={(e: React.MouseEvent<any>) => config.handleNavigateClick(e, res)} active={res == active}>
                {config.getIcon(res)}<span title={res.label}>{res.label}</span>
                {!p.engine.sideMenuExpanded && !p.engine.fullSidebarView && <div className={"nav-item-float"}>{res.label}</div>}
              </Nav.Link>
            </Nav.Item>
          );
        }

        if (res.type == "Header") {
          return (
            <div key={key ?? randomKey()} className={"nav-item-header" + (p.engine.sideMenuExpanded ? "" : " mini")}>
              {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}
              {p.engine.sideMenuExpanded && <span>{res.label}</span>}

              {!p.engine.sideMenuExpanded && !p.engine.fullSidebarView && <div className={"nav-item-float"}>{res.label}</div>}
            </div>
          );
        }

        return <Nav.Item key={key ?? randomKey()} style={{ color: "red" }}>{"No Content or Url found"}</Nav.Item>;

      default:
        throw new Error("Unexpected " + res.type);
    }
  }

  function sidebarItemAction() {
    if (p.engine.fullSidebarView === true && p.engine.setSideMenuExpanded)
        p.engine.setSideMenuExpanded(false);
  }

  function getIcon(res: ToolbarClient.ToolbarResponse<any>) {

    var icon = parseIcon(res.iconName);

    return icon && <FontAwesomeIcon icon={icon} className={"icon"} color={res.iconColor} fixedWidth />
  }
}

function withKey(e: React.ReactElement<any>, index: number) {
  return React.cloneElement(e, { key: index });
}

function CustomSidebarDropdown(props: { parentTitle: string | undefined, sideMenuExpanded: boolean | undefined, icon: any, children: any }) {
  var [show, setShow] = React.useState(false);

  return (
    <div>
      <div className="nav-item">
        <div
          title={props.parentTitle}
          className={"nav-link"}
          onClick={() => setShow(!show)}
          style={{ paddingLeft: props.sideMenuExpanded === true ? 25 : 13, cursor: 'pointer' }}>
          <div style={{ display: 'inline-block', position: 'relative' }}>
            <div style={{ position: 'absolute', opacity: 0.2 }}>{props.icon}</div>
            {show ? <FontAwesomeIcon icon={"caret-up"} /> : <FontAwesomeIcon icon={"caret-down"} />}
          </div>
          <span className={"nav-item-dropdown-elem"} style={{ marginLeft: "16px", verticalAlign: "middle" }}>{props.parentTitle}</span>
          {!props.sideMenuExpanded && <div className={"nav-item-float"}>{props.parentTitle}</div>}
        </div>
      </div>
      <div style={{ display: show ? "block" : "none" }}>
        {show && props.children}
      </div>
    </div>
  );
}
