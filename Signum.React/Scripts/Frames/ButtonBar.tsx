import * as React from 'react'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame, IRenderButtons, ButtonsContext } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'

export interface ButtonBarProps extends ButtonsContext {
    align?: "left" | "right";
}

export default class ButtonBar extends React.Component<ButtonBarProps, void>{

    static onButtonBarRender: Array<(ctx: ButtonsContext) => Array<React.ReactElement<any>>> = [];

    render() {

        var ctx: ButtonsContext = this.props;

        var c = ctx.frame.component as any as IRenderButtons;

        var buttons = (c.renderButtons ? c.renderButtons(ctx) : [])
            .concat(ButtonBar.onButtonBarRender.flatMap(func => func(this.props) || [])).map((a, i) => React.cloneElement(a, { key: i }));

        return (
            <div className={classes("btn-toolbar", "sf-button-bar", this.props.align == "right" ? "right" : null) } >
                { buttons }
            </div>
        );
    }
}

