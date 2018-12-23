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

interface DynamicMixinConnectionComponentProps {
  ctx: TypeContext<DynamicMixinConnectionEntity>;
}

export default class DynamicMixinConnectionComponent extends React.Component<DynamicMixinConnectionComponentProps> {

  render() {
    const ctx = this.props.ctx;

    return (
      <div>
        <MixinCombo
          binding={Binding.create(ctx.value, a => a.mixinName)}
          labelText={ctx.niceName(a => a.mixinName)}
          labelColumns={2}
          onChange={() => this.forceUpdate()} />
        <EntityLine ctx={ctx.subCtx(dt => dt.entityType)} />
      </div>
    );
  }
}

export interface MixinComboProps {
  binding: Binding<string | null | undefined>;
  labelText: string;
  labelColumns: number;
  onChange?: () => void;
}

export class MixinCombo extends React.Component<MixinComboProps>{

  handleGetItems = (query: string) => {
    return Finder.fetchEntitiesWithFilters(
      DynamicTypeEntity,
      [
        { token: DynamicTypeEntity.token().entity(e => e.baseType), operation: "EqualTo", value: "MixinEntity" },
        { token: DynamicTypeEntity.token().entity(e => e.typeName), operation: "StartsWith", value: query },
      ], [], 5)
      .then(lites => lites && lites.map(a => a.toStr));
  }

  handleOnChange = (newValue: string) => {
    this.props.binding.setValue(newValue);
    this.forceUpdate();

    if (this.props.onChange)
      this.props.onChange();
  }

  render() {
    let lc = this.props.labelColumns;
    return (
      <div className="form-group form-group-sm row" >
        <label className={classes("col-form-label col-form-label-sm", "col-sm-" + (lc == null ? 2 : lc))}>
          {this.props.labelText}
        </label>
        <div className={"col-sm-" + (lc == null ? 10 : 12 - lc)}>
          <div style={{ position: "relative" }}>
            <Typeahead
              inputAttrs={{ className: "form-control form-control-sm sf-entity-autocomplete" }}
              getItems={this.handleGetItems}
              value={this.props.binding.getValue() || ""}
              onChange={this.handleOnChange} />
          </div>
        </div>
      </div>);
  }
}
