import * as React from 'react'
import { Link } from 'react-router-dom'
import { DateTime } from 'luxon'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import SelectorModal from '@framework/SelectorModal'
import ValueLineModal from '@framework/ValueLineModal'
import { AlertEntity, AlertTypeSymbol, AlertOperation, DelayOption, AlertMessage, SendNotificationEmailTaskEntity } from './Signum.Entities.Alerts'
import * as QuickLinks from '@framework/QuickLinks'
import { andClose } from '@framework/Operations/EntityOperations';
import * as AuthClient from '../Authorization/AuthClient'
import { ajaxGet } from '@framework/Services'
import * as Finder from '@framework/Finder'
import { Entity, getToString, isEntity, isLite, Lite, toLite } from '@framework/Signum.Entities'
import { EntityLink } from '@framework/Search'
import Alert from './Templates/Alert'
import { ISymbol, PropertyRoute, symbolNiceName } from '@framework/Reflection'

export function start(options: { routes: JSX.Element[], showAlerts?: (typeName: string, when: "CreateAlert" | "QuickLink") => boolean }) {
  Navigator.addSettings(new EntitySettings(AlertEntity, e => import('./Templates/Alert')));
  Navigator.addSettings(new EntitySettings(AlertTypeSymbol, e => import('./Templates/AlertType')));
  Navigator.addSettings(new EntitySettings(SendNotificationEmailTaskEntity, e => import('./Templates/SendNotificationEmailTask')));

  const couldHaveAlerts = options.showAlerts ?? ((typeName, when) => true);

  Operations.addSettings(new EntityOperationSettings(AlertOperation.CreateAlertFromEntity, {
    isVisible: ctx => couldHaveAlerts(ctx.entity.Type, "CreateAlert"),
    icon: "bell",
    iconColor: "darkorange",
    color: "warning",
    contextual: { isVisible: ctx => couldHaveAlerts(ctx.context.lites[0].EntityType, "CreateAlert"), }
  }));

  QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
    queryName: AlertEntity,
    filterOptions: [{ token: AlertEntity.token(e => e.target), value: ctx.lite}]
  }, {
    isVisible: Navigator.isViewable(AlertEntity) && couldHaveAlerts(ctx.lite.EntityType, "QuickLink"),
    icon: "bell",
    iconColor: "orange",
  }));

  Operations.addSettings(new EntityOperationSettings(AlertOperation.Attend, {
    alternatives: eoc => [andClose(eoc)],
    hideOnCanExecute: true
  }));

  Operations.addSettings(new EntityOperationSettings(AlertOperation.Unattend, {
    hideOnCanExecute: true
  }));

  Operations.addSettings(new EntityOperationSettings(AlertOperation.Delay, {
    commonOnClick: (eoc) => chooseDate().then(d => d && eoc.defaultClick(d.toISO())),
    hideOnCanExecute: true,
    contextualFromMany: { onClick: (coc) => chooseDate().then(d => d && coc.defaultClick(d.toISO())) },
  }));

  var cellFormatter = new Finder.CellFormatter((cell, ctx) => {

    if (cell == null)
      return undefined;

    var alert: Partial<AlertEntity> = {
      createdBy: ctx.row.columns[ctx.columns.indexOf(AlertEntity.token(a => a.createdBy).toString())],
      creationDate: ctx.row.columns[ctx.columns.indexOf(AlertEntity.token(a => a.creationDate).toString())],
      alertDate: ctx.row.columns[ctx.columns.indexOf(AlertEntity.token(a => a.alertDate).toString())],
      target: ctx.row.columns[ctx.columns.indexOf(AlertEntity.token(a => a.target).toString())],
      linkTarget: ctx.row.columns[ctx.columns.indexOf(AlertEntity.token(a => a.linkTarget).toString())],
      textArguments: ctx.row.columns[ctx.columns.indexOf(AlertEntity.token(a => a.entity.textArguments).toString())]
    };
    return format(cell, alert);
  }, true);

  Finder.registerPropertyFormatter(PropertyRoute.tryParse(AlertEntity, "Text"), cellFormatter);

  Finder.addSettings({
    queryName: AlertEntity,
    hiddenColumns: [
      { token: AlertEntity.token(a => a.target) },
      { token: AlertEntity.token(a => a.linkTarget) },
      { token: AlertEntity.token(a => a.entity.textArguments) }
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
      return ValueLineModal.show({
        title: AlertMessage.CustomDelay.niceToString(),
        type: mi.type,
        unit: mi.unit,
        label: mi.niceName,
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

    if (groups.url)
      nodes.push(<Link to={replacePlaceHolders(groups.url, alert)!}>{replacePlaceHolders(groups.text, alert) ?? getToString(lite)}</Link>);
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

const TextPlaceholder = /{(?<prop>(\w|\d|\.)+)}/
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

export module API {

  export function myAlerts(): Promise<AlertEntity[]> {
    return ajaxGet({ url: "~/api/alerts/myAlerts", avoidNotifyPendingRequests: true });
  }

  export function myAlertsCount(): Promise<NumAlerts> {
    return ajaxGet({ url: "~/api/alerts/myAlertsCount", avoidNotifyPendingRequests: true });
  }
}

export interface NumAlerts { numAlerts: number, lastAlert?: string };
