import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { SearchValueLine } from '@framework/Search'
import { ExceptionEntity } from '@framework/Signum.Entities.Basics'
import { TypeContext } from '@framework/TypeContext'
import { Pop3ReceptionEntity, EmailMessageEntity, EmailReceptionMixin } from '../Signum.Mailing'

export default function Pop3Reception(p : { ctx: TypeContext<Pop3ReceptionEntity> }){
  const sc = p.ctx;

  return (
    <div>
      <EntityLine ctx={sc.subCtx(s => s.pop3Configuration)} />
      <ValueLine ctx={sc.subCtx(s => s.startDate)} />
      <ValueLine ctx={sc.subCtx(s => s.endDate)} />
      <ValueLine ctx={sc.subCtx(s => s.newEmails)} />
      <EntityLine ctx={sc.subCtx(s => s.exception)} />
      <SearchValueLine ctx={sc} findOptions={{ queryName: EmailMessageEntity, filterOptions: [{ token: EmailMessageEntity.token(a => a.entity).mixin(EmailReceptionMixin).append(a => a.receptionInfo!.reception), value: sc.value }]}} />
      <SearchValueLine ctx={sc} findOptions={{ queryName: ExceptionEntity, filterOptions: [{ token: ExceptionEntity.token(a => a.entity).expression("Pop3Reception"), value: sc.value }]}} />
    </div>
  );
}
