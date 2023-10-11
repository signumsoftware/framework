import * as React from 'react'
import { MemberInfo, isTypeEnum } from '../Reflection'
import { LineBaseController, LineBaseProps } from '../Lines/LineBase'
import { EnumLine } from './EnumLine'
import { CheckBoxLine } from './CheckBoxLine'
import { DateTimeLine, TimeLine } from './DateTimeLine'
import { GuidLine, TextBoxLine } from './TextBoxLine'
import { DecimalLine, NumberLine } from './NumberLine'

export interface AutoLineProps extends LineBaseProps {
  unit?: React.ReactChild;
  format?: string;
}

export function AutoLine(props: AutoLineProps) {
  var t = props.type;

  if (t == null)
    return null;

  if (t.isCollection || t.isLite)
    return null;

  if (isTypeEnum(t.name) || t.name == "boolean" && !t.isNotNullable)
    return <EnumLine {...props} />;

  if (t.name == "boolean")
    return <CheckBoxLine {...props} />;

  if (t.name == "DateTime" || t.name == "DateTimeOffset" || t.name == "DateOnly")
    return <DateTimeLine {...props} />;

  if (t.name == "string")
    return <TextBoxLine {...props} />;

  if (t.name == "Guid")
    return <GuidLine {...props} />;

  if (t.name == "number")
    return <NumberLine {...props} />;

  if (t.name == "decimal")
    return <DecimalLine {...props} />;

  if (t.name == "TimeSpan" || t.name == "TimeOnly")
    return <TimeLine {...props} />;

  return <span className="text-alert">Not supported type by AutoLine</span>;
}

