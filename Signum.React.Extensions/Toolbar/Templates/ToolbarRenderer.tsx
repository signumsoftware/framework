import * as React from 'react'
import { classes } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { ToolbarLocation } from '../Signum.Entities.Toolbar'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarClient";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import * as PropTypes from "prop-types";
import { Dropdown, DropdownToggle, DropdownMenu, DropdownItem, NavItem } from '@framework/Components';
import { NavLink } from '@framework/Components/NavItem';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { parseIcon } from '../../Dashboard/Admin/Dashboard';

export interface ToolbarRendererProps {
  location?: ToolbarLocation;
  tag?: boolean;
}

export interface ToolbarRendererState {
  response?: ToolbarClient.ToolbarResponse<any>;
  expanded: ToolbarClient.ToolbarResponse<any>[];
  avoidCollapse: ToolbarClient.ToolbarResponse<any>[];
}

export default class ToolbarRenderer extends React.Component<ToolbarRendererProps, ToolbarRendererState>
{
  static defaultProps = { location: "Top" as ToolbarLocation, tag: true };

  constructor(props: {}) {
    super(props);
    this.state = {
      expanded: [],
      avoidCollapse: [],
    };
  }

  isAlive = true;
  componentWillUnmount() {
    this.isAlive = false;
  }

  componentWillMount() {
    ToolbarClient.API.getCurrentToolbar(this.props.location!)
      .then(res => this.isAlive && this.setState({ response: res }))
      .done();
  }

  render() {

    const r = this.state.response;

    if (!r)
      return null;

    if (this.props.location == "Top") {

      var navItems = r.elements && r.elements.map((res, i) => withKey(this.renderNavItem(res, i), i));

      if (!this.props.tag)
        return navItems;

      return (
        <div className={classes("nav navbar-nav", this.props.tag)}>
          {navItems}
        </div>
      );
    }
    else
      return (
        <DropdownItemContainer tag={this.props.tag}>
          {r.elements && r.elements.flatMap(sr => this.renderDropdownItem(sr, 0, r)).map((sr, i) => withKey(sr, i))}
        </DropdownItemContainer>
      );
  }

  handleOnToggle = (res: ToolbarClient.ToolbarResponse<any>) => {

    if (this.state.avoidCollapse.contains(res)) {
      this.state.avoidCollapse.remove(res);
      return;
    }

    if (!this.state.expanded.contains(res))
      this.state.expanded.push(res);
    else
      this.state.expanded.clear();

    this.forceUpdate();
  }

  renderNavItem(res: ToolbarClient.ToolbarResponse<any>, index: number) {

    switch (res.type) {
     
      case "Divider":
        return (
          <NavItem>{"|"}</NavItem>
        );

      case "Header":
      case "Item":
        if (res.elements && res.elements.length) {
          var title = res.label || res.content!.toStr;
          var icon = this.icon(res);
          return (
            <Dropdown
              toggle={() => this.handleOnToggle(res)}
              isOpen={this.state.expanded.contains(res)}>
              <DropdownToggle nav caret>
                {!icon ? title : (<span>{icon}{title}</span>)}
              </DropdownToggle>
              <DropdownMenu>
                {res.elements && res.elements.flatMap(sr => this.renderDropdownItem(sr, 1, res)).map((sr, i) => withKey(sr, i))}
              </DropdownMenu>
            </Dropdown>
          );
        }
        
        if (res.url) {
          return (
            <NavItem>
              <NavLink onClick={e => Navigator.pushOrOpenInTab(res.url!, e)}>
                {ToolbarConfig.coloredIcon(res.iconName ? parseIcon(res.iconName) : undefined, res.iconColor)}{res.label}
              </NavLink>
            </NavItem>
          );
        }

        if (res.content) {
          var config = ToolbarClient.configs[res.content!.EntityType];
          if (!config)
            return <NavItem style={{ color: "red" }}>{res.content!.EntityType + "ToolbarConfig not registered"}</NavItem>;

          return (
            <NavItem>
              <NavLink onClick={e => config.handleNavigateClick(e, res)}>
                {config.getIcon(res)}{config.getLabel(res)}
              </NavLink>
            </NavItem>
          );
        }

        if (res.type == "Header") {
          return (
            <NavItem>{this.icon(res)}{res.label}</NavItem>
          );
        }
      
        return <NavItem style={{ color: "red" }}>{"No Content or Url found"}</NavItem>;

      default:
        throw new Error("Unexpected " + res.type);
    }
  }



  handleClick = (e: React.MouseEvent<any>, res: ToolbarClient.ToolbarResponse<any>, topRes: ToolbarClient.ToolbarResponse<any>) => {

    this.state.avoidCollapse.push(topRes);

    var path = findPath(res, [topRes]);

    if (!path)
      throw new Error("Path not found");

    if (this.state.expanded.contains(res))
      path.pop();

    this.state.expanded = path;

    this.forceUpdate();
  }

  renderDropdownItem(res: ToolbarClient.ToolbarResponse<any>, indent: number, topRes: ToolbarClient.ToolbarResponse<any>): React.ReactElement<any>[] {
    
    const menuItemN = "menu-item-" + indent;

    switch (res.type) {

      case "Divider":
        return [
          <DropdownItem divider className={menuItemN} />
        ];

      case "Header":
      case "Item":

        var isHeader = res.type == "Header";

        if (res.elements && res.elements.length) {
          return [
            <DropdownItem header={isHeader} onClick={e => this.handleClick(e, res, topRes)}
              className={classes(menuItemN, this.state.expanded.contains(res) && "active")}>
              {this.icon(res)}{res.label || res.content!.toStr}<FontAwesomeIcon icon={this.state.expanded.contains(res) ? "chevron-down" : "chevron-left"} className="arrow-align" />
            </DropdownItem>
          ].concat(res.elements && res.elements.length && this.state.expanded.contains(res) ? res.elements.flatMap(r => this.renderDropdownItem(r, indent + 1, topRes)) : [])
        }

        if (res.url) {
          return [
            <DropdownItem header={isHeader} onClick={e => Navigator.pushOrOpenInTab(res.url!, e)} className={menuItemN}>
              {ToolbarConfig.coloredIcon(res.iconName ? parseIcon(res.iconName) : undefined, res.iconColor)}{res.label}
            </DropdownItem>
          ];
        }

        if (res.content) {
          var config = ToolbarClient.configs[res.content!.EntityType];
          if (!config) {
            return [
              <DropdownItem header={isHeader} style={{ color: "red" }} className={menuItemN}>
                {res.content!.EntityType + "ToolbarConfig not registered"}
              </DropdownItem>
            ];
          }

          return [
            <DropdownItem header={isHeader} onClick={e => config.handleNavigateClick(e, res)} className={menuItemN}>
              {config.getIcon(res)}{config.getLabel(res)}
            </DropdownItem>
          ];
        }

        if (res.type == "Header")
          return [
            <DropdownItem header className={menuItemN}>{this.icon(res)}{res.label}</DropdownItem>
          ];
        
        return [<DropdownItem style={{ color: "red" }} className={menuItemN}>{"No Content or Url found"}</DropdownItem>];
      default: throw new Error("Unexpected " + res.type);
    }
  }

  icon(res: ToolbarClient.ToolbarResponse<any>) {

    var icon = parseIcon(res.iconName);

    return icon && <FontAwesomeIcon icon={icon} className={"icon"} color={res.iconColor} />
  }
}

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


export class DropdownItemContainer extends React.Component<{ tag?: boolean }> {

  handleToggle = () => {

  }

  getChildContext() {
    return { toggle: this.handleToggle };
  }

  static childContextTypes = { "toggle": PropTypes.func };

  render() {

    if (!this.props.tag)
      return this.props.children;

    return (
      <div className="nav">
        {this.props.children}
      </div>
    );
  }
}


