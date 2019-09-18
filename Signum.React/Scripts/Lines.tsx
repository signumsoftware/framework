import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormSize, IRenderButtons } from './TypeContext'
export { TypeContext, StyleContext, StyleOptions, FormGroupStyle, FormSize, IRenderButtons };

import { PropertyRoute, Binding, ReadonlyBinding } from './Reflection'
export { Binding, ReadonlyBinding, PropertyRoute };

import { tasks, ChangeEvent, LineBaseController, LineBaseProps } from './Lines/LineBase'
export { tasks, ChangeEvent }

import { FormGroup, FormGroupProps } from './Lines/FormGroup'
export { FormGroup, FormGroupProps }

import { FormControlReadonly, FormControlReadonlyProps } from './Lines/FormControlReadonly'
export { FormControlReadonly, FormControlReadonlyProps }

import { ValueLine, ValueLineType, ValueLineProps, OptionItem, ValueLineController } from './Lines/ValueLine'
export { ValueLine, ValueLineType, ValueLineProps, OptionItem }

export { RenderEntity } from './Lines/RenderEntity'

export { AutocompleteConfig, FindOptionsAutocompleteConfig, LiteAutocompleteConfig } from './Lines/AutoCompleteConfig'

import { EntityBaseController } from './Lines/EntityBase'
export { EntityBaseController }

export { EntityLine } from './Lines/EntityLine'

export { EntityCombo } from './Lines/EntityCombo'

export { EntityDetail } from './Lines/EntityDetail'

export { EntityList } from './Lines/EntityList'

export { EntityRepeater } from './Lines/EntityRepeater'

export { EntityTabRepeater } from './Lines/EntityTabRepeater'

export { EntityStrip } from './Lines/EntityStrip'

export { EntityCheckboxList } from './Lines/EntityCheckboxList'

export { EnumCheckboxList } from './Lines/EnumCheckboxList'
export { MultiValueLine } from './Lines/MultiValueLine'


import { EntityTable, EntityTableColumn, EntityTableRow } from './Lines/EntityTable'

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
export function taskSetFormat(lineBase: LineBaseController<any>, state: LineBaseProps) {
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
export function taskSetReadOnly(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (!state.ctx.readOnly &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.isReadOnly) {
    state.ctx.readOnly = true;
  }
}

tasks.push(taskSetMandatory);
export function taskSetMandatory(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.required) {
    state.mandatory = true;
  }
}


tasks.push(taskSetMove);
export function taskSetMove(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof EntityListBaseController &&
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
