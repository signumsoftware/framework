import * as React from 'react'
import { IsByAll, MemberInfo, PropertyRoute, PseudoType, Type, TypeInfo, TypeReference, isTypeEnum, isTypeModel, tryGetTypeInfos } from '../Reflection'
import { LineBaseController, LineBaseProps, tasks } from '../Lines/LineBase'
import { CheckboxLine, ColorLine, DateTimeLine, DateTimeLineController, EntityCheckboxList, EntityCombo, EntityDetail, EntityLine, EntityMultiSelect, EntityRepeater, EntityStrip, EntityTable, EnumCheckboxList, EnumLine, GuidLine, MultiValueLine, NumberLine, NumberLineController, PasswordLine, TextAreaLine, TextBoxLine, TimeLine, TypeContext } from '../Lines'
import { Entity, Lite, ModifiableEntity } from '../Signum.Entities'

export interface AutoLineProps extends LineBaseProps {
  unit?: string;
  format?: string;
  extraButtons?: (vl: any) => React.ReactNode; /* Not always implemented */
  extraButtonsBefore?: (vl: any) => React.ReactNode; /* Not always implemented */
}


export function AutoLine(p: AutoLineProps): React.ReactElement | null {
  const pr = p.ctx.propertyRoute;

  if (p.type == null && pr == null)
    return <span className="text-danger">Unable to render AutoLine with type = null and propertyRoute = null</span>;

  const factory = React.useMemo(() => AutoLine.getComponentFactory(p.type ?? pr!.typeReference(), pr), [pr?.propertyPath(), p.type?.name]);

  return factory(p);
}

export interface AutoLineFactoryRule {
  name: string;
  factory: (tr: TypeReference, pr?: PropertyRoute) => undefined | ((p: AutoLineProps) => React.ReactElement<any>);
}

export namespace AutoLine {
  const customTypeComponent: {
    [typeName: string]: AutoLineFactoryRule[];
  } = {};

  export function registerComponent(type: string, factory: (tr: TypeReference, pr?: PropertyRoute) => undefined | ((p: AutoLineProps) => React.ReactElement<any>), name?: string) {
    (customTypeComponent[type] ??= []).push({ name: name ?? type, factory });
  }

  export function getComponentFactory(tr: TypeReference, pr?: PropertyRoute): (props: AutoLineProps) => React.ReactElement<any> {

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
          return p => <EntityCheckboxList {...p} />; //Alternative <EntityCheckboxList {...p} />

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
        if (pr?.member!.format == "Password")
          return p => <PasswordLine {...p} />;

        if (pr?.member!.format == "Color")
          return p => <ColorLine {...p} />;

        if (pr?.member!.isMultiline)
          return p => <TextAreaLine {...p} />;

        return p => <TextBoxLine {...p} />;
      }

      if (tr.name == "Guid")
        return p => <GuidLine {...p} />;

      if (tr.name == "number" || tr.name == "decimal")
        return p => <NumberLine {...p} />;

      if (tr.name == "TimeSpan" || tr.name == "TimeOnly")
        return p => <TimeLine {...p} />;

      return p => <span className="text-danger">Not supported type {tr.name} by AutoLine</span>;
    }
  }
}


