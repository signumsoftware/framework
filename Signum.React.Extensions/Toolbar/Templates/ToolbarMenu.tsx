import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ToolbarMenuEntity } from '../Signum.Entities.Toolbar'

export default class ToolbarMenu extends React.Component<{ ctx: TypeContext<ToolbarMenuEntity> }> {

    render() {
        const ctx = this.props.ctx;
        
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.name)} />
                <EntityRepeater ctx={ctx.subCtx(f => f.elements)} />
            </div>
        );
    }
}
