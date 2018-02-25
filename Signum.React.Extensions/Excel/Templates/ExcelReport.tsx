import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { ExcelReportEntity} from '../Signum.Entities.Excel'

export default class ExcelReport extends React.Component<{ ctx: TypeContext<ExcelReportEntity> }> {

    render() {
        
        const e = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={e.subCtx(f => f.query) }  />
                <ValueLine ctx={e.subCtx(f => f.displayName) }  />
                <FileLine ctx={e.subCtx(f => f.file) }  />
            </div>
        );
    }
}

