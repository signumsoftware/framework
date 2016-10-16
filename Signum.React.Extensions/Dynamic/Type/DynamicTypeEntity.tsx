import * as React from 'react'
import { Button } from 'react-bootstrap'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { DynamicTypeEntity, DynamicTypeMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute } from '../../../../Framework/Signum.React/Scripts/Reflection'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import * as DynamicTypeClient from '../DynamicTypeClient'
import { DynamicTypeDefinitionComponent, PropertyRepeaterComponent } from './DynamicTypeDefinitionComponent'


interface DynamicTypeEntityComponentProps {
    ctx: TypeContext<DynamicTypeEntity>;
}

interface DynamicTypeEntityComponentState {
    typeDefinition?: DynamicTypeClient.DynamicTypeDefinition;
}

export default class DynamicTypeEntityComponent extends React.Component<DynamicTypeEntityComponentProps, DynamicTypeEntityComponentState> {

    constructor(props: DynamicTypeEntityComponentProps) {
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
            { baseType: "Entity", entityData: "Transactional", entityKind: "Main", properties: [] } as DynamicTypeClient.DynamicTypeDefinition :
            JSON.parse(ctx.value.typeDefinition) as DynamicTypeClient.DynamicTypeDefinition;

        this.changeState(s => {
            s.typeDefinition = def;
        });
    }

   
 
    render() {
        const ctx = this.props.ctx;
        const dc = { refreshView: () => this.forceUpdate() };

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(dt => dt.typeName)} onChange={() => this.forceUpdate()} />

                {this.state.typeDefinition &&
                    <div>
                        <DynamicTypeDefinitionComponent dc={dc} definition={this.state.typeDefinition} typeName={ctx.value.typeName || ""} />
                    </div>
                }
            </div>
        );
    }
}
