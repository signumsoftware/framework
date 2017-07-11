import * as React from 'react'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { TypeContext, StyleOptions, EntityFrame } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import { EntityPack, Entity, Lite, JavascriptMessage, entityInfo, getToString } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from '../../../../Framework/Signum.React/Scripts/Frames/Widgets'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import ButtonBar from '../../../../Framework/Signum.React/Scripts/Frames/ButtonBar'
import { ToolbarElementEmbedded, ToolbarElementType, ToolbarMenuEntity, ToolbarLocation } from '../Signum.Entities.Toolbar'
import * as ToolbarClient from '../ToolbarClient'
import { Navbar, Nav, NavItem, NavDropdown, MenuItem } from 'react-bootstrap'
import { LinkContainer, IndexLinkContainer } from 'react-router-bootstrap'
import { ToolbarConfig } from "../ToolbarClient";
import '../../../../Framework/Signum.React/Scripts/Frames/MenuIcons.css'


export interface ToolbarRendererState {
    response?: ToolbarClient.ToolbarResponse<any>;
    expanded: ToolbarClient.ToolbarResponse<any>[];
    avoidCollapse: ToolbarClient.ToolbarResponse<any>[];
    isRtl: boolean;
}

export default class ToolbarRenderer extends React.Component<{ location?: ToolbarLocation;}, ToolbarRendererState>
{
    static defaultProps = { location: "Top" as ToolbarLocation };

    constructor(props: {}) {
        super(props);
        this.state = {
            expanded: [],
            avoidCollapse: [],
            isRtl: document.body.classList.contains("rtl-mode")
        };
    }

    componentWillMount() {
        ToolbarClient.API.getCurrentToolbar(this.props.location!)
            .then(res => this.setState({ response: res }))
            .done();
    }

    render() {

        const r = this.state.response;

        if (!r)
            return null;

        if (this.props.location == "Top")
            return (
                <ul className="nav navbar-nav">
                    {r.elements && r.elements.map((res, i) => withKey(this.renderNavItem(res, i), i))}
                </ul>
            );
        else
            return (
                <ul className="nav">
                    {r.elements && r.elements.flatMap(sr => this.renderMenuItem(sr, 0, r)).map((sr, i) => withKey(sr, i))}
                </ul>
            );
    }

    handleOnToggle = (isOpen: boolean, res: ToolbarClient.ToolbarResponse<any>)  => {

        if (this.state.avoidCollapse.contains(res)) {
            this.state.avoidCollapse.remove(res);
            return;
        }

        if (isOpen)
            this.state.expanded.push(res);
        else
            this.state.expanded.remove(res);

        this.forceUpdate();
    }

    renderNavItem(res: ToolbarClient.ToolbarResponse<any>, index: number) {

        switch (res.type) {
            case "Menu":


                var title = res.label || res.content!.toStr;

                var icon = this.icon(res);

                return (
                    <NavDropdown title={!icon ? title : (<span>{icon}{title}</span>) as any}
                        id={"menu-" + index}
                        onToggle={isOpen => this.handleOnToggle(isOpen, res)}
                        open={this.state.expanded.contains(res)}>
                        {res.elements && res.elements.flatMap(sr => this.renderMenuItem(sr, 0, res)).map((sr, i) => withKey(sr, i))}
                    </NavDropdown>
                );
            case "Header":
                return (
                    <NavItem>{this.icon(res)}{res.label}</NavItem>
                );

            case "Divider":
                return (
                    <NavItem>{"|"}</NavItem>
                );

            case "Link":

                if (res.content == null) {
                    if (!res.url)
                        return <MenuItem style={{ color: "red" }}>{"No Content or Url found"}</MenuItem>;

                    return (
                        <NavItem onClick={e => Navigator.pushOrOpenInTab(res.url!, e)}>
                            {ToolbarConfig.coloredIcon(res.iconName, res.iconColor)}{res.label}
                        </NavItem>
                    );

                } else {

                    var config = ToolbarClient.configs[res.content!.EntityType];

                    if (!config)
                        return <MenuItem style={{ color: "red" }}>{res.content!.EntityType + "ToolbarConfig not registered"}</MenuItem>;

                    return (
                        <NavItem onClick={e => config.handleNavigateClick(e, res)}>
                            {config.getIcon(res)}{config.getLabel(res)}
                        </NavItem>
                    );
                }
            default: throw new Error("Unexpected " + res.type);
        }
    }

    handleClick = (e: React.MouseEvent<any>, res: ToolbarClient.ToolbarResponse<any>, topRes: ToolbarClient.ToolbarResponse<any>) => {

        this.state.avoidCollapse.push(topRes);

        if (this.state.expanded.contains(res))
            this.state.expanded.remove(res);
        else
            this.state.expanded.push(res);

        this.forceUpdate();
    }
    
    renderMenuItem(res: ToolbarClient.ToolbarResponse<any>, indent: number, topRes: ToolbarClient.ToolbarResponse<any>): React.ReactElement<any>[] {

        var padding  = (indent * 20) + "px";

        var style: React.CSSProperties = {
            paddingLeft: this.state.isRtl ? "" : padding,
            paddingRight: this.state.isRtl ? padding : ""
        };

        switch (res.type) {
            case "Menu":
                return [
                    <MenuItem onClick={e => this.handleClick(e, res, topRes)} style={style} className={this.state.expanded.contains(res) ? "active" : undefined}>
                        {this.icon(res)}{res.label || res.content!.toStr}<span className="fa arrow" />
                    </MenuItem>
                ].concat(res.elements && res.elements.length && this.state.expanded.contains(res) ? res.elements.flatMap(r => this.renderMenuItem(r, indent + 1, topRes)) : []);
            case "Header":
                return [
                    <MenuItem header style={style}>{this.icon(res)}{res.label}</MenuItem>
                ];

            case "Divider":
                return [
                    <MenuItem divider style={style}/>
                ];

            case "Link":

                if (res.content == null) {
                    if (!res.url)
                        return [<MenuItem style={{ color: "red" }}>{"No Content or Url found"}</MenuItem>];

                    return [
                        <NavItem onClick={e => Navigator.pushOrOpenInTab(res.url!, e)}>
                            {ToolbarConfig.coloredIcon(res.iconName, res.iconColor)}{res.label}
                        </NavItem>
                    ];

                } else {
                    var config = ToolbarClient.configs[res.content!.EntityType]

                    if (!config)
                        return [<MenuItem style={{ color: "red", ...style }}> {res.content!.EntityType + "ToolbarConfig not registered"}</MenuItem>];

                    return [
                        <MenuItem onClick={e => config.handleNavigateClick(e, res)} style={style}>
                            {config.getIcon(res)}{config.getLabel(res)}
                        </MenuItem>
                    ];
                }
            default: throw new Error("Unexpected " + res.type);
        }
    }

    icon(res: ToolbarClient.ToolbarResponse<any>) {

        if (res.iconName == null)
            return null;

        return <span className={"icon " + res.iconName} style={{ color: res.iconColor }} />
    }
}

function withKey(e: React.ReactElement<any>, index: number) {
    return React.cloneElement(e, { key: index });
}

