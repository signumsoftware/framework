import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { classes } from '../../../Framework/Signum.React/Scripts/Globals'
import { StyleContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { ajaxPost } from '../../../Framework/Signum.React/Scripts/Services'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { CountSearchControl, SearchControl } from '../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, Options, CompilationError } from './DynamicClient'
import CSharpCodeMirror from '../Codemirror/CSharpCodeMirror'
import * as AuthClient from '../Authorization/AuthClient'
import { DynamicPanelPermission, DynamicTypeMessage } from './Signum.Entities.Dynamic'

require("!style!css!./DynamicPanelPage.css");

interface DynamicPanelProps extends ReactRouter.RouteComponentProps<{}, {}> {
}

export default class DynamicPanelPage extends React.Component<DynamicPanelProps, void> {

    handleSelect = (key: any /*string*/) => {
        Navigator.currentHistory.push("~/dynamic/panel?step=" + key);
    }

    render() {
        AuthClient.asserPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel);

        let step = this.props.location.query["step"] as "compile" | "restartServer" | "migrations" | "refreshClients" | undefined;

        return (
            <Tabs defaultActiveKey={step || "compile"} id="dynamicPanelTabs" style={{ marginTop: "20px" }} onSelect={this.handleSelect}>
                <Tab eventKey="compile" title="1. Check">
                    <CompileStep />
                </Tab>
        
                <Tab eventKey="restartServer" title="2. Restart Server">
                    <RestartServerStep />
                </Tab>

                {Options.getDynaicMigrationsStep &&

                    <Tab eventKey="migrations" title="3. Sql Migrations">
                        {Options.getDynaicMigrationsStep()}
                    </Tab>
                }
                <Tab eventKey="refreshClients" title={(Options.getDynaicMigrationsStep ? "4." : "3.") + " Refresh Clients"}>
                    <RefreshClientsStep />
                </Tab>
            </Tabs>
        );
    }
}

interface DynamicCompileStepState {
    complationErrors?: CompilationError[];
    selectedErrorIndex?: number;
    applicationRestarting?: moment.Moment;
}

export class CompileStep extends React.Component<void, DynamicCompileStepState>{

    constructor(props: any) {
        super(props);
        this.state = {};
    }

    handleCompile = (e: React.MouseEvent) => {
        e.preventDefault();
        API.getCompilationErrors()
            .then(errors => this.changeState(s => { s.complationErrors = errors; s.selectedErrorIndex = undefined; }))
            .done();
    }

    render() {

        var sc = new StyleContext(undefined, { labelColumns: { sm: 3 } });

        const lines = Options.onGetDynamicLineForPanel.map(f => f(sc));
        const lineContainer = React.cloneElement(<div className="form-horizontal" />, undefined, ...lines);

        const errors = this.state.complationErrors;

        return (
            <div>
                {lineContainer}
                <br />
                {<a href="#" className="sf-button btn btn-success" onClick={this.handleCompile}>Compile</a>}
                {errors && this.renderCompileResult(errors)}
            </div>
        );
    }

    renderCompileResult(errors: CompilationError[]) {

        return (
            <div>
                <br/>
                <div className={`alert alert-${errors.length == 0 ? "success" : "danger"}`} role="alert">
                    <strong>{errors.length} Errors!</strong> {errors.length == 0 ?
                        "The dynamic code compiled successfully" :
                        "Please fix this errors in the dynamic entities"}
                </div>
                <br />
                {errors.length > 0 && this.renderErrorTable(errors) }
            </div>
        );
    }

    renderErrorTable(errors: CompilationError[]) {
        var err = this.state.selectedErrorIndex == null ? undefined : errors[this.state.selectedErrorIndex]

        return (
            <div>
                <table className="table table-condensed">
                    <thead style={{ color: "#a94464" }}>
                        <tr>
                            <th>Error Number</th>
                            <th>Error Text</th>
                            <th>File</th>
                        </tr>
                    </thead>
                    <tbody>
                        {
                            errors.map((e, i) =>
                                <tr key={i}
                                    onClick={() => this.changeState(s => s.selectedErrorIndex = i)}
                                    className={classes("dynamic-error-line", i == this.state.selectedErrorIndex ? "active" : undefined)}>
                                    <td>{e.errorNumber}</td>
                                    <td>{e.errorText}</td>
                                    <td>{e.fileName}({e.line}:{e.column})</td>
                                </tr>
                            )
                        }
                    </tbody>
                </table>
                {err && <div className="code-container">
                    <h4>{err.fileName}</h4>
                    <CSharpCodeMirror
                        script={err.fileContent}
                        isReadOnly={true}
                        errorLineNumber={err.line} />
                </div>
                }
            </div>
        );
    }
}

interface RestartServerStepState {
    serverRestarting?: moment.Moment;
}

export class RestartServerStep extends React.Component<{}, RestartServerStepState>{

    constructor(props: any) {
        super(props);
        this.state = {};
    }

    handleRestartApplication = (e: React.MouseEvent) => {
        e.preventDefault();
        API.restartServer()
            .then(() => {
                this.changeState(s => s.serverRestarting = moment());
                API.pingServer()
                    .then(() => { this.changeState(s => s.serverRestarting = undefined); })
                    .catch(error => {
                        this.changeState(s => s.serverRestarting = undefined);
                        throw error;
                    })
                    .done();
            })
            .done();
    }

    render() {
        return (
            <div>
                {
                    this.state.serverRestarting ?
                        this.renderProgress(this.state.serverRestarting) :
                        AuthClient.isPermissionAuthorized(DynamicPanelPermission.RestartApplication) &&
                        <a href="#" className="sf-button btn btn-danger" onClick={this.handleRestartApplication}>Restart Server</a>
                }
            </div>
        );
    }

    renderProgress(since: moment.Moment) {

        return (
            <div className="progress">
                <div className="progress-bar progress-bar-striped progress-bar-warning active" role="progressbar" style={{ width: "100%" }}>
                    <span>Restarting...</span>
                </div>
            </div>
        );
    }

}

interface RefreshClientsStepState {

}

export class RefreshClientsStep extends React.Component<{}, RefreshClientsStepState>{

    handleRefreshClient = (e: React.MouseEvent) => {
        e.preventDefault();
        window.location.reload(true);
    }

    render() {
        return (
            <div>
                <p>Now you need to refresh the clients manually (i.e. pressing F5).</p>
                <a href="#" className="sf-button btn btn-warning" onClick={this.handleRefreshClient}>Refresh this client</a>
            </div>
        );
    }

}




