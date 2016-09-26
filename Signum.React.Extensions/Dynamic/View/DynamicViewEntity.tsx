import * as React from 'react'
import { DynamicViewEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'

export default class DynamicViewEntityComponent extends React.Component<{ ctx: TypeContext<DynamicViewEntity> }, void> {

    render() {
        const ctx = this.props.ctx;
        return (
            <div>
               
            </div>
        );
    }
}

