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
import { LinkButton } from "./LinkButton";



export default function ChangeLogViewer(p: { extraInformation?: string }): React.ReactElement {
  const hasUser = AppContext.currentUser != null;

  const [lastDateString, reloadLastDate] = useAPIWithReload(
    () => (!hasUser ? null : ChangeLogClient.API.getLastDate()),
    [hasUser],
    { avoidReset: true }
  );

  const logs = useAPI(() => (!hasUser ? null : ChangeLogClient.getChangeLogs()), [hasUser]);

  const triggerRef = React.useRef<HTMLAnchorElement | null>(null);

  if (!hasUser || logs == null)
    return <VersionInfo extraInformation={p.extraInformation} />;

  const lastDate = lastDateString ? DateTime.fromISO(lastDateString) : null;

  const countLogs = logs.filter(l => !lastDate ? true : DateTime.fromISO(l.deployDate) > lastDate).length;

  async function handleOpenChangeLogs(e: React.MouseEvent<HTMLAnchorElement>) {

    await MessageModal.show({
      title: ChangeLogMessage.ChangeLogs.niceToString(),
      size: 'md',
      message: <ShowLogs logs={logs!} lastDate={lastDate} />,
      buttons: "ok",
      autoFocusonTitle: true,
    });


    if (triggerRef.current) {
      triggerRef.current.focus();
    }

    await ChangeLogClient.API.updateLastDate();
    reloadLastDate();
  }

  return (
    <OverlayTrigger
      placement="bottom-end"
      overlay={
        <Tooltip id="tooltip-buildId">
          <VersionInfoTooltip extraInformation={p.extraInformation} />
        </Tooltip>
      }
    >
      <LinkButton
        title={undefined}
        ref={triggerRef}
        className="sf-pointer nav-link"
        aria-haspopup="dialog"
        aria-expanded="false"
        onClick={handleOpenChangeLogs}
        onKeyDown={e => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            handleOpenChangeLogs(e as any);
          }
        }}
      >
        <FontAwesomeIcon icon="circle-info" aria-hidden="true" />
        <span className="sr-only">{ConnectionMessage.VersionInfo.niceToString()}</span>
        {countLogs > 0 && (
          <span className="badge text-bg-info badge-pill sf-change-log-badge">
            {countLogs}
          </span>
        )}
      </LinkButton>
    </OverlayTrigger>
  );
}

function ShowLogs(p: { logs: ChangeLogClient.ChangeItem[], lastDate: DateTime | null }) {

  const [seeMore, setSeeMore] = React.useState(2);

  const logsByDate = p.logs.orderByDescending(l => l.deployDate).groupBy(l => l.deployDate);
  const filterdLogs = logsByDate.slice(0, seeMore);

  const firstNewItemRef = React.useRef<HTMLLIElement | null>(null);

  const handleSeeMore = React.useCallback(() => {
    setSeeMore(prev => prev + 2);
  }, []);

  return (
    <div role="region" aria-label={ChangeLogMessage.ChangeLogEntries.niceToString()}>
      {filterdLogs.map((gr, groupIndex) => {
        const deployedDate = DateTime.fromISO(gr.key);
        const isNew = p.lastDate == null || p.lastDate < deployedDate;

        return (
          <section key={gr.key} aria-labelledby={`deployed-${gr.key}`}>
            <h3 id={`deployed-${gr.key}`}>
              <time dateTime={gr.key} title={ChangeLogMessage.DeployedOn0.niceToString(gr.key)}>
                {isNew ? <strong>{gr.key}</strong> : gr.key}
              </time>
            </h3>

            <ul className="mb-2 p-0" role="list">
              {gr.elements.flatMap(e =>
                Array.isArray(e.changeLog)
                  ? e.changeLog.map(cl => ({
                    module: e.module,
                    implDate: e.implDate,
                    changeLog: cl,
                  }))
                  : [{ module: e.module, implDate: e.implDate, changeLog: e.changeLog }]
              ).map((a, i) => {
                const isFirstNew = groupIndex === seeMore - 2 && i === 0; //first entry when see more is clicked

                return (
                  <li
                    ref={isFirstNew ? firstNewItemRef : null}
                    className="ms-5 pb-1"
                    key={i}
                    tabIndex={0}
                    role="article"
                    aria-label={ChangeLogMessage._0ImplementedOn1WithFollowingChanges2.niceToString(a.module, a.implDate, a.changeLog)}>
                    <strong>
                      <samp>{a.module} &gt; </samp>
                    </strong>
                    <span>{a.changeLog}</span>
                  </li>
                );
              })}
            </ul>
          </section>
        );
      })}

      {logsByDate && logsByDate.length > seeMore && (
        <button
          type="button"
          style={{
            display: "block",
            background: "none",
            border: "none",
            color: "#007bff",
            textDecoration: "underline",
            cursor: "pointer"
          }}
          onClick={handleSeeMore}
          aria-label={ChangeLogMessage.SeeMoreChangeLogEntries.niceToString()}>
          {ChangeLogMessage.SeeMore.niceToString()}
        </button>
      )}
    </div>
  );
}
