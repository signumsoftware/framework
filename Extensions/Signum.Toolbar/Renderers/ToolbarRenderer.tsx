import * as React from 'react'
import { useLocation, Location } from 'react-router'
import * as AppContext from '@framework/AppContext'
import { ToolbarClient, ToolbarResponse } from '../ToolbarClient'
import { ToolbarConfig, ToolbarContext } from "../ToolbarConfig";
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
import { ToolbarEntity, ToolbarMenuEntity, ToolbarMessage, ToolbarSwitcherEntity } from '../Signum.Toolbar';
import DropdownList from "react-widgets-up/DropdownList";


export default function ToolbarRenderer(p: {
  onAutoClose?: () => void;
}): React.ReactElement | null {

  Navigator.useEntityChanged(ToolbarEntity, () => reload(), []);
  Navigator.useEntityChanged(ToolbarMenuEntity, () => reload(), []);
  Navigator.useEntityChanged(ToolbarSwitcherEntity, () => reload(), []);

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

  const ctx: ToolbarContext = {
    active,
    onRefresh: handleRefresh,
    onAutoClose: p.onAutoClose,
  }

  return (
    <div className={"sidebar-inner"}>
      <div className={"close-sidebar"}
        onClick={() => p.onAutoClose && p.onAutoClose()}>
        <FontAwesomeIcon icon={"angles-left"} aria-label="Close" />
      </div>

      <ul>
        {response && response.elements && response.elements.map((res: ToolbarResponse<any>, i: number) => renderNavItem(res, i, ctx))}
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



export function renderNavItem(res: ToolbarResponse<any>, key: string | number, ctx: ToolbarContext): React.JSX.Element {

  switch (res.type) {
    case "Divider":
      return <hr style={{ margin: "10px 0 5px 0px" }} key={key}></hr>;
    case "Header":
    case "Item":
      if (ToolbarMenuEntity.isLite(res.content)) {
        return <ToolbarMenu response={res} key={key} ctx={ctx}/>;
      }

      if (ToolbarSwitcherEntity.isLite(res.content)) {
        return <ToolbarSwitcher response={res} key={key} ctx={ctx}/>;
      }

      if (res.url) {
        let url = res.url!;
        const isExternalLink = url.startsWith("http") && !url.startsWith(window.location.origin + "/" + window.__baseName);
        const config = res.content && ToolbarClient.getConfig(res);
        return (
          <ToolbarNavItem key={key} title={res.label} isExternalLink={isExternalLink} extraIcons={renderExtraIcons(res.extraIcons, ctx)}
            active={res == ctx.active} icon={<>
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

              if (ctx.onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
                ctx.onAutoClose();
            }} />
        );
      }

      if (res.content) {
        const config = ToolbarClient.getConfig(res);
        if (!config)
          return <Nav.Item className="text-danger">{res.content!.EntityType + "ToolbarConfig not registered"}</Nav.Item>;

        return config.getMenuItem(res, key, ctx);
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

function ToolbarMenu(p: { response: ToolbarResponse<ToolbarMenuEntity>, ctx: ToolbarContext }) {

  const title = p.response.label || getToString(p.response.content);
  const icon = ToolbarConfig.coloredIcon(parseIcon(p.response.iconName), p.response.iconColor);

  const key = "toolbar-menu-" + p.response.content!.id;

  const [show, setShow] = React.useState(localStorage.getItem(key) != null);

  function handleSetShow(value: boolean) {
    if (value)
      localStorage.setItem(key, "1");
    else
      localStorage.removeItem(key);

    setShow(value);
  }

  React.useEffect(() => {

    if (p.ctx.active && !show) {
      const isContained = containsResponse(p.response, p.ctx.active!);
      if (isContained)
        handleSetShow(true);
    }
  }, [p.ctx.active]);


  return (
    <li>
      <ul>
        <ToolbarNavItem title={title} extraIcons={renderExtraIcons(p.response.extraIcons, p.ctx)} onClick={() => handleSetShow(!show)}
          icon={
            <div style={{ display: 'inline-block', position: 'relative' }}>
              <div className="nav-arrow-icon" style={{ position: 'absolute' }}>
                <FontAwesomeIcon icon={show ? "chevron-down" : "chevron-right"} className="icon" />
              </div>
              <div className="nav-icon-with-arrow">
                {icon ?? <div className="icon" />}
              </div>
            </div>
          }
        />
       {show && <li>
          <ul style={{ display: show ? "block" : "none" }} className="nav-item-sub-menu">
            {p.response.elements!.map((sr, i) => renderNavItem(sr, i, p.ctx))}
          </ul>
        </li>}  
      </ul>
    </li>
  );
}

function containsResponse(r: ToolbarResponse<any>, active: ToolbarResponse<any>): boolean {
  return r == active || r.elements != null && r.elements.some(e => containsResponse(e, active)); 
}

function ToolbarSwitcher(p: { response: ToolbarResponse<ToolbarSwitcherEntity>, ctx: ToolbarContext }) {

  const ts = p.response.content!;

  const key = "toolbar-switcher-" + ts.id!;

  const [selectedOption, setSelectedOption] = React.useState(() => {
    const sel = localStorage.getItem(key);
    return p.response.elements?.onlyOrNull(a => a.content!.id!.toString() == sel);
  });

  function handleSetShow(value: ToolbarResponse<any>) {
    localStorage.setItem(key, value.content!.id!.toString());

    setSelectedOption(value);
  }

  React.useEffect(() => {

    if (p.ctx.active) {
      const menu = p.response.elements?.firstOrNull(e => containsResponse(e, p.ctx.active!));
      if (menu != null && menu != selectedOption)
        handleSetShow(menu);
    }
  }, [p.ctx.active]);

  function renderDropDownOptions(p: ToolbarResponse<any>) {
    if (p == null)
      return <span> - </span>;

    const icon = ToolbarConfig.coloredIcon(parseIcon(p.iconName), p.iconColor);
    const title = p.label || getToString(p.content);
    return <span>{icon}{title}</span>
  }

  const icon = ToolbarConfig.coloredIcon(parseIcon(p.response.iconName), p.response.iconColor);
  const title = p.response.label || getToString(p.response.content);

  return (
    <li>
      <ul>
        <Nav.Item title={title} className="d-flex mx-2 mb-2">
          {icon}
          <DropdownList id={key}
            inputProps={{disabled: true}}
            value={selectedOption}
            dataKey={((r: ToolbarResponse<any>) => r && r.content!.id) as any}
            textField={((r: ToolbarResponse<any>) => r && r.label || getToString(r.content)) as any}
            onChange={e => handleSetShow(e)}
            data={p.response.elements}
            renderValue={a => renderDropDownOptions(a.item)}
            renderListItem={a => renderDropDownOptions(a.item)}
          />
          {renderExtraIcons(p.response.extraIcons, p.ctx)}
        </Nav.Item>

        {selectedOption &&
          <li>
            <ul>
              {selectedOption.elements && selectedOption.elements.map((sr, i) => renderNavItem(sr, i, p.ctx))}
            </ul>
          </li>
        }
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

export function renderExtraIcons(extraIcons: ToolbarResponse<any>[] | undefined, ctx: ToolbarContext): React.ReactElement | undefined {
  if (extraIcons == null)
    return undefined;

  return (<>
    {extraIcons?.map((ei, i) => {

      if (ei.url) {
        return <button className={classes("btn btn-sm border-0 py-0 m-0 sf-extra-icon", ei == ctx.active && "active")} key={i} onClick={e => {
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

          if (ctx.onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
            ctx.onAutoClose();

        }}>{ToolbarConfig.coloredIcon(parseIcon(ei.iconName!), ei.iconColor)}</button>;
      }


      var config = ToolbarClient.getConfig(ei);
      if (config == null) {
        return <span className="text-danger sf-extra-icon">{ei.content!.EntityType + "ToolbarConfig not registered"}</span>
      }
      else {

        return <button className={classes("btn btn-sm border-0 py-0 m-0 sf-extra-icon", ei == ctx.active && "active")} key={i} onClick={e => {
          e.preventDefault();
          e.stopPropagation();
          config!.handleNavigateClick(e, ei);

          if (ctx.onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
            ctx.onAutoClose();

        }} >{config.getIcon(ei)}</button>
      };

    })}
  </>);
}
