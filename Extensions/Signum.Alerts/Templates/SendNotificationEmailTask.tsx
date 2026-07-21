import * as React from 'react'
import { DateTime } from 'luxon'
import { EntityCheckboxList, AutoLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { AlertEntity, AlertState, AlertTypeSymbol, SendNotificationEmailTaskEntity } from '../Signum.Alerts'
import { useForceUpdate } from '@framework/Hooks';
import { SearchValueLine } from '@framework/Search';
import { toLite } from '@framework/Signum.Entities'

export default function SendNotificationEmailTask(p: { ctx: TypeContext<SendNotificationEmailTaskEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();

  var maxValue = React.useMemo(() => ctx.value.sendNotificationsOlderThan == null ? null :  DateTime.local().minus({ minutes: ctx.value.sendNotificationsOlderThan }).toISO()!, [ctx.value.sendNotificationsOlderThan]);
  var minValue = React.useMemo(() => ctx.value.ignoreNotificationsOlderThan == null ? null :  DateTime.local().minus({ days: ctx.value.ignoreNotificationsOlderThan }).toISO()!, [ctx.value.ignoreNotificationsOlderThan]);

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(n => n.sendNotificationsOlderThan)} labelColumns={4} onChange={forceUpdate} valueColumns={2} />
      <AutoLine ctx={ctx.subCtx(n => n.ignoreNotificationsOlderThan)} labelColumns={4} onChange={forceUpdate} valueColumns={2}/>
      <AutoLine ctx={ctx.subCtx(n => n.sendBehavior)} labelColumns={4} onChange={forceUpdate} />
      {(ctx.value.sendBehavior == "Exclude" || ctx.value.sendBehavior == "Include") && < EntityCheckboxList ctx={ctx.subCtx(n => n.alertTypes)} columnCount={1} onChange={forceUpdate}/>}
      <SearchValueLine ctx={ctx} findOptions={AlertEntity.findOptions(token => ({
        filterOptions: [
          token(a => a.entity.state).filter("EqualTo", "Saved"),
          token(a => a.entity.emailNotificationsSent).filter("EqualTo", false),
          token(a => a.entity.recipient).filter("DistinctTo", null),
          ctx.value.sendBehavior == "All" ? null :
            token(a => a.entity.alertType).filter(ctx.value.sendBehavior == "Include" ? "IsIn" : "IsNotIn", ctx.value.alertTypes.map(at => toLite(at.element))),
          token(a => a.entity.alertDate).filter("LessThan", maxValue),
          minValue == null ? null :
            token(a => a.entity.alertDate).filter("GreaterThan", minValue),
        ],
        groupResults: true,
        columnOptions: [
          token().count(),
          token(a => a.recipient),
        ],
        columnOptionsMode: "ReplaceAll"
      }))} />
    </div>
  );
}
