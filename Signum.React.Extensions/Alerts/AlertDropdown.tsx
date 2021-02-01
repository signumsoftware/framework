import * as React from 'react'
import { isRtl } from '@framework/AppContext'
import * as Operations from '@framework/Operations'
import { getTypeInfo } from '@framework/Reflection'
import * as Finder from '@framework/Finder'
import { is, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { Toast, NavItem } from 'react-bootstrap'
import { DateTime } from 'luxon'
import { useAPI, useAPIWithReload, useForceUpdate, useInterval, usePrevious, useThrottle, useUpdatedRef } from '@framework/Hooks';
import { LinkContainer } from '@framework/Components'
import * as AuthClient from '../Authorization/AuthClient'
import * as Navigator from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AlertEntity, AlertMessage, AlertOperation } from './Signum.Entities.Alerts'
import * as AlertsClient from './AlertsClient'
import "./AlertDropdown.css"
import { Link } from 'react-router-dom';
import { classes, Dic } from '../../../Framework/Signum.React/Scripts/Globals'
import MessageModal from '../../../Framework/Signum.React/Scripts/Modals/MessageModal'
import { EntityLink } from '../../../Framework/Signum.React/Scripts/Search'



export function AlertToast(p: { alert: AlertEntity, onClose: (e: AlertEntity[]) => void, className?: string; }) {


  function formatText(a: AlertEntity) {
    if (!a.target)
      return a.text;

    if (a.text.contains("[Target]"))
      return (
        <>
          {a.text.before("[Target]")}
          <EntityLink lite={a.target} />
          {a.text.after("[Target]")}
        </>
      );

    if (a.text.contains("[Target:"))
      return (
        <>
          {a.text.before("[Target:")}
          <EntityLink lite={a.target}>{a.text.after("[Target:").beforeLast("]")}</EntityLink>
          {a.text.afterLast("]")}
        </>
      );

    return (
      <>
        {a.text}
        <br />
        <EntityLink lite={a.target} />
      </>
    );
  }

  var icon = p.alert.alertType && p.alert.alertType.key && AlertToast.icons[p.alert.alertType.key]


  return (
    <Toast onClose={() => p.onClose([p.alert])} className={p.className}>
      <Toast.Header>
        {icon && <span className="mr-2">{icon}</span>}
        <strong className="mr-auto">{p.alert.title ?? p.alert.alertType?.name ?? AlertEntity.niceName()}</strong>
        <small>{DateTime.fromISO(p.alert.alertDate!).toRelative()}</small>
      </Toast.Header>
      <Toast.Body style={{ whiteSpace: "pre-wrap" }}>
        {formatText(p.alert)}
        {p.alert.createdBy && <small className="sf-alert-signature">{p.alert.createdBy?.toStr}</small>}
      </Toast.Body>
    </Toast>
  );
}

AlertToast.icons = {} as { [alertTypeKey: string]: React.ReactNode };

export function AlertGroupToast(p: { alerts: Array<AlertEntity>, onClose: (e: AlertEntity[]) => void}) {

  const [showAlerts, setShowAlert] = React.useState<number>(1);

  const alert = p.alerts[0];

  var icon = alert.alertType && alert.alertType.key && AlertToast.icons[alert.alertType.key]

  return (
    <div className="mb-2">
      {p.alerts.filter((a, i) => i < showAlerts).map((a, i) => <AlertToast key={a.id} alert={a} onClose={p.onClose} className="mb-0 mt-0" />)}
      {
        p.alerts.length > showAlerts &&
        <Toast className="mt-0" onClose={() => p.onClose(p.alerts)}>
          <Toast.Header>
            {icon && <span className="mr-2">{icon}</span>}
            <strong >{AlertMessage._0SimilarAlerts.niceToString(p.alerts.length - showAlerts)}</strong>
            <a href="#" className="mr-auto ml-auto" onClick={e => { e.preventDefault(); setShowAlert(a => a + 3); }}><small>{AlertMessage.ViewMore.niceToString()}</small></a>
            <small>{AlertMessage.CloseAll.niceToString()}</small>
          </Toast.Header>
        </Toast>
      }
    </div>
  );
}


export default function AlertDropdown(props: { checkForChangesEvery?: number, keepRingingFor?: number }) {

  if (!Navigator.isViewable(AlertEntity))
    return null;

  return <AlertDropdownImp checkForChangesEvery={props.checkForChangesEvery ?? 30 * 1000} keepRingingFor={props.keepRingingFor ?? 10 * 1000} />;
}

function AlertDropdownImp(props: { checkForChangesEvery: number, keepRingingFor: number }) {



  const forceUpdate = useForceUpdate();
  const [isOpen, setIsOpen] = React.useState<boolean>(false);
  const [ringing, setRinging] = React.useState<boolean>(false);
  const ringingRef = useUpdatedRef(ringing);

  const [showAlerts, setShowAlert] = React.useState<number>(5);

  var ticks = useInterval(props.checkForChangesEvery, 0, n => n + 1);

  const isOpenRef = useUpdatedRef(isOpen);

  var [countResult, reloadCount] = useAPIWithReload<AlertsClient.NumAlerts>((signal, oldResult) => AlertsClient.API.myAlertsCount().then(res => {
    if (oldResult) {
      if (res.lastAlert != null && (oldResult.lastAlert == null || oldResult.lastAlert < res.lastAlert)) {

        if (!ringingRef.current)
          setRinging(true);

        if (isOpenRef.current) {
          AlertsClient.API.myAlerts()
            .then(als => {
              setAlerts(als);
            })
            .done();
        }
      }
    }
    return res;
  }), [ticks], { avoidReset: true });

  React.useEffect(() => {
    if (ringing) {
      var handler = setTimeout(() => {
        setRinging(false);
      }, 60 * 1000);

      return () => { clearTimeout(handler) };
    }
  }, [ringing])

  const [alerts, setAlerts] = React.useState<AlertEntity[] | undefined>(undefined);

  function handleOnToggle() {

    if (!isOpen) {
      AlertsClient.API.myAlerts()
        .then(alerts => setAlerts(alerts))
        .done();
    }

    setIsOpen(!isOpen);
  }

  function handleOnCloseAlerts(toRemove: AlertEntity[]) {

    //Optimistic
    let wasClosed = false;
    if (alerts) {
      alerts.extract(a => toRemove.some(r => is(r, a)));
      if (alerts.length == 0) {
        setIsOpen(false);
        wasClosed = true;
      }
    }
    if (countResult)
      countResult.numAlerts -= toRemove.length;
    forceUpdate();
    


    Operations.API.executeMultiple(toRemove.map(a => toLite(a)), AlertOperation.Attend)
      .then(res => {

        const errors = Dic.getValues(res.errors).filter(a => Boolean(a));
        if (errors.length) {
          MessageModal.showError(<ul>{errors.map((a, i) => <li key={i}>{a}</li>)}</ul>, "Errors attending alerts").done();
        }

        // Pesimistic
        AlertsClient.API.myAlerts()
          .then(alerts => {
            if (wasClosed && alerts.length > 0)
              setIsOpen(true);

            setAlerts(alerts);
          })
          .done();

        reloadCount();

      })
      .done();
  }

  var alertsGroups = alerts == null ? null :
    alerts.orderByDescending(a => a.alertDate).groupBy(a => a.alertType == null ? "none" : a.alertType.id + "-" + a.createdBy?.id)
      .orderBy(a => a.key);
    

  return (
    <>
      <div className="nav-link sf-bell-container" onClick={handleOnToggle}>
        <FontAwesomeIcon icon="bell" className={classes("sf-bell", ringing && "ringing", isOpen && "open", countResult && countResult.numAlerts > 0 && "active")}/>
          {countResult && countResult.numAlerts > 0 && <span className="badge badge-danger badge-pill sf-alerts-badge">{countResult.numAlerts}</span>}
      </div>
      {isOpen && <div className="sf-alerts-toasts">
        {alertsGroups == null ? <Toast> <Toast.Body>{JavascriptMessage.loading.niceToString()}</Toast.Body></Toast> :
      
          <>
              { alertsGroups.length == 0 && <Toast><Toast.Body>{AlertMessage.YouDoNotHaveAnyActiveAlert.niceToString()}</Toast.Body></Toast> }
              {
                alertsGroups.filter((gr, i) => i < showAlerts)
                  .flatMap(gr => gr.key == "none" || gr.elements.length <= 2 ? gr.elements.map(a => <AlertToast alert={a} key={a.id} onClose={handleOnCloseAlerts}  />) :
                    [<AlertGroupToast key={gr.key} alerts={gr.elements} onClose={handleOnCloseAlerts}/>])
              }
              {
                alertsGroups.length > showAlerts &&
                <Toast onClose={()=>handleOnCloseAlerts(alerts!)}>
                  <Toast.Header>
                    <strong >{AlertMessage._0SimilarAlerts.niceToString(alertsGroups.filter((a, i) => i >= showAlerts).sum(gr => gr.elements.length))}</strong>
                    <a href="#" className="mr-auto ml-auto" onClick={e => { e.preventDefault(); setShowAlert(a => a + 3); }}><small>{AlertMessage.ViewMore.niceToString()}</small></a>
                    <small>{AlertMessage.CloseAll.niceToString()}</small>
                  </Toast.Header>
                </Toast>
              }
              <Toast>
              <Toast.Body style={{ textAlign: "center" }}>
                <Link onClick={() => setIsOpen(false)} to={Finder.findOptionsPath({
                    queryName: AlertEntity,
                    filterOptions: [
                      { token: AlertEntity.token().entity(a => a.recipient), value: AuthClient.currentUser() },
                    ],
                    orderOptions: [
                      { token: AlertEntity.token().entity(a => a.alertDate), orderType: "Descending" },
                    ],
                    columnOptions: [
                      { token: AlertEntity.token().entity(a => a.id) },
                      { token: AlertEntity.token().entity(a => a.alertDate) },
                      { token: AlertEntity.token().entity(a => a.alertType) },
                      { token: AlertEntity.token().entity(a => a.target) },
                      { token: AlertEntity.token(a => a.text) },
                      { token: AlertEntity.token().entity(a => a.target) },
                      { token: AlertEntity.token().entity().expression("CurrentState") },
                      { token: AlertEntity.token().entity(a => a.createdBy) },
                      { token: AlertEntity.token().entity(a => a.recipient) },
                    ],
                    columnOptionsMode: "Replace"
                  })}>{AlertMessage.AllMyAlerts.niceToString()}</Link>
                </Toast.Body>
              </Toast>
            </>
        }
      </div>}
    </>
  );
}
