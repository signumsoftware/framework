import * as React from 'react'
import * as Operations from '@framework/Operations'
import * as Finder from '@framework/Finder'
import { is, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { Toast, Button, ButtonGroup } from 'react-bootstrap'
import { DateTime } from 'luxon'
import { useAPIWithReload, useForceUpdate, useUpdatedRef } from '@framework/Hooks';
import * as AuthClient from '../Authorization/AuthClient'
import * as Navigator from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AlertDropDownGroup, AlertEntity, AlertMessage, AlertOperation } from './Signum.Entities.Alerts'
import * as AlertsClient from './AlertsClient'
import "./AlertDropdown.css"
import { Link } from 'react-router-dom';
import { classes, Dic } from '@framework/Globals'
import MessageModal from '@framework/Modals/MessageModal'
import { useSignalRCallback, useSignalRConnection } from './useSignalR'

export default function AlertDropdown(props: { keepRingingFor?: number }) {

  if (!Navigator.isViewable(AlertEntity))
    return null;

  return <AlertDropdownImp keepRingingFor={props.keepRingingFor ?? 10 * 1000} />;
}

function AlertDropdownImp(props: { keepRingingFor: number }) {

  const conn = useSignalRConnection("~/api/alertshub", {
    accessTokenFactory: () => AuthClient.getAuthToken()!,
  });

  useSignalRCallback(conn, "AlertsChanged", () => {
    reloadCount();
  }, []);

  const forceUpdate = useForceUpdate();
  const [isOpen, setIsOpen] = React.useState<boolean>(false);
  const [ringing, setRinging] = React.useState<boolean>(false);
  const ringingRef = useUpdatedRef(ringing);

  const [showAlerts, setShowAlert] = React.useState<number>(5);
  
  const isOpenRef = useUpdatedRef(isOpen);

  var [countResult, reloadCount] = useAPIWithReload<AlertsClient.NumAlerts>((signal, oldResult) => AlertsClient.API.myAlertsCount().then(res => {
    if (res.lastAlert != null) {
      if (oldResult == null || oldResult.lastAlert == null || oldResult.lastAlert < res.lastAlert) {
        if (!ringingRef.current)
          setRinging(true);
      }

      if (isOpenRef.current) {
        AlertsClient.API.myAlerts()
          .then(als => {
            setAlerts(als);
          })
          .done();
      }

    } else {
      if (ringingRef.current)
        setRinging(false);

      setAlerts([]);
    }

    return res;
  }), [], { avoidReset: true });

  React.useEffect(() => {
    if (ringing) {
      var handler = setTimeout(() => {
        setRinging(false);
      }, props.keepRingingFor);

      return () => { clearTimeout(handler) };
    }
  }, [ringing]);

  const [alerts, setAlerts] = React.useState<AlertEntity[] | undefined>(undefined);
  const [groupBy, setGroupBy] = React.useState<AlertDropDownGroup>("ByTypeAndUser");

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
    alerts.orderByDescending(a => a.alertDate).groupBy(a =>
      groupBy == "ByType" ? (a.alertType == null ? "none" : a.alertType.id) :
        groupBy == "ByUser" ? (a.createdBy?.id) :
          groupBy == "ByTypeAndUser" ? ((a.alertType == null ? "none" : a.alertType.id) + "-" + a.createdBy?.id) : "none"
    );


  function groupByButton(type: AlertDropDownGroup) {
    return <Button active={type == groupBy} variant="light" onClick={ ()=> setGroupBy(type)}>{AlertDropDownGroup.niceToString(type)}</Button>
  }

  return (
    <>
      <div className="nav-link sf-bell-container" onClick={handleOnToggle}>
        <FontAwesomeIcon icon="bell" className={classes("sf-bell", ringing && "ringing", isOpen && "open", countResult && countResult.numAlerts > 0 && "active")} />
        {countResult && countResult.numAlerts > 0 && <span className="badge btn-danger badge-pill sf-alerts-badge">{countResult.numAlerts}</span>}
      </div>
      {isOpen && <div className="sf-alerts-toasts">
        {alertsGroups == null ? <Toast> <Toast.Body>{JavascriptMessage.loading.niceToString()}</Toast.Body></Toast> :

          <>
            {alertsGroups.length == 0 && <Toast><Toast.Body>{AlertMessage.YouDoNotHaveAnyActiveAlert.niceToString()}</Toast.Body></Toast>}
          
            {
              alertsGroups.filter((gr, i) => i < showAlerts)
                .flatMap(gr => gr.key == "none" ? gr.elements.map(a => <AlertToast alert={a} key={a.id} onClose={handleOnCloseAlerts} refresh={reloadCount} />) :
                  [<AlertGroupToast key={gr.key} alerts={gr.elements} onClose={handleOnCloseAlerts} refresh={reloadCount} />])
            }
            {
              alertsGroups.length > showAlerts &&
              <Toast onClose={() => handleOnCloseAlerts(alerts!.map(a=>a))}>
                <Toast.Header>
                  <strong >{AlertMessage._0SimilarAlerts.niceToString(alertsGroups.filter((a, i) => i >= showAlerts).sum(gr => gr.elements.length))}</strong>
                  <a href="#" className="me-auto ms-auto" onClick={e => { e.preventDefault(); setShowAlert(a => a + 3); }}><small>{AlertMessage.ViewMore.niceToString()}</small></a>
                  <small>{AlertMessage.CloseAll.niceToString()}</small>
                </Toast.Header>
              </Toast>
            }
            <Toast>
              <Toast.Body style={{ textAlign: "center" }}>
                <Link onClick={() => setIsOpen(false)} to={Finder.findOptionsPath({
                  queryName: AlertEntity,
                  filterOptions: [
                    { token: AlertEntity.token(a => a.entity.recipient), value: AuthClient.currentUser() },
                  ],
                  orderOptions: [
                    { token: AlertEntity.token(a => a.entity.alertDate), orderType: "Descending" },
                  ],
                  columnOptions: [
                    { token: AlertEntity.token(a => a.entity.id) },
                    { token: AlertEntity.token(a => a.entity.alertDate) },
                    { token: AlertEntity.token(a => a.entity.alertType) },
                    { token: AlertEntity.token("Text") },
                    { token: AlertEntity.token(a => a.entity.target) },
                    { token: AlertEntity.token(a => a.entity).expression("CurrentState") },
                    { token: AlertEntity.token(a => a.entity.createdBy) },
                    { token: AlertEntity.token(a => a.entity.recipient) },
                  ],
                  columnOptionsMode: "Replace"
                })}>{AlertMessage.AllMyAlerts.niceToString()}</Link>
              </Toast.Body>
            </Toast>
            {alerts && alerts.length > 1 && <Toast><ButtonGroup size="sm" className="w-100">
              {groupByButton("ByType")}
              {groupByButton("ByUser")}
              {groupByButton("ByTypeAndUser")}
            </ButtonGroup></Toast>}
          </>
        }
      </div>}
    </>
  );
}

export function AlertToast(p: { alert: AlertEntity, onClose: (e: AlertEntity[]) => void, refresh: () => void, className?: string; }) {

  var icon = p.alert.alertType && p.alert.alertType.key && AlertToast.icons[p.alert.alertType.key]


  return (
    <Toast onClose={() => p.onClose([p.alert])} className={p.className}>
      <Toast.Header>
        {icon && <span className="me-2">{icon}</span>}
        <strong className="me-auto">{AlertsClient.getTitle(p.alert.titleField, p.alert.alertType)}</strong>
        <small>{DateTime.fromISO(p.alert.alertDate!).toRelative()}</small>
      </Toast.Header>
      <Toast.Body style={{ whiteSpace: "pre-wrap" }}>
        {AlertsClient.formatText(p.alert.textField || p.alert.textFromAlertType || "", p.alert, p.refresh)}
        {p.alert.createdBy && <small className="sf-alert-signature">{p.alert.createdBy?.toStr}</small>}
      </Toast.Body>
    </Toast>
  );
}

AlertToast.icons = {} as { [alertTypeKey: string]: React.ReactNode };

export function AlertGroupToast(p: { alerts: Array<AlertEntity>, onClose: (e: AlertEntity[]) => void, refresh: () => void }) {

  const [showAlerts, setShowAlert] = React.useState<number>(1);

  const alert = p.alerts[0];

  var icon = alert.alertType && alert.alertType.key && AlertToast.icons[alert.alertType.key]

  return (
    <div className="mb-2">
      {p.alerts.filter((a, i) => i < showAlerts).map((a, i) => <AlertToast key={a.id} alert={a} onClose={p.onClose} refresh={p.refresh} className="mb-0 mt-0" />)}
      {
        p.alerts.length > showAlerts &&
        <Toast className="mt-0" onClose={() => p.onClose(p.alerts)}>
          <Toast.Header>
            {icon && <span className="me-2">{icon}</span>}
            <strong >{AlertMessage._0SimilarAlerts.niceToString(p.alerts.length - showAlerts)}</strong>
            <a href="#" className="me-auto ms-auto" onClick={e => { e.preventDefault(); setShowAlert(a => a + 3); }}><small>{AlertMessage.ViewMore.niceToString()}</small></a>
            <small>{AlertMessage.CloseAll.niceToString()}</small>
          </Toast.Header>
        </Toast>
      }
    </div>
  );
}
