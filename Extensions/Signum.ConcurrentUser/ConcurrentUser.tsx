import * as React from 'react'
import { DateTime } from 'luxon'
import * as AppContext from '@framework/AppContext'
import { useSignalRCallback, useSignalRConnection, useSignalRGroup } from '@framework/useSignalR'
import { ConcurrentUserEntity, ConcurrentUserMessage } from './Signum.ConcurrentUser'
import { OverlayTrigger, Popover } from 'react-bootstrap';
import { Entity, getToString, Lite, liteKey, toLite } from '@framework/Signum.Entities'
import { useAPI, useForceUpdate, useUpdatedRef, useVersion } from '@framework/Hooks'
import { GraphExplorer } from '@framework/Reflection'
import { Navigator } from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { classes } from '@framework/Globals';
import MessageModal from '@framework/Modals/MessageModal'
import './ConcurrentUser.css'
import { ConcurrentUserClient } from './ConcurrentUserClient';
import { HubConnectionState } from '@microsoft/signalr'
import { SmallProfilePhoto } from '../Signum.Authorization/Templates/ProfilePhoto'
import { UserEntity } from '../Signum.Authorization/Signum.Authorization'

export default function ConcurrentUser(p: { entity: Entity, isExecuting: boolean, onReload: () => void }): React.JSX.Element | null {
  
  const conn = useSignalRConnection("/api/concurrentUserHub");

  const entityKey = liteKey(toLite(p.entity));
  const userKey = liteKey(toLite(AppContext.currentUser! as UserEntity))

  const [ticks, setTicks] = React.useState<string>(p.entity.ticks!);
  const forceUpdate = useForceUpdate();
  React.useEffect(() => {
    setTicks(p.entity.ticks!);
  }, [entityKey]);

  useSignalRGroup(conn, {
    enterGroup: co => p.entity == null ? Promise.resolve(undefined) : co.send("EnterEntity", entityKey, userKey),
    exitGroup: co => p.entity == null ? Promise.resolve(undefined) : co.send("ExitEntity", entityKey, userKey),
    deps: [entityKey]
  });

  const isModified = React.useRef(false);

  React.useEffect(() => {

    if (conn) {

      function updateModified() {
        const modified = GraphExplorer.hasChangesNoClean(p.entity);

        if (modified != isModified.current && conn?.state == HubConnectionState.Connected) {
          conn?.send("EntityModified", entityKey, userKey, modified);
          isModified.current = modified;
        }
      }

      updateModified();
      const handler = setInterval(() => {
        updateModified();
      }, 1000);

      return () => clearInterval(handler);
    }
  }, [conn, p.entity, ticks]);

  var [concurrentUserVersion, updateConcurrentUsers] = useVersion();

  var concurrentUsers = useAPI(() => ConcurrentUserClient.API.getUsers(entityKey), [concurrentUserVersion, isModified.current, entityKey]);

  useSignalRCallback(conn, "EntitySaved", (a: string) => {
    console.log(`${DateTime.now().toISO()}: EntitySaved ${a}`);
    setTicks(a);
  }, []);

  useSignalRCallback(conn, "ConcurrentUsersChanged", () => updateConcurrentUsers(), []);

  if (window.__disableSignalR)
    return <FontAwesomeIcon icon="triangle-exclamation" color={"#ddd"} title={window.__disableSignalR} />;

  //const ticksRef = useUpdatedRef(ticks);
  //const entityRef = useUpdatedRef(p.entity);

  React.useEffect(() => {
    console.log(`${DateTime.now().toISO()}: NewEntity ${p.entity.ticks}`);
  }, [p.entity.ticks]);

  //Is conditionally but the condition is a constant
    React.useEffect(() => {
        console.log(`${DateTime.now().toISO()}: isExecuting: ${p.isExecuting} useEffect ${ticks} (ticks) = ${p.entity.ticks} (entity)`);
      //console.log("Effect", { ticks: ticksRef.current, entityTicks: entityRef.current.ticks });
      if (!p.isExecuting && ticks != p.entity.ticks) {
        MessageModal.show({
          title: ConcurrentUserMessage.DatabaseChangesDetected.niceToString(),
          style: "warning",
          message:
            <div>
              <p>{ConcurrentUserMessage.LooksLikeSomeoneJustSaved0ToTheDatabase.niceToString().formatHtml(<strong>{getToString(p.entity)}</strong>)}</p>
              <p>{ConcurrentUserMessage.DoYouWantToReloadIt.niceToString()}</p>
              {isModified.current &&
                <>
                  <p className="text-danger">
                    {ConcurrentUserMessage.WarningYouWillLostYourCurrentChanges.niceToString()}
                  </p>
                  <p>
                    {ConcurrentUserMessage.ConsiderOpening0InANewTabAndApplyYourChangesManually.niceToString().formatHtml(<a href={Navigator.navigateRoute(p.entity)} target="_blank">{getToString(p.entity)}</a>)}
                  </p>
                </>
              }
            </div>,
          buttons: "yes_cancel",
        }).then(b => b == "yes" && p.onReload());
      }
  }, [ticks, p.entity.ticks, p.isExecuting]);

  //console.log("Render", { ticks, entityTicks: p.entity.ticks });

  var otherUsers = concurrentUsers?.filter(u => u.connectionID !== conn?.connectionId);

  if (otherUsers == null || otherUsers.length == 0)
    return null;

  return (
    <OverlayTrigger
      trigger="click"
      onToggle={show => forceUpdate()}
      placement={"bottom-end"}
      overlay={
        <Popover>
          <Popover.Header as="h3">{ConcurrentUserMessage.ConcurrentUsers.niceToString()}</Popover.Header>
          <Popover.Body>
            
            {otherUsers?.map((a, i) =>
              <div key={i} className="d-flex align-items-center" >
                <SmallProfilePhoto user={a.user} className="me-2"/> {getToString(a.user)} <small className="ms-1 text-muted">({DateTime.fromISO(a.startTime).toRelative()})</small>
                {a.isModified && <FontAwesomeIcon icon="pen-to-square" color={"#FFAA44"} title={ConcurrentUserMessage.CurrentlyEditing.niceToString()} style={{ marginLeft: "10px" }} />}
              </div>)}

            {isModified.current ?
              (ticks !== p.entity.ticks ?
                <div className="mt-3">
                  <small>
                  {ConcurrentUserMessage.YouHaveLocalChangesBut0HasAlreadyBeenSavedInTheDatabaseYouWillNotBeAbleToSaveChanges.niceToString().formatHtml(<strong>{getToString(p.entity)}</strong>)}
                    {ConcurrentUserMessage.ConsiderOpening0InANewTabAndApplyYourChangesManually.niceToString().formatHtml(<a href={Navigator.navigateRoute(p.entity)} target="_blank">{getToString(p.entity)}</a>)}
                  </small>
                </div> :
                otherUsers.some(u => u.isModified) && isModified.current ?
                  <div className="mt-3">
                    <small>{ConcurrentUserMessage.LooksLikeYouAreNotTheOnlyOneCurrentlyModifiying0OnlyTheFirstOneWillBeAbleToSaveChanges.niceToString().formatHtml(<strong>{getToString(p.entity)}</strong>)}</small>
                  </div>
                : 
                <div className="mt-3">
                  <small>{ConcurrentUserMessage.YouHaveLocalChangesIn0ThatIsCurrentlyOpenByOtherUsersSoFarNoOneElseHasMadeModifications.niceToString().formatHtml(<strong>{getToString(p.entity)}</strong>)}</small>
                </div>
              ) : ticks !== p.entity.ticks ? 
                <div className="mt-3">
                  <small>
                    {ConcurrentUserMessage.ThisIsNotTheLatestVersionOf0.niceToString().formatHtml(<strong>{getToString(p.entity)}</strong>)}
                    <button className="btn btn-primary btn-sm" onClick={p.onReload}>{ConcurrentUserMessage.ReloadIt.niceToString()}</button>
                  </small>
                </div> : null
            }
          </Popover.Body>
        </Popover>
      }>
      <div className={classes("sf-pointer", isModified.current ? "blinking" : undefined)} title={window.__disableSignalR ?? undefined}>
        <FontAwesomeIcon icon={otherUsers.length == 1 ? "user" : otherUsers.length == 2 ? "user-group" : "users"}
          color={ticks !== p.entity.ticks ? "#E4032E" : otherUsers.some(u => u.isModified) ? "#FFAA44" : "#6BB700"} />
        <strong className="ms-1 me-3" style={{ userSelect: "none" }}>{UserEntity.niceCount(otherUsers.length)}</strong>
      </div>
    </OverlayTrigger>
  );
}
