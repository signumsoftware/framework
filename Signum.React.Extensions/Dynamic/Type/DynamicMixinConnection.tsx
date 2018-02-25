import * as React from 'react'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { DynamicTypeEntity, DynamicTypeMessage, DynamicMixinConnectionEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute, symbolNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as DynamicTypeClient from '../DynamicTypeClient'

interface DynamicMixinConnectionComponentProps {
    ctx: TypeContext<DynamicMixinConnectionEntity>;
}

export default class DynamicMixinConnectionComponent extends React.Component<DynamicMixinConnectionComponentProps> {

    render() {
        const ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(dt => dt.dynamicMixin)}
                    findOptions={{
                        queryName: DynamicTypeEntity,
                        filterOptions: [
                            { columnName: "Entity.BaseType", operation: "EqualTo", value: "MixinEntity" },
                        ]
                    }} />
                <EntityLine ctx={ctx.subCtx(dt => dt.entityType)} />
            </div>
        );
    }
}
