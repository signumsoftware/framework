import * as React from 'react'
import { IsByAll, MemberInfo, PropertyRoute, PseudoType, Type, TypeInfo, TypeReference, isTypeEnum, isTypeModel, tryGetTypeInfos } from '../Reflection'
import { LineBaseController, LineBaseProps, tasks } from '../Lines/LineBase'
import { CheckboxLine, DateTimeLine, DateTimeLineController, EntityCheckboxList, EntityCombo, EntityDetail, EntityLine, EntityRepeater, EntityStrip, EntityTable, EnumCheckboxList, EnumLine, GuidLine, MultiValueLine, NumberLine, NumberLineController, PasswordLine, TextBoxLine, TimeLine, TypeContext } from '../Lines'
import { Entity, Lite, ModifiableEntity } from '../Signum.Entities'

export interface AutoLineProps extends LineBaseProps {
  unit?: string;
  format?: string;
}


export function AutoLine(p: AutoLineProps) {
  const pr = p.ctx.propertyRoute;

  if (p.type == null && pr == null)
    return undefined;

  const factory = React.useMemo(() => AutoLine.getComponentFactory(p.type ?? pr!.typeReference(), pr), [pr?.propertyPath(), p.type?.name]);

  return factory(p);
}

export interface AutoLineFactoryRule {
  name: string;
  factory: (tr: TypeReference, pr?: PropertyRoute) => undefined | ((p: AutoLineProps) => React.ReactElement<any> | undefined);
}

export namespace AutoLine {
  const customTypeComponent: {
    [typeName: string]: AutoLineFactoryRule[];
  } = {};

  export function registerComponent(type: string, factory: (tr: TypeReference, pr?: PropertyRoute) => undefined | ((p: AutoLineProps) => React.ReactElement<any> | undefined), name?: string) {
    (customTypeComponent[type] ??= []).push({ name: name ?? type, factory });
  }

  export function getComponentFactory(tr: TypeReference, pr?: PropertyRoute): (props: AutoLineProps) => React.ReactElement<any> | undefined {

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

      if (tr.name == "[ALL]")
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
        if (pr?.member!.format == "Password")
          return p => <PasswordLine {...p} />;

        if (pr?.member!.format == "Color")
          return p => <PasswordLine {...p} />;

        return p => <TextBoxLine {...p} />;
      }

      if (tr.name == "Guid")
        return p => <GuidLine {...p} />;

      if (tr.name == "number" || tr.name == "decimal")
        return p => <NumberLine {...p} />;

      if (tr.name == "TimeSpan" || tr.name == "TimeOnly")
        return p => <TimeLine {...p} />;

      return p => <span className="text-danger">Not supported type by AutoLine</span>;
    }
  }

}


