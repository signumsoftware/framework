import * as React from 'react'
import { classes } from '../Globals'
import { VersionFilter } from '../Services'
import { ConnectionMessage } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import './VersionChangedAlert.css'
import { useForceUpdate } from '../Hooks';

export default function VersionChangedAlert(p: { blink?: boolean }) {

  var forceUpdate = useForceUpdate();

  React.useEffect(() => {
    VersionChangedAlert.forceUpdateSingletone = forceUpdate;
    return () => VersionChangedAlert.forceUpdateSingletone = undefined;
  })

  function handleRefresh(e: React.MouseEvent<any>) {
    e.preventDefault();
    location.reload(true);
  }

  if (VersionFilter.latestVersion == VersionFilter.initialVersion)
    return null;

  return (
    <div className={classes("alert alert-warning", "version-alert", p.blink && "blink")} style={{ textAlign: "center" }}>
      <FontAwesomeIcon icon="sync-alt" aria-hidden="true" />&nbsp;
                {ConnectionMessage.ANewVersionHasJustBeenDeployedSaveChangesAnd0.niceToString()
        .formatHtml(<a href="#" onClick={handleRefresh}>{ConnectionMessage.Refresh.niceToString()}</a>)}
    </div>
  );
}

VersionChangedAlert.forceUpdateSingletone = undefined as (() => void) | undefined;
VersionChangedAlert.defaultProps = { blink: true };
