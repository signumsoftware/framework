import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityFrame, EntityTabRepeater, EntityDetail} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EmailConfigurationEntity } from '../Signum.Entities.Mailing'

export default class EmailConfiguration extends EntityComponent<EmailConfigurationEntity> {

    renderEntity() {

        var sc = this.props.ctx;
        var ac = this.props.ctx.subCtx({ formGroupStyle: FormGroupStyle.Basic });

        return (
            <div>
                <ValueLine ctx={sc.subCtx(ca => ca.reciveEmails) }  />
                <ValueLine ctx={sc.subCtx(ca => ca.sendEmails) }  />
                <ValueLine ctx={sc.subCtx(ca => ca.overrideEmailAddress) }  />
                <EntityCombo ctx={sc.subCtx(ca => ca.defaultCulture) }  />
                <ValueLine ctx={sc.subCtx(ca => ca.urlLeft) }  />
                
                <fieldset className="form-vertical">
                    <legend>Async</legend>
                    <div className="row">
                        <div className="col-sm-6">
                            <ValueLine ctx={ac.subCtx(ca => ca.avoidSendingEmailsOlderThan) }  />
                            <ValueLine ctx={ac.subCtx(ca => ca.chunkSizeSendingEmails) }  />
                        </div>
                        <div className="col-sm-6">
                            <ValueLine ctx={ac.subCtx(ca => ca.maxEmailSendRetries) }  />
                            <ValueLine ctx={ac.subCtx(ca => ca.asyncSenderPeriod) }  />
                        </div>
                    </div>
                </fieldset>
            </div>
        );  
    };
}

