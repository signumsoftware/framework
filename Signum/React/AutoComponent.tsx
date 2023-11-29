import * as React from 'react'
import { Dic } from './Globals'
import { tryGetTypeInfos, TypeReference, TypeInfo, IsByAll, isTypeModel } from './Reflection'
import { ModifiableEntity } from './Signum.Entities'
import * as Navigator from './Navigator'
import { ViewReplacer } from './Frames/ReactVisitor'
import { EntityLine, EntityCombo, EntityDetail, EntityStrip, TypeContext, EntityCheckboxList, EnumCheckboxList, EntityTable, PropertyRoute } from './Lines'
import { AutoLine } from './Lines/AutoLine'

export default function AutoComponent({ ctx, viewName }: { ctx: TypeContext<ModifiableEntity>, viewName?: string }) {
  const subContexts = subContext(ctx);
  const components = subContexts.filter(ctx => ctx.propertyRoute?.member?.name != "Id").map(ctx => <AutoLine ctx={ ctx}/>).filter(a => !!a).map(a => a!);
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


AutoComponent.withViewOverrides = true;
