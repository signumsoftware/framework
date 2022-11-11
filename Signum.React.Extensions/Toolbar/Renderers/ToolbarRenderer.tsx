import * as React from 'react'
import * as History from 'history'
import * as AppContext from '@framework/AppContext'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarClient";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import { Nav } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI, useUpdatedRef, useHistoryListen, useWindowEvent, useAPIWithReload } from '@framework/Hooks'
import * as Navigator from '@framework/Navigator'
import { QueryString } from '@framework/QueryString'
import { getToString } from '@framework/Signum.Entities'
import { parseIcon } from '../../Basics/Templates/IconTypeahead'
import { urlVariables } from '../../Dashboard/UrlVariables';
import { Dic } from '@framework/Globals';
import { ToolbarEntity, ToolbarMenuEntity } from '../Signum.Entities.Toolbar';



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

  function changeActive(location: History.Location) {
    var query = QueryString.parse(location.search);
    if (responseRef.current) {

      var newActive = inferActive(responseRef.current, location, query);
      setActive(newActive?.response ?? null);
    }
  }

  useHistoryListen((location: History.Location, action: History.Action) => {
    changeActive(location);
  }, response != null);

  React.useEffect(() => changeActive(AppContext.history.location), [response]);

  function handleRefresh() {
    return setTimeout(() => setRefresh(!refresh), 500)
  }

  return (
    <div className={"sidebar-inner"}>
      {p.appTitle}
      <div className={"close-sidebar"}
        onClick={() => p.onAutoClose && p.onAutoClose()}>
        <FontAwesomeIcon icon={"angles-left"} />
      </div>

      <div>
        {response && response.elements && response.elements.map((res: ToolbarClient.ToolbarResponse<any>, i: number) => renderNavItem(res, active, i, handleRefresh, p.onAutoClose))}
      </div>
    </div>
  );
}

export function isCompatibleWithUrl(r: ToolbarClient.ToolbarResponse<any>, location: History.Location, query: any): number {
  if (r.url)
    return (location.pathname + location.search).startsWith(AppContext.toAbsoluteUrl(r.url)) ? 1 : 0;

  if (!r.content)
    return 0;

  var config = ToolbarClient.getConfig(r);
  if (!config)
    return 0;

  return config.isCompatibleWithUrlPrio(r, location, query);
}

export function inferActive(r: ToolbarClient.ToolbarResponse<any>, location: History.Location, query: any): { prio: number, response: ToolbarClient.ToolbarResponse<any> } | null {
  if (r.elements)
    return r.elements.map(e => inferActive(e, location, query)).notNull().withMax(a => a.prio) ?? null;

  var prio = isCompatibleWithUrl(r, location, query);

  if (prio > 0)
    return { prio, response: r };

  return null;
}


export function renderNavItem(res: ToolbarClient.ToolbarResponse<any>, active: ToolbarClient.ToolbarResponse<any> | null, key: string | number, onRefresh: () => void, onAutoClose?: ()=> void, ) {

  switch (res.type) {
    case "Divider":
      return <hr style={{ margin: "10px 0 5px 0px" }} key={key}></hr>;
    case "Header":
    case "Item":
      if (res.elements && res.elements.length) {
        var title = res.label || getToString(res.content);
        var icon = ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor);

        return (
          <ToolbarDropdown parentTitle={title} icon={icon} key={key}>
            {res.elements && res.elements.map((sr, i) => renderNavItem(sr, active, i, onRefresh, onAutoClose))}
          </ToolbarDropdown>
        );
      }

      if (res.url) {
        return (
          <ToolbarNavItem key={key} title={res.label} onClick={(e: React.MouseEvent<any>) => {
            var url = res.url!;
            Dic.getKeys(urlVariables).forEach(v => {
              url = url.replaceAll(v, urlVariables[v]());
            });

            AppContext.pushOrOpenInTab(url, e);
            if (onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
              onAutoClose();
          }}
            active={res == active} icon={ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)} />
        );
      }

      if (res.content) {
        var config = ToolbarClient.getConfig(res);
        if (!config)
          return <Nav.Item style={{ color: "red" }}>{res.content!.EntityType + "ToolbarConfig not registered"}</Nav.Item>;

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

function ToolbarDropdown(props: { parentTitle: string | undefined, icon: any, children: any }) {
  var [show, setShow] = React.useState(false);

  return (
    <div>
      <ToolbarNavItem title={props.parentTitle} onClick={() => setShow(!show)}
        icon={
          <div style={{ display: 'inline-block', position: 'relative' }}>
            <div className="nav-arrow-icon" style={{ position: 'absolute' }}><FontAwesomeIcon icon={show ? "caret-down" : "caret-right"} className="icon" /></div>
            <div className="nav-icon-with-arrow">
              {props.icon ?? <div className="icon"/>}
            </div>
          </div>
        }
      />
      <div style={{ display: show ? "block" : "none" }} className="nav-item-sub-menu">
        {show && props.children}
      </div>
    </div>
  );
}

export function ToolbarNavItem(p: { title: string | undefined, active?: boolean, onClick: (e: React.MouseEvent) => void, icon?: React.ReactNode }) {
  return (
    <Nav.Item >
      <Nav.Link title={p.title} onClick={p.onClick} onAuxClick={p.onClick} active={p.active}>
        {p.icon} 
        <span className={"nav-item-text"}>{p.title}</span>
        <div className={"nav-item-float"}>{p.title}</div>
      </Nav.Link>
    </Nav.Item >
  );
}
