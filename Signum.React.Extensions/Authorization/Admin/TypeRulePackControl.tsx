import * as React from 'react'
import * as numbro from 'numbro'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { notifySuccess }from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { TypeContext, ButtonsContext, IRenderButtons, EntityFrame } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityLine, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import MessageModal from '../../../../Framework/Signum.React/Scripts/Modals/MessageModal'

import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfo, Binding, GraphExplorer } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage, OperationSymbol, ModelEntity, newMListElement, NormalControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, properties, queries, operations } from '../AuthClient'
import {
    TypeRulePack, AuthAdminMessage, PermissionSymbol, AuthMessage, TypeAllowed, TypeAllowedRule,
    TypeAllowedAndConditions, TypeAllowedBasic, TypeConditionRuleEmbedded, AuthThumbnail, PropertyRulePack, OperationRulePack, QueryRulePack, RoleEntity
} from '../Signum.Entities.Authorization'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import { TypeConditionSymbol } from '../../Basics/Signum.Entities.Basics'
import { QueryEntity, PropertyRouteEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


import "./AuthAdmin.css"
import { Button } from '../../../../Framework/Signum.React/Scripts/Components';
import { is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';

export default class TypesRulesPackControl extends React.Component<{ ctx: TypeContext<TypeRulePack> }, { filter: string }> implements IRenderButtons {

    state = { filter: "" };

    handleSaveClick = (bc: ButtonsContext) => {
        let pack = this.props.ctx.value;

        API.saveTypeRulePack(pack)
            .then(() => API.fetchTypeRulePack(pack.role.id!))
            .then(newPack => {
                notifySuccess();
                bc.frame.onReload({ entity: newPack, canExecute: {} });
            })
            .done();
    }

    handleResetChangesClick = (bc: ButtonsContext) => {
        let pack = this.props.ctx.value;

        API.fetchTypeRulePack(pack.role.id!)
            .then(newPack => {
                bc.frame.onReload({ entity: newPack, canExecute: {} });
            })
            .done();
    }

    handleSwitchToClick = (bc: ButtonsContext) => {
        let pack = this.props.ctx.value;

        Finder.find(RoleEntity).then(r => {
            if (!r)
                return;

            API.fetchTypeRulePack(r.id!)
                .then(newPack => bc.frame.onReload({ entity: newPack, canExecute: {} }))
                .done();
        });
    }

    updateFrame() {
        this.props.ctx.frame!.frameComponent.forceUpdate();
    }

    renderButtons(bc: ButtonsContext) {

        GraphExplorer.propagateAll(bc.pack.entity);
        
        const hasChanges = bc.pack.entity.modified;

        return [
            <Button color="primary" disabled={!hasChanges} onClick={() => this.handleSaveClick(bc)}>{AuthMessage.Save.niceToString()}</Button>,
            <Button color="warning" disabled={!hasChanges} onClick={() => this.handleResetChangesClick(bc)}>{AuthAdminMessage.ResetChanges.niceToString()}</Button>,
            <Button color="info" disabled={hasChanges} onClick={() => this.handleSwitchToClick(bc)}>{AuthAdminMessage.SwitchTo.niceToString()}</Button>
        ];
    }

    handleSetFilter = (e: React.FormEvent<any>) => {
        this.setState({
            filter: (e.currentTarget as HTMLInputElement).value
        });
    }

    render() {
        const parts = this.state.filter.match(/[+-]?((!?\w+)|\*)/g);

        const isMatch = (rule: TypeAllowedRule): boolean => {

            if (!parts || parts.length == 0)
                return true;

            const array = [
                rule.resource.namespace,
                rule.resource.cleanName,
                getTypeInfo(rule.resource.cleanName).niceName
            ];

  
            const str = array.join("|");

            for (let i = parts.length - 1; i >= 0; i--) {
                const p = parts[i];
                const pair = p.startsWith("+") ? { isPositive: true, token: p.after("+") } :
                    p.startsWith("-") ? { isPositive: false, token: p.after("-") } :
                        { isPositive: true, token: p };
                        
                if (pair.token == "*")
                    return pair.isPositive;

                if (pair.token.startsWith("!"))
                {
                    if ("overriden".startsWith(pair.token.after("!")) && !typeAllowedEquals(rule.allowed, rule.allowedBase))
                        return pair.isPositive;

                    if ("conditions".startsWith(pair.token.after("!")) && rule.allowed.conditions.length)
                        return pair.isPositive;
                }

                if (str.toLowerCase().contains(pair.token.toLowerCase()))
                    return pair.isPositive; 
            }

            return false;
        };

        let ctx = this.props.ctx;

        return (
            <div>
                <div className="form-compact">
                    <EntityLine ctx={ctx.subCtx(f => f.role) }  />
                    <ValueLine ctx={ctx.subCtx(f => f.strategy) }  />
                </div>
                
                <table className="table table-sm sf-auth-rules">
                    <thead>
                        <tr>
                            <th>
                                <div style={{ marginBottom: "-2px"}}>
                                    <input type="text" className="form-control-sm" id="filter" placeholder="Auth-!overriden+!conditions" value={this.state.filter} onChange={this.handleSetFilter} />
                                </div>
                            </th>

                            <th style={{ textAlign: "center" }}>
                                {TypeAllowed.niceToString("Create") }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {TypeAllowed.niceToString("Modify") }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {TypeAllowed.niceToString("Read") }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {TypeAllowed.niceToString("None") }
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
                        {ctx.mlistItemCtxs(a => a.rules)
                            .filter((n, i) => isMatch(n.value))
                            .groupBy(a => a.value.resource.namespace).orderBy(a => a.key).flatMap(gr => [
                            <tr key={gr.key} className="sf-auth-namespace">
                                <td colSpan={10}><b>{gr.key}</b></td>
                            </tr>
                        ].concat(gr.elements.orderBy(a => a.value.resource.className).flatMap(c => this.renderType(c))))}
                    </tbody>
                </table>

            </div>
        );
    }

    handleAddConditionClick = (remainig: TypeConditionSymbol[], taac: TypeAllowedAndConditions) => {
        SelectorModal.chooseElement(remainig, { buttonDisplay: a => a.toStr.tryAfter(".") || a.toStr })
            .then(tc => {
                if (!tc)
                    return;

                taac.conditions.push(newMListElement(TypeConditionRuleEmbedded.New({
                    typeCondition : tc!,
                    allowed : "None"
                })));

                this.updateFrame();
            })
            .done();
    }

    handleRemoveConditionClick = (taac: TypeAllowedAndConditions, con: TypeConditionRuleEmbedded) => {
        taac.conditions!.remove(taac.conditions.filter(mle => mle.element == con).single());
        taac.modified = true;
        this.updateFrame();
    }

    renderType(ctx: TypeContext<TypeAllowedRule>) {

        let roleId = this.props.ctx.value.role.id!;

        let used = ctx.value.allowed.conditions.map(mle => mle.element.typeCondition.id!);

        let remaining = ctx.value.availableConditions.filter(tcs => !used.contains(tcs.id!));

        var typeInfo = getTypeInfo(ctx.value.resource.cleanName);

        var masterClass = typeInfo.entityData == "Master" ? "sf-master" : undefined;

        let fallback = Binding.create(ctx.value.allowed, a => a.fallback);
        return [
            <tr key={ctx.value.resource.namespace + "." + ctx.value.resource.className} className={classes("sf-auth-type", ctx.value.allowed.conditions.length > 0 && "sf-auth-with-conditions")}>
                <td>
                    { remaining.length > 0 ? <a className="fa fa-plus-circle sf-condition-icon" aria-hidden="true" onClick={() => this.handleAddConditionClick(remaining, ctx.value.allowed) }></a> :
                        <i className="fa fa-circle sf-placeholder-icon" aria-hidden="true"></i> }
                    &nbsp;
                    {typeInfo.niceName} {typeInfo.entityData && <small title={typeInfo.entityData}>{typeInfo.entityData[0]}</small>}
                </td>
                <td style={{ textAlign: "center" }} className={masterClass}>
                    {this.colorRadio(fallback, "Create", "#0099FF") }
                </td>
                <td style={{ textAlign: "center" }} className={masterClass}>
                    {this.colorRadio(fallback, "Modify", "green") }
                </td>
                <td style={{ textAlign: "center" }}>
                    {this.colorRadio(fallback, "Read", "#FFAD00") }
                </td>
                <td style={{ textAlign: "center" }}>
                    {this.colorRadio(fallback, "None", "red") }
                </td>
                <td style={{ textAlign: "center" }}>
                    <GrayCheckbox checked={!typeAllowedEquals(ctx.value.allowed, ctx.value.allowedBase)} onUnchecked={() => {
                        ctx.value.allowed = JSON.parse(JSON.stringify(ctx.value.allowedBase));
                        ctx.value.modified = true;
                        this.updateFrame();
                    }} />
                </td>
                {properties && <td style={{ textAlign: "center" }}>
                    {this.link("fa fa-pencil-square-o", ctx.value.modified ? "Invalidated" : ctx.value.properties,
                        () => API.fetchPropertyRulePack(ctx.value.resource.cleanName, roleId),
                        m => ctx.value.properties = m.rules.every(a => a.element.allowed == "None") ? "None" :
                            m.rules.every(a => a.element.allowed == "Modify") ? "All" : "Mix"
                    )}
                </td>}
                {operations && <td style={{ textAlign: "center" }}>
                    {this.link("fa fa-bolt", ctx.value.modified ? "Invalidated" :  ctx.value.operations,
                        () => API.fetchOperationRulePack(ctx.value.resource.cleanName, roleId),
                        m => ctx.value.operations = m.rules.every(a => a.element.allowed == "None") ? "None" :
                            m.rules.every(a => a.element.allowed == "Allow") ? "All" : "Mix")}
                </td>}
                {queries && <td style={{ textAlign: "center" }}>
                    {this.link("fa fa-search", ctx.value.modified ? "Invalidated" : ctx.value.queries,
                        () => API.fetchQueryRulePack(ctx.value.resource.cleanName, roleId),
                        m => ctx.value.queries = m.rules.every(a => a.element.allowed == "None") ? "None" :
                            m.rules.every(a => a.element.allowed == "Allow") ? "All" : "Mix")}
                </td>}
            </tr>
        ].concat(ctx.value.allowed!.conditions!.map(mle => mle.element).map(c => {
            let b = Binding.create(c, ca => ca.allowed);
            return (
                <tr key={ctx.value.resource.namespace + "." + ctx.value.resource.className + "_" + c.typeCondition.id} className= "sf-auth-condition" >
                    <td>
                        &nbsp; &nbsp;
                        <a className="fa fa-minus-circle sf-condition-icon" aria-hidden="true" onClick={() => this.handleRemoveConditionClick(ctx.value.allowed, c) }></a>
                        &nbsp;
                        <small>{ c.typeCondition.toStr.tryAfter(".") || c.typeCondition.toStr }</small>
                    </td>
                    <td style={{ textAlign: "center" }} className={masterClass}>
                        {this.colorRadio(b, "Create", "#0099FF") }
                    </td>
                    <td style={{ textAlign: "center" }} className={masterClass}>
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

    colorRadio(b: Binding<TypeAllowed | null>, basicAllowed: TypeAllowedBasic, color: string) {
        const allowed = b.getValue();

        const niceName = TypeAllowedBasic.niceToString(basicAllowed)!;

        const title = !allowed ? niceName :
            getDB(allowed) == getUI(allowed) && getUI(allowed) == basicAllowed ? niceName :
                getDB(allowed) == basicAllowed ? AuthAdminMessage._0InDB.niceToString(niceName) :
                    getUI(allowed) == basicAllowed ? AuthAdminMessage._0InUI.niceToString(niceName) :
                        niceName;

        const icon = !allowed ? null :
            getDB(allowed) == getUI(allowed) && getUI(allowed) == basicAllowed ? null :
                getDB(allowed) == basicAllowed ? "fa fa-database" :
                    getUI(allowed) == basicAllowed ? "fa fa-window-restore" :
                        null;

        return <ColorRadio
            checked={isActive(allowed, basicAllowed)}
            title={title}
            color={color}
            icon={icon}
            onClicked={e => { b.setValue(select(b.getValue(), basicAllowed, e)); this.updateFrame(); }}
        />;
    }

    link<T extends ModelEntity>(icon: string, allowed: AuthThumbnail | null | "Invalidated", action: () => Promise<T>, setNewValue: (model: T) => void) {
        
        if (!allowed)
            return undefined;

        let onClick = () => {
            GraphExplorer.propagateAll(this.props.ctx.value);

            if (this.props.ctx.value.modified) {
                MessageModal.show({
                    title: NormalControlMessage.SaveChangesFirst.niceToString(),
                    message: AuthAdminMessage.PleaseSaveChangesFirst.niceToString(),
                    buttons: "ok",
                    style: "warning",
                    icon: "warning"
                }).done();
            }
            else {
                action()
                    .then(m => Navigator.view(m))
                    .then(m => {
                       if (m) {
                          setNewValue(m);
                          this.updateFrame();
                       }
                     })
                    .done();
            }
        };

        return (
            <a onClick={onClick} title={allowed}
                className={classes("sf-auth-link", icon)}
                style={{
                    color: allowed == "Invalidated" ? "gray" :
                        allowed == "All" ? "green" :
                            allowed == "Mix" ? "#FFAD00" : "red"
                }}>
            </a>
        );
    }
}

function typeAllowedEquals(allowed: TypeAllowedAndConditions, allowedBase: TypeAllowedAndConditions) {
    return allowed.fallback == allowedBase.fallback
        && allowed.conditions!.length == allowedBase.conditions!.length
        && allowed.conditions!.map(mle => mle.element)
            .every((c, i) => {
                let b = allowedBase.conditions![i].element;
                return c.allowed == b.allowed && is(c.typeCondition, b.typeCondition);
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


function select(current: TypeAllowed | null, basicAllowed: TypeAllowedBasic, e: React.MouseEvent<any>) {
    if (!(e.shiftKey || e.ctrlKey) || current == null)
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
