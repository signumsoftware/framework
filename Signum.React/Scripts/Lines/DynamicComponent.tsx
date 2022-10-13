import * as React from 'react'
import { Dic } from '../Globals'
import { tryGetTypeInfos, TypeReference, TypeInfo, IsByAll, isTypeModel } from '../Reflection'
import { ModifiableEntity } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import { ViewReplacer } from '../Frames/ReactVisitor'
import { ValueLine, EntityLine, EntityCombo, EntityDetail, EntityStrip, TypeContext, EntityCheckboxList, EnumCheckboxList, EntityTable, PropertyRoute } from '../Lines'
import { Type } from '../Reflection';
import { EntityRepeater } from './EntityRepeater';
import { MultiValueLine } from './MultiValueLine';
import { ValueLineController } from './ValueLine';

export default function DynamicComponent({ ctx, viewName }: { ctx: TypeContext<ModifiableEntity>, viewName?: string }) {
  const subContexts = subContext(ctx);
  const components = subContexts.map(ctx => getAppropiateComponent(ctx)).filter(a => !!a).map(a => a!);
  const result = React.createElement("div", undefined, ...components);
  const es = Navigator.getSettings(ctx.value.Type);

  var vos = es?.viewOverrides?.filter(a => a.viewName == viewName); //Should user viewDispatcher.getViewOverrides promise instead

  if (vos?.length) {
    const replacer = new ViewReplacer(result, ctx);
    vos.forEach(vo => vo.override(replacer));
    return replacer.result;
  } else {
    return result;
  }

  function subContext(ctx: TypeContext<ModifiableEntity>): TypeContext<any>[] {
    const members = ctx.propertyRoute!.subMembers();
    const result = Dic.map(members, (field, m) => ctx.subCtx(field));

    return result;
  }
}

export const customTypeComponent: {
  [typeName: string]: (ctx: TypeContext<any>) => React.ReactElement<any> | null | undefined | "continue";
} = {};

export const customPropertyComponent: {
  [propertyRoute: string]: (ctx: TypeContext<any>) => React.ReactElement<any> | null | undefined;
} = {};

export function registerCustomPropertyComponent<T extends ModifiableEntity, V>(type: Type<T>, property: (e: T) => V, component: (ctx: TypeContext<any>) => React.ReactElement<any> | undefined) {

  var pi = type.tryPropertyRoute(property);
  if (pi == null)
    return;

  customPropertyComponent[pi.toString()] = component;
}

export function getAppropiateComponent(ctx: TypeContext<any>): React.ReactElement<any> | undefined {
  return getAppropiateComponentFactory(ctx.propertyRoute!)(ctx);
}

export function getAppropiateComponentFactory(pr: PropertyRoute): (ctx: TypeContext<any>) => React.ReactElement<any> | undefined {
  const mi = pr.member;
  if (mi && (mi.name == "Id" || mi.notVisible == true))
    return ctx => undefined;

  const ccProp = customPropertyComponent[pr.toString()];
  if (ccProp) {
    return ctx => ccProp(ctx) ?? undefined;
  }

  const tr = pr.typeReference();
  const ccType = customTypeComponent[tr.name];
  if (ccType) {
    var basic = getAppropiateComponentFactoryBasic(tr);

    return ctx => {
      var result = ccType(ctx);
      return result == "continue" ? basic(ctx) : result ?? undefined;
    };
  }

  return getAppropiateComponentFactoryBasic(tr);
}

export function getAppropiateComponentFactoryBasic(tr: TypeReference): (ctx: TypeContext<any>) => React.ReactElement<any> | undefined {
  let tis = tryGetTypeInfos(tr) as TypeInfo[];
  if (tis.length == 1 && tis[0] == undefined)
    tis = [];

  if (tr.isCollection) {
    if (tr.name == IsByAll)
      return ctx => <EntityStrip ctx={ctx} />;

    if (tis.length) {
      if (tis.length == 1 && tis.first().kind == "Enum")
        return ctx => <EnumCheckboxList ctx={ctx} />;

      if (tis.length == 1 && (tis.first().entityKind == "Part" || tis.first().entityKind == "SharedPart" || isTypeModel(tis.first())) && !tr.isLite)
        return ctx => <EntityTable ctx={ctx} />;

      if (tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart"))
        return ctx => <EntityRepeater ctx={ctx} />;

      if (tis.every(t => t.isLowPopulation == true))
        return ctx => <EntityCheckboxList ctx={ctx} />;

      return ctx => <EntityStrip ctx={ctx} />;
    }

    if (tr.isEmbedded)
      return ctx => <EntityTable ctx={ctx} />;

    return ctx => <MultiValueLine ctx={ctx} />;

  } else {

    if (tr.name == "[ALL]")
      return ctx => <EntityLine ctx={ctx} />;

    if (tis.length) {
      if (tis.length == 1 && tis.first().kind == "Enum")
        return ctx => <ValueLine ctx={ctx} />;

      if (tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart") && !tr.isLite)
        return ctx => <EntityDetail ctx={ctx} />;

      if (tis.every(t => t.isLowPopulation == true))
        return ctx => <EntityCombo ctx={ctx} />;

      return ctx => <EntityLine ctx={ctx} />;
    }

    if (tr.isEmbedded)
      return ctx => <EntityDetail ctx={ctx} />;

    if (ValueLineController.getValueLineType(tr) != undefined)
      return ctx => <ValueLine ctx={ctx} />;

    return ctx => undefined;
  }
}

DynamicComponent.withViewOverrides = true;
