import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater, EntityTable } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { ToolbarEntity, ToolbarElementEmbedded } from '../Signum.Entities.Toolbar'

export default class Toolbar extends React.Component<{ ctx: TypeContext<ToolbarEntity> }> {
    
    render() {
        const ctx = this.props.ctx;
        const ctx3 = ctx.subCtx({ labelColumns: 3 });
        return (
            <div>
                <div className="row">
                    <div className="col-sm-7">
                        <ValueLine ctx={ctx3.subCtx(f => f.name)} />
                        <EntityLine ctx={ctx3.subCtx(e => e.owner)} />
                    </div>

                    <div className="col-sm-5">
                        <ValueLine ctx={ctx3.subCtx(f => f.location)} />
                        <ValueLine ctx={ctx3.subCtx(e => e.priority)} />
                    </div>
                </div>
                <EntityRepeater ctx={ctx.subCtx(f => f.elements)} />
            </div>
        );
    }
}
