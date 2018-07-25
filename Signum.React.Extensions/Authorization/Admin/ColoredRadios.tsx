import * as React from 'react'
import * as numbro from 'numbro'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { notifySuccess }from '@framework/Operations/EntityOperations'
import EntityLink from '@framework/SearchControl/EntityLink'
import { TypeContext, ButtonsContext, IRenderButtons } from '@framework/TypeContext'
import { EntityLine, ValueLine } from '@framework/Lines'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '@framework/Signum.Entities'
import { PermissionRulePack, AuthAdminMessage, PermissionSymbol, AuthMessage } from '../Signum.Entities.Authorization'


import "./AuthAdmin.css"
interface ColorRadioProps {
    checked: boolean;
    onClicked: (e: React.MouseEvent<HTMLAnchorElement>) => void;
    color: string;
    title?: string;
    icon?: IconProp | null;
}

export class ColorRadio extends React.Component<ColorRadioProps> {
    render() {
        return (
            <a onClick={e => { e.preventDefault(); this.props.onClicked(e); }} title={this.props.title}
                className="sf-auth-chooser"
                style={{ color: this.props.checked ? this.props.color : "#aaa" }}>
                <FontAwesomeIcon icon={this.props.icon || ["far", (this.props.checked ? "dot-circle" : "circle")]} />
            </a>
        );
    }
}

export class GrayCheckbox extends React.Component<{ checked: boolean, onUnchecked: () => void }> {
    render() {
        return (
            <span className="sf-auth-checkbox" onClick={this.props.checked ? this.props.onUnchecked : undefined}>
                <FontAwesomeIcon icon={["far", this.props.checked ? "check-square" : "square"]}/>
            </span>
        );
    }
}




