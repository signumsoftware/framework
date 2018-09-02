import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormSize, IRenderButtons } from './TypeContext'
export { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormSize, IRenderButtons };

import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, PropertyRoute, Binding, ReadonlyBinding } from './Reflection'
export { Binding, ReadonlyBinding }

import { ModifiableEntity, EntityPack, ModelState } from './Signum.Entities'
import * as Navigator from './Navigator'
import { ViewReplacer } from  './Frames/ReactVisitor'

export { PropertyRoute };

import { LineBase, LineBaseProps, tasks, ChangeEvent } from './Lines/LineBase'
export { LineBase, LineBaseProps, tasks, ChangeEvent }

import { FormGroup, FormGroupProps } from './Lines/FormGroup'
export { FormGroup, FormGroupProps }

import { FormControlReadonly, FormControlReadonlyProps } from './Lines/FormControlReadonly'
export { FormControlReadonly, FormControlReadonlyProps }

import { ValueLine, ValueLineType, ValueLineProps, OptionItem } from './Lines/ValueLine'
export { ValueLine, ValueLineType, ValueLineProps, OptionItem }

export { RenderEntity } from  './Lines/RenderEntity'

import { EntityBase } from  './Lines/EntityBase'
export { EntityBase }

export { AutocompleteConfig, FindOptionsAutocompleteConfig, LiteAutocompleteConfig } from './Lines/AutoCompleteConfig'

export { EntityLine } from './Lines/EntityLine'

export { EntityCombo } from  './Lines/EntityCombo'

export { EntityDetail } from  './Lines/EntityDetail'

import { EntityListBase, EntityListBaseProps } from  './Lines/EntityListBase'
export { EntityListBase, EntityListBaseProps }

export { EntityList } from  './Lines/EntityList'

export { EntityRepeater } from  './Lines/EntityRepeater'

export { EntityTabRepeater } from  './Lines/EntityTabRepeater'

export { EntityStrip } from  './Lines/EntityStrip'

export { EntityCheckboxList } from  './Lines/EntityCheckboxList'

export { EnumCheckboxList } from  './Lines/EnumCheckboxList'
export { MultiValueLine } from  './Lines/MultiValueLine'


import { EntityTable, EntityTableColumn, EntityTableRow } from './Lines/EntityTable'
export { EntityTable, EntityTableColumn, EntityTableRow };

tasks.push(taskSetNiceName);
export function taskSetNiceName(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (!state.labelText &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == "Field") {
        state.labelText = state.ctx.propertyRoute.member!.niceName;
    }
}

tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        const vProps = state as ValueLineProps;

        if (!vProps.unitText &&
            state.ctx.propertyRoute &&
            state.ctx.propertyRoute.propertyRouteType == "Field") {
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
            state.ctx.propertyRoute.propertyRouteType == "Field") {
            vProps.formatText = state.ctx.propertyRoute.member!.format;
        }
    }
}

tasks.push(taskSetReadOnly);
export function taskSetReadOnly(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (!state.ctx.readOnly &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == "Field" &&
        state.ctx.propertyRoute.member!.isReadOnly) {
        state.ctx.readOnly = true;
    }
}

tasks.push(taskSetMove);
export function taskSetMove(lineBase: LineBase<any, any>, state: LineBaseProps) {
    if (lineBase instanceof EntityListBase &&
        state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == "Field" &&
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
    if (vl && pr && pr.propertyRouteType == "Field" && (s.valueLineType == "TextBox" || s.valueLineType == "TextArea")) {

        var member = pr.member!;

        if (member.maxLength != undefined && !s.ctx.readOnly) {

            if (!s.valueHtmlAttributes)
                s.valueHtmlAttributes = {};

            s.valueHtmlAttributes.maxLength = member.maxLength;

            s.valueHtmlAttributes.size = maxValueLineSize == undefined ? member.maxLength : Math.min(maxValueLineSize, member.maxLength);
        }

        if (member.isMultiline)
            s.valueLineType = "TextArea";
    }
}