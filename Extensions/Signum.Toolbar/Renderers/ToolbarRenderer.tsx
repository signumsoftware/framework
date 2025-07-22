import * as React from 'react'
import { useLocation, Location } from 'react-router'
import * as AppContext from '@framework/AppContext'
import { ToolbarClient, ToolbarResponse } from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarConfig";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import { Nav } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI, useUpdatedRef, useWindowEvent, useAPIWithReload } from '@framework/Hooks'
import { Navigator } from '@framework/Navigator'
import { QueryString } from '@framework/QueryString'
import { getToString } from '@framework/Signum.Entities'
import { parseIcon } from '@framework/Components/IconTypeahead'
import { urlVariables } from '../UrlVariables';
import { Dic, classes } from '@framework/Globals';
import { ToolbarEntity, ToolbarMenuEntity, ToolbarMessage } from '../Signum.Toolbar';



export default function ToolbarRenderer(p: {
  onAutoClose?: () => void;
}): React.ReactElement | null {

  Navigator.useEntityChanged(ToolbarEntity, () => reload(), []);
  Navigator.useEntityChanged(ToolbarMenuEntity, () => reload(), []);

  const [response, reload] = useAPIWithReload(() => ToolbarClient.API.getCurrentToolbar("Side"), [], { avoidReset: true });
  const responseRef = useUpdatedRef(response);

  const [refresh, setRefresh] = React.useState(false);
  const [active, setActive] = React.useState<ToolbarResponse<any> | null>(null);

  const location = useLocation();

  function changeActive(location: Location) {
    var query = QueryString.parse(location.search);
    if (responseRef.current) {

      var newActive = inferActive(responseRef.current, location, query);
      setActive(newActive?.response ?? null);
    }
  }

  React.useEffect(() => {
    if (response)
      changeActive(location)
  }, [location, response]);

  function handleRefresh() {
    return window.setTimeout(() => setRefresh(!refresh), 500)
  }

  return (
    <div className={"sidebar-inner"}>
      <div className={"close-sidebar"}
        onClick={() => p.onAutoClose && p.onAutoClose()}>
        <FontAwesomeIcon icon={"angles-left"} aria-label="Close" />
      </div>

      <ul>
        {response && response.elements && response.elements.map((res: ToolbarResponse<any>, i: number) => renderNavItem(res, active, i, handleRefresh, p.onAutoClose))}
      </ul>
    </div>
  );
}

export function isCompatibleWithUrl(r: ToolbarResponse<any>, location: Location, query: any): number {
  if (r.url){
    const current = AppContext.toAbsoluteUrl(location.pathname).replace(/\/+$/, "");
    const target = AppContext.toAbsoluteUrl(r.url).replace(/\/+$/, "");

    const currentSegments = current.split("/");
    const targetSegments = target.split("/");

    return currentSegments.length >= targetSegments.length && targetSegments.every((seg, i) => currentSegments[i] === seg) ? 1 : 0;
  }

  if (!r.content)
    return 0;

  var config = ToolbarClient.getConfig(r);
  if (!config)
    return 0;

  return config.isCompatibleWithUrlPrio(r, location, query);
}

export function inferActive(r: ToolbarResponse<any>, location: Location, query: any): { prio: number, response: ToolbarResponse<any> } | null {
  if (r.elements)
    return r.elements.map(e => inferActive(e, location, query)).notNull().maxBy(a => a.prio) ?? null;

  var prio = isCompatibleWithUrl(r, location, query);
  var bestExtra = r.extraIcons?.map(e => inferActive(e, location, query)).notNull().maxBy(a => a.prio) ?? null;

  if (bestExtra != null && bestExtra.prio > 0 && bestExtra.prio > prio)
    return bestExtra;

  if (prio > 0)
    return { prio, response: r };

  return null;
}


export function renderNavItem(res: ToolbarResponse<any>, active: ToolbarResponse<any> | null, key: string | number, onRefresh: () => void, onAutoClose?: () => void): React.JSX.Element {

  switch (res.type) {
    case "Divider":
      return <hr style={{ margin: "10px 0 5px 0px" }} key={key}></hr>;
    case "Header":
    case "Item":
      if (res.elements && res.elements.length) {
        const title = res.label || getToString(res.content);
        const icon = ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor);

        return (
          <ToolbarDropdown parentTitle={title} icon={icon} key={key} toolbarMenuId={res.content?.id} extraIcons={renderExtraIcons(res.extraIcons, active, onAutoClose)}>
            {res.elements && res.elements.map((sr, i) => renderNavItem(sr, active, i, onRefresh, onAutoClose))}
          </ToolbarDropdown>
        );
      }

      if (res.url) {
        let url = res.url!;
        const isExternalLink = url.startsWith("http") && !url.startsWith(window.location.origin + "/" + window.__baseName);
        const config = res.content && ToolbarClient.getConfig(res);
        return (
          <ToolbarNavItem key={key} title={res.label} isExternalLink={isExternalLink} extraIcons={renderExtraIcons(res.extraIcons, active, onAutoClose)}
            active={res == active} icon={<>
              {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}
              {config?.getCounter(res)}
            </>}
            onClick={(e: React.MouseEvent<any>) => {

              Dic.getKeys(urlVariables).forEach(v => {
                url = url.replaceAll(v, urlVariables[v]());
              });

              if (isExternalLink)
                window.open(AppContext.toAbsoluteUrl(url));
              else
                AppContext.pushOrOpenInTab(url, e);

              if (onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
                onAutoClose();
            }} />
        );
      }

      if (res.content) {
        const config = ToolbarClient.getConfig(res);
        if (!config)
          return <Nav.Item className="text-danger">{res.content!.EntityType + "ToolbarConfig not registered"}</Nav.Item>;

        return config.getMenuItem(res, active, key, onAutoClose);
      }

      if (res.type == "Header") {
        return (
          <li key={key} className={"nav-item-header"}>
            {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}
            <span className={"nav-item-text"}>{res.label}</span>
            <div className={"nav-item-float"}>{res.label}</div>
          </li>
        );
      }

      return <Nav.Item key={key} style={{ color: "red" }}>{"No Content or Url found"}</Nav.Item>;

    default:
      throw new Error("Unexpected " + res.type);
  }
}

function ToolbarDropdown(p: { parentTitle: string | undefined, icon: any, children: any, toolbarMenuId: string | number | undefined, extraIcons: React.ReactElement | undefined }) {
  var [show, setShow] = React.useState(localStorage.getItem("toolbar-menu-" + p.toolbarMenuId) != null);

  function handleSetShow(value: boolean) {
    if (value)
      localStorage.setItem("toolbar-menu-" + p.toolbarMenuId, "1");
    else
      localStorage.removeItem("toolbar-menu-" + p.toolbarMenuId);

    setShow(value);
  }


  return (
    <li>
      <ul>
        <ToolbarNavItem title={p.parentTitle} extraIcons={p.extraIcons} onClick={() => handleSetShow(!show)}
          icon={
            <div style={{ display: 'inline-block', position: 'relative' }}>
              <div className="nav-arrow-icon" style={{ position: 'absolute' }}>
                <FontAwesomeIcon icon={show ? "chevron-down" : "chevron-right"} className="icon" />
              </div>
              <div className="nav-icon-with-arrow">
                {p.icon ?? <div className="icon" />}
              </div>
            </div>
          }
        />
       {show && <li>
          <ul style={{ display: show ? "block" : "none" }} className="nav-item-sub-menu">
            {p.children}
          </ul>
        </li>}  
      </ul>
    </li>
  );
}

export function ToolbarNavItem(p: { title: string | undefined, active?: boolean, isExternalLink?: boolean, extraIcons?: React.ReactElement, onClick: (e: React.MouseEvent) => void, icon?: React.ReactNode, onAutoCloseExtraIcons?: () => void }): React.JSX.Element {
  return (
    <li className="nav-item d-flex">
      <Nav.Link title={p.title} onClick={p.onClick} onAuxClick={p.onClick} active={p.active} className="d-flex w-100">
        {p.icon}
        <span className={"nav-item-text"}>
          {p.title}
          {p.isExternalLink && <FontAwesomeIcon icon="arrow-up-right-from-square" transform="shrink-5 up-3" />}
        </span>
        {p.extraIcons}
        <div className={"nav-item-float"}>{p.title}</div>
      </Nav.Link>
    </li>
  );
}

export function renderExtraIcons(extraIcons?: ToolbarResponse<any>[], active?: ToolbarResponse<any> | null, autoClose?: () => void): React.ReactElement | undefined {
  if (extraIcons == null)
    return undefined;

  return (<>
    {extraIcons?.map((ei, i) => {

      if (ei.url) {
        return <button className={classes("btn btn-sm border-0 py-0 m-0 sf-extra-icon", ei == active && "active")} key={i} onClick={e => {
          e.preventDefault();
          e.stopPropagation();

          let url = ei.url!;
          var isExternalLink = url.startsWith("http") && !url.startsWith(window.location.origin + "/" + window.__baseName);
          Dic.getKeys(urlVariables).forEach(v => {
            url = url.replaceAll(v, urlVariables[v]());
          });

          if (isExternalLink)
            window.open(AppContext.toAbsoluteUrl(url));
          else
            AppContext.pushOrOpenInTab(url, e);

          if (autoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
            autoClose();

        }}>{ToolbarConfig.coloredIcon(parseIcon(ei.iconName!), ei.iconColor)}</button>;
      }


      var config = ToolbarClient.getConfig(ei);
      if (config == null) {
        return <span className="text-danger sf-extra-icon">{ei.content!.EntityType + "ToolbarConfig not registered"}</span>
      }
      else {

        return <button className={classes("btn btn-sm border-0 py-0 m-0 sf-extra-icon", ei == active && "active")} key={i} onClick={e => {
          e.preventDefault();
          e.stopPropagation();
          config!.handleNavigateClick(e, ei);

          if (autoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
            autoClose();

        }} >{config.getIcon(ei)}</button>
      };

    })}
  </>);
}
