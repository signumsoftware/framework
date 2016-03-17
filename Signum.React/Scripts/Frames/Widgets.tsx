import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import { EntityFrame } from '../Lines'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'

require("!style!css!./Widgets.css");

export interface WidgetContext {
    ctx: TypeContext<ModifiableEntity>;
    pack: EntityPack<ModifiableEntity>;
}

export const onWidgets: Array<(ctx: WidgetContext) => React.ReactElement<any>> = [];

export function renderWidgets(wc: WidgetContext): React.ReactNode
{
    const widgets = onWidgets.map(a => a(wc)).filter(a => a != null);

    if (widgets.length == 0)
        return null;

    return <ul className="sf-widgets">
        {widgets.map((w, i) => <li key={i}>{w}</li>)}
    </ul>;
}

export interface EmbeddedWidget {
    embeddedWidget: React.ReactElement<any>;
    position: EmbeddedWidgetPosition;
}

export enum EmbeddedWidgetPosition {
    Top = "Top" as any,
    Bottom = "Bottom" as any
}

export var onEmbeddedWidgets: Array<(ctx: WidgetContext) => EmbeddedWidget> = [];

export function renderEmbeddedWidgets(wc: WidgetContext): { top: React.ReactElement<any>[]; bottom: React.ReactElement<any>[] } {
    const widgets = onEmbeddedWidgets.map(a => a(wc)).filter(a => a != null);
    
    return {
        top: widgets.filter(ew => ew.position == EmbeddedWidgetPosition.Top).map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i })),
        bottom: widgets.filter(ew => ew.position == EmbeddedWidgetPosition.Bottom).map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i }))
    };
}