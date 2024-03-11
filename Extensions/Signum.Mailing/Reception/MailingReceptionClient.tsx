import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Constructor } from '@framework/Constructor'
import { Finder } from '@framework/Finder'
import { getMixin } from '@framework/Signum.Entities'
import { EntityLine, AutoLine } from '@framework/Lines'
import { Tab } from 'react-bootstrap';
import { EmailReceptionConfigurationEntity, EmailReceptionEntity, EmailReceptionMixin } from './Signum.Mailing.Reception'
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
        <AutoLine ctx={riCtx.subCtx(f => f.uniqueId)} />
        <AutoLine ctx={riCtx.subCtx(f => f.sentDate)} />
        <AutoLine ctx={riCtx.subCtx(f => f.receivedDate)} />
        <AutoLine ctx={riCtx.subCtx(f => f.deletionDate)} />
      </fieldset>
      <h3>{riCtx.niceName(a => a.rawContent)}</h3>
      <pre>{riCtx.value.rawContent?.text}</pre>
    </Tab >);
  });
}
