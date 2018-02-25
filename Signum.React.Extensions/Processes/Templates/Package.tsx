import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { PackageEntity, PackageLineEntity } from '../Signum.Entities.Processes'

export default class Package extends React.Component<{ ctx: TypeContext<PackageEntity> }> {

    render() {
        
        const e = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={e.subCtx(f => f.name)}  />
                <fieldset>
                    <legend>{ PackageLineEntity.nicePluralName() }</legend>
                    <SearchControl findOptions={{ queryName: PackageLineEntity, parentColumn: "Package", parentValue : e.value}}  />
                </fieldset>
            </div>
        );
    }
}

