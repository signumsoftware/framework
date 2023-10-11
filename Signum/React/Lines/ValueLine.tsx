import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { DatePicker, DropdownList, Combobox } from 'react-widgets'
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { Dic, addClass, classes } from '../Globals'
import { MemberInfo, TypeReference, toLuxonFormat, toNumberFormat, isTypeEnum, timeToString, tryGetTypeInfo, toFormatWithFixes, splitLuxonFormat, dateTimePlaceholder, timePlaceholder, toLuxonDurationFormat } from '../Reflection'
import { LineBaseController, LineBaseProps, tasks, useController } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { KeyCodes } from '../Components/Basic';
import { getTimeMachineIcon } from './TimeMachineIcon'

tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof ValueLineController) {
    const vProps = state as ValueLineProps;

    if (vProps.unit === undefined &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.unit = state.ctx.propertyRoute.member!.unit;
    }
  }
}

tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof ValueLineController) {
    const vProps = state as ValueLineProps;

    if (!vProps.format &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.format = state.ctx.propertyRoute.member!.format;
      if (vProps.valueLineType == "TextBox" && state.ctx.propertyRoute.member!.format == "Password")
        vProps.valueLineType = "Password";
      else if (vProps.valueLineType == "TextBox" && state.ctx.propertyRoute.member!.format == "Color")
        vProps.valueLineType = "Color";
    }
  }
}
