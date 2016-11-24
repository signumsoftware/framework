import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { PrintLineEntity} from '../Signum.Entities.Printing'

export default class Package extends React.Component<{ ctx: TypeContext<PrintLineEntity> }, void> {

    render() {
        
        const e = this.props.ctx.subCtx({readOnly: true});

        return (
            <div>    
                <EntityLine ctx={e.subCtx(f => f.package)} />
                <EntityLine ctx={e.subCtx(f => f.file)} />
                <EntityLine ctx={e.subCtx(f => f.referred)} />
                <ValueLine ctx={e.subCtx(f => f.exception)}  />
                
            </div>
        );
    }
}

