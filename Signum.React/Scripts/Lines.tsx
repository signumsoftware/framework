import { TypeContext, StyleContext } from './TypeContext'
import type { StyleOptions, FormGroupStyle, FormSize, IRenderButtons } from './TypeContext'
export { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormSize, IRenderButtons };

import { PropertyRoute, Binding, ReadonlyBinding } from './Reflection'
export { Binding, ReadonlyBinding, PropertyRoute };

import { tasks, LineBaseController} from './Lines/LineBase'
import type { ChangeEvent, LineBaseProps } from './Lines/LineBase'
export { tasks, ChangeEvent, LineBaseProps }

import { FormGroup } from './Lines/FormGroup'
import type { FormGroupProps } from './Lines/FormGroup'
export { FormGroup, FormGroupProps }

import { FormControlReadonly } from './Lines/FormControlReadonly'
import type { FormControlReadonlyProps } from './Lines/FormControlReadonly'
export { FormControlReadonly, FormControlReadonlyProps }

import { ValueLine, ValueLineController } from './Lines/ValueLine'
import type { ValueLineType, ValueLineProps, OptionItem } from './Lines/ValueLine'
export { ValueLine, ValueLineType, ValueLineProps, OptionItem }

export { RenderEntity } from './Lines/RenderEntity'

export { FindOptionsAutocompleteConfig, LiteAutocompleteConfig } from './Lines/AutoCompleteConfig'
export type { AutocompleteConfig } from './Lines/AutoCompleteConfig'

import { EntityBaseController } from './Lines/EntityBase'
export { EntityBaseController }

export { FetchInState, FetchAndRemember } from './Lines/Retrieve'

export { EntityLine } from './Lines/EntityLine'

export { EntityCombo } from './Lines/EntityCombo'

export { EntityDetail } from './Lines/EntityDetail'

export { EntityList } from './Lines/EntityList'

export { EntityRepeater } from './Lines/EntityRepeater'

export { EntityTabRepeater } from './Lines/EntityTabRepeater'

export { EntityStrip } from './Lines/EntityStrip'

export { EntityCheckboxList } from './Lines/EntityCheckboxList'
export { EntityRadioButtonList } from './Lines/EntityRadioButtonList'

export { EnumCheckboxList } from './Lines/EnumCheckboxList'
export { MultiValueLine } from './Lines/MultiValueLine'


import { EntityTable, EntityTableRow } from './Lines/EntityTable'
import type { EntityTableColumn } from './Lines/EntityTable'

import DynamicComponent from './Lines/DynamicComponent';
import { EntityListBaseController, EntityListBaseProps } from './Lines/EntityListBase';
export { DynamicComponent }

export { EntityTable, EntityTableColumn, EntityTableRow };

tasks.push(taskSetNiceName);
export function taskSetNiceName(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (!state.labelText &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field") {
    state.labelText = state.ctx.propertyRoute.member!.niceName;
  }
}

tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof ValueLineController) {
    const vProps = state as ValueLineProps;

    if (!vProps.unitText &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.unitText = state.ctx.propertyRoute.member!.unit;
    }
  }
}

tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof ValueLineController) {
    const vProps = state as ValueLineProps;

    if (!vProps.formatText &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.formatText = state.ctx.propertyRoute.member!.format;
      if (vProps.valueLineType == "TextBox" && state.ctx.propertyRoute.member!.format == "Password")
        vProps.valueLineType = "Password";
    }
  }
}

tasks.push(taskSetReadOnlyProperty);
export function taskSetReadOnlyProperty(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (!state.ctx.readOnly &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.isReadOnly) {
    state.ctx.readOnly = true;
  }
}

tasks.push(taskSetReadOnly);
export function taskSetReadOnly(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (!state.ctx.readOnly &&
    state.ctx.binding.getIsReadonly()) {
    state.ctx.readOnly = true;
  }
}

tasks.push(taskSetMandatory);
export function taskSetMandatory(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (state.ctx.propertyRoute && state.mandatory == undefined &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.required) {
    state.mandatory = true;
  }
}


tasks.push(taskSetMove);
export function taskSetMove(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof EntityListBaseController && (state as EntityListBaseProps).move == undefined &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.preserveOrder) {
    (state as EntityListBaseProps).move = true;
  }
}

export let maxValueLineSize = 100;

tasks.push(taskSetHtmlProperties);
export function taskSetHtmlProperties(lineBase: LineBaseController<any>, state: LineBaseProps) {
  const vl = lineBase instanceof ValueLineController ? lineBase : undefined;
  const pr = state.ctx.propertyRoute;
  const s = state as ValueLineProps;
  if (vl && pr?.propertyRouteType == "Field" && (s.valueLineType == "TextBox" || s.valueLineType == "TextArea")) {

    var member = pr.member!;

    if (member.maxLength != undefined && !s.ctx.readOnly) {

      if (!s.valueHtmlAttributes)
        s.valueHtmlAttributes = {};

      if (s.valueHtmlAttributes.maxLength == undefined)
        s.valueHtmlAttributes.maxLength = member.maxLength;

      if (s.valueHtmlAttributes.size == undefined)
        s.valueHtmlAttributes.size = maxValueLineSize == undefined ? member.maxLength : Math.min(maxValueLineSize, member.maxLength);
    }

    if (member.isMultiline)
      s.valueLineType = "TextArea";
  }
}
