import * as React from 'react'
import { Dic } from '../Globals'
import { getTypeInfos, TypeReference } from '../Reflection'
import { ModifiableEntity } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import { ViewReplacer } from '../Frames/ReactVisitor'
import { ValueLine, EntityLine, EntityCombo, EntityDetail, EntityStrip, TypeContext, EntityCheckboxList, EnumCheckboxList, EntityTable, PropertyRoute } from '../Lines'
import { Type } from '../Reflection';
import { EntityRepeater } from './EntityRepeater';
import { MultiValueLine } from './MultiValueLine';
import { faUnderline } from '@fortawesome/free-solid-svg-icons';

export default class DynamicComponent extends React.Component<{ ctx: TypeContext<ModifiableEntity>, viewName?: string }> {
  render() {
    const subContexts = this.subContext(this.props.ctx);
    const components = subContexts.map(ctx => DynamicComponent.getAppropiateComponent(ctx)).filter(a => !!a).map(a => a!);
    const result = React.createElement("div", undefined, ...components);
    const es = Navigator.getSettings(this.props.ctx.value.Type);

    var vos = es && es.viewOverrides && es.viewOverrides.filter(a => a.viewName == this.props.viewName); //Should user viewDispatcher.getViewOverrides promise instead

    if (vos && vos.length) {
      const replacer = new ViewReplacer(result, this.props.ctx);
      vos.forEach(vo => vo.override(replacer));
      return replacer.result;
    } else {
      return result;
    }
  }

  subContext(ctx: TypeContext<ModifiableEntity>): TypeContext<any>[] {
    const members = ctx.propertyRoute.subMembers();
    const result = Dic.map(members, (field, m) => ctx.subCtx(field));

    return result;
  }

  static customTypeComponent: {
    [typeName: string]: (ctx: TypeContext<any>) => React.ReactElement<any> | null | undefined | "continue";
  } = {};

  static customPropertyComponent: {
    [propertyRoute: string]: (ctx: TypeContext<any>) => React.ReactElement<any> | null | undefined;
  } = {};

  static registerCustomPropertyComponent<T extends ModifiableEntity, V>(type: Type<T>, property: (e: T) => V, component: (ctx: TypeContext<any>) => React.ReactElement<any> | undefined) {
    DynamicComponent.customPropertyComponent[type.propertyRoute(property).toString()] = component;
  }

  static getAppropiateComponent(ctx: TypeContext<any>): React.ReactElement<any> | undefined {
    return DynamicComponent.getAppropiateComponentFactory(ctx.propertyRoute)(ctx);
  }

  static getAppropiateComponentFactory(pr: PropertyRoute): (ctx: TypeContext<any>) => React.ReactElement<any> | undefined {
    const mi = pr.member;
    if (mi && (mi.name == "Id" || mi.notVisible == true))
      return ctx => undefined;

    const ccProp = DynamicComponent.customPropertyComponent[pr.toString()];
    if (ccProp) {
      return ctx => ccProp(ctx) || undefined;
    }

    const tr = pr.typeReference();
    const ccType = DynamicComponent.customTypeComponent[tr.name];
    if (ccType) {
      var basic = DynamicComponent.getAppropiateComponentFactoryBasic(tr);

      return ctx => {
        var result = ccType(ctx);
        return result == "continue" ? basic(ctx) : result || undefined;
      };
    }

    return DynamicComponent.getAppropiateComponentFactoryBasic(tr);
  }

  static getAppropiateComponentFactoryBasic(tr: TypeReference): (ctx: TypeContext<any>) => React.ReactElement<any> | undefined {
    let tis = getTypeInfos(tr);
    if (tis.length == 1 && tis[0] == undefined)
      tis = [];

    if (tr.isCollection) {
      if (tr.name == "[ALL]")
        return ctx => <EntityStrip ctx={ctx} />;

      if (tis.length) {
        if (tis.length == 1 && tis.first().kind == "Enum")
          return ctx => <EnumCheckboxList ctx={ctx} />;

        if (tis.length == 1 && (tis.first().entityKind == "Part" || tis.first().entityKind == "SharedPart"))
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

        if (tis.every(t => t.entityKind == "Part" || t.entityKind == "SharedPart"))
          return ctx => <EntityDetail ctx={ctx} />;

        if (tis.every(t => t.isLowPopulation == true))
          return ctx => <EntityCombo ctx={ctx} />;

        return ctx => <EntityLine ctx={ctx} />;
      }

      if (tr.isEmbedded)
        return ctx =><EntityDetail ctx={ctx} />;

      if (ValueLine.getValueLineType(tr) != undefined)
        return ctx =><ValueLine ctx={ctx} />;

      return ctx => undefined;
    }
  }
}


(DynamicComponent.prototype.render as any).withViewOverrides = true;
