import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { PropertyRouteEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics';
import { notifySuccess } from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { TypeContext, ButtonsContext, IRenderButtons } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityLine, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines'

import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API } from '../AuthClient'
import { PropertyRulePack, PropertyAllowedRule, PropertyAllowed, AuthAdminMessage, PermissionSymbol, AuthMessage } from '../Signum.Entities.Authorization'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'

import "./AuthAdmin.css"
import { Button } from '../../../../Framework/Signum.React/Scripts/Components';


export default class PropertyRulesPackControl extends React.Component<{ ctx: TypeContext<PropertyRulePack> }> implements IRenderButtons {

    handleSaveClick = (bc: ButtonsContext) => {
        let pack = this.props.ctx.value;

        API.savePropertyRulePack(pack)
            .then(() => API.fetchPropertyRulePack(pack.type.cleanName!, pack.role.id!))
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

    handleHeaderClick(e: React.MouseEvent<HTMLAnchorElement>, hc: PropertyAllowed) {

        this.props.ctx.value.rules.forEach(mle => {
            if (!mle.element.coercedValues!.contains(hc)) {
                mle.element.allowed = hc;
                mle.element.modified = true;
            }
        });

        this.forceUpdate();
    }

    render() {

        let ctx = this.props.ctx;

        return (
            <div>
                <div className="form-compact">
                    <EntityLine ctx={ctx.subCtx(f => f.role)} />
                    <ValueLine ctx={ctx.subCtx(f => f.strategy)} />
                    <EntityLine ctx={ctx.subCtx(f => f.type)} />
                </div>
                <table className="table table-sm sf-auth-rules">
                    <thead>
                        <tr>
                            <th>
                                { PropertyRouteEntity.niceName() }
                            </th>
                            <th style={{ textAlign: "center" }}>
                                <a onClick={e => this.handleHeaderClick(e, "Modify")}>{PropertyAllowed.niceToString("Modify")}</a>
                            </th>
                            <th style={{ textAlign: "center" }}>
                                <a onClick={e => this.handleHeaderClick(e, "Read")}>{PropertyAllowed.niceToString("Read")}</a>
                            </th>
                            <th style={{ textAlign: "center" }}>
                                <a onClick={e => this.handleHeaderClick(e, "None")}>{PropertyAllowed.niceToString("None")}</a>
                            </th>
                            <th style={{ textAlign: "center" }}>
                                {AuthAdminMessage.Overriden.niceToString()}
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        {ctx.mlistItemCtxs(a => a.rules).map((c, i) =>
                            <tr key={i}>
                                <td>
                                    {c.value.resource.path}
                                </td>
                                <td style={{ textAlign: "center" }}>
                                    {this.renderRadio(c.value, "Modify", "green")}
                                </td>
                                <td style={{ textAlign: "center" }}>
                                    {this.renderRadio(c.value, "Read", "#FFAD00")}
                                </td>
                                <td style={{ textAlign: "center" }}>
                                    {this.renderRadio(c.value, "None", "red")}
                                </td>
                                <td style={{ textAlign: "center" }}>
                                    <GrayCheckbox checked={c.value.allowed != c.value.allowedBase} onUnchecked={() => {
                                        c.value.allowed = c.value.allowedBase;
                                        ctx.value.modified = true;
                                        this.forceUpdate();
                                    }} />
                                </td>
                            </tr>
                        )
                        }
                    </tbody>
                </table>

            </div>
        );
    }


    renderRadio(c: PropertyAllowedRule, allowed: PropertyAllowed, color: string) {

        if (c.coercedValues!.contains(allowed))
            return;

        return <ColorRadio
            checked={c.allowed == allowed}
            color={color}
            onClicked={a => { c.allowed = allowed; c.modified = true; this.forceUpdate() }}
        />;
    }
}


