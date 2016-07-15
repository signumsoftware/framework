import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'

require("!style!css!./Widgets.css");

export interface WidgetContext {
    ctx: TypeContext<ModifiableEntity>;
    pack: EntityPack<ModifiableEntity>;
}

export const onWidgets: Array<(ctx: WidgetContext) => React.ReactElement<any>> = [];

export function renderWidgets(wc: WidgetContext): React.ReactNode | undefined
{
    const widgets = onWidgets.map(a => a(wc)).filter(a => a != undefined);

    if (widgets.length == 0)
        return undefined;

    return <ul className="sf-widgets">
        {widgets.map((w, i) => <li key={i}>{w}</li>)}
    </ul>;
}

export interface EmbeddedWidget {
    embeddedWidget: React.ReactElement<any>;
    position: EmbeddedWidgetPosition;
}

export type EmbeddedWidgetPosition = "Top" | "Bottom";

export const onEmbeddedWidgets: Array<(ctx: WidgetContext) => EmbeddedWidget> = [];

export function renderEmbeddedWidgets(wc: WidgetContext): { top: React.ReactElement<any>[]; bottom: React.ReactElement<any>[] } {
    const widgets = onEmbeddedWidgets.map(a => a(wc)).filter(a => a != undefined);
    
    return {
        top: widgets.filter(ew => ew.position == "Top").map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i })),
        bottom: widgets.filter(ew => ew.position == "Bottom").map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i }))
    };
}