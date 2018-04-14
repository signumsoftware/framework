import * as React from 'react'
import { Route } from 'react-router'
import { classes } from '../../../Framework/Signum.React/Scripts/Globals'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityData, EntityKind, isTypeEnum, PropertyRoute } from '../../../Framework/Signum.React/Scripts/Reflection'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import { StyleContext } from '../../../Framework/Signum.React/Scripts/TypeContext'

import { Typeahead } from '../../../Framework/Signum.React/Scripts/Components'
import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import * as TypeHelpClient from './TypeHelpClient'
import ContextMenu from '../../../Framework/Signum.React/Scripts/SearchControl/ContextMenu'
import { ContextMenuPosition } from '../../../Framework/Signum.React/Scripts/SearchControl/ContextMenu'

import "./TypeHelpComponent.css"

interface TypeHelpComponentProps {
    initialType?: string;
    mode: TypeHelpClient.TypeHelpMode;
    onMemberClick?: (pr: PropertyRoute) => void;
    renderContextMenu?: (pr: PropertyRoute) => React.ReactElement<any>;
}

interface TypeHelpComponentState {
    history: string[];
    historyIndex: number;
    help?: TypeHelpClient.TypeHelp | false;
    tempQuery?: string;

    selected?: PropertyRoute;
    contextualMenu?: {
        position: ContextMenuPosition;
    };
}

export default class TypeHelpComponent extends React.Component<TypeHelpComponentProps, TypeHelpComponentState> {

    constructor(props: TypeHelpComponentProps) {
        super(props);
        this.state = { history: [], historyIndex: -1 };
    }

    static getExpression(initial: string, pr: PropertyRoute | string, mode: TypeHelpClient.TypeHelpMode, options?: { stronglyTypedMixinTS?: boolean }): string {

        if (pr instanceof PropertyRoute)
            pr = pr.propertyPath();

        return pr.split(".").reduce((prev, curr) => {
            if (curr.startsWith("[") && curr.endsWith("]")) {
                const mixin = curr.trimStart("[").trimEnd("]");
                return mode == "CSharp" ?
                    `${prev}.Mixin<${mixin}>()` :
                    options && options.stronglyTypedMixinTS ?
                        `getMixin(${prev}, ${mixin})` :
                        `${prev}.mixins["${mixin}"]`;
            }
            else
                return mode == "TypeScript" ?
                    `${prev}.${curr.firstLower()}` :
                    `${prev}.${curr}`;
        }, initial);
    }

    componentWillMount() {
        if (this.props.initialType)
            this.goTo(this.props.initialType);
    }

    componentWillReceiveProps(newProps: TypeHelpComponentProps) {
        if (newProps.initialType != this.props.initialType && newProps.initialType)
            this.goTo(newProps.initialType);
    }


    goTo(type: string) {
        var s = this.state;
        while (s.history.length - 1 > s.historyIndex)
            s.history.removeAt(s.history.length - 1);
        s.history.push(type);

        this.setState({
            historyIndex: s.historyIndex + 1
        });
        this.loadMembers(type);
    }

    loadMembers(typeName: string) {
        this.setState({ help: undefined });
        TypeHelpClient.API.typeHelp(typeName, this.props.mode)
            .then(th => {
                if (!th)
                    this.setState({ help: false });
                else if (th.cleanTypeName == this.currentType())
                    this.setState({ help: th });
            })
            .done();
    }

    handleGoHistory = (e: React.MouseEvent<any>, newIndex: number) => {
        e.preventDefault();
        this.setState({ historyIndex: newIndex });
        this.loadMembers(this.state.history[newIndex]);
    }



    render() {
        return (
            <div className="sf-type-help" ref={(th) => this.typeHelpContainer = th!}>
                {this.renderHeader()}
                {this.state.help == undefined ? <h4>Loading {this.currentType()}…</h4> :
                    this.state.help == false ? <h4>Not found {this.currentType()}</h4> :
                        this.renderHelp(this.state.help)}
                {this.state.contextualMenu && this.renderContextualMenu()}
            </div>
        );
    }

    renderContextualMenu() {
        let menu = this.props.renderContextMenu!(this.state.selected!);
        return (menu && <ContextMenu position={this.state.contextualMenu!.position} onHide={this.handleContextOnHide}>
            {menu.props.children}
        </ContextMenu>)
    }


    handleContextOnHide = () => {
        this.setState({
            selected: undefined,
            contextualMenu: undefined
        });
    }

    handleGetItems = (query: string) => {
        return TypeHelpClient.API.autocompleteEntityCleanType({
            query: query,
            limit: 5,
        });
    }

    handleSelect = (item: string) => {
        this.setState({ tempQuery: undefined });
        this.goTo(item);
        return item;
    }

    currentType(): string {
        return this.state.historyIndex == -1 ? "" : this.state.history[this.state.historyIndex];
    }

    canBack() {
        return this.state.historyIndex > 0;
    }

    canForth() {
        return this.state.historyIndex < this.state.history.length - 1;
    }

    renderHeader() {
        return (
            <div className="sf-type-help-bar">
                <div className="input-group input-group-sm">
                    <span className="input-group-prepend">
                        <button className="btn input-group-text" disabled={!this.canBack()}
                            onClick={e => this.handleGoHistory(e, this.state.historyIndex - 1)} type="button">
                            <span className="fa fa-arrow-circle-left" />
                        </button>
                        <button className="btn input-group-text" disabled={!this.canForth()}
                            onClick={e => this.handleGoHistory(e, this.state.historyIndex + 1)} type="button">
                            <span className="fa fa-arrow-circle-right" />
                        </button>
                    </span>
                    <Typeahead
                        inputAttrs={{ className: "form-control form-control-sm sf-entity-autocomplete" }}
                        getItems={this.handleGetItems}
                        value={this.state.tempQuery == undefined ? this.currentType() : this.state.tempQuery}
                        onBlur={() => this.setState({ tempQuery: undefined })}
                        onChange={newValue => this.setState({ tempQuery: newValue })}
                        onSelect={this.handleSelect} />
                    <span className="input-group-append">
                        <div className="input-group-text" style={{ color: "white", backgroundColor: this.props.mode == "CSharp" ? "#007e01" : "#017acc" }}>
                            {this.props.mode == "CSharp" ? "C#" : this.props.mode}
                        </div>
                    </span>
                </div>

            </div>
        );
    }

    typeHelpContainer!: HTMLElement;

    renderHelp(h: TypeHelpClient.TypeHelp) {
        return (
            <div>
                <h4 className="mb-1 mt-2">{h.type}</h4>

                <ul className="sf-members" style={{ paddingLeft: "0px" }}>
                    {h.members.map((m, i) => this.renderMember(h, m, i))}
                </ul>
            </div>
        );
    }

    handleOnMemberClick = (m: TypeHelpClient.TypeMemberHelp) => {
        if (this.props.onMemberClick && m.propertyString) {
            var pr = PropertyRoute.parse((this.state.help as TypeHelpClient.TypeHelp).cleanTypeName, m.propertyString);
            this.props.onMemberClick(pr);
        }
    }

    handleOnContextMenuClick = (m: TypeHelpClient.TypeMemberHelp, e: React.MouseEvent<any>) => {

        if (!m.propertyString)
            return;

        e.preventDefault();
        e.stopPropagation();
        var pr = PropertyRoute.parse((this.state.help as TypeHelpClient.TypeHelp).cleanTypeName, m.propertyString);

        this.setState({
            selected: pr,
            contextualMenu: {
                position: ContextMenu.getPosition(e, this.typeHelpContainer)
            }
        });
    }

    renderMember(h: TypeHelpClient.TypeHelp, m: TypeHelpClient.TypeMemberHelp, index: number): React.ReactChild {

        var className = "sf-member-name";
        var onClick: React.MouseEventHandler<any> | undefined;
        if (this.props.onMemberClick) {
            className = classes(className, "sf-member-click");
            onClick = () => this.handleOnMemberClick(m);
        }

        var onContextMenu: React.MouseEventHandler<any> | undefined;
        if (this.props.renderContextMenu) {
            onContextMenu = (e) => this.handleOnContextMenuClick(m, e);
        }

        return (
            <li key={index}>
                {h.isEnum ?
                    <span className={className} onClick={onClick} onContextMenu={onContextMenu}>{m.name}</span>
                    :
                    <div>
                        {this.props.mode == "CSharp" ?
                            <span>
                                {this.renderType(m.type, m.cleanTypeName)}{" "}<span className={className} onClick={onClick} onContextMenu={onContextMenu}>{m.name}{m.name && (m.isExpression ? "()" : "")}</span>
                            </span> :
                            <span>
                                <span className={className} onClick={onClick} onContextMenu={onContextMenu}>{m.name ? m.name + ": " : ""}</span>{this.renderType(m.type, m.cleanTypeName)}
                            </span>}

                        {m.subMembers.length > 0 &&
                            <ul className="sf-members">
                                {m.subMembers.map((sm, i) => this.renderMember(h, sm, i))}
                            </ul>}
                    </div>}
            </li>
        );
    }

    renderType(type: string, cleanType?: string | null): React.ReactChild {

        var startIndex = type.indexOf("<");
        var endIndex = type.lastIndexOf(">");

        if (startIndex != -1) {
            return (
                <span>
                    {this.renderType(type.substr(0, startIndex))}
                    {"<"}
                    {this.renderType(type.substr(startIndex + 1, endIndex - startIndex - 1), cleanType)}
                    {">"}
                </span>
            );
        }

        if (type.endsWith("?")) {
            return (
                <span>
                    {this.renderType(type.substr(0, type.length - 1), cleanType)}
                    {this.props.mode == "TypeScript" ? " | " : "?"}
                    {this.props.mode == "TypeScript" && <span className="sf-member-primitive">null</span>}
                </span>
            );
        }

        if (cleanType != null)
            return (
                <span>
                    <a href="#" className={"sf-member-" + (isTypeEnum(type) ? "enum" : "class")}
                        onClick={(e) => { e.preventDefault(); this.goTo(cleanType); }}>
                        {type}
                    </a>
                </span>
            );

        var kind = type.firstLower() == type ? "primitive" :
            type == "DateTime" ? "date" :
                type == "Lite" ? "lite" :
                    type == "IEnumerable" || type == "IQueryable" || type == "List" || type == "MList" ? "collection" :
                        isTypeEnum(type) ? "enum" : "others";

        return <span className={"sf-member-" + kind} title={kind}>{type}</span>;
    }
}
