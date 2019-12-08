import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { DynamicTypeEntity, DynamicTypeMessage, DynamicMixinConnectionEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext, LiteAutocompleteConfig } from '@framework/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '@framework/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute, symbolNiceName, getQueryNiceName } from '@framework/Reflection'
import * as DynamicTypeClient from '../DynamicTypeClient'
import { Typeahead } from '@framework/Components';
import { useForceUpdate } from '@framework/Hooks'

interface DynamicMixinConnectionComponentProps {
  ctx: TypeContext<DynamicMixinConnectionEntity>;
}

export default function DynamicMixinConnectionComponent(p : DynamicMixinConnectionComponentProps){
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;

  return (
    <div>
      <MixinCombo
        binding={Binding.create(ctx.value, a => a.mixinName)}
        labelText={ctx.niceName(a => a.mixinName)}
        labelColumns={2}
        onChange={() => forceUpdate()} />
      <EntityLine ctx={ctx.subCtx(dt => dt.entityType)} />
    </div>
  );
}

export interface MixinComboProps {
  binding: Binding<string | null | undefined>;
  labelText: string;
  labelColumns: number;
  onChange?: () => void;
}

export function MixinCombo(p : MixinComboProps){
  const forceUpdate = useForceUpdate();
  function handleGetItems(query: string) {
    return Finder.fetchEntitiesWithFilters(
      DynamicTypeEntity,
      [
        { token: DynamicTypeEntity.token().entity(e => e.baseType), operation: "EqualTo", value: "MixinEntity" },
        { token: DynamicTypeEntity.token().entity(e => e.typeName), operation: "StartsWith", value: query },
      ], [], 5)
      .then(lites => lites?.map(a => a.toStr));
  }

  function handleOnChange(newValue: string) {
    p.binding.setValue(newValue);
    forceUpdate();

    if (p.onChange)
      p.onChange();
  }

  let lc = p.labelColumns;
  return (
    <div className="form-group form-group-sm row" >
      <label className={classes("col-form-label col-form-label-sm", "col-sm-" + (lc == null ? 2 : lc))}>
        {p.labelText}
      </label>
      <div className={"col-sm-" + (lc == null ? 10 : 12 - lc)}>
        <Typeahead
          inputAttrs={{ className: "form-control form-control-sm sf-entity-autocomplete" }}
          getItems={handleGetItems}
          value={p.binding.getValue() ?? ""}
          onChange={handleOnChange} />
      </div>
    </div>
  );
}
