import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { PrintLineEntity } from '../Signum.Entities.Printing'
import { ProcessExceptionLineEntity } from '../../Processes/Signum.Entities.Processes'
import FileLine from '../../Files/FileLine'

export default class PrintLine extends React.Component<{ ctx: TypeContext<PrintLineEntity> }> {

    render() {
        
        const e = this.props.ctx.subCtx({readOnly: true});

        return (
            <div>
                <ValueLine ctx={e.subCtx(f => f.creationDate)} />
                <EntityLine ctx={e.subCtx(f => f.referred)} />
                <FileLine ctx={e.subCtx(f => f.file)} />
                <ValueLine ctx={e.subCtx(f => f.state)} />
                <ValueLine ctx={e.subCtx(f => f.printedOn)} />
                <fieldset>
                    <legend>{ProcessExceptionLineEntity.nicePluralName() }</legend>
                    <SearchControl findOptions={{ queryName: ProcessExceptionLineEntity, parentColumn: "Line", parentValue : e.value}}  />
                </fieldset>
            </div>
        );
    }
}

