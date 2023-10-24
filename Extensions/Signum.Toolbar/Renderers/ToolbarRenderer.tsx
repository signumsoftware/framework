import * as React from 'react'
import { useLocation, Location } from 'react-router'
import * as AppContext from '@framework/AppContext'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarConfig";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import { Nav } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI, useUpdatedRef, useWindowEvent, useAPIWithReload } from '@framework/Hooks'
import * as Navigator from '@framework/Navigator'
import { QueryString } from '@framework/QueryString'
import { getToString } from '@framework/Signum.Entities'
import { parseIcon } from '@framework/Components/IconTypeahead'
import { urlVariables } from '../UrlVariables';
import { Dic } from '@framework/Globals';
import { ToolbarEntity, ToolbarMenuEntity, ToolbarMessage } from '../Signum.Toolbar';



export default function ToolbarRenderer(p: {
  onAutoClose?: () => void;
  appTitle: React.ReactNode
}): React.ReactElement | null {

  Navigator.useEntityChanged(ToolbarEntity, () => reload(), []);
  Navigator.useEntityChanged(ToolbarMenuEntity, () => reload(), []);

  const [response, reload] = useAPIWithReload(() => ToolbarClient.API.getCurrentToolbar("Side"), [], { avoidReset: true });
  const responseRef = useUpdatedRef(response);

  const [refresh, setRefresh] = React.useState(false);
  const [active, setActive] = React.useState<ToolbarClient.ToolbarResponse<any> | null>(null);

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
      {p.appTitle}
      <div className={"close-sidebar"}
        onClick={() => p.onAutoClose && p.onAutoClose()}>
        <FontAwesomeIcon icon={"angles-left"} aria-label="Close" />
      </div>

      <ul>
        {response && response.elements && response.elements.map((res: ToolbarClient.ToolbarResponse<any>, i: number) => renderNavItem(res, active, i, handleRefresh, p.onAutoClose))}
      </ul>
    </div>
  );
}

export function isCompatibleWithUrl(r: ToolbarClient.ToolbarResponse<any>, location: Location, query: any): number {
  if (r.url)
    return AppContext.toAbsoluteUrl(location.pathname + location.search).startsWith(AppContext.toAbsoluteUrl(r.url)) ? 1 : 0;

  if (!r.content)
    return 0;

  var config = ToolbarClient.getConfig(r);
  if (!config)
    return 0;

  return config.isCompatibleWithUrlPrio(r, location, query);
}

export function inferActive(r: ToolbarClient.ToolbarResponse<any>, location: Location, query: any): { prio: number, response: ToolbarClient.ToolbarResponse<any> } | null {
  if (r.elements)
    return r.elements.map(e => inferActive(e, location, query)).notNull().maxBy(a => a.prio) ?? null;

  var prio = isCompatibleWithUrl(r, location, query);

  if (prio > 0)
    return { prio, response: r };

  return null;
}


export function renderNavItem(res: ToolbarClient.ToolbarResponse<any>, active: ToolbarClient.ToolbarResponse<any> | null, key: string | number, onRefresh: () => void, onAutoClose?: () => void) {

  switch (res.type) {
    case "Divider":
      return <hr style={{ margin: "10px 0 5px 0px" }} key={key}></hr>;
    case "Header":
    case "Item":
      if (res.elements && res.elements.length) {
        var title = res.label || getToString(res.content);
        var icon = ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor);

        return (
          <ToolbarDropdown parentTitle={title} icon={icon} key={key} extraIcons={res.extraIcons} onAutoCloseExtraIcons={onAutoClose}>
            {res.elements && res.elements.map((sr, i) => renderNavItem(sr, active, i, onRefresh, onAutoClose))}
          </ToolbarDropdown>
        );
      }

      if (res.url) {
        var url = res.url!;
        var isExternalLink = url.startsWith("http") && !url.startsWith(window.location.origin + "/" + window.__baseName);
        return (
          <ToolbarNavItem key={key} title={res.label} isExternalLink={isExternalLink} extraIcons={res.extraIcons} onAutoCloseExtraIcons={onAutoClose}
            active={res == active} icon={ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}
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
        var config = ToolbarClient.getConfig(res);
        if (!config)
          return <Nav.Item className="text-danger">{res.content!.EntityType + "ToolbarConfig not registered"}</Nav.Item>;

        return config.getMenuItem(res, res == active, key, onAutoClose);
      }

      if (res.type == "Header") {
        return (
          <div key={key} className={"nav-item-header"}>
            {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}
            <span className={"nav-item-text"}>{res.label}</span>
            <div className={"nav-item-float"}>{res.label}</div>
          </div>
        );
      }

      return <Nav.Item key={key} style={{ color: "red" }}>{"No Content or Url found"}</Nav.Item>;

    default:
      throw new Error("Unexpected " + res.type);
  }
}

function ToolbarDropdown(p: { parentTitle: string | undefined, icon: any, children: any, extraIcons: ToolbarClient.ToolbarResponse<any>[] | undefined, onAutoCloseExtraIcons?: () => void }) {
  var [show, setShow] = React.useState(false);

  return (
    <div>
      <ToolbarNavItem title={p.parentTitle} extraIcons={p.extraIcons} onClick={() => setShow(!show)} onAutoCloseExtraIcons={p.onAutoCloseExtraIcons}
        icon={
          <div style={{ display: 'inline-block', position: 'relative' }}>
            <div className="nav-arrow-icon" style={{ position: 'absolute' }}><FontAwesomeIcon icon={show ? "caret-down" : "caret-right"} className="icon" /></div>
            <div className="nav-icon-with-arrow">
              {p.icon ?? <div className="icon" />}
            </div>
          </div>
        }
      />
      <div style={{ display: show ? "block" : "none" }} className="nav-item-sub-menu">
        {show && p.children}
      </div>
    </div>
  );
}

export function ToolbarNavItem(p: { title: string | undefined, active?: boolean, isExternalLink?: boolean, extraIcons?: ToolbarClient.ToolbarResponse<any>[], onClick: (e: React.MouseEvent) => void, icon?: React.ReactNode, onAutoCloseExtraIcons?: () => void }) {
  return (
    <li className="nav-item d-flex">
      <Nav.Link title={p.title} onClick={p.onClick} onAuxClick={p.onClick} active={p.active} className="d-flex w-100">
        {p.icon}
        <span className={"nav-item-text"}>
          {p.title}
          {p.isExternalLink && <FontAwesomeIcon icon="arrow-up-right-from-square" transform="shrink-5 up-3" />}
        </span>
        {p.extraIcons?.map((ei, i) => {
          if (ei.content) {
            var config = ToolbarClient.getConfig(ei);
            if (config == null) {
              return <span className="text-danger sf-extra-icon">{ei.content!.EntityType + "ToolbarConfig not registered"}</span>
            }
            else {

              return <button className="btn btn-sm border-0 py-0 m-0 sf-extra-icon" key={i} onClick={e => {
                e.preventDefault();
                e.stopPropagation();
                config!.handleNavigateClick(e, ei);

                if (p.onAutoCloseExtraIcons && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
                  p.onAutoCloseExtraIcons();

              }} >{config.getIcon(ei)}</button>
            };
          }

          return <button className="btn btn-sm border-0 py-0 m-0 sf-extra-icon" key={i} onClick={e => {
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

            if (p.onAutoCloseExtraIcons && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
              p.onAutoCloseExtraIcons();

          }}>{ToolbarConfig.coloredIcon(parseIcon(ei.iconName!), ei.iconColor)}</button>

        })}
        <div className={"nav-item-float"}>{p.title}</div>
      </Nav.Link>
    </li>
  );
}
