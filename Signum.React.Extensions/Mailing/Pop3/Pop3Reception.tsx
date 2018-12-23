import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { ValueSearchControlLine } from '@framework/Search'
import { ExceptionEntity } from '@framework/Signum.Entities.Basics'
import { TypeContext } from '@framework/TypeContext'
import { Pop3ReceptionEntity, EmailMessageEntity, EmailReceptionMixin } from '../Signum.Entities.Mailing'

export default class Pop3Reception extends React.Component<{ ctx: TypeContext<Pop3ReceptionEntity> }> {
  render() {
    const sc = this.props.ctx;

    return (
      <div>
        <EntityLine ctx={sc.subCtx(s => s.pop3Configuration)} />
        <ValueLine ctx={sc.subCtx(s => s.startDate)} />
        <ValueLine ctx={sc.subCtx(s => s.endDate)} />
        <ValueLine ctx={sc.subCtx(s => s.newEmails)} />
        <EntityLine ctx={sc.subCtx(s => s.exception)} />
        <ValueSearchControlLine ctx={sc} findOptions={{ queryName: EmailMessageEntity, parentToken: EmailMessageEntity.token().entity().mixin(EmailReceptionMixin).append(a => a.receptionInfo!.reception), parentValue: sc.value }} />
        <ValueSearchControlLine ctx={sc} findOptions={{ queryName: ExceptionEntity, parentToken: ExceptionEntity.token().entity().expression("Pop3Reception"), parentValue: sc.value }} />
      </div>
    );
  }
}
