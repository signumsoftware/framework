import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import { Constructor } from '@framework/Constructor'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import { AutoLine, EntityLine, TypeContext } from '@framework/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage, getToString } from '@framework/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute, symbolNiceName, getQueryNiceName } from '@framework/Reflection'
import { DynamicTypeClient } from '../DynamicTypeClient'
import { Typeahead } from '@framework/Components';
import { useForceUpdate } from '@framework/Hooks'
import { DynamicMixinConnectionEntity } from '../Signum.Dynamic.Mixins'
import { DynamicTypeEntity } from '../Signum.Dynamic.Types'

interface DynamicMixinConnectionComponentProps {
  ctx: TypeContext<DynamicMixinConnectionEntity>;
}

export default function DynamicMixinConnectionComponent(p : DynamicMixinConnectionComponentProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;

  return (
    <div>
      <MixinCombo
        binding={Binding.create(ctx.value, a => a.mixinName)}
        label={ctx.niceName(a => a.mixinName)}
        labelColumns={2}
        onChange={() => forceUpdate()} />
      <EntityLine ctx={ctx.subCtx(dt => dt.entityType)} />
    </div>
  );
}

export interface MixinComboProps {
  binding: Binding<string | null | undefined>;
  label: string;
  labelColumns: number;
  onChange?: () => void;
}

export function MixinCombo(p : MixinComboProps){
  const forceUpdate = useForceUpdate();
  function handleGetItems(query: string) {
    return Finder.fetchLites({
      queryName: DynamicTypeEntity,
      filterOptions: [
        { token: DynamicTypeEntity.token(e => e.entity.baseType), operation: "EqualTo", value: "MixinEntity" },
        { token: DynamicTypeEntity.token(e => e.entity.typeName), operation: "StartsWith", value: query },
      ],
      orderOptions: [],
      count: 5
    }).then(lites => lites?.map(a => getToString(a)));
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
        {p.label}
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
