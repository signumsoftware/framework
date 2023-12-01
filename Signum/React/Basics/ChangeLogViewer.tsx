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

export default function ChangeLogViewer() {

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

  return (
    <a style={{ alignSelf: "center" }} title="Change logs" onClick={(() => {
      MessageModal.show({
        title: "Change logs",
        size: 'md',
        message: <ShowLogs logs={logs!} date={date} />,
        buttons: "ok"
      });
    })}>
      <FontAwesomeIcon icon="list-timeline" color={logs?.some(l => !date ? true : DateTime.fromISO(l.date) > date) ? "orange" : undefined} />
    </a>
  );
}

function ShowLogs(p: { logs: ChangeItem[], date: DateTime | null }) {

  const [seeMore, setSeeMore] = React.useState(2);

  var logsByDate = p.logs.orderByDescending(l => l.date).groupBy(l => l.date);
  var filterdLogs = logsByDate.slice(0, seeMore);

  return (
    <div>
      {filterdLogs.map(gr =>
        <div>
          {p.date == null || p.date < DateTime.fromISO(gr.key) ? <strong>{gr.key}</strong> : <span>{gr.key}</span>}

          <ul className="mb-2 p-0" key={gr.key}>
            {gr.elements.flatMap(e =>Array.isArray(e.changeLog) ? e.changeLog.map(cl => ({module: e.module, changeLog: cl })) : [{module: e.module, changeLog: e.changeLog}])
                .map((a, i) => <li className="ms-5 pb-1" key={i}><strong><samp>{a.module}{" > "}</samp></strong>{a.changeLog}</li>)}
          </ul>
        </div>
      )}
      {logsByDate && logsByDate?.length > seeMore && <a href="#" style={{ display: "block" }} onClick={() => setSeeMore(a => a + 2)}>{ChangeLogMessage.SeeMore.niceToString()}</a>}
    </div>
  );
}
