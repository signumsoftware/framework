import * as React from 'react'
import { Button } from 'react-bootstrap'
import { Link } from 'react-router'
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
import { Api } from '../AuthClient'
import { PermissionRulePack, AuthAdminMessage, PermissionSymbol, AuthMessage } from '../Signum.Entities.Authorization'


require("./AuthAdmin.css");

export default class PermissionRulesPackControl extends React.Component<{ ctx: TypeContext<PermissionRulePack> }, void> implements IRenderButtons {

    handleSaveClick = (bc: ButtonsContext) => {
        var pack = this.props.ctx.value;

        Api.savePermissionRulePack(pack)
            .then(() => Api.fetchPermissionRulePack(pack.role.id))
            .then(newPack => {
                notifySuccess();
                bc.frame.onReload({ entity: newPack, canExecute: null });
            })
            .done();
    }

    renderButtons(bc: ButtonsContext) {
        return [
            <Button bsStyle="primary" onClick={() => this.handleSaveClick(bc) }>{AuthMessage.Save.niceToString() }</Button>
        ];
    }


    render() {

        var ctx = this.props.ctx;

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
                        { ctx.mlistItemCtxs(a => a.rules).map((c, i) =>
                            <tr key={i}>
                                <td>
                                    {c.value.resource.key}
                                </td>
                                <td style={{ textAlign:"center" }}>
                                    <ColorRadio checked={c.value.allowed} color="green" onClicked={a => { c.value.allowed = true; this.forceUpdate() } }/>
                                </td>
                                <td style={{ textAlign: "center" }}>
                                    <ColorRadio checked={!c.value.allowed} color="red" onClicked={a => { c.value.allowed = false; this.forceUpdate() } }/>
                                </td>
                                <td style={{ textAlign: "center" }}>
                                    <GrayCheckbox checked={c.value.allowed != c.value.allowedBase}/>
                                </td>
                            </tr>
                        )
                        }
                    </tbody>
                </table>

            </div>
        );
    }
}

class ColorRadio extends React.Component<{ checked: boolean, onClicked: (e: React.MouseEvent) => void, color: string }, void>{

    render() {
        return (
            <a href="#" onClick={e => { e.preventDefault(); this.props.onClicked(e); } }
                className={classes("sf-auth-chooser", "fa", this.props.checked ? "fa-dot-circle-o" : "fa-circle-o")}
                style={{ color: this.props.checked ? this.props.color : "#aaa" }}>
            </a>
        );
    }
}

class GrayCheckbox extends React.Component<{ checked: boolean }, void>{

    render() {
        return (
            <i className={classes("sf-auth-checkbox", "fa", this.props.checked ? "fa-check-square-o" : "fa-square-o") }
                style={{ color: "#aaa" }}>
            </i>
        );
    }
}




