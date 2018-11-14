import * as React from 'react'
import { ValueLine, EntityLine, EntityDetail } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ScheduledTaskEntity } from '../Signum.Entities.Scheduler'

export default class ScheduledTask extends React.Component<{ ctx: TypeContext<ScheduledTaskEntity> }> {
  render() {
    const e = this.props.ctx;

    return (
      <div>
        <ValueLine ctx={e.subCtx(f => f.suspended)} />
        <EntityLine ctx={e.subCtx(f => f.task)} create={false} />
        <EntityDetail ctx={e.subCtx(f => f.rule)} />
        <ValueLine ctx={e.subCtx(f => f.machineName)} />
        <ValueLine ctx={e.subCtx(f => f.applicationName)} />
        <EntityLine ctx={e.subCtx(f => f.user)} />
      </div>
    );
  }
}

