import React, { useState } from "react";
import { API, ChangeLogLine, changeLogs } from './ChangeLogClient'
import { useAPI } from "../Hooks";
import MessageModal from "../Modals/MessageModal";
import { DateTime } from "luxon";
import { Last } from "react-bootstrap/esm/PageItem";
import { ChangeLogMessage } from "../Signum.Basics";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { JavascriptMessage } from "../Signum.Entities";

interface ChangeItem {
  module: string;
  date: string;
  changeLog: ChangeLogLine | ChangeLogLine[];
}

export function ChangeLogViewer() {

  var changes = useAPI(() => Promise.all(Object.values(changeLogs).map(v => v())), []);

  var lastDate = useAPI(() => API.getLastDate(), []);

  var logs = changes == null ? null : Object.keys(changeLogs).flatMap((m, i) => {
    var dic = changes![i].default;
    return Object.keys(dic).map<ChangeItem>(date => {
      return { module: m, date: date, changeLog: dic[date] };
    });
  });
  
  if (logs == null)
    return null;
  
  var date = lastDate ? DateTime.fromISO(lastDate) : null;

  return (<a style={{ alignSelf: "center" }} title="Change logs" onClick={() => {
    MessageModal.show({
      title: "Change logs",
      size: 'md',
      message: <ShowLogs logs={logs} date={date} />,
      buttons: "ok"
    });
  }}><FontAwesomeIcon icon="list-timeline" color={logs?.filter(l => !date ? true : l.date >= date.toFormat('yyyy.MM.d')).length ? "orange" : undefined} /></a>);
}

function ShowLogs(p: { logs: ChangeItem[] | null, date: DateTime | null }) {
  const [seeMore, setSeeMore] = React.useState(false);
  var filterdLogs = !seeMore && p.date ? p.logs?.filter(l => l.date >= p.date.toFormat('yyyy.MM.d')) : p.logs;

  return (
    <div>
      {filterdLogs?.length == 0 ? <div>
        {ChangeLogMessage.ThereIsNotAnyNewChangesFrom0.niceToString(p.date!.toLocaleString())}

        <a href="#" style={{ display: "block" }} onClick={() => setSeeMore(true)}>{ChangeLogMessage.SeeMore.niceToString()}</a>
        
      </div> :
        filterdLogs!.orderByDescending(l => l.date).groupBy(l => l.date).map(gr => <ul className="mb-2 p-0" key={gr.key}><strong>{gr.key}</strong>
          {gr.elements.map((e, i) => <li className="ms-5 pb-1" key={i}><strong><samp>{e.module}{" > "}</samp></strong>{Array.isArray(e.changeLog) ? e.changeLog.joinComma(', ') : e.changeLog.toString()}</li>)}
        </ul>)}
    </div>
  );
}
