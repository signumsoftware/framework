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

import { EntityLine, EntityLineProps} from 'Framework/Signum.React/Scripts/Lines/EntityBase'
export { EntityLine, EntityLineProps };


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
Tasks.push(taskSetUnit);
Tasks.push(taskSetFormat);

export function taskSetNiceName(lineBase: LineBase<any, any>, props: LineBaseProps) {
    if (!props.labelText && props.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
        props.labelText = props.ctx.propertyRoute.member.niceName;
    }
}

export function taskSetUnit(lineBase: LineBase<any, any>, props: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        var vProps = props as ValueLineProps;

        if (!vProps.unitText && props.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.unitText = props.ctx.propertyRoute.member.unit;
        }
    }
}

export function taskSetFormat(lineBase: LineBase<any, any>, props: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        var vProps = props as ValueLineProps;

        if (!vProps.unitText && props.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.formatText = props.ctx.propertyRoute.member.unit;
        }
    }
}