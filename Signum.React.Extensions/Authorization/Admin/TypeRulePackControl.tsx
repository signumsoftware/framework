import * as React from 'react'
import { Button } from 'react-bootstrap'
import { Link } from 'react-router'
import * as numbro from 'numbro'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { notifySuccess }from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { TypeContext, ButtonsContext, IRenderButtons } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityLine, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'

import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfo, Binding, GraphExplorer } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { Api, properties, queries, operations } from '../AuthClient'
import { TypeRulePack, AuthAdminMessage, PermissionSymbol, AuthMessage, TypeAllowed, TypeAllowedRule, TypeAllowedAndConditions, TypeAllowedBasic, TypeConditionRule, AuthThumbnail } from '../Signum.Entities.Authorization'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import { TypeConditionSymbol } from '../../Basics/Signum.Entities.Basics'
import { OperationSymbol, ModelEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { QueryEntity, PropertyRouteEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


require("./AuthAdmin.css");

export default class TypesRulesPackControl extends React.Component<{ ctx: TypeContext<TypeRulePack> }, void> implements IRenderButtons {

    handleSaveClick = (bc: ButtonsContext) => {
        let pack = this.props.ctx.value;

        Api.saveTypeRulePack(pack)
            .then(() => Api.fetchTypeRulePack(pack.role.id!))
            .then(newPack => {
                notifySuccess();
                bc.frame.onReload({ entity: newPack, canExecute: {} });
            })
            .done();
    }

    renderButtons(bc: ButtonsContext) {
        return [
            <Button bsStyle="primary" onClick={() => this.handleSaveClick(bc) }>{AuthMessage.Save.niceToString() }</Button>
        ];
    }


    render() {

        let ctx = this.props.ctx;

        return (
            <div>
                <div className="form-compact">
                    <EntityLine ctx={ctx.subCtx(f => f.role) }  />
                    <ValueLine ctx={ctx.subCtx(f => f.strategy) }  />
                </div>
                <table className="table table-condensed sf-auth-rules">
                    <thead>
                        <tr>
                            <th>
                                <b>{ TypeEntity.niceName() }</b>
                            </th>

                            <th style={{ textAlign: "center" }}>
                                {TypeAllowed.niceName("Create") }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {TypeAllowed.niceName("Modify") }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {TypeAllowed.niceName("Read") }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {TypeAllowed.niceName("None") }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {AuthAdminMessage.Overriden.niceToString() }
                            </th>
                            {properties && <th style={{ textAlign: "center" }}>
                                {PropertyRouteEntity.niceName() }
                            </th>}
                            {operations && <th style={{ textAlign: "center" }}>
                                {OperationSymbol.niceName() }
                            </th>}
                            {queries && <th style={{ textAlign: "center" }}>
                                {QueryEntity.niceName() }
                            </th>}
                        </tr>
                    </thead>
                    <tbody>
                        { ctx.mlistItemCtxs(a => a.rules).groupBy(a => a.value.resource.fullClassName!.tryBeforeLast(".") || "").orderBy(a => a.key).flatMap(gr => [
                            <tr key={gr.key} className="sf-auth-namespace">
                                <td colSpan={10}><b>{gr.key}</b></td>
                            </tr>
                        ].concat(gr.elements.orderBy(a => a.value.resource.fullClassName).flatMap(c => this.renderType(c)))) }
                    </tbody>
                </table>

            </div>
        );
    }

    handleAddConditionClick = (remainig: TypeConditionSymbol[], taac: TypeAllowedAndConditions) => {
        SelectorModal.chooseElement(remainig, { display: a => a.toStr.tryAfter(".") || a.toStr })
            .then(tc => {
                if (!tc)
                    return;

                taac.conditions.push(TypeConditionRule.New(tcr => {
                    tcr.typeCondition = tc!;
                    tcr.allowed = "None";
                }));

                this.forceUpdate();
            })
            .done();
    }

    handleRemoveConditionClick = (taac: TypeAllowedAndConditions, con: TypeConditionRule) => {
        taac.conditions!.remove(con);
        this.forceUpdate();
    }

    renderType(ctx: TypeContext<TypeAllowedRule>) {

        let roleId = this.props.ctx.value.role.id!;

        let used = ctx.value.allowed.conditions.map(tcs => tcs.typeCondition.id!);

        let remaining = ctx.value.availableConditions.filter(tcs => !used.contains(tcs.id!));

        let fallback = Binding.create(ctx.value.allowed, a => a.fallback);
        return [
            <tr key={ctx.value.resource.fullClassName!} className={ classes("sf-auth-type", ctx.value.allowed.conditions.length > 0 && "sf-auth-with-conditions") }>
                <td>
                    { remaining.length > 0 ? <a className="fa fa-plus-circle sf-condition-icon" aria-hidden="true" onClick={() => this.handleAddConditionClick(remaining, ctx.value.allowed) }></a> :
                        <i className="fa fa-circle sf-placeholder-icon" aria-hidden="true"></i> }
                    &nbsp;
                    { getTypeInfo(ctx.value.resource.cleanName).niceName }
                </td>
                <td style={{ textAlign: "center" }}>
                    {this.colorRadio(fallback, "Create", "#0099FF") }
                </td>
                <td style={{ textAlign: "center" }}>
                    {this.colorRadio(fallback, "Modify", "green") }
                </td>
                <td style={{ textAlign: "center" }}>
                    {this.colorRadio(fallback, "Read", "#FFAD00") }
                </td>
                <td style={{ textAlign: "center" }}>
                    {this.colorRadio(fallback, "None", "red") }
                </td>
                <td style={{ textAlign: "center" }}>
                    <GrayCheckbox checked={!typeAllowedEquals(ctx.value.allowed, ctx.value.allowedBase) }/>
                </td>
                {properties && <td style={{ textAlign: "center" }}>
                    {this.link("fa fa-pencil-square-o", ctx.value.properties, () => Api.fetchPropertyRulePack(ctx.value.resource.cleanName, roleId)) }
                </td>}
                {operations && <td style={{ textAlign: "center" }}>
                    {this.link("fa fa-bolt", ctx.value.operations, () => Api.fetchOperationRulePack(ctx.value.resource.cleanName, roleId)) }
                </td>}
                {queries && <td style={{ textAlign: "center" }}>
                    {this.link("fa fa-search", ctx.value.queries, () => Api.fetchQueryRulePack(ctx.value.resource.cleanName, roleId)) }
                </td>}
            </tr>
        ].concat(ctx.value.allowed!.conditions!.map(c => {
            let b = Binding.create(c, ca => ca.allowed);
            return (
                <tr key={ctx.value.resource.fullClassName + "_" + c.typeCondition.id} className="sf-auth-condition">
                    <td>
                        &nbsp; &nbsp;
                        <a className="fa fa-minus-circle sf-condition-icon" aria-hidden="true" onClick={() => this.handleRemoveConditionClick(ctx.value.allowed, c) }></a>
                        &nbsp;
                        <small>{ c.typeCondition.toStr.tryAfter(".") || c.typeCondition.toStr }</small>
                    </td>
                    <td style={{ textAlign: "center" }}>
                        {this.colorRadio(b, "Create", "#0099FF") }
                    </td>
                    <td style={{ textAlign: "center" }}>
                        {this.colorRadio(b, "Modify", "green") }
                    </td>
                    <td style={{ textAlign: "center" }}>
                        {this.colorRadio(b, "Read", "#FFAD00") }
                    </td>
                    <td style={{ textAlign: "center" }}>
                        {this.colorRadio(b, "None", "red") }
                    </td>
                    <td style={{ textAlign: "center" }}>
                    </td>
                </tr>
            );
        }));

    }

    colorRadio(b: Binding<TypeAllowed | null>, part: TypeAllowedBasic, color: string) {
        return <ColorRadio
            checked={isActive(b.getValue(), part)}
            color={color}
            onClicked={e => { b.setValue(select(b.getValue(), part, e)); this.forceUpdate(); } }/>;
    }

    link(icon: string, allowed: AuthThumbnail | undefined | null, action: () => Promise<ModelEntity>) {

        if (!allowed)
            return undefined;

        let onClick = () => {
            GraphExplorer.propagateAll(this.props.ctx.value);

            if (this.props.ctx.value.modified) {
                alert(AuthAdminMessage.PleaseSaveChangesFirst.niceToString());
                return;
            }

            action()
                .then(m => Navigator.navigate(m))
                .done();
        };

        return (
            <a onClick={onClick}
                className={classes("sf-auth-link", icon) }
                style={{
                    color: allowed == "All" ? "green" :
                        allowed == "Mix" ? "#FFAD00" : "red"
                }}>
            </a>
        );
    }
}

function typeAllowedEquals(allowed: TypeAllowedAndConditions, allowedBase: TypeAllowedAndConditions) {
    return allowed.fallback == allowedBase.fallback
        && allowed.conditions!.length == allowedBase.conditions!.length
        && allowed.conditions!
            .every((c, i) => {
                let b = allowedBase.conditions![i];
                return c.allowed == b.allowed && c.typeCondition!.id == b.typeCondition!.id;
            });
}

function getDB(allowed: TypeAllowed): TypeAllowedBasic {
    if (allowed.contains("DB"))
        return allowed.after("DB").before("UI") as TypeAllowedBasic;

    return allowed as TypeAllowedBasic;
}

function getUI(allowed: TypeAllowed): TypeAllowedBasic {
    if (allowed.contains("UI"))
        return allowed.after("UI") as TypeAllowedBasic;

    return allowed as TypeAllowedBasic;
}

let values: TypeAllowedBasic[] = ["Create", "Modify", "Read", "None"];

function combine(val1: TypeAllowedBasic, val2: TypeAllowedBasic): TypeAllowed {

    let db: TypeAllowedBasic;
    let ui: TypeAllowedBasic;
    if (values.indexOf(val1) < values.indexOf(val2)) {
        db = val1;
        ui = val2;
    } else {
        db = val2;
        ui = val1;
    }

    return "DB" + db + "UI" + ui as TypeAllowed;
}

function isActive(allowed: TypeAllowed | null, basicAllowed: TypeAllowedBasic) {
    if (!allowed)
        return false;

    return getDB(allowed) == basicAllowed || getUI(allowed) == basicAllowed;
}


function select(current: TypeAllowed | null, basicAllowed: TypeAllowedBasic, e: React.MouseEvent) {
    if (!e.shiftKey || current == null)
        return basicAllowed as TypeAllowedBasic;

    let db = getDB(current);
    let ui = getUI(current);

    if (db != ui) {
        if (basicAllowed == ui)
            return db;

        if (basicAllowed == db)
            return ui;
    } else {
        if (basicAllowed != db)
            return combine(db, basicAllowed);
    }

    return current;
}