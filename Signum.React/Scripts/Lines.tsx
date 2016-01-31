/// <reference path="globals.ts" />

import * as React from 'react'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from './TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo} from './Reflection'

import { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, Tasks} from './Lines/LineBase'
export { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, Tasks};

import { ValueLine, ValueLineType, ValueLineProps } from './Lines/ValueLine'
export { ValueLine, ValueLineType, ValueLineProps};

import { EntityBase, EntityBaseProps } from  './Lines/EntityBase'
export { EntityBase, EntityBaseProps };

import { EntityLine, EntityLineProps } from  './Lines/EntityLine'
export { EntityLine, EntityLineProps };

import { EntityCombo, EntityComboProps } from  './Lines/EntityCombo'
export { EntityCombo, EntityComboProps };

import { EntityListBase, EntityListBaseProps } from  './Lines/EntityListBase'
export { EntityListBase, EntityListBaseProps };


import { EntityList, EntityListProps } from  './Lines/EntityList'
export { EntityList, EntityListProps };


export class EntityComponent<T> extends React.Component<{ ctx: TypeContext<T> }, {}>{

    get value() {
        return this.props.ctx.value;
    }

    subCtx<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R> {
        return this.props.ctx.subCtx(property, styleOptions);
    }

    niceName(property: (val: T) => any) {
        return this.props.ctx.niceName(property);
    }
}

Tasks.push(taskSetNiceName);
export function taskSetNiceName(lineBase: LineBase<any>, state: LineBaseProps) {
    if (!state.labelText &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
        state.labelText = state.ctx.propertyRoute.member.niceName;
    }
}

Tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBase<any>, state: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        const vProps = state as ValueLineProps;

        if (!vProps.unitText &&
            state.ctx.propertyRoute &&
            state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.unitText = state.ctx.propertyRoute.member.unit;
        }
    }
}

Tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBase<any>, state: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        const vProps = state as ValueLineProps;

        if (!vProps.formatText &&
            state.ctx.propertyRoute &&
            state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.formatText = state.ctx.propertyRoute.member.format;
        }
    }
}

Tasks.push(taskSetReadOnly);
export function taskSetReadOnly(lineBase: LineBase<any>, state: LineBaseProps) {
    if (!state.ctx.readOnly &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field &&
        state.ctx.propertyRoute.member.isReadOnly) {
        state.ctx.readOnly = true;
    }
}