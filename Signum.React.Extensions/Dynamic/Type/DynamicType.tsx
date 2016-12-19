import * as React from 'react'
import { Link } from 'react-router'
import { Button } from 'react-bootstrap'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { DynamicTypeEntity, DynamicTypeMessage, DynamicSqlMigrationMessage, DynamicPanelPermission } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute, symbolNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import * as DynamicTypeClient from '../DynamicTypeClient'
import { DynamicTypeDefinitionComponent, PropertyRepeaterComponent } from './DynamicTypeDefinitionComponent'

interface DynamicTypeComponentProps {
    ctx: TypeContext<DynamicTypeEntity>;
}

interface DynamicTypeComponentState {
    typeDefinition?: DynamicTypeClient.DynamicTypeDefinition;
    showDatabaseMapping?: boolean;
}

export default class DynamicTypeComponent extends React.Component<DynamicTypeComponentProps, DynamicTypeComponentState> {

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

    parseDefinition() {

        const ctx = this.props.ctx;

        const def = !ctx.value.typeDefinition ?
            { baseType: "Entity", entityData: "Transactional", entityKind: "Main", properties: [], queryFields: ["Id"], registerSave: true, registerDelete: true } as DynamicTypeClient.DynamicTypeDefinition :
            JSON.parse(ctx.value.typeDefinition) as DynamicTypeClient.DynamicTypeDefinition;

        this.setState({
            typeDefinition: def,
            showDatabaseMapping: !!def.tableName || def.properties.some(p => !!p.columnName || !!p.columnType)
        });
    }

   
 
    render() {
        const ctx = this.props.ctx;
        const dc = { refreshView: () => this.forceUpdate() };

        return (
            <div>
                <div className="row">
                    <div className="col-sm-8">
                        <ValueLine ctx={ctx.subCtx(dt => dt.typeName)} labelColumns={3} onChange={() => this.forceUpdate()} unitText="Entity" />
                    </div>
                    <div className="col-sm-4">
                        <button className={classes("btn btn-xs btn-success pull-right", this.state.showDatabaseMapping && "active")}
                            onClick={() => this.setState({ showDatabaseMapping: !this.state.showDatabaseMapping })} >
                            Show Database Mapping
                        </button>
                    </div>
                </div>
                {this.state.typeDefinition &&
                    <DynamicTypeDefinitionComponent dc={dc}
                        dynamicType={ctx.value}
                        definition={this.state.typeDefinition}
                        showDatabaseMapping={this.state.showDatabaseMapping!}
                        />
                }
            </div>
        );
    }
}
