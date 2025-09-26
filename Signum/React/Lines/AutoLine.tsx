import * as React from 'react'
import { IsByAll, MemberInfo, PropertyRoute, PseudoType, Type, TypeInfo, TypeReference, isNumberType, isTypeEnum, isTypeModel, tryGetTypeInfos } from '../Reflection'
import { LineBaseController, LineBaseProps, tasks } from '../Lines/LineBase'
import { CheckboxLine, ColorLine, DateTimeLine, DateTimeLineController, EntityCheckboxList, EntityCombo, EntityDetail, EntityLine, EntityMultiSelect, EntityRepeater, EntityStrip, EntityTable, EnumCheckboxList, EnumLine, GuidLine, MultiValueLine, NumberLine, NumberLineController, PasswordLine, TextAreaLine, TextBoxLine, TimeLine, TypeContext } from '../Lines'
import { Entity, Lite, ModifiableEntity } from '../Signum.Entities'
import { isNumber } from '../Globals'

export interface AutoLineProps extends LineBaseProps<any> {
  propertyRoute?: PropertyRoute; //For AutoLineModal
  valueHtmlAttributes?: React.HTMLAttributes<any>;
}


export function AutoLine(p: AutoLineProps): React.ReactElement | null {
  const pr = p.ctx.propertyRoute;

  var isHidden = p.type == null && pr == null || p.visible == false || p.hideIfNull && (p.ctx.value == undefined || p.ctx.value == "");
  if(isHidden)
    return null;


  const factory = React.useMemo(() => AutoLine.getComponentFactory(p.type ?? pr!.typeReference(), p.propertyRoute ?? pr), [(p.propertyRoute ?? pr)?.propertyPath(), p.type?.name]);

  return factory(p);
}

export interface AutoLineFactoryRule {
  name: string;
  factory: (tr: TypeReference, pr?: PropertyRoute) => undefined | ((p: AutoLineProps) => React.ReactElement);
}

export namespace AutoLine {
  const customTypeComponent: {
    [typeName: string]: AutoLineFactoryRule[];
  } = {};

  export function registerComponent(type: string, factory: (tr: TypeReference, pr?: PropertyRoute) => undefined | ((p: AutoLineProps) => React.ReactElement), name?: string): void {
    (customTypeComponent[type] ??= []).push({ name: name ?? type, factory });
  }

  export function getComponentFactory(tr: TypeReference, pr?: PropertyRoute, options?: { format?: string, multiLine?: boolean }): (props: AutoLineProps) => React.ReactElement {

    const customs = customTypeComponent[tr.name]?.map(rule => rule.factory(tr, pr)).notNull().first();

    if (customs != null)
      return customs

    let tis = tryGetTypeInfos(tr) as TypeInfo[];
    if (tis.length == 1 && tis[0] == undefined)
      tis = [];

    if (tr.isCollection) {
      if (tr.name == IsByAll)
        return p => <EntityStrip {...p} />; 

      if (tis.length) {
        if (tis.length == 1 && tis.first().kind == "Enum")
          return p => <EnumCheckboxList {...p} />; 

        if (tis.length == 1 && (tis.first().entityKind == "Part" || tis.first().entityKind == "SharedPart" || isTypeModel(tis.first())) && !tr.isLite)
          return p => <EntityTable {...p} />;

        if (tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart"))
          return p => <EntityRepeater {...p} />;

        if (tis.every(t => t.isLowPopulation == true))
          return p => <EntityCheckboxList {...p} />; 

        return p => <EntityStrip {...p} />; 
      }

      if (tr.isEmbedded)
        return p => <EntityTable {...p} />;

      return p => <MultiValueLine {...p} />;

    } else {

      if (tr.name == IsByAll)
        return p => <EntityLine {...p} />; 

      if (tis.length) {
        if (tis.length == 1 && tis.first().kind == "Enum")
          return p => <EnumLine {...p} />; 

        if (tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart") && !tr.isLite)
          return p => <EntityDetail {...p} />;

        if (tis.every(t => t.isLowPopulation == true))
          return p => <EntityCombo {...p} />;

        return p => <EntityLine {...p} />; 
      }

      if (tr.isEmbedded)
        return p => <EntityDetail {...p} />;

      if (isTypeEnum(tr.name) || tr.name == "boolean" && !tr.isNotNullable)
        return p => <EnumLine {...p} />; 

      if (tr.name == "boolean")
        return p => <CheckboxLine {...p} />; 

      if (tr.name == "DateTime" || tr.name == "DateTimeOffset" || tr.name == "DateOnly")
        return p => <DateTimeLine {...p} />; 

      if (tr.name == "string") {
        var member = pr == null ? null : pr.propertyRouteType == "MListItem" ? pr.parent?.member : pr.member;
        if (member?.format == "Password")
          return p => <PasswordLine {...p} />; 

        if (member?.format == "Color")
          return p => <ColorLine {...p} />; 

        if (member?.isMultiline)
          return p => <TextAreaLine {...p} />; 

        return p => <TextBoxLine {...p} />; 
      }

      if (tr.name == "Guid")
        return p => <GuidLine {...p} />; 

      if (isNumberType(tr.name))
        return p => <NumberLine {...p} />; 

      if (tr.name == "TimeSpan" || tr.name == "TimeOnly")
        return p => <TimeLine {...p} />; 

      return p => <span className="text-danger">Not supported type {tr.name} by AutoLine</span>;
    }
  }
}


