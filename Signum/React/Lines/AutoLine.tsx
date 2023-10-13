import * as React from 'react'
import { IsByAll, MemberInfo, PropertyRoute, PseudoType, Type, TypeInfo, TypeReference, isTypeEnum, isTypeModel, tryGetTypeInfos } from '../Reflection'
import { LineBaseController, LineBaseProps, tasks } from '../Lines/LineBase'
import { CheckBoxLine, DateTimeLine, DateTimeLineController, DecimalLine, EntityCheckboxList, EntityCombo, EntityDetail, EntityLine, EntityRepeater, EntityStrip, EntityTable, EnumCheckboxList, EnumLine, GuidLine, MultiValueLine, NumberLine, NumberLineController, PasswordLine, TextBoxLine, TimeLine, TypeContext } from '../Lines'
import { Entity, Lite, ModifiableEntity } from '../Signum.Entities'

export interface AutoLineProps extends LineBaseProps {
  unit?: React.ReactChild;
  format?: string;
}


export function AutoLine(p: AutoLineProps) {
  const pr = p.ctx.propertyRoute;

  const factory = React.useMemo(() => AutoLine.getComponentFactory(p.type ?? pr!.typeReference(), pr), [pr?.propertyPath(), p.type?.name]);

  return factory(p);
}

export interface AutoLineFactoryRule {
  name: string;
  factory: (tr: TypeReference, pr?: PropertyRoute) => undefined | ((p: AutoLineProps) => React.ReactElement<any> | undefined);
}

export type AutoProps<T> = Omit<AutoLineProps, "ctx"> & { ctx: TypeContext<T> };

export namespace AutoLine {
  const customTypeComponent: {
    [typeName: string]: AutoLineFactoryRule[];
  } = {};

  export function registerCustomTypeComponent(type: PseudoType, factory: (tr: TypeReference, pr?: PropertyRoute) => undefined | ((p: AutoLineProps) => React.ReactElement<any> | undefined), name?: string) {
    (customTypeComponent[type.toString()] ??= []).push({ name: name ?? type.toString(), factory });
  }

  export function registerCustomEntityComponent<T extends ModifiableEntity>(type: Type<T>, factory: (props: AutoProps<T>) => React.ReactElement | undefined, name?: string) {
  }

  export function registerCustomLiteComponent<T extends Entity>(type: Type<T>, factory: (props: AutoProps<T>) => React.ReactElement | undefined, name?: string) {
  }

  export function registerCustomMListEntityComponent<T extends ModifiableEntity>(type: Type<T>, factory: (props: AutoProps<T>) => React.ReactElement | undefined, name?: string) {
  }

  export function registerCustomMListLiteComponent<T extends Entity>(type: Type<T>, factory: (props: AutoProps<T>) => React.ReactElement | undefined, name?: string) {
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
        return p => <CheckBoxLine {...p} />;

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

      if (tr.name == "number")
        return p => <NumberLine {...p} />;

      if (tr.name == "decimal")
        return p => <DecimalLine {...p} />;

      if (tr.name == "TimeSpan" || tr.name == "TimeOnly")
        return p => <TimeLine {...p} />;

      return p => <span className="text-alert">Not supported type by AutoLine</span>;
    }
  }

}

tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof DateTimeLineController || lineBase instanceof NumberLineController) {
    const vProps = state as AutoLineProps;

    if (!vProps.format &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.format = state.ctx.propertyRoute.member!.format;
    }
  }
}
