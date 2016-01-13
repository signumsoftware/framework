/// <reference path="globals.ts" />

import * as React from 'react'
import * as moment from 'moment'
import { Input, Tab } from 'react-bootstrap'
//import { DatePicker } from 'react-widgets'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from 'Framework/Signum.React/Scripts/TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo} from 'Framework/Signum.React/Scripts/Reflection'

import { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, Tasks} from 'Framework/Signum.React/Scripts/Lines/LineBase'
export { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, Tasks};

import { ValueLine, ValueLineType, ValueLineProps } from 'Framework/Signum.React/Scripts/Lines/ValueLine'
export { ValueLine, ValueLineType, ValueLineProps};

import { EntityBase, EntityBaseProps, EntityLine, EntityLineProps, EntityCombo, EntityComboProps, EntityListBase, EntityListBaseProps } from  'Southwind.React/Templates/EntityControls'//'Framework/Signum.React/Scripts/Lines/EntityControls'
export { EntityBase, EntityBaseProps, EntityLine, EntityLineProps, EntityCombo, EntityComboProps, EntityListBase, EntityListBaseProps };


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
        var vProps = state as ValueLineProps;

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
        var vProps = state as ValueLineProps;

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