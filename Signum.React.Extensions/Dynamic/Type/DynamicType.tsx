import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { DynamicTypeEntity, DynamicTypeMessage, DynamicSqlMigrationMessage, DynamicPanelPermission } from '../Signum.Entities.Dynamic'
import { IHasChanges } from '@framework/TypeContext'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '@framework/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute, symbolNiceName, GraphExplorer } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import * as DynamicTypeClient from '../DynamicTypeClient'
import { DynamicTypeDefinitionComponent, PropertyRepeaterComponent } from './DynamicTypeDefinitionComponent'

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

  componentWillMount() {
    this.parseDefinition();
  }

  beforeSave() {
    const ctx = this.props.ctx;
    this.state.typeDefinition!.properties.forEach(a => delete a._propertyType_);
    ctx.value.typeDefinition = JSON.stringify(this.state.typeDefinition);
    ctx.value.modified = true;
  }

  entityHasChanges() {
    const entity = this.props.ctx.value;

    GraphExplorer.propagateAll(entity);

    var clone = JSON.parse(JSON.stringify(this.state.typeDefinition)) as DynamicTypeClient.DynamicTypeDefinition;
    clone.properties.forEach(a => delete a._propertyType_);
    return entity.modified || entity.typeDefinition != JSON.stringify(clone);
  }

  parseDefinition() {

    const ctx = this.props.ctx;

    const def = !ctx.value.typeDefinition ?
      { entityData: "Transactional", entityKind: "Main", properties: [], queryFields: ["e.Id"], registerSave: true, registerDelete: true } as DynamicTypeClient.DynamicTypeDefinition :
      JSON.parse(ctx.value.typeDefinition) as DynamicTypeClient.DynamicTypeDefinition;

    this.setState({
      typeDefinition: def,
      showDatabaseMapping: !!def.tableName || def.properties.some(p => !!p.columnName || !!p.columnType)
    });
  }

  render() {
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
            <ValueLine ctx={ctx.subCtx(dt => dt.baseType)} labelColumns={3} onChange={() => this.forceUpdate()} readOnly={!ctx.value.isNew} />
            <ValueLine ctx={ctx.subCtx(dt => dt.typeName)} labelColumns={3} onChange={() => this.forceUpdate()} unit={suffix} />
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
