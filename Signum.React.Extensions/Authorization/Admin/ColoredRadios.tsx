import * as React from 'react'
import { Button } from 'react-bootstrap'
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
import { PermissionRulePack, AuthAdminMessage, PermissionSymbol, AuthMessage } from '../Signum.Entities.Authorization'


require("./AuthAdmin.css");

export class ColorRadio extends React.Component<{ checked: boolean, onClicked: (e: React.MouseEvent<any>) => void, color: string }, void>{

    render() {
        return (
            <a onClick={e => { e.preventDefault(); this.props.onClicked(e); } }
                className={classes("sf-auth-chooser", "fa", this.props.checked ? "fa-dot-circle-o" : "fa-circle-o")}
                style={{ color: this.props.checked ? this.props.color : "#aaa" }}>
            </a>
        );
    }
}

export class GrayCheckbox extends React.Component<{ checked: boolean, onUnchecked: () => void }, void>{

    render() {
        return (
            <i className={classes("sf-auth-checkbox", "fa", this.props.checked ? "fa-check-square-o" : "fa-square-o")}
                onClick={this.props.checked ? this.props.onUnchecked : undefined}>
            </i>
        );
    }
}




