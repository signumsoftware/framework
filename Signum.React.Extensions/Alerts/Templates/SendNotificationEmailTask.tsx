import * as React from 'react'
import { EntityCheckboxList, ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { AlertTypeSymbol, SendNotificationEmailTaskEntity } from '../Signum.Entities.Alerts'
import { useForceUpdate } from '../../../Signum.React/Scripts/Hooks';

export default function SendNotificationEmailTask(p: { ctx: TypeContext<SendNotificationEmailTaskEntity> }) {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(n => n.sendNotificationsOlderThan)} labelColumns={4} />
      <ValueLine ctx={ctx.subCtx(n => n.sendBehavior)} labelColumns={4} onChange={forceUpdate} />
      {(ctx.value.sendBehavior == "Exclude" || ctx.value.sendBehavior == "Include") && < EntityCheckboxList ctx={ctx.subCtx(n => n.alertTypes)} columnCount={1} />}
    </div>
  );
}
