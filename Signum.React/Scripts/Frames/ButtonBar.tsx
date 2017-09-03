import * as React from 'react'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import * as OrderUtils from '../Frames/OrderUtils'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame, IRenderButtons, ButtonsContext } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'

export interface ButtonBarProps extends ButtonsContext {
    align?: "left" | "right";
}

export default class ButtonBar extends React.Component<ButtonBarProps>{

    static onButtonBarRender: Array<(ctx: ButtonsContext) => Array<React.ReactElement<any> | undefined> | undefined> = [];

    render() {

        const ctx: ButtonsContext = this.props;

        const rb = ctx.frame.entityComponent as any as IRenderButtons;

        const buttons = ButtonBar.onButtonBarRender.flatMap(func => func(this.props) || [])
            .concat(rb && rb.renderButtons ? rb.renderButtons(ctx) : [])
            .filter(a => a != null)
            .orderBy(a => OrderUtils.getOrder(a!))
            .map((a, i) => OrderUtils.cloneElementWithoutOrder(a!, { key: i }));

        return (
            <div className={classes("btn-toolbar", "sf-button-bar", this.props.align == "right" ? "right" : undefined) } >
                { buttons }
            </div>
        );
    }
}

