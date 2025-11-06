import * as React from 'react'
import { useLocation, Location } from 'react-router'
import { is } from '@framework/Signum.Entities'
import * as AppContext from '@framework/AppContext'
import { ToolbarClient, ToolbarResponse } from '../ToolbarClient'
import { ToolbarConfig, ToolbarContext, InferActiveResponse } from "../ToolbarConfig";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import { Nav } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI, useUpdatedRef, useAPIWithReload, useForceUpdate } from '@framework/Hooks'
import { Navigator } from '@framework/Navigator'
import { QueryString } from '@framework/QueryString'
import { Entity, getToString, Lite } from '@framework/Signum.Entities'
import { parseIcon } from '@framework/Components/IconTypeahead'
import { ToolbarUrl } from '../ToolbarUrl';
import { classes } from '@framework/Globals';
import { LayoutMessage, ToolbarEntity, ToolbarMenuEntity,  ToolbarSwitcherEntity } from '../Signum.Toolbar';
import { Binding, getTypeInfo, newLite, queryAllowedInContext } from '../../../Signum/React/Reflection';
import { Finder } from '../../../Signum/React/Finder';
import { EntityLine, TypeContext } from '../../../Signum/React/Lines'
import { RightCaretDropdown } from './RightCaretDropdown'


export default function ToolbarRenderer(p: {
  onAutoClose?: () => void;
}): React.ReactElement | null {

  Navigator.useEntityChanged(ToolbarEntity, () => reload(), []);
  Navigator.useEntityChanged(ToolbarMenuEntity, () => reload(), []);
  Navigator.useEntityChanged(ToolbarSwitcherEntity, () => reload(), []);

  const [response, reload] = useAPIWithReload(() => ToolbarClient.API.getCurrentToolbar("Side"), [], { avoidReset: true });
  const responseRef = useUpdatedRef(response);

  const [refresh, setRefresh] = React.useState(false);
  const [active, setActive] = React.useState<InferActiveResponse | null>(null);

  const location = useLocation();

  function changeActive(location: Location) {
    var query = QueryString.parse(location.search);
    if (responseRef.current) {

      var newActive = inferActive(responseRef.current, location, query);
      setActive(newActive);
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
        <FontAwesomeIcon aria-hidden={true} icon={"angles-left"} aria-label="Close" />
      </div>

      <ul>
        {response && response.elements && response.elements.map((res: ToolbarResponse<any>, i: number) => renderNavItem(res, i, ctx, null))}
      </ul>
    </div>
  );
}

export function isCompatibleWithUrl(r: ToolbarResponse<any>, location: Location, query: any, entityType: string | undefined): { prio: number, inferredEntity?: Lite<Entity> } | null {
  if (r.url) {
    const current = AppContext.toAbsoluteUrl(location.pathname).replace(/\/+$/, "");
    const target = AppContext.toAbsoluteUrl(r.url).replace(/\/+$/, "");

    const currentSegments = current.split("/");
    const targetSegments = target.split("/");

    let id: number | string | undefined;
    let type: string | undefined;
    const idRegex = "[0-9A-Za-z-]+";
    const typeRegex = "[A-Za-z-]+";
    const toStrRegex = ".*";

    function assertValidId(id: string | undefined) {

      if (!id)
        return;

      const m = id.match(new RegExp("^" + idRegex + "$"));
      if (m == null)
        throw new Error("Id is not valid:" + id);
    }

    var matches = currentSegments.length == targetSegments.length && targetSegments.every((pattern, i) => {

      const value = currentSegments[i];

      if (value.toLowerCase() === pattern.toLowerCase())
        return true;

      if (pattern.contains(":id") || pattern.contains(":type") || pattern.contains(":key") || pattern.contains(":toStr") ||
        pattern.contains(":id2") || pattern.contains(":type2") || pattern.contains(":key2") || pattern.contains(":toStr2")) {

        const regexPattern = "^" +
          pattern
            .replace(":id2", idRegex)
            .replace(":type2", typeRegex)
            .replace(":key2", typeRegex + ";" + idRegex)
            .replace(":toStr2", toStrRegex)
            .replace(":id", "(?<id>" + idRegex + ")")
            .replace(":type", "(?<type>" + typeRegex + ")")
            .replace(":key", "(?<type>" + typeRegex + ")" + ";" + "(?<id>" + idRegex + ")")
            .replace(":toStr", toStrRegex)
          + "$";

        const regex = new RegExp(regexPattern);
        const match = value.match(regex);
        if (match == null)
          return null;

        if (match.groups?.id) {
          id = match.groups?.id;
          assertValidId(id);
        }

        if (match.groups?.type)
          type = match!.groups?.type;

        if (type != null && type.toLowerCase() != entityType?.toLowerCase())
          return false;

        return true;
      }

      return false;
    });

    if (matches)
      return { prio: 1, inferredEntity: entityType && id ? newLite(entityType, id) : undefined };

    return null;
  } else {

    if (!r.content)
      return null;

    var config = ToolbarClient.getConfig(r);
    if (!config)
      return null;

    return config.isCompatibleWithUrlPrio(r, location, query);
  }
}


export function inferActive(r: ToolbarResponse<any>, location: Location, query: any, entityType?: string): InferActiveResponse | null {
  if (r.elements) {

    var result = r.elements.map(e => inferActive(e, location, query, entityType ?? r.entityType)).notNull().maxBy(a => a.prio) ?? null;

    if (result == null)
      return null;

    if (r.entityType == null)
      return result;

    if (!result.inferredEntity)
      return result;

    if (result.inferredEntity.EntityType != r.entityType)
      return null;

    return {
      ...result,
      inferredEntity: undefined,
      menuWithEntity: { menu: r, entity: result.inferredEntity }
    };
  }

  var main = isCompatibleWithUrl(r, location, query, r.entityType ?? entityType);
  var bestExtra = r.extraIcons?.map(e => inferActive(e, location, query)).notNull().maxBy(a => a.prio) ?? null;

  if (bestExtra != null && (main == null || bestExtra.prio > main.prio))
    return bestExtra;

  if (main != null)
    return {
      prio: main.prio,
      response: r,
      inferredEntity: main.inferredEntity
    };

  return null;
}

export function renderNavItem(res: ToolbarResponse<any>, key: string | number, ctx: ToolbarContext, selectedEntity: Lite<Entity> | null): React.JSX.Element {

  switch (res.type) {
    case "Divider":
      return <hr style={{ margin: "10px 0 5px 0px" }} key={key}></hr>;
    case "Header":
    case "Item":
      if (ToolbarMenuEntity.isLite(res.content)) {
        return <ToolbarMenu response={res} key={key} ctx={ctx} selectedEntity={selectedEntity} />;
      }

      if (ToolbarSwitcherEntity.isLite(res.content)) {
        return <ToolbarSwitcher response={res} key={key} ctx={ctx} selectedEntity={selectedEntity} />;
      }

      if (res.url) {
        const config = res.content && ToolbarClient.getConfig(res);
        return (
          <ToolbarNavItem key={key} title={res.label} isExternalLink={ToolbarUrl.isExternalLink(res.url)
      }
            extraIcons={renderExtraIcons(res.extraIcons, ctx, selectedEntity)}
            active={isActive(ctx.active, res, selectedEntity)} icon={<>
              {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}
              {config?.getCounter(res, selectedEntity)}
            </>}
            onClick={(e: React.MouseEvent<any>) => linkClick(res, selectedEntity, e, ctx)} />
        );
      }

      if (res.content) {
        const config = ToolbarClient.getConfig(res);
        if (!config)
          return <Nav.Item className="text-danger">{res.content!.EntityType + "ToolbarConfig not registered"}</Nav.Item>;

        return config.getMenuItem(res, key, ctx, selectedEntity);
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

function responseClick(r: ToolbarResponse<ToolbarMenuEntity>, selectedEntity: Lite<Entity> | null, e: React.SyntheticEvent | undefined, ctx: ToolbarContext) {
  if (r.url != null) {
    linkClick(r, selectedEntity, e as React.MouseEvent | undefined, ctx);
  }
  else {
    var config = ToolbarClient.getConfig(r);
    if (config != null)
      config.handleNavigateClick(e as React.MouseEvent, r, selectedEntity);
  }
}

async function linkClick(r: ToolbarResponse<ToolbarMenuEntity>, selectedEntity: Lite<Entity> | null, e: React.MouseEvent | undefined, ctx: ToolbarContext) {

  let url = r.url!;
  if (ToolbarUrl.hasSubEntity(url)) {
    const config = r.content && ToolbarClient.getConfig(r);
    const subEntity = config && await config.selectSubEntityForUrl(r, selectedEntity);
    if (subEntity == null)
      return;

    url = ToolbarUrl.replaceSubEntity(url, subEntity)
  }

  url = ToolbarUrl.replaceVariables(url)

  if (selectedEntity)
    url = ToolbarUrl.replaceEntity(url, selectedEntity)

  if (ToolbarUrl.isExternalLink(url))
    window.open(url);
  else
    AppContext.pushOrOpenInTab(url, e);


  if (ctx.onAutoClose && !(e && (e.ctrlKey || (e as React.MouseEvent<any>).button == 1)))
    ctx.onAutoClose();
}



function ToolbarMenu(p: { response: ToolbarResponse<ToolbarMenuEntity>, ctx: ToolbarContext, selectedEntity: Lite<Entity> | null }): React.ReactElement {

  const title = p.response.label || getToString(p.response.content);
  const icon = ToolbarConfig.coloredIcon(parseIcon(p.response.iconName), p.response.iconColor);

  const key = "toolbar-menu-" + p.response.content!.id;

  const [show, setShow] = React.useState(localStorage.getItem(key) != null);

  function handleShowClick(e: React.MouseEvent | null) {

    var value = !show;

    if (value)
      localStorage.setItem(key, "1");
    else
      localStorage.removeItem(key);

    setShow(value);

    if (value && e) {
      var autoSelect = p.response.elements?.firstOrNull(a => a.autoSelect && !a.withEntity);
      if (autoSelect) {
        responseClick(autoSelect, p.selectedEntity, e, p.ctx);
      }
    }
  }

  React.useEffect(() => {

    if (p.ctx.active && !show) {
      const isContained = containsResponse(p.response, p.ctx.active.response);
      if (isContained)
        handleShowClick(null);
    }
  }, [p.ctx.active]);


  return (
    <li>
      <ul>
        <ToolbarNavItem title={title} extraIcons={renderExtraIcons(p.response.extraIcons, p.ctx, p.selectedEntity)} isGroup={true} onClick={e => handleShowClick(e)}
          icon={
            <div style={{ position: 'relative' }}>
              <div className="nav-arrow-icon" style={{ position: 'absolute' }}>
                <FontAwesomeIcon icon={show ? "chevron-down" : "chevron-right"} className="icon" />
              </div>
              <div className="nav-icon-with-arrow">
                {icon}
              </div>
            </div>
          }
        />
        {show && <li>
          <ul style={{ display: show ? "block" : "none" }} className="nav-item-sub-menu">
            <ToolbarMenuItems response={p.response} ctx={p.ctx} selectedEntity={p.selectedEntity} />
          </ul>
        </li>}
      </ul>
    </li>
  );
}

export function ToolbarMenuItems(p: { response: ToolbarResponse<ToolbarMenuEntity>, ctx: ToolbarContext, selectedEntity: Lite<Entity> | null }): React.ReactNode {
  const entityType = p.response.entityType;
  if (entityType)
    return <ToolbarMenuItemsEntityType response={p.response} ctx={p.ctx} selectedEntity={p.selectedEntity} />

  return <>
    {p.response.elements!.map((sr, i) => renderNavItem(sr, i, p.ctx, p.selectedEntity))}
  </>;
}


function ToolbarMenuItemsEntityType(p: { response: ToolbarResponse<ToolbarMenuEntity>, ctx: ToolbarContext, selectedEntity: Lite<Entity> | null }): React.ReactNode {

  const entityType = p.response.entityType!;
  const selEntityRef = React.useRef<Lite<Entity> | null>(null);
  const forceUpdate = useForceUpdate();

  function setSelectedEntity(entity: Lite<Entity> | null) {
    selEntityRef.current = entity;
    forceUpdate();
  }


  const entities = useAPI(() => Finder.API.fetchAllLites({ types: entityType }), [entityType]);

  const active = p.ctx.active;

  React.useEffect(() => {

    if (active?.menuWithEntity && p.response.entityType &&
      is(active.menuWithEntity.menu.content, p.response.content)) {

      if (!is(active.menuWithEntity.entity, selEntityRef.current))
        setSelectedEntity(active.menuWithEntity.entity);
    }
    else if (active != null)
      setSelectedEntity(null);

  }, [active?.menuWithEntity, p.response]);

  React.useEffect(() => {

    if (selEntityRef.current && !selEntityRef.current.model && entities) {
      var only = entities.onlyOrNull(a => is(a, selEntityRef.current));
      if (only != null)
        setSelectedEntity(only);
    }

  }, [selEntityRef.current, entities]);

  function handleSelect(e: React.SyntheticEvent | undefined) {

    forceUpdate();
    var autoSelect = p.response.elements?.firstOrNull(a => a.autoSelect && a.withEntity == Boolean(selEntityRef.current));
    if (autoSelect) {
      responseClick(autoSelect, selEntityRef.current, e, p.ctx);
    }
  }

  const ctx = new TypeContext<Lite<Entity> | null>(undefined, undefined, undefined, new Binding(selEntityRef, "current"));
  var ti = getTypeInfo(entityType);
  return (
    <>
      {entityType && (
        <Nav.Item title={ti.niceName} className="d-flex mx-2 mb-2">
          <div style={{ width: "100%" }}>
            <EntityLine ctx={ctx} type={{ name: entityType, isLite: true }} view={false}
              inputAttributes={{ placeholder: LayoutMessage.SelectA0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName) }}
              onChange={e => handleSelect(e.originalEvent)} create={false} formGroupStyle="SrOnly" />
          </div>
          {renderExtraIcons(p.response.extraIcons, p.ctx, selEntityRef.current ?? p.selectedEntity)}
        </Nav.Item>
      )}
      {selEntityRef.current ?
        simplifyForEntity(p.response.elements!.filter(sr => sr.withEntity), selEntityRef.current).map((sr, i) => renderNavItem(sr, i, p.ctx, selEntityRef.current ?? p.selectedEntity)) :
        p.response.elements!.filter(sr => !sr.withEntity).map((sr, i) => renderNavItem(sr, i, p.ctx, selEntityRef.current ?? p.selectedEntity))
      }
    </>
  );
}

function simplifyForEntity(resp: ToolbarResponse<any>[], selectedEntity: Lite<Entity>): ToolbarResponse<any>[] {
  var result = resp
    .map(tr => {

      if (tr.queryKey != null && !queryAllowedInContext(tr.queryKey, selectedEntity))
        return null;

      if (tr.elements && tr.elements.length > 0) {
        const inner = simplifyForEntity(tr.elements, selectedEntity);
        if (inner.length == 0)
          return null;

        tr = { ...tr, elements: inner };
      }

      if (tr.extraIcons && tr.extraIcons.length > 0) {
        const extraIcons = simplifyForEntity(tr.extraIcons, selectedEntity);

        tr = { ...tr, extraIcons };
      }

      return tr;
    }).notNull();

  while (true) {
    var extraDividers = result.filter((a, i) => a.type == "Divider" && (
      i == 0 ||
      result[i - 1].type == "Divider" ||
      i == result.length - 1
    ));

    extraDividers.forEach(ed => result.remove(ed));

    function isPureHeader(tr: ToolbarResponse<any>): boolean {
      return tr.type == "Header" && !tr.content && !tr.url;
    }

    var extraHeaders = result.filter((a, i) => isPureHeader(a) && (
      i == result.length - 1 ||
      isPureHeader(result[i + 1]) ||
      result[i + 1].type == "Divider" ||
      result[i + 1].type == "Header" && ToolbarMenuEntity.isLite(result[i + 1].content)
    ));
    extraHeaders.forEach(eh => result.remove(eh));

    if (extraDividers.length == 0 && extraHeaders.length == 0)
      break;
  }

  return result;
}

function containsResponse(r: ToolbarResponse<any>, active: ToolbarResponse<any>): boolean {
  return r == active || r.elements != null && r.elements.some(e => containsResponse(e, active));
}

function ToolbarSwitcher(p: { response: ToolbarResponse<ToolbarSwitcherEntity>, ctx: ToolbarContext, selectedEntity: Lite<Entity> | null }) {

  const ts = p.response.content!;

  const key = "toolbar-switcher-" + ts.id!;

  const [selectedOption, setSelectedOption] = React.useState(() => {
    const sel = localStorage.getItem(key);
    return p.response.elements?.onlyOrNull(a => a.content!.id!.toString() == sel);
  });

  function handleSetShow(value: ToolbarResponse<any>, e: React.SyntheticEvent | null) {
    localStorage.setItem(key, value.content!.id!.toString());

    setSelectedOption(value);

    if (value && e) {
      var autoSelect = value.elements?.firstOrNull(a => a.autoSelect && !a.withEntity);
      if (autoSelect) {
        responseClick(autoSelect, p.selectedEntity, e, p.ctx);
      }

    }
  }


  React.useEffect(() => {

    if (p.ctx.active) {
      const menu = p.response.elements?.firstOrNull(e => containsResponse(e, p.ctx.active!.response!));
      if (menu != null && menu != selectedOption)
        handleSetShow(menu, null);
    }
  }, [p.ctx.active]);

  const icon = ToolbarConfig.coloredIcon(parseIcon(p.response.iconName), p.response.iconColor);
  const title = p.response.label || getToString(p.response.content);

  const options = (p.response.elements ?? []).map(el => ({
    value: el,
    label: el.label || getToString(el.content),
    icon: el.iconName ? ToolbarConfig.coloredIcon(parseIcon(el.iconName), el.iconColor) : undefined
  }));
  return (
    <li>
      <ul>
        <Nav.Item title={title} className="d-flex mx-2 mb-2">
          {icon}
          <RightCaretDropdown
            options={options}
            value={selectedOption ?? null}
            onChange={val => val && handleSetShow(val, null)}
            placeholder={title}
            disabled={false} />
          {renderExtraIcons(p.response.extraIcons, p.ctx, p.selectedEntity)}
        </Nav.Item>

        {selectedOption &&
          <li>
            <ul>
              {selectedOption.elements && <ToolbarMenuItems response={selectedOption} ctx={p.ctx} selectedEntity={p.selectedEntity} />}
            </ul>
          </li>
        }
      </ul>
    </li>
  );
}

export function ToolbarNavItem(p: { title: string | undefined, active?: boolean, isExternalLink?: boolean, isGroup?: boolean, extraIcons?: React.ReactElement, onClick: (e: React.MouseEvent) => void, icon?: React.ReactNode, onAutoCloseExtraIcons?: () => void }): React.JSX.Element {
  return (
    <li className="nav-item d-flex">
      <Nav.Link title={p.title} onClick={p.onClick} onAuxClick={p.onClick} active={p.active} className="d-flex w-100" >
        <div>{p.icon}</div>
        <span className={classes("nav-item-text", p.isGroup && "nav-item-group")}>
          {p.title}
          {p.isExternalLink && <FontAwesomeIcon aria-hidden={true} icon="arrow-up-right-from-square" transform="shrink-5 up-3" />}
        </span>
        {p.extraIcons}
        <div className={classes("nav-item-float", p.isGroup && "nav-item-group")}>{p.title}</div>
      </Nav.Link>
    </li>
  );
}

export function isActive(active: InferActiveResponse | null, res: ToolbarResponse<any>, selectedEntity: Lite<Entity> | null): boolean {

  function isSame(a: ToolbarResponse<any>, b: ToolbarResponse<any>) {
    return a == b || a.content == b.content && a.url == b.url; //simplifyForEntity clones responses
  }

  return active != null && isSame(active.response, res) && (active.menuWithEntity == null || is(active.menuWithEntity.entity, selectedEntity));
}

export function renderExtraIcons(extraIcons: ToolbarResponse<any>[] | undefined, ctx: ToolbarContext, selectedEntity: Lite<Entity> | null): React.ReactElement | undefined {
  if (extraIcons == null)
    return undefined;

  return (<>
    {extraIcons?.map((ei, i) => {

      if (ei.url) {
        return <button type="button" className={classes("btn btn-sm border-0 py-0 m-0 sf-extra-icon", isActive(ctx.active, ei, selectedEntity) && "active")} key={i}
          onClick={e => { e.stopPropagation(); linkClick(ei, selectedEntity, e, ctx); } }>
          {ToolbarConfig.coloredIcon(parseIcon(ei.iconName!), ei.iconColor)}
        </button>;
      }

      var config = ToolbarClient.getConfig(ei);
      if (config == null) {
        return <span className="text-danger sf-extra-icon">{ei.content!.EntityType + "ToolbarConfig not registered"}</span>
      }
      else {

        return <button type="button" className={classes("btn btn-sm border-0 py-0 m-0 sf-extra-icon", isActive(ctx.active, ei, selectedEntity) && "active")} key={i} onClick={e => {
          e.stopPropagation();
          config!.handleNavigateClick(e, ei, selectedEntity);

          if (ctx.onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
            ctx.onAutoClose();

        }} >{config.getIcon(ei, selectedEntity)}</button>
      };

    })}
  </>);
}
