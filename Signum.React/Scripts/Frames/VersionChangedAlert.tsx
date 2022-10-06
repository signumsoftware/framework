import * as React from 'react'
import { DateTime } from 'luxon'
import { classes } from '../Globals'
import { VersionFilter } from '../Services'
import { ConnectionMessage } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import './VersionChangedAlert.css'
import { useForceUpdate } from '../Hooks';
import { Button, OverlayTrigger, Tooltip } from 'react-bootstrap';
import { Placement } from 'popper.js';

export function VersionChangedAlert(p: { blink?: boolean }) {

  var forceUpdate = useForceUpdate();

  React.useEffect(() => {
    VersionChangedAlert.forceUpdateSingletone = forceUpdate;
    return () => VersionChangedAlert.forceUpdateSingletone = undefined;
  })

  function handleRefresh(e: React.MouseEvent<any>) {
    e.preventDefault();
    location.reload();
  }

  if (VersionFilter.latestVersion == VersionFilter.initialVersion)
    return null;

  return (
    <div className={classes("alert alert-warning", "version-alert", p.blink && "blink")} style={{ textAlign: "center" }}>
      <FontAwesomeIcon icon="rotate" aria-hidden="true" />&nbsp;
                {ConnectionMessage.ANewVersionHasJustBeenDeployedSaveChangesAnd0.niceToString()
        .formatHtml(<a href="#" onClick={handleRefresh}>{ConnectionMessage.Refresh.niceToString()}</a>)}
    </div>
  );
}

VersionChangedAlert.forceUpdateSingletone = undefined as (() => void) | undefined;
VersionChangedAlert.defaultProps = { blink: true };

export function VersionInfo(p: { extraInformation?: string }) {
  return (
    <div className="nav-link">
      <OverlayTrigger
        placement={"bottom-end"}
        overlay={
          <Tooltip id={`tooltip-buildId`}>
            <VersionInfoTooltip extraInformation={p.extraInformation} />
          </Tooltip>
        }>
        <div><FontAwesomeIcon icon="circle-info" className="sf-version-info" /></div>
      </OverlayTrigger>
    </div>
  );
}

function VersionInfoTooltip(p: { extraInformation?: string}) {

  var bt = DateTime.fromISO(VersionFilter.initialBuildTime!);

  return (
    <div style={{ whiteSpace: "nowrap" }}>
      Version {VersionFilter.initialVersion}
      <br/>
      {bt.toFormat("DDDD")}
      <br />
      {bt.toFormat("tt")} ({bt.toRelative()})
      {p.extraInformation && <br/>}
      {p.extraInformation}
    </div>
  );
}

