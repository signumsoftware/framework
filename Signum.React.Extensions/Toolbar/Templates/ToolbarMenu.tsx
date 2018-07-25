import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
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
