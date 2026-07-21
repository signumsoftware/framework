import * as React from 'react'
import { AutoLine, EntityLine } from '@framework/Lines'
import { SearchValueLine } from '@framework/Search'
import { ExceptionEntity } from '@framework/Signum.Basics'
import { TypeContext } from '@framework/TypeContext'
import { EmailReceptionEntity, EmailReceptionMixin } from '../Signum.Mailing.Reception'
import { EmailMessageEntity } from '../../Signum.Mailing'

export default function EmailReception(p: { ctx: TypeContext<EmailReceptionEntity> }): React.JSX.Element {
  const sc = p.ctx;

  return (
    <div>
      <EntityLine ctx={sc.subCtx(s => s.emailReceptionConfiguration)} />
      <AutoLine ctx={sc.subCtx(s => s.startDate)} />
      <AutoLine ctx={sc.subCtx(s => s.endDate)} />
      <AutoLine ctx={sc.subCtx(s => s.newEmails)} />
      <EntityLine ctx={sc.subCtx(s => s.exception)} />
      <SearchValueLine ctx={sc} findOptions={EmailMessageEntity.findOptions(token => ({ filterOptions: [token(a => a.entity).mixin(EmailReceptionMixin).append(a => a.receptionInfo!.reception).filter("EqualTo", sc.value)]}))} />
      <SearchValueLine ctx={sc} findOptions={ExceptionEntity.findOptions(token => ({ filterOptions: [token(a => a.entity).expression("Pop3Reception").filter("EqualTo", sc.value)]}))} />
    </div>
  );
}
