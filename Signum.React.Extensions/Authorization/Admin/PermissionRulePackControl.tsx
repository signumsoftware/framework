import * as React from 'react'
import * as numbro from 'numbro'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { notifySuccess }from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { TypeContext, ButtonsContext, IRenderButtons } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityLine, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines'

import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API } from '../AuthClient'
import { PermissionRulePack, PermissionAllowedRule, AuthAdminMessage, PermissionSymbol, AuthMessage } from '../Signum.Entities.Authorization'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'

import "./AuthAdmin.css"
import { Button } from '../../../../Framework/Signum.React/Scripts/Components';

export default class PermissionRulesPackControl extends React.Component<{ ctx: TypeContext<PermissionRulePack> }> implements IRenderButtons {

    handleSaveClick = (bc: ButtonsContext) => {
        let pack = this.props.ctx.value;

        API.savePermissionRulePack(pack)
            .then(() => API.fetchPermissionRulePack(pack.role.id!))
            .then(newPack => {
                notifySuccess();
                bc.frame.onReload({ entity: newPack, canExecute: {} });
            })
            .done();
    }

    renderButtons(bc: ButtonsContext) {
        return [
            <Button color="primary" onClick={() => this.handleSaveClick(bc)}>{AuthMessage.Save.niceToString()}</Button>
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
                <table className="table table-sm sf-auth-rules">
                    <thead>
                        <tr>
                            <th>
                                { PermissionSymbol.niceName() }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {AuthAdminMessage.Allow.niceToString() }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {AuthAdminMessage.Deny.niceToString() }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {AuthAdminMessage.Overriden.niceToString() }
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        { ctx.mlistItemCtxs(a => a.rules).orderBy(a => a.value.resource.key).map((c, i) =>
                            <tr key={i}>
                                <td>
                                    {c.value.resource.key}
                                </td>
                                <td style={{ textAlign: "center" }}>
                                    {this.renderRadio(c.value, true, "green") }
                                </td>
                                <td style={{ textAlign: "center" }}>
                                    {this.renderRadio(c.value, false, "red") }
                                </td>
                                <td style={{ textAlign: "center" }}>
                                     <GrayCheckbox checked={c.value.allowed != c.value.allowedBase} onUnchecked={() => {
                                        c.value.allowed = c.value.allowedBase;     
                                        ctx.value.modified = true; 
                                        this.forceUpdate();
                                    }} />
                                </td>
                            </tr>
                        ) }
                    </tbody>
                </table>

            </div>
        );
    }

    renderRadio(c: PermissionAllowedRule, allowed: boolean, color: string) {
        return <ColorRadio checked={c.allowed == allowed} color={color} onClicked={a => { c.allowed = allowed; c.modified = true; this.forceUpdate() } }/>;
    }
}



