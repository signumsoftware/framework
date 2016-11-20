import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { EntityData, EntityKind, isTypeEnum } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { EntityOperationSettings } from '../../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import * as EntityOperations from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { Entity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { StyleContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'

import Typeahead from '../../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { DynamicTypeEntity, DynamicTypeOperation, DynamicPanelPermission, DynamicSqlMigrationEntity } from '../Signum.Entities.Dynamic'
import * as DynamicClient from '../DynamicClient'

require("!style!css!./TypeHelpComponent.css");

interface TypeHelpComponentProps {
    initialType?: string;
    mode: DynamicClient.TypeHelpMode;
}

interface TypeHelpComponentState {
    history: string[];
    historyIndex: number;
    help?: DynamicClient.TypeHelp;
    tempQuery?: string;
}

export default class TypeHelpComponent extends React.Component<TypeHelpComponentProps, TypeHelpComponentState> {

    constructor(props: TypeHelpComponentProps) {
        super(props);
        this.state = { history: [], historyIndex: -1 };
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
        this.changeState(s => {
            while (s.history.length - 1 > s.historyIndex)
                s.history.removeAt(s.history.length - 1);
            s.history.push(type);
            s.historyIndex++;
        });
        this.loadMembers(type);
    }

    loadMembers(typeName: string) {
        this.changeState(s => s.help = undefined);
        DynamicClient.API.typeHelp(typeName, this.props.mode)
            .then(th => {

                if (th.cleanTypeName == this.currentType())
                    this.changeState(s => s.help = th);
            })
            .done();
    }

    handleGoHistory = (e: React.MouseEvent, newIndex: number) => {
        e.preventDefault();
        this.changeState(s => s.historyIndex = newIndex);
        this.loadMembers(this.state.history[newIndex]);
    }



    render() {
        return (
            <div className="sf-dynamic-type-help">
                {this.renderHeader()}
                {this.state.help == undefined ? <h4>Loading {this.currentType()}…</h4> : this.renderHelp(this.state.help)}
            </div>
        );
    }

    input: HTMLInputElement;


    handleGetItems = (query: string) => {
        return DynamicClient.API.autocompleteEntityCleanType({
            query: query,
            limit: 5,
        });
    }

    handleSelect = (item: string) => {
        this.changeState(s => s.tempQuery = undefined);
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
            <div className="form-sm sf-dynamic-type-help-bar">
                <div className="input-group">
                    <span className="input-group-btn">
                        <button className="btn btn-default" disabled={!this.canBack()}
                            onClick={e => this.handleGoHistory(e, this.state.historyIndex - 1)} type="button">
                            <span className="glyphicon glyphicon-circle-arrow-left" />
                        </button>
                        <button className="btn btn-default" disabled={!this.canForth()}
                            onClick={e => this.handleGoHistory(e, this.state.historyIndex + 1)} type="button">
                            <span className="glyphicon glyphicon-circle-arrow-right" />
                        </button>
                    </span>
                    <div style={{ position: "relative" }}>
                        <Typeahead
                            inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
                            getItems={this.handleGetItems}
                            value={this.state.tempQuery == undefined ? this.currentType() : this.state.tempQuery}
                            onBlur={() => this.changeState(s => s.tempQuery = undefined)}
                            onChange={newValue => this.changeState(s => s.tempQuery = newValue)}
                            onSelect={this.handleSelect} />
                    </div>
                </div>
            </div>
        );
    }

    renderHelp(h: DynamicClient.TypeHelp) {
        return (
            <div>
                <h4>{h.type}</h4>

                <ul className="sf-dynamic-members" style={{ paddingLeft: "0px" }}>
                    {h.members.map((m, i) => this.renderMember(m, i))}
                </ul>
            </div>
        );
    }

    renderMember(m: DynamicClient.TypeMemberHelp, index: number): React.ReactChild {

        return (
            <li>
                {this.props.mode == "CSharp" ?
                    <span>{this.renderType(m.type, m.cleanTypeName)}{" "}<span className="sf-dynamic-member-name">{m.name}{m.name && (m.isExpression ? "()" : "")}</span></span> :
                    <span><span className="sf-dynamic-member-name">{m.name}</span>{": "}{this.renderType(m.type, m.cleanTypeName)}</span>}

                {m.subMembers.length > 0 && <ul className="sf-dynamic-members">
                    {m.subMembers.map((sm, i) => this.renderMember(sm, i))}
                </ul>}
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
                    {this.props.mode == "Typescript"? " | " : "?"}
                    {this.props.mode == "Typescript" && <span className="sf-dynamic-member-primitive">null</span> }
                </span>
            );
        }

        if (cleanType != null)
            return (
                <a href="" className="sf-dynamic-member-class"
                    onClick={(e) => { e.preventDefault(); this.goTo(cleanType); } }>
                    {type}
                </a>
            );

        var kind = type.firstLower() == type ? "primitive" :
            type == "DateTime" ? "date" :
                type == "Lite" ? "lite" :
                    type == "IEnumerable" || type == "IQueryable" || type == "List" || type == "MList" ? "collection" :
                        isTypeEnum(type) ? "enum" : "others";

        return <span className={"sf-dynamic-member-" + kind} title={kind}>{type}</span>;
    }

}
