import * as React from 'react'
import { AutoLine, EntityCombo } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailConfigurationEmbedded } from '../Signum.Mailing'
import { CultureInfoEntity } from '@framework/Signum.Basics';


export default function EmailConfiguration(p : { ctx: TypeContext<EmailConfigurationEmbedded> }): React.JSX.Element {
  const sc = p.ctx;
  const ac = p.ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div>
      <AutoLine ctx={sc.subCtx(ca => ca.reciveEmails)} />
      <AutoLine ctx={sc.subCtx(ca => ca.sendEmails)} />
      <AutoLine ctx={sc.subCtx(ca => ca.overrideEmailAddress)} />
      <EntityCombo ctx={sc.subCtx(ca => ca.defaultCulture)} findOptions={{
        queryName: CultureInfoEntity,
        filterOptions: [{ token: CultureInfoEntity.token(a => a.entity).expression<boolean>("IsNeutral"), value: false }]
      }} />
      <AutoLine ctx={sc.subCtx(ca => ca.urlLeft)} />

      <fieldset>
        <legend>Async</legend>
        <div className="row">
          <div className="col-sm-6">
            <AutoLine ctx={ac.subCtx(ca => ca.avoidSendingEmailsOlderThan)} />
            <AutoLine ctx={ac.subCtx(ca => ca.chunkSizeSendingEmails)} />
          </div>
          <div className="col-sm-6">
            <AutoLine ctx={ac.subCtx(ca => ca.maxEmailSendRetries)} />
            <AutoLine ctx={ac.subCtx(ca => ca.asyncSenderPeriod)} />
          </div>
        </div>
      </fieldset>
    </div>
  );
}

