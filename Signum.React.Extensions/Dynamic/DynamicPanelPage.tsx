import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { StyleContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { ajaxPost } from '../../../Framework/Signum.React/Scripts/Services'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { CountSearchControl, SearchControl } from '../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, Options, CompilationError } from './DynamicClient'
import * as AuthClient from '../Authorization/AuthClient'
import { DynamicPanelPermission } from './Signum.Entities.Dynamic'


interface DynamicPanelProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

export default class DynamicPanelPage extends React.Component<DynamicPanelProps, void> {

    render() {

        AuthClient.asserPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel);
        
        var tabList = [<Tab key="compile" title="Compile">
            <DynamicCompileTab />
        </Tab>]
            .concat(Options.onGetDynamicTab.map(a => a()));

        var tabs = React.cloneElement(<Tabs defaultActiveKey="compile" />, undefined, ...tabList);
        
        return (
            <div>
                <h2>Dynamic Panel</h2>

                {tabs}
            </div>
        );
    }
}


interface DynamicPanelState {
    complationErrors?: CompilationError[];
    applicationRestarting?: moment.Moment;
}

export class DynamicCompileTab extends React.Component<{}, DynamicPanelState>{

    constructor(props: any) {
        super(props);
        this.state = {};
    }

    handleCompile = (e: React.MouseEvent) => {
        API.getCompilationErrors()
            .then(errors => this.state.complationErrors = errors)
            .done();
    }

    restartInterval?: number;

    handleRestartApplication = (e: React.MouseEvent) => {
        API.restartApplication()
            .then(() => {
                this.changeState(s => s.applicationRestarting = moment());
                this.restartInterval = setInterval(() => {
                    API.pingApplication()
                        .then(s => this.changeState(s => s.applicationRestarting = undefined))
                        .catch(error => { throw error; })
                        .done();
                }, 500);
            })
            .catch(error => { throw error; })
            .done();
    }

    componentWillUnmount() {
        if (this.restartInterval)
            clearInterval(this.restartInterval);
    }

    render() {

        var sc = new StyleContext(undefined, { labelColumns: { sm: 4 } });

        const lines = Options.onGetDynamicLine.map(f => f(sc));
        const lineContainer = React.cloneElement(<div />, undefined, ...lines);

        const errors = this.state.complationErrors;

        return (
            <div>
                {lineContainer}

                <div className="btn-toolbar">
                    {<a href="#" className="sf-button btn btn-success" onClick={this.handleCompile}>Compile</a>}
                </div>

                {errors && this.renderErrors(errors)}
                {
                    this.state.applicationRestarting ?
                        this.renderProgress(this.state.applicationRestarting) :
                        AuthClient.isPermissionAuthorized(DynamicPanelPermission.RestartApplication) &&
                        <div className="btn-toolbar">
                            {<a href="#" className="sf-button btn btn-danger" onClick={this.handleRestartApplication}>Restart Application</a>}
                        </div>
                }
            </div>
        );
    }

    renderProgress(since: moment.Moment) {
        return (<div className="progress">
            <div className="progress-bar progress-bar-striped active" role="progressbar" style={{ width: "100%" }}>
                <span className="sr-only">Restarting for {moment().diff(since) / 1000} seconds</span>
            </div>
        </div>);
    }

    renderErrors(errors: CompilationError[]) {

        return (
            <div>
                <div className={`alert alert-${errors.length}`} role="alert">
                    <strong>{errors.length}Errors!</strong> {errors.length == 0 ?
                        "The dynamic code compiled successfully" :
                        "Please fix this errors in the dynamic entities"}
                </div>

                <table>
                    <thead>
                        <tr>
                            <th>Error Code</th>
                            <th>Error Message</th>
                            <th>File</th>
                        </tr>
                    </thead>
                    <tbody>
                        {
                            errors.map((e, i) => <tr>
                                <td>{e.errorCode}</td>
                                <td>{e.errorMessage}</td>
                                <td>{e.fileName}({e.line}:{e.column})</td>
                            </tr>)
                        }
                    </tbody>
                </table>
            </div>
        );
    }
}



