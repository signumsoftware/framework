import * as React from 'react'
import { DateTime } from 'luxon'
import * as AppContext from '@framework/AppContext'
import { useSignalRCallback, useSignalRConnection, useSignalRGroup } from '../Alerts/useSignalR'
import { ConcurrentUserEntity, ConcurrentUserMessage } from './Signum.Entities.ConcurrentUser'
import { OverlayTrigger, Popover } from 'react-bootstrap';
import { Entity, Lite, liteKey, toLite } from '@framework/Signum.Entities'
import { UserEntity } from '../Authorization/Signum.Entities.Authorization'
import { useAPI, useForceUpdate, useUpdatedRef } from '../../Signum.React/Scripts/Hooks'
import { GraphExplorer } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { classes } from '@framework/Globals';
import MessageModal from '@framework/Modals/MessageModal'
import './ConcurrentUser.css'
import * as ConcurrentUserClient from './ConcurrentUserClient';

export default function ConcurrentUser(p: { entity: Entity, onReload: ()=> void }) {
  
  const conn = useSignalRConnection("~/api/concurrentUserHub");

  const entityKey = liteKey(toLite(p.entity));
  const userKey = liteKey(toLite(AppContext.currentUser! as UserEntity))
  const startTime = React.useMemo(() => DateTime.utc().toISO(), [entityKey]);

  const [ticks, setTicks] = React.useState<string>(p.entity.ticks);
  const forceUpdate = useForceUpdate();
  React.useEffect(() => {
    setTicks(p.entity.ticks);
  }, [entityKey]);

  useSignalRGroup(conn, {
    enterGroup: co => p.entity == null ? Promise.resolve(undefined) : co.send("EnterEntity", entityKey, startTime, userKey),
    exitGroup: co => p.entity == null ? Promise.resolve(undefined) : co.send("ExitEntity", entityKey, startTime, userKey),
    deps: [entityKey]
  });

  const isModified = React.useRef(false);

  React.useEffect(() => {

    if (conn) {

      function updateModified() {
        GraphExplorer.propagateAll(p.entity);

        if (p.entity.modified != isModified.current) {
          conn?.send("EntityModified", entityKey, startTime, userKey, p.entity.modified).done();
          isModified.current = p.entity.modified;
        }
      }

      updateModified();
      const handler = setInterval(() => {
        updateModified();
      }, 1000);

      return () => clearInterval(handler);
    }
  }, [conn, p.entity, ticks]);

  var [refreshKey, setRefreshKey] = React.useState(0);

  var concurrentUsers = useAPI(() => ConcurrentUserClient.API.getUsers(entityKey), [refreshKey, isModified.current, entityKey]);

  useSignalRCallback(conn, "EntitySaved", (a: string) => setTicks(a), []);

  useSignalRCallback(conn, "ConcurrentUsersChanged", () => setRefreshKey(a => a + 1), []);


  const ticksRef = useUpdatedRef(ticks);
  const entityRef = useUpdatedRef(p.entity);

  React.useEffect(() => {
    const handle = setTimeout(() => {
      if (ticksRef.current != null && ticksRef.current != entityRef.current.ticks) {
        MessageModal.show({
          title: ConcurrentUserMessage.DatabaseChangesDetected.niceToString(),
          style: "warning",
          message:
            <div>
              <p>{ConcurrentUserMessage.LooksLikeSomeoneJustSaved0ToTheDatabase.niceToString().formatHtml(<strong>{p.entity.toStr}</strong>)}</p>
              <p>{ConcurrentUserMessage.DoYouWantToReloadIt.niceToString()}</p>
              {isModified.current &&
                <>
                  <p className="text-danger">
                    {ConcurrentUserMessage.WarningYouWillLostYourCurrentChanges.niceToString()}
                  </p>
                  <p>
                    {ConcurrentUserMessage.ConsiderOpening0InANewTabAndApplyYourChangesManually.niceToString().formatHtml(<a href={Navigator.navigateRoute(p.entity)} target="_blank">{p.entity.toStr}</a>)}
                  </p>
                </>
              }
            </div>,
          buttons: "yes_cancel",
        }).then(b => b == "yes" && p.onReload())
          .done();
      }
    }, 1000);
    return () => clearTimeout(handle);
  }, [ticks !== p.entity.ticks]);


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
            
            {otherUsers.map((a, i) =>
              <div key={i} style={{ whiteSpace: "nowrap" }} >
                <UserCircle user={a.user} /> {a.user.toStr} ({DateTime.fromISO(a.startTime).toRelative()})
                {a.isModified && <FontAwesomeIcon icon="edit" color={"#FFAA44"} title={ConcurrentUserMessage.CurrentlyEditing.niceToString()} style={{ marginLeft: "10px" }} />}
              </div>)}

            {isModified.current &&
              (ticks !== p.entity.ticks ?
                <div className="mt-3">
                  <small>
                    {ConcurrentUserMessage.YouHaveLocalChangesButTheEntityHasBeenSavedInTheDatabaseYouWillNotBeAbleToSaveChanges.niceToString().formatHtml(<strong>{p.entity.toStr}</strong>)}
                    {ConcurrentUserMessage.ConsiderOpening0InANewTabAndApplyYourChangesManually.niceToString().formatHtml(<a href={Navigator.navigateRoute(p.entity)} target="_blank">{p.entity.toStr}</a>)}
                  </small>
                </div> :
                otherUsers.some(u => u.isModified) && isModified.current ?
                  <div className="mt-3">
                    <small>{ConcurrentUserMessage.LooksLikeYouAreNotTheOnlyOneCurrentlyModifiying0OnlyTheFirstOneWillBeAbleToSaveChanges.niceToString().formatHtml(<strong>{p.entity.toStr}</strong>)}</small>
                  </div>
                  : null
              )
            }
          </Popover.Body>
        </Popover>
      }>
      <div className={(otherUsers.some(u => u.isModified) || ticks !== p.entity.ticks) && isModified.current ? "blinking" : undefined}>
        <FontAwesomeIcon icon={otherUsers.length == 1 ? "user" : otherUsers.length == 2 ? "user-friends" : "users"}
          color={ticks !== p.entity.ticks ? "#E4032E" : otherUsers.some(u => u.isModified) ? "#FFAA44" : "#6BB700"} />
        <strong className="ms-1 me-3" style={{ userSelect: "none" }}>{UserEntity.niceCount(otherUsers.length)}</strong>
      </div>
    </OverlayTrigger>
  );
}

const colors = "#750b1c #a4262c #d13438 #ca5010 #986f0b #498205 #0b6a0b #038387 #005b70 #0078d4 #004e8c #4f6bed #5c2e91 #8764b8 #881798 #c239b3 #e3008c #8e562e #7a7574 #69797e".split(" ");

export function getUserColor(u: Lite<UserEntity>): string {

  var id = u.id as number;

  return colors[id % colors.length];
}

export function getUserInitials(u: Lite<UserEntity>): string {
  return u.toStr?.split(" ").map(m => m[0]).filter((a, i) => i < 2).join("").toUpperCase() ?? "";
}

export function UserCircle(p: { user: Lite<UserEntity>, className?: string }) {
  var color = getUserColor(p.user);
  return (
    <span className={classes("user-circle", p.className)} style={{ color: "white", backgroundColor: color }} title={p.user.toStr}>
      {getUserInitials(p.user)}
    </span>
  );
}

