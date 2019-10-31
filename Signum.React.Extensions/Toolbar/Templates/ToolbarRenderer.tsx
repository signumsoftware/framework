import * as React from 'react'
import * as History from 'history'
import * as QueryString from 'query-string'
import { classes } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ToolbarLocation } from '../Signum.Entities.Toolbar'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarClient";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import * as PropTypes from "prop-types";
import { DropdownButton, Dropdown } from 'react-bootstrap';
import { Nav } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { parseIcon } from '../../Dashboard/Admin/Dashboard';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';
import { useAPI, useUpdatedRef } from '@framework/Hooks'


function isCompatibleWithUrl(r: ToolbarClient.ToolbarResponse<any>, location: History.Location, query: QueryString.ParsedQuery<string>): boolean {
  if (r.url)
    return (location.pathname + location.search).startsWith(Navigator.toAbsoluteUrl(r.url));

  if (!r.content)
    return false;

  var config = ToolbarClient.configs[r.content.EntityType];
  if (!config)
    return false;

  return config.isCompatibleWithUrl(r, location, query);
}

function inferActive(r: ToolbarClient.ToolbarResponse<any>, location: History.Location, query: QueryString.ParsedQuery<string>): ToolbarClient.ToolbarResponse<any> | null {
  if (r.elements)
    return r.elements.map(e => inferActive(e, location, query)).notNull().onlyOrNull();

  if (isCompatibleWithUrl(r, location, query))
    return r;

  return null;
}

export interface ToolbarRendererState {
  response?: ToolbarClient.ToolbarResponse<any>;
  expanded: ToolbarClient.ToolbarResponse<any>[];
  avoidCollapse: ToolbarClient.ToolbarResponse<any>[];
  active?: ToolbarClient.ToolbarResponse<any> | null;
}

export default function ToolbarRenderer(p: { location?: ToolbarLocation; }): React.ReactElement | null {
  const response = useAPI(() => ToolbarClient.API.getCurrentToolbar(p.location!), [p.location]);

  const [expanded, setExpanded] = React.useState<ToolbarClient.ToolbarResponse<any>[]>([]);
  const [avoidCollapse, setAvoidCollapse] = React.useState<ToolbarClient.ToolbarResponse<any>[]>([]);

  const [active, setActive] = React.useState<ToolbarClient.ToolbarResponse<any> | null>(null);
  const activeRef = useUpdatedRef(active);
  const unregisterCallback = React.useRef<History.UnregisterCallback | undefined>(undefined);

  React.useEffect(() => {
    function locationChanged(location: History.Location, action: History.Action) {
      var query = QueryString.parse(location.search);
      if (response) {
        if (activeRef.current && isCompatibleWithUrl(activeRef.current, location, query)) {
          return;
        }

        var newActive = inferActive(response, location, query);
        setActive(newActive);
      }
    }

    unregisterCallback.current = Navigator.history.listen(locationChanged);

    return () => { unregisterCallback.current!(); }
  }, [response]);

  if (!response)
    return null;

  if (p.location == "Top") {

    var navItems = response.elements && response.elements.map((res, i) => withKey(renderNavItem(res, i), i));


    return (
      <div className={classes("nav navbar-nav")}>
        {navItems}
      </div>
    );
  }
  else
    return (
      <div className="nav">
        {response.elements && response.elements.flatMap(sr => renderDropdownItem(sr, 0, response)).map((sr, i) => withKey(sr, i))}
      </div>
    );


  function handleOnToggle(res: ToolbarClient.ToolbarResponse<any>) {
    if (avoidCollapse.contains(res)) {
      avoidCollapse.remove(res);
      return;
    }

    if (!expanded.contains(res))
      setExpanded([...expanded, res]);
    else
      setExpanded([]);
  }

  function renderNavItem(res: ToolbarClient.ToolbarResponse<any>, index: number) {

    switch (res.type) {

      case "Divider":
        return (
          <Nav.Item>{"|"}</Nav.Item>
        );

      case "Header":
      case "Item":
        if (res.elements && res.elements.length) {
          var title = res.label || res.content!.toStr;
          var icon = getIcon(res);
          return (
            <Dropdown
              onToggle={() => handleOnToggle(res)}
              show={expanded.contains(res)} >
              <Dropdown.Toggle id={"button" + index}>{!icon ? title : (<span>{icon}{title}</span>)}</Dropdown.Toggle>
              <Dropdown.Menu>
                {res.elements && res.elements.flatMap(sr => renderDropdownItem(sr, 1, res)).map((sr, i) => withKey(sr, i))}
              </Dropdown.Menu>
            </Dropdown>
          );
        }

        if (res.url) {
          return (
            <Nav.Item>
              <Nav.Link onClick={(e: React.MouseEvent<any>) => Navigator.pushOrOpenInTab(res.url!, e)}>
                {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}{res.label}
              </Nav.Link>
            </Nav.Item>
          );
        }

        if (res.content) {
          var config = ToolbarClient.configs[res.content!.EntityType];
          if (!config)
            return <Nav.Item style={{ color: "red" }}>{res.content!.EntityType + "ToolbarConfig not registered"}</Nav.Item>;

          return (
            <Nav.Item>
              <Nav.Link onClick={(e: React.MouseEvent<any>) => config.handleNavigateClick(e, res)}>
                {config.getIcon(res)}{config.getLabel(res)}
              </Nav.Link>
            </Nav.Item>
          );
        }

        if (res.type == "Header") {
          return (
            <Nav.Item>{getIcon(res)}{res.label}</Nav.Item>
          );
        }

        return <Nav.Item style={{ color: "red" }}>{"No Content or Url found"}</Nav.Item>;

      default:
        throw new Error("Unexpected " + res.type);
    }
  }



  function handleClick(e: React.MouseEvent<any>, res: ToolbarClient.ToolbarResponse<any>, topRes: ToolbarClient.ToolbarResponse<any>) {

    avoidCollapse.push(topRes);

    var path = findPath(res, [topRes]);

    if (!path)
      throw new Error("Path not found");

    if (expanded.contains(res))
      path.pop();

    setExpanded(path);
  }

  function renderDropdownItem(res: ToolbarClient.ToolbarResponse<any>, indent: number, topRes: ToolbarClient.ToolbarResponse<any>): React.ReactElement<any>[] {

    const menuItemN = "menu-item-" + indent;

    switch (res.type) {

      case "Divider":
        return [
          <Dropdown.Divider className={menuItemN} />
        ];

      case "Header":
      case "Item":

        var HeaderOrItem = res.type == "Header" ? Dropdown.Item : Dropdown.Header;

        if (res.elements && res.elements.length) {
          return [
            <HeaderOrItem onClick={(e: React.MouseEvent<any>) => handleClick(e, res, topRes)}
              className={classes(menuItemN, "sf-cursor-pointer", expanded.contains(res) && "active")}>
              {getIcon(res)}{res.label || res.content!.toStr}<FontAwesomeIcon icon={expanded.contains(res) ? "chevron-down" : "chevron-left"} className="arrow-align" />
            </HeaderOrItem>
          ].concat(res.elements && res.elements.length && expanded.contains(res) ? res.elements.flatMap(r => renderDropdownItem(r, indent + 1, topRes)) : [])
        }

        if (res.url) {
          return [
            <HeaderOrItem onClick={(e: React.MouseEvent<any>) => Navigator.pushOrOpenInTab(res.url!, e)} className={classes("sf-cursor-pointer", menuItemN)} >
              {ToolbarConfig.coloredIcon(parseIcon(res.iconName), res.iconColor)}{res.label}
            </HeaderOrItem>
          ];
        }

        if (res.content) {
          var config = ToolbarClient.configs[res.content!.EntityType];
          if (!config) {
            return [
              <HeaderOrItem style={{ color: "red" }} className={menuItemN}>
                {res.content!.EntityType + "ToolbarConfig not registered"}
              </HeaderOrItem>
            ];
          }

          return [
            <HeaderOrItem onClick={(e: React.MouseEvent<any>) => config.handleNavigateClick(e, res)} className={classes("sf-cursor-pointer", menuItemN, res == this.state.active)}>
              {config.getIcon(res)}{config.getLabel(res)}
            </HeaderOrItem>
          ];
        }

        if (res.type == "Header")
          return [
            <HeaderOrItem className={menuItemN}>{getIcon(res)}{res.label}</HeaderOrItem>
          ];

        return [
          <Dropdown.Item style={{ color: "red" }} className={menuItemN}>
            {"No Content or Url found"}
          </Dropdown.Item>
        ];

      default: throw new Error("Unexpected " + res.type);
    }
  }

  function getIcon(res: ToolbarClient.ToolbarResponse<any>) {

    var icon = parseIcon(res.iconName);

    return icon && <FontAwesomeIcon icon={icon} className={"icon"} color={res.iconColor} fixedWidth />
  }
}

ToolbarRenderer.defaultProps = { location: "Top" as ToolbarLocation, tag: true };

function withKey(e: React.ReactElement<any>, index: number) {
  return React.cloneElement(e, { key: index });
}

function findPath(target: ToolbarClient.ToolbarResponse<any>, list: ToolbarClient.ToolbarResponse<any>[]): ToolbarClient.ToolbarResponse<any>[] | null {

  const last = list.last();

  if (last.elements) {
    for (let i = 0; i < last.elements.length; i++) {
      const elem = last.elements[i];

      list.push(elem);

      if (elem == target)
        return list;

      var result = findPath(target, list);

      if (result)
        return result;

      list.pop();
    }
  }

  return null;
}


