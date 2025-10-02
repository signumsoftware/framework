import React, { useState } from "react";
import { ChangeLogClient } from './ChangeLogClient'
import { useAPI, useAPIWithReload } from "../Hooks";
import MessageModal from "../Modals/MessageModal";
import { DateTime } from "luxon";
import { Last } from "react-bootstrap/esm/PageItem";
import { ChangeLogMessage } from "../Signum.Basics";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import * as AppContext from "../AppContext";
import { ConnectionMessage, JavascriptMessage } from "../Signum.Entities";
import { OverlayTrigger, Tooltip, Button } from "react-bootstrap";
import { VersionInfo, VersionInfoTooltip } from "../Frames/VersionChangedAlert";
import './ChangeLog.css'



export default function ChangeLogViewer(p: { extraInformation?: string }): React.ReactElement {
  var hasUser = AppContext.currentUser != null;

  var [lastDateString, reloadLastDate] = useAPIWithReload(() => !hasUser ? null : ChangeLogClient.API.getLastDate(), [hasUser], { avoidReset: true });
  var logs = useAPI(() => !hasUser ? null : ChangeLogClient.getChangeLogs(), [hasUser]);

  if (!hasUser || logs == null)
    return <VersionInfo extraInformation={p.extraInformation} />;

  var lastDate = lastDateString ? DateTime.fromISO(lastDateString) : null;

  var countLogs = logs.filter(l => !lastDate ? true : DateTime.fromISO(l.deployDate) > lastDate).length;

  return (
    <OverlayTrigger
      placement={"bottom-end"}
      overlay={
        <Tooltip id={`tooltip-buildId`}>
          <VersionInfoTooltip extraInformation={p.extraInformation} />
        </Tooltip>
      }>
      <a href="#" className="sf-pointer nav-link" onClick={async e => {
        e.preventDefault();
        await MessageModal.show({
          title: "Change logs",
          size: 'md',
          message: <ShowLogs logs={logs!} lastDate={lastDate} />,
          buttons: "ok"
        });

        await ChangeLogClient.API.updateLastDate();
        reloadLastDate();
      }}>
        <FontAwesomeIcon icon="circle-info" />
        <span className="sr-only">{ConnectionMessage.VersionInfo.niceToString()}</span>
        {countLogs > 0 && <span className="badge text-bg-info badge-pill sf-change-log-badge">{countLogs}</span>}
      </a>
  </OverlayTrigger>
  );
}

function ShowLogs(p: { logs: ChangeLogClient.ChangeItem[], lastDate: DateTime | null }) {

  const [seeMore, setSeeMore] = React.useState(2);

  var logsByDate = p.logs.orderByDescending(l => l.deployDate).groupBy(l => l.deployDate);
  var filterdLogs = logsByDate.slice(0, seeMore);

  return (
    <div>
      {filterdLogs.map(gr =>
        <div key={gr.key}>
          {p.lastDate == null || p.lastDate < DateTime.fromISO(gr.key) ? <strong title={"Deployed on " + gr.key}>{gr.key}</strong> : <span>{gr.key}</span>}

          <ul className="mb-2 p-0" key={gr.key}>
            {gr.elements.flatMap(e => Array.isArray(e.changeLog) ? e.changeLog.map(cl => ({ module: e.module, implDate: e.implDate, changeLog: cl, })) : [{ module: e.module, implDate: e.implDate, changeLog: e.changeLog }])
              .map((a, i) => <li className="ms-5 pb-1" key={i}><strong title={"Implemented on " + a.implDate}><samp>{a.module}{" > "}</samp></strong>{a.changeLog}</li>)}
          </ul>
        </div>
      )}
      {logsByDate && logsByDate?.length > seeMore && <a href="#" style={{ display: "block" }} onClick={() => setSeeMore(a => a + 2)}>{ChangeLogMessage.SeeMore.niceToString()}</a>}
    </div>
  );
}
