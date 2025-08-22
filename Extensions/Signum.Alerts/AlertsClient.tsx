import * as React from 'react'
import { RouteObject } from 'react-router'
import { Link } from 'react-router-dom'
import { DateTime } from 'luxon'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import SelectorModal from '@framework/SelectorModal'
import AutoLineModal from '@framework/AutoLineModal'
import { AlertEntity, AlertTypeSymbol, AlertOperation, DelayOption, AlertMessage, SendNotificationEmailTaskEntity } from './Signum.Alerts'
import { QuickLinkClient, QuickLinkExplore } from '@framework/QuickLinkClient'
import { EntityOperations } from '@framework/Operations/EntityOperations';
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { ajaxGet } from '@framework/Services'
import { Finder } from '@framework/Finder'
import { Entity, getToString, isEntity, isLite, Lite, toLite } from '@framework/Signum.Entities'
import { EntityLink } from '@framework/Search'
import Alert from './Templates/Alert'
import { getQueryKey, ISymbol, PropertyRoute, symbolNiceName } from '@framework/Reflection'
import { toAbsoluteUrl } from '@framework/AppContext'
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient'

export namespace AlertsClient {
  
  export function start(options: { routes: RouteObject[], showAlerts: (typeName: string, when: "CreateAlert" | "QuickLink") => boolean }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Alerts", () => import("./Changelog"));
  
    Navigator.addSettings(new EntitySettings(AlertEntity, e => import('./Templates/Alert')));
    Navigator.addSettings(new EntitySettings(AlertTypeSymbol, e => import('./Templates/AlertType')));
    Navigator.addSettings(new EntitySettings(SendNotificationEmailTaskEntity, e => import('./Templates/SendNotificationEmailTask')));
  
    const couldHaveAlerts = options.showAlerts ?? ((typeName, when) => true);
  
    Operations.addSettings(new EntityOperationSettings(AlertOperation.CreateAlertFromEntity, {
      isVisible: ctx => couldHaveAlerts(ctx.entity.Type, "CreateAlert"),
      isVisibleOnlyType: type => couldHaveAlerts(type, "CreateAlert"),
      icon: "bell",
      iconColor: "darkorange",
      color: "warning",
      contextual: { isVisible: ctx => couldHaveAlerts(ctx.context.lites[0].EntityType, "CreateAlert"), }
    }));
  
    QuickLinkClient.registerGlobalQuickLink(entityType => Promise.resolve([new QuickLinkExplore(entityType, ctx => ({ queryName: AlertEntity, filterOptions: [{ token: AlertEntity.token(e => e.target), value: ctx.lite }] }),
      {
        key: getQueryKey(AlertEntity),
        text: () => AlertEntity.nicePluralName(),
        isVisible: Finder.isFindable(AlertEntity, false) && couldHaveAlerts(entityType, "QuickLink"),
        icon: "clock-rotate-left",
        iconColor: "green",
        color: "success",
      }
    )]));
  
    Operations.addSettings(new EntityOperationSettings(AlertOperation.Attend, {
      alternatives: eoc => [EntityOperations.andClose(eoc)],
      hideOnCanExecute: true
    }));
  
    Operations.addSettings(new EntityOperationSettings(AlertOperation.Unattend, {
      hideOnCanExecute: true
    }));
  
    Operations.addSettings(new EntityOperationSettings(AlertOperation.Delay, {
      commonOnClick: (eoc) => chooseDate().then(d => d && eoc.defaultClick(d.toISO()!)),
      hideOnCanExecute: true,
      contextualFromMany: { onClick: (coc) => chooseDate().then(d => d && coc.defaultClick(d.toISO()!)) },
    }));
  
    var cellFormatter = new Finder.CellFormatter((cell, ctx) => {
  
      if (cell == null)
        return undefined;
  
      var alert: Partial<AlertEntity> = {
        createdBy: ctx.searchControl?.getRowValue(ctx.row, AlertEntity.token(a => a.createdBy)),
        creationDate: ctx.searchControl?.getRowValue(ctx.row, AlertEntity.token(a => a.creationDate)),
        alertDate: ctx.searchControl?.getRowValue(ctx.row, AlertEntity.token(a => a.alertDate)),
        target: ctx.searchControl?.getRowValue(ctx.row, AlertEntity.token(a => a.target)),
        linkTarget: ctx.searchControl?.getRowValue(ctx.row, AlertEntity.token(a => a.linkTarget)),
        textArguments: ctx.searchControl?.getRowValue(ctx.row, AlertEntity.token(a => a.entity.textArguments))
      };
      return format(cell, alert);
    }, true);
  
    Finder.registerPropertyFormatter(PropertyRoute.tryParse(AlertEntity, "Text"), cellFormatter);
  
    Finder.addSettings({
      queryName: AlertEntity,
      hiddenColumns: [
        { token: AlertEntity.token(a => a.target) },
        { token: AlertEntity.token(a => a.linkTarget) },
        { token: AlertEntity.token(a => a.entity.textArguments) },
        { token: AlertEntity.token(a => a.creationDate) }
      ],
      formatters: {
        "Text": cellFormatter
      }
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
        return AutoLineModal.show({
          title: AlertMessage.CustomDelay.niceToString(),
          type: mi.type,
          unit: mi.unit,
          label: mi.niceName,
          initialValue: result.toISO()!
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
  
  let LinkPlaceholder = /\[(?<prop>(\w|\d|\.)+)(\:(?<text>.+))?\](\((?<url>.+)\))?/g;
  
  export function getTitle(titleField: string | null, type: AlertTypeSymbol | null): string | null {
    if (titleField)
      return titleField;
  
    if (type == null)
      return " - ";
  
    if (type.key)
      return symbolNiceName(type! as Entity & ISymbol);
  
    return type.name;
  }
  export function format(text: string, alert: Partial<AlertEntity>, onNavigated?: () => void): React.ReactElement {
    var nodes: (string | React.ReactElement)[] = [];
    var pos = 0;
    for (const match of Array.from(text.matchAll(LinkPlaceholder))) {
      nodes.push(replacePlaceHolders(text.substring(pos, match.index), alert)!)
  
      var groups = (match as any).groups as { prop: string, text?: string, url?: string };
  
      var prop = getPropertyValue(groups.prop, alert);
  
      var lite = isEntity(prop) ? toLite(prop) :
        isLite(prop) ? prop : null;
  
      if (groups.url) {
        var url = replacePlaceHolders(groups.url, alert)!;
        var text = replacePlaceHolders(groups.text, alert) ?? getToString(lite);
        if (url.startsWith("http"))
          nodes.push(<a href={url} target="_blank">{text}</a>);
        else {
          var routerUrl = url.startsWith("~") ? url.after("~") : url;
          nodes.push(<Link to={routerUrl}>{text}</Link>);
        }
      }
      else if (lite != null) {
        if (groups.text)
          nodes.push(<EntityLink lite={lite} onNavigated={onNavigated}>{replacePlaceHolders(groups.text, alert)}</EntityLink>);
        else
          nodes.push(<EntityLink lite={lite} onNavigated={onNavigated} />);
      }
  
      pos = match.index! + match[0].length;
    }
  
    nodes.push(replacePlaceHolders(text.substring(pos), alert)!);
  
    if (nodes.length == 1 && alert.target != null) {
      nodes.push(<br />);
      nodes.push(<EntityLink lite={alert.target} />)
    }
  
    return React.createElement(React.Fragment, {}, ...nodes);
  }
  
  const TextPlaceholder = /{(?<prop>(\w|\d|\.)+)}/g
  const NumericPlaceholder = /^[ \d]+$/;
  
  function replacePlaceHolders(value: string | null | undefined, alert: Partial<AlertEntity>) {
    if (value == null)
      return null;
  
    return value.replace(TextPlaceholder, args => {
  
      var prop = args[1];
  
      if (NumericPlaceholder.test(prop))
        return alert.textArguments?.split("\n###\n")[parseInt(prop)];
  
      return getPropertyValue(prop, alert)?.ToString()! ?? "";
    });
  }
  
  function getPropertyValue(str: string, object: Partial<AlertEntity>): any {
    if (str.contains(".")) {
      var obj2 = getPropertyValue(str.before("."), object);
  
      return obj2 == null ? obj2 : obj2[str.after(".").firstLower()];
    }
  
    return (object as any)[str.firstLower()]
  }
  
  export namespace API {
  
    export function myAlerts(): Promise<AlertEntity[]> {
      return ajaxGet({ url: "/api/alerts/myAlerts", avoidNotifyPendingRequests: true });
    }
  
    export function myAlertsCount(): Promise<NumAlerts> {
      return ajaxGet({ url: "/api/alerts/myAlertsCount", avoidNotifyPendingRequests: true });
    }
  }
  
  export interface NumAlerts { numAlerts: number, lastAlert?: string };
}
