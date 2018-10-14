import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { DynamicTypeEntity, DynamicTypeMessage, DynamicMixinConnectionEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '@framework/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute, symbolNiceName } from '@framework/Reflection'
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
                            { token: "Entity.BaseType", operation: "EqualTo", value: "MixinEntity" },
                        ]
                    }} />
                <EntityLine ctx={ctx.subCtx(dt => dt.entityType)} />
            </div>
        );
    }
}
