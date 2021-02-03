import * as React from 'react'
import { DateTime } from 'luxon'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import SelectorModal from '@framework/SelectorModal'
import ValueLineModal from '@framework/ValueLineModal'
import { AlertEntity, AlertTypeEntity, AlertOperation, DelayOption, AlertMessage } from './Signum.Entities.Alerts'
import * as QuickLinks from '@framework/QuickLinks'
import { andClose } from '@framework/Operations/EntityOperations';
import * as AuthClient from '../Authorization/AuthClient'
import { ajaxGet } from '@framework/Services'
import * as Finder from '@framework/Finder'
import { Entity, Lite } from '@framework/Signum.Entities'
import { EntityLink } from '@framework/Search'

export function start(options: { routes: JSX.Element[], couldHaveAlerts?: (typeName: string) => boolean }) {
  Navigator.addSettings(new EntitySettings(AlertEntity, e => import('./Templates/Alert')));
  Navigator.addSettings(new EntitySettings(AlertTypeEntity, e => import('./Templates/AlertType')));

  const couldHaveAlerts = options.couldHaveAlerts ?? (typeName => true);

  Operations.addSettings(new EntityOperationSettings(AlertOperation.CreateAlertFromEntity, {
    isVisible: ctx => couldHaveAlerts(ctx.entity.Type),
    icon: "bell",
    iconColor: "darkorange",
    color: "warning",
    contextual: { isVisible: ctx => couldHaveAlerts(ctx.context.lites[0].EntityType), }
  }));

  QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
    queryName: AlertEntity,
    parentToken: AlertEntity.token(e => e.target),
    parentValue: ctx.lite
  }, {
    isVisible: Navigator.isViewable(AlertEntity) && couldHaveAlerts(ctx.lite.EntityType),
    icon: "bell",
    iconColor: "orange",
  }));

  Operations.addSettings(new EntityOperationSettings(AlertOperation.Attend, {
    alternatives: eoc => [andClose(eoc)],
  }));

  Operations.addSettings(new EntityOperationSettings(AlertOperation.Delay, {
    onClick: (eoc) => chooseDate().then(d => d && eoc.defaultClick(d.toISO())).done(),
    contextual: { onClick: (coc) => chooseDate().then(d => d && coc.defaultContextualClick(d.toISO())).done() },
    contextualFromMany: { onClick: (coc) => chooseDate().then(d => d && coc.defaultContextualClick(d.toISO())).done() }
  }));

  Finder.registerPropertyFormatter(AlertEntity.tryPropertyRoute(a => a.text), new Finder.CellFormatter((cell, ctx) => {
    return formatText(cell, ctx.row.columns[ctx.columns.indexOf("Target")]);
  }));

  Finder.addSettings({
    queryName: AlertEntity,
    hiddenColumns: [{ token: "Target" }]
  })
}

function chooseDate(): Promise<DateTime | undefined> {
  return SelectorModal.chooseElement(DelayOption.values(), {
    title: AlertMessage.DelayDuration.niceToString(),
    buttonDisplay: v => DelayOption.niceToString(v)!,
  }).then(val => {
    if (!val)
      return undefined;

    var result = DateTime.local();
    if (val == "Custom") {
      var mi = AlertEntity.memberInfo(a => a.alertDate);
      return ValueLineModal.show({
        title: AlertMessage.CustomDelay.niceToString(),
        type: mi.type,
        unitText: mi.unit,
        labelText: mi.niceName,
        initiallyFocused: true,
        initialValue: result.toISO()
      });
    } else {
      switch (val) {
        case "_5Mins": return result.plus({ minutes: 5 });
        case "_15Mins": return result.plus({ minutes: 15 });
        case "_30Mins": return result.plus({ minutes: 30 });
        case "_1Hour": return result.plus({ hour: 1 });
        case "_2Hours": return result.plus({ hour: 2 });
        case "_1Day": return result.plus({ day: 1 });
        default: throw new Error("Unexpected " + val);
      }
    }
  });
}


export function formatText(text: string, target: Lite<Entity> | null) {
  if (!target)
    return text;

  if (text.contains("[Target]"))
    return (
      <>
        {text.before("[Target]")}
        <EntityLink lite={target} />
        {text.after("[Target]")}
      </>
    );

  if (text.contains("[Target:"))
    return (
      <>
        {text.before("[Target:")}
        <EntityLink lite={target}>{text.after("[Target:").beforeLast("]")}</EntityLink>
        {text.afterLast("]")}
      </>
    );

  return (
    <>
      {text}
      <br />
      <EntityLink lite={target} />
    </>
  );
}


export module API {

  export function myAlerts(): Promise<AlertEntity[]> {
    return ajaxGet({ url: "~/api/alerts/myAlerts", avoidNotifyPendingRequests: true });
  }

  export function myAlertsCount(): Promise<NumAlerts> {
    return ajaxGet({ url: "~/api/alerts/myAlertsCount", avoidNotifyPendingRequests : true });
  }
}

export interface NumAlerts { numAlerts: number, lastAlert?: string };
