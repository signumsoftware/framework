import * as React from 'react'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormGroupSize, IRenderButtons } from './TypeContext'
export { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormGroupSize, IRenderButtons };

import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, PropertyRoute, Binding, ReadonlyBinding } from './Reflection'
export { Binding, ReadonlyBinding }

import { ModifiableEntity, EntityPack, ModelState } from './Signum.Entities'
import * as Navigator from './Navigator'
import { ViewReplacer } from  './Frames/ReactVisitor'

export { PropertyRoute };

import { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, tasks} from './Lines/LineBase'
export { FormGroup, FormGroupProps, FormControlStatic, FormControlStaticProps, LineBase, LineBaseProps, tasks};

import { ValueLine, ValueLineType, ValueLineProps } from './Lines/ValueLine'
export { ValueLine, ValueLineType, ValueLineProps};

import { RenderEntity } from  './Lines/RenderEntity'
export { RenderEntity };

import { EntityBase } from  './Lines/EntityBase'
export { EntityBase };

import { EntityLine } from  './Lines/EntityLine'
export { EntityLine };

import { EntityCombo } from  './Lines/EntityCombo'
export { EntityCombo };


import { EntityDetail } from  './Lines/EntityDetail'
export { EntityDetail };

import { EntityListBase, EntityListBaseProps } from  './Lines/EntityListBase'
export { EntityListBase };

import { EntityList } from  './Lines/EntityList'
export { EntityList };

import { EntityRepeater } from  './Lines/EntityRepeater'
export { EntityRepeater };

import { EntityTabRepeater } from  './Lines/EntityTabRepeater'
export { EntityTabRepeater };

import { EntityStrip } from  './Lines/EntityStrip'
export { EntityStrip };

import { EntityCheckboxList } from  './Lines/EntityCheckboxList'
export { EntityCheckboxList };

import { EnumCheckboxList } from  './Lines/EnumCheckboxList'
export { EnumCheckboxList };


import { EntityTable } from './Lines/EntityTable'
export { EntityTable };

tasks.push(taskSetNiceName);
export function taskSetNiceName(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (!state.labelText &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
        state.labelText = state.ctx.propertyRoute.member!.niceName;
    }
}

tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        const vProps = state as ValueLineProps;

        if (!vProps.unitText &&
            state.ctx.propertyRoute &&
            state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.unitText = state.ctx.propertyRoute.member!.unit;
        }
    }
}

tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        const vProps = state as ValueLineProps;

        if (!vProps.formatText &&
            state.ctx.propertyRoute &&
            state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.formatText = state.ctx.propertyRoute.member!.format;
        }
    }
}

tasks.push(taskSetReadOnly);
export function taskSetReadOnly(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (!state.ctx.readOnly &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field &&
        state.ctx.propertyRoute.member!.isReadOnly) {
        state.ctx.readOnly = true;
    }
}

tasks.push(taskSetMove);
export function taskSetMove(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (lineBase instanceof EntityListBase &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field &&
        state.ctx.propertyRoute.member!.preserveOrder) {
        (state as EntityListBaseProps).move = true;
    }
}

export let maxValueLineSize = 100; 

tasks.push(taskSetHtmlProperties);
export function taskSetHtmlProperties(lineBase: LineBase<any, any>, state: LineBaseProps) {
    const vl = lineBase instanceof ValueLine ? lineBase as ValueLine : undefined;
    const pr = state.ctx.propertyRoute;
    const s = state as ValueLineProps;
    if (vl && pr && pr.propertyRouteType == PropertyRouteType.Field && (s.valueLineType == ValueLineType.TextBox || s.valueLineType == ValueLineType.TextArea)) {

        var member = pr.member!;

        if (member.maxLength != undefined) {

            if (!s.valueHtmlProps)
                s.valueHtmlProps = {};

            s.valueHtmlProps.maxLength = member.maxLength;

            s.valueHtmlProps.size = maxValueLineSize == undefined ? member.maxLength : Math.min(maxValueLineSize, member.maxLength);
        }

        if (member.isMultiline)
            s.valueLineType = ValueLineType.TextArea;
    }
}