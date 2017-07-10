import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'

import "./Widgets.css"

export interface WidgetContext<T extends ModifiableEntity> {
    ctx: TypeContext<T>;
    pack: EntityPack<T>;
}

export const onWidgets: Array<(ctx: WidgetContext<ModifiableEntity>) => React.ReactElement<any>> = [];

export function renderWidgets(wc: WidgetContext<ModifiableEntity>): React.ReactNode | undefined
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

export const onEmbeddedWidgets: Array<(ctx: WidgetContext<ModifiableEntity>) => EmbeddedWidget | undefined> = [];

export function renderEmbeddedWidgets(wc: WidgetContext<ModifiableEntity>): { top: React.ReactElement<any>[]; bottom: React.ReactElement<any>[] } {
    const widgets = onEmbeddedWidgets.map(a => a(wc)).filter(a => a != undefined).map(a => a!);
    
    return {
        top: widgets.filter(ew => ew.position == "Top").map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i })),
        bottom: widgets.filter(ew => ew.position == "Bottom").map((ew, i) => React.cloneElement(ew.embeddedWidget, { key: i }))
    };
}