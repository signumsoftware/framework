import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ToolbarEntity, ToolbarElementEntity } from '../Signum.Entities.Toolbar'

export default class Toolbar extends React.Component<{ ctx: TypeContext<ToolbarEntity> }, void> {
    
    render() {
        const ctx = this.props.ctx;
        
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.name)} />
                <div className="row">
                    <div className="col-sm-8">
                        <EntityLine ctx={ctx.subCtx(e => e.owner)} labelColumns={3} />
                    </div>

                    <div className="col-sm-4">
                        <ValueLine ctx={ctx.subCtx(e => e.priority)} />
                    </div>
                </div>
                <EntityRepeater ctx={ctx.subCtx(f => f.elements)} />
            </div>
        );
    }
}
