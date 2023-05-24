import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import { EntitySettings } from '@framework/Navigator'
import { getMixin } from '@framework/Signum.Entities'
import { EntityLine, ValueLine } from '@framework/Lines'
import { Tab } from 'react-bootstrap';
import { EmailReceptionConfigurationEntity, EmailReceptionEntity, EmailReceptionMixin } from '../Signum.MailingReception'
import { EmailMessageEntity } from '../Signum.Mailing'

export function start(options: {  routes: RouteObject[] }) {

  Navigator.addSettings(new EntitySettings(EmailReceptionConfigurationEntity, e => import('./Templates/EmailReceptionConfiguration')));
  Navigator.addSettings(new EntitySettings(EmailReceptionEntity, e => import('./Templates/EmailReception')));

  Navigator.getSettings(EmailMessageEntity)!.overrideView((rep) => {

    var erm = getMixin(rep.ctx.value, EmailReceptionMixin);
    if (!erm.receptionInfo)
      return null;

    const riCtx = rep.ctx.subCtx(EmailReceptionMixin).subCtx(a => a.receptionInfo!);
    rep.insertTabAfter("mainTab", <Tab title={riCtx.niceName()} eventKey="receptionMixin" >
      <fieldset>
        <legend>Properties</legend>
        <EntityLine ctx={riCtx.subCtx(f => f.reception)} />
        <ValueLine ctx={riCtx.subCtx(f => f.uniqueId)} />
        <ValueLine ctx={riCtx.subCtx(f => f.sentDate)} />
        <ValueLine ctx={riCtx.subCtx(f => f.receivedDate)} />
        <ValueLine ctx={riCtx.subCtx(f => f.deletionDate)} />
      </fieldset>
      <h3>{riCtx.niceName(a => a.rawContent)}</h3>
      <pre>{riCtx.value.rawContent?.text}</pre>
    </Tab >);
  });
}
