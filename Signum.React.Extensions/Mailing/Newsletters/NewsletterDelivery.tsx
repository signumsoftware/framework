import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { NewsletterDeliveryEntity } from '../Signum.Entities.Mailing'
import { ProcessExceptionLineEntity } from '../../Processes/Signum.Entities.Processes'

export default class NewsletterDelivery extends React.Component<{ ctx: TypeContext<NewsletterDeliveryEntity> }> {
  render() {
    const nc = this.props.ctx;

    return (
      <div>
        <ValueLine ctx={nc.subCtx(n => n.sent)} />
        <ValueLine ctx={nc.subCtx(n => n.sendDate)} />
        <EntityLine ctx={nc.subCtx(n => n.recipient)} />
        <EntityLine ctx={nc.subCtx(n => n.newsletter)} />
        <SearchControl findOptions={{ queryName: ProcessExceptionLineEntity, parentToken: ProcessExceptionLineEntity.token(e => e.line), parentValue: nc.value }} />
      </div>
    );
  }
}

