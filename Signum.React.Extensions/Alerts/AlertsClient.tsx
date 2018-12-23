import * as moment from 'moment'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import SelectorModal from '@framework/SelectorModal'
import ValueLineModal from '@framework/ValueLineModal'
import { AlertEntity, AlertTypeEntity, AlertOperation, DelayOption, AlertMessage } from './Signum.Entities.Alerts'
import * as QuickLinks from '@framework/QuickLinks'

export function start(options: { routes: JSX.Element[], couldHaveAlerts?: (typeName: string) => boolean }) {
  Navigator.addSettings(new EntitySettings(AlertEntity, e => import('./Templates/Alert')));
  Navigator.addSettings(new EntitySettings(AlertTypeEntity, e => import('./Templates/AlertType')));


  const couldHaveAlerts = options.couldHaveAlerts || (typeName => true);

  Operations.addSettings(new EntityOperationSettings(AlertOperation.CreateAlertFromEntity, {
    isVisible: ctx => couldHaveAlerts(ctx.entity.Type),
    contextual: { icon: "bell", iconColor: "darkorange", color: "warning", isVisible: ctx => couldHaveAlerts(ctx.context.lites[0].EntityType), }
  }));

  QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
    queryName: AlertEntity,
    parentToken: AlertEntity.token(e => e.target),
    parentValue: ctx.lite
  }, { isVisible: couldHaveAlerts(ctx.lite.EntityType), icon: "bell", iconColor: "orange" }));

  Operations.addSettings(new EntityOperationSettings(AlertOperation.Attend, {
    withClose: true,
  }));

  Operations.addSettings(new EntityOperationSettings(AlertOperation.Delay, {
    onClick: (eoc) => chooseDate().then(d => d && eoc.defaultClick(d.format())).done(),
    contextual: { onClick: (coc) => chooseDate().then(d => d && coc.defaultContextualClick(d.format())).done() },
    contextualFromMany: { onClick: (coc) => chooseDate().then(d => d && coc.defaultContextualClick(d.format())).done() }
  }));
}

function chooseDate(): Promise<moment.Moment | undefined> {
  return SelectorModal.chooseElement(DelayOption.values(), {
    title: AlertMessage.DelayDuration.niceToString(),
    buttonDisplay: v => DelayOption.niceToString(v)!,
  }).then(val => {
    if (!val)
      return undefined;

    var result = moment();
    if (val == "Custom") {
      var mi = AlertEntity.memberInfo(a => a.alertDate);
      return ValueLineModal.show({
        title: AlertMessage.CustomDelay.niceToString(),
        type: mi.type,
        unitText: mi.unit,
        labelText: mi.niceName,
        initiallyFocused: true,
        initialValue: result.format()
      });
    } else {
      switch (val) {
        case "_5Mins": return result.add(5, "minute");
        case "_15Mins": return result.add(15, "minute");
        case "_30Mins": return result.add(30, "minute");
        case "_1Hour": return result.add(1, "hour");
        case "_2Hours": return result.add(2, "hour");
        case "_1Day": return result.add(1, "day");
        default: throw new Error("Unexpected " + val);
      }
    }
  });
}
