import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import { EntityFrame } from '../Lines'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'

export interface ButtonsContext {
    pack: EntityPack<ModifiableEntity>;
    frame: EntityFrame<ModifiableEntity>;
    showOperations: boolean;
}

export interface ButtonBarProps extends ButtonsContext {

}

export default class ButtonBar extends React.Component<ButtonBarProps, void>{

    static onButtonBarRender: Array<(ctx: ButtonsContext) => Array<React.ReactElement<any>>> = [];

    render() {

        var ctx: ButtonsContext = this.props;

        var buttons = ButtonBar.onButtonBarRender.flatMap(func => func(this.props) || []).map((a, i) => React.cloneElement(a, { key: i }));

        return (
            <div className="btn-toolbar sf-button-bar">
                { buttons }
            </div>
        );
    }
}

