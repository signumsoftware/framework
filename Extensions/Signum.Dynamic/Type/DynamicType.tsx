import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import { Constructor } from '@framework/Constructor'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import { IHasChanges } from '@framework/TypeContext'
import { AutoLine, EntityLine, TypeContext } from '@framework/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '@framework/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute, symbolNiceName, GraphExplorer } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import { DynamicTypeClient } from '../DynamicTypeClient'
import { DynamicTypeDefinitionComponent, PropertyRepeaterComponent } from './DynamicTypeDefinitionComponent'
import { DynamicTypeEntity } from '../Signum.Dynamic.Types'
import { JSX } from 'react'

interface DynamicTypeComponentProps {
  ctx: TypeContext<DynamicTypeEntity>;
}

interface DynamicTypeComponentState {
  typeDefinition?: DynamicTypeClient.DynamicTypeDefinition;
  showDatabaseMapping?: boolean;
}

export default class DynamicTypeComponent extends React.Component<DynamicTypeComponentProps, DynamicTypeComponentState> implements IHasChanges {

  constructor(props: DynamicTypeComponentProps) {
    super(props);

    this.state = {};
  }

  componentWillMount() : void {
    this.parseDefinition();
  }

  beforeSave(): void {
    const ctx = this.props.ctx;
    this.state.typeDefinition!.properties.forEach(a => delete a._propertyType_);
    ctx.value.typeDefinition = JSON.stringify(this.state.typeDefinition);
    ctx.value.modified = true;
  }

  entityHasChanges(): boolean {
    const entity = this.props.ctx.value;

    GraphExplorer.propagateAll(entity);

    var clone = JSON.parse(JSON.stringify(this.state.typeDefinition)) as DynamicTypeClient.DynamicTypeDefinition;
    clone.properties.forEach(a => delete a._propertyType_);
    return entity.modified || entity.typeDefinition != JSON.stringify(clone);
  }

  parseDefinition(): void{

    const ctx = this.props.ctx;

    const def = !ctx.value.typeDefinition ?
      { entityData: "Transactional", entityKind: "Main", properties: [], queryFields: ["e.Id"], registerSave: true, registerDelete: true } as DynamicTypeClient.DynamicTypeDefinition :
      JSON.parse(ctx.value.typeDefinition) as DynamicTypeClient.DynamicTypeDefinition;

    this.setState({
      typeDefinition: def,
      showDatabaseMapping: !!def.tableName || def.properties.some(p => !!p.columnName || !!p.columnType)
    });
  }

  render(): JSX.Element {
    const ctx = this.props.ctx;
    const dc = { refreshView: () => this.forceUpdate() };

    const suffix =
      ctx.value.baseType == "MixinEntity" ? "Mixin" :
        ctx.value.baseType == "EmbeddedEntity" ? "Embedded" :
          ctx.value.baseType == "ModelEntity" ? "Model" :
            "Entity";

    return (
      <div>
        <div className="row">
          <div className="col-sm-8">
            <AutoLine ctx={ctx.subCtx(dt => dt.baseType)} labelColumns={3} onChange={() => this.forceUpdate()} readOnly={!ctx.value.isNew} />
            <AutoLine ctx={ctx.subCtx(dt => dt.typeName)} labelColumns={3} onChange={() => this.forceUpdate()} unit={suffix} />
          </div>
          <div className="col-sm-4">
            <button className={classes("btn btn-sm btn-success float-end", this.state.showDatabaseMapping && "active")}
              onClick={() => this.setState({ showDatabaseMapping: !this.state.showDatabaseMapping })}>
              Show Database Mapping
                        </button>
          </div>
        </div>
        {this.state.typeDefinition &&
          <DynamicTypeDefinitionComponent
            dc={dc}
            dynamicType={ctx.value}
            definition={this.state.typeDefinition}
            showDatabaseMapping={this.state.showDatabaseMapping!}
          />
        }
      </div>
    );
  }
}
