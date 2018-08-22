import * as React from 'react'
import { Dic, classes } from '../Globals'
import { VersionFilter } from '../Services'
import { ConnectionMessage } from '../Signum.Entities';

import './VersionChangedAlert.css'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export default class VersionChangedAlert extends React.Component<{ blink?: boolean }>{

    static defaultProps = { blink: true };

    handleRefresh = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        location.reload(true);
    }

    static singleton: VersionChangedAlert | undefined;

    componentWillMount() {
        VersionChangedAlert.singleton = this;
    }

    componentWillUnmount() {
        if (VersionChangedAlert.singleton == this)
            VersionChangedAlert.singleton = undefined;
    }

    render() {
        if (VersionFilter.latestVersion == VersionFilter.initialVersion)
            return null;

        return (
            <div className={classes("alert alert-warning", "version-alert", this.props.blink && "blink")} style={{ textAlign: "center" }}>
                <FontAwesomeIcon icon="sync-alt" aria-hidden="true" />&nbsp;
                {ConnectionMessage.ANewVersionHasJustBeenDeployedSaveChangesAnd0.niceToString()
                    .formatHtml(<a href="#" onClick={this.handleRefresh}>{ConnectionMessage.Refresh.niceToString()}</a>)}
            </div>
        );
    }
}

