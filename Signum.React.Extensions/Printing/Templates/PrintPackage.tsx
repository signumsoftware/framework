import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '@framework/Lines'
import {SearchControl }  from '@framework/Search'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { PrintPackageEntity, PrintLineEntity } from '../Signum.Entities.Printing'

export default class PrintPackage extends React.Component<{ ctx: TypeContext<PrintPackageEntity> }> {

    render() {
        
        const e = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={e.subCtx(f => f.name)}  />
                <fieldset>
                    <legend>{PrintLineEntity.nicePluralName()}</legend>
                    <SearchControl findOptions={{ queryName: PrintLineEntity, parentToken: "Package", parentValue : e.value}}  />
                </fieldset>
            </div>
        );
    }
}

