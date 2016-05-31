import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EmailAddressEntity } from '../Signum.Entities.Mailing'

export default class EmailAddress extends React.Component<{ ctx: TypeContext<EmailAddressEntity> }, void> {

    render() {

         var sc = this.props.ctx.subCtx({ placeholderLabels: true, formGroupStyle: "SrOnly"});

        return (
            <div className="row form-vertical">
                <div className="col-sm-4 col-sm-offset-2">
                     <EntityLine ctx={sc.subCtx(ea => ea.emailOwner)}  />
                </div>
                 <div className="col-sm-3">
                        <ValueLine ctx={sc.subCtx(c => c.emailAddress)}  />{/*vl.ValueHtmlProps.Remove("size"*/})
                 </div>
                 <div className="col-sm-3">
                       <ValueLine ctx={sc.subCtx(c => c.displayName)}  />
                 </div>
            </div>
        );
    }
}

