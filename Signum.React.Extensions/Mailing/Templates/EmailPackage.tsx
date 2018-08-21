import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater} from '@framework/Lines'
import { SearchControl }  from '@framework/Search'
import { getToString }  from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { EmailPackageEntity, EmailMessageEntity} from '../Signum.Entities.Mailing'

export default class EmailPackage extends React.Component<{ ctx: TypeContext<EmailPackageEntity> }> {

    render() {

        const e = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={e.subCtx(f => f.name) } readOnly={true} />
                <fieldset>
                    <legend>{EmailMessageEntity.nicePluralName()}</legend>
                    <SearchControl findOptions={{
                        queryName: EmailMessageEntity,
                        parentToken: "package",
                        parentValue: e.value
                    }}/>
                </fieldset>
            </div>
        );
    }
}

