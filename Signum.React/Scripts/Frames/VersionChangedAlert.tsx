import * as React from 'react'
import { Dic, classes } from '../Globals'
import { VersionFilter } from '../Services'
import { ConnectionMessage } from '../Signum.Entities';

import './VersionChangedAlert.css'

export default class VersionChangedAlert extends React.Component<{ blink?: boolean }>{

    static defaultProps = { blink: true };

    handleRefresh = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        location.reload(true);
    }

    render() {
        if (VersionFilter.latestVersion == VersionFilter.initialVersion)
            return null;

        return (
            <div className={classes("alert alert-warning", "version-alert", this.props.blink && "blink")} style={{ textAlign: "center" }}>
                <i className="fa fa-refresh" aria-hidden="true" />&nbsp;
                {ConnectionMessage.ANewVersionHasJustBeenDeployedSaveChangesAnd0.niceToString()
                    .formatHtml(<a href="#" onClick={this.handleRefresh}>{ConnectionMessage.Refresh.niceToString()}</a>)}
            </div>
        );
    }
}

