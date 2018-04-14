import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { PrintPackageEntity, PrintLineEntity } from '../Signum.Entities.Printing'

export default class PrintPackage extends React.Component<{ ctx: TypeContext<PrintPackageEntity> }> {

    render() {
        
        const e = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={e.subCtx(f => f.name)}  />
                <fieldset>
                    <legend>{PrintLineEntity.nicePluralName()}</legend>
                    <SearchControl findOptions={{ queryName: PrintLineEntity, parentColumn: "Package", parentValue : e.value}}  />
                </fieldset>
            </div>
        );
    }
}

