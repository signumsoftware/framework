import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import { EmailReceptionMixin, Pop3ConfigurationEntity, Pop3ReceptionEntity } from './Signum.MailingReception'
import { EntitySettings } from '@framework/Navigator'
import { EmailMessageEntity } from '../Signum.Mailing/Signum.Mailing'
import { getMixin } from '@framework/Signum.Entities'
import { EntityLine, ValueLine } from '@framework/Lines'
import { Tab } from 'react-bootstrap';

export function start(options: {
  routes: RouteObject[],
}) {

  Navigator.addSettings(new EntitySettings(Pop3ConfigurationEntity, e => import('./Templates/Pop3Configuration')));
  Navigator.addSettings(new EntitySettings(Pop3ReceptionEntity, e => import('./Templates/Pop3Reception')));

  Navigator.getSettings(EmailMessageEntity)!.overrideView((rep) => {

    var erm = getMixin(rep.ctx.value, EmailReceptionMixin);
    if (!erm.receptionInfo)
      return null;

    const ri = rep.ctx.subCtx(EmailReceptionMixin).subCtx(a => a.receptionInfo!);
    rep.insertTabAfter("mainTab", <Tab title={ri.niceName()} eventKey="receptionMixin" >
      <fieldset>
        <legend>Properties</legend>
        <EntityLine ctx={ri.subCtx(f => f.reception)} />
        <ValueLine ctx={ri.subCtx(f => f.uniqueId)} />
        <ValueLine ctx={ri.subCtx(f => f.sentDate)} />
        <ValueLine ctx={ri.subCtx(f => f.receivedDate)} />
        <ValueLine ctx={ri.subCtx(f => f.deletionDate)} />
      </fieldset>
      <pre>{ri.value.rawContent?.text}</pre>
    </Tab >);
  });
}
