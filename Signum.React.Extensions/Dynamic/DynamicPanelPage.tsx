import * as React from 'react'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { classes } from '../../../Framework/Signum.React/Scripts/Globals'
import { StyleContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { ajaxPost } from '../../../Framework/Signum.React/Scripts/Services'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { WebApiHttpError } from '../../../Framework/Signum.React/Scripts/Services'
import { ValueSearchControl, SearchControl } from '../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, Options, CompilationError } from './DynamicClient'
import CSharpCodeMirror from '../Codemirror/CSharpCodeMirror'
import * as AuthClient from '../Authorization/AuthClient'
import { DynamicPanelPermission, DynamicTypeMessage } from './Signum.Entities.Dynamic'
import { RouteComponentProps } from "react-router";
import * as QueryString from 'query-string';

import "./DynamicPanelPage.css"
import { Tab, Tabs } from '../../../Framework/Signum.React/Scripts/Components/Tabs';

interface DynamicPanelProps extends RouteComponentProps<{}> {
}

interface DynamicPanelState {
    startErrors?: WebApiHttpError[];
}

export default class DynamicPanelPage extends React.Component<DynamicPanelProps, DynamicPanelState> {

    handleSelect = (key: any /*string*/) => {
        Navigator.history.push("~/dynamic/panel?step=" + key);
    }

    handleErrorClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        this.handleSelect("restartServerApp");
    }

    constructor(props: DynamicPanelProps) {
        super(props);

        this.state = {};
    }

    componentWillMount() {
        API.getStartErrors()
            .then(errors => this.setState({ startErrors: errors }))
            .done();
    }

    render() {
        AuthClient.asserPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel);

        let step = QueryString.parse(this.props.location.search).step as "compile" | "restartServerApp" | "migrations" | "refreshClients" | undefined;

        const errors = this.state.startErrors
        return (
            <div>
                {errors && errors.length > 0 &&
                    <div role="alert" className="alert alert-danger" style={{ marginTop: "20px" }}>
                        <p>
                        <span className="fa fa-exclamation-triangle"></span>
                        {" "}The server started, but there {errors.length > 1 ? "are" : "is"} <a href="#" onClick={this.handleErrorClick}>{errors.length} {errors.length > 1 ? "errors" : "error"}</a>.
                        </p>
                    </div>
                }
                <Tabs activeEventKey={step || "compile"} id="dynamicPanelTabs" style={{ marginTop: "20px" }} toggle={this.handleSelect}>
                    <Tab eventKey="compile" title="1. Edit and Compile">
                        <CompileStep />
                    </Tab>

                    <Tab eventKey="restartServerApp" title="2. Restart Server Application">
                        <RestartServerAppStep
                            startErrors={this.state.startErrors}
                            setStartErrors={errors => this.setState({ startErrors: errors })} />
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
            </div>
        );
    }
}

interface DynamicCompileStepState {
    complationErrors?: CompilationError[];
    selectedErrorIndex?: number;
    applicationRestarting?: moment.Moment;
}

export class CompileStep extends React.Component<{}, DynamicCompileStepState>{

    constructor(props: any) {
        super(props);
        this.state = {};
    }

    handleCompile = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        API.getCompilationErrors()
            .then(errors => this.setState({ complationErrors: errors, selectedErrorIndex: undefined }))
            .done();
    }

    render() {

        var sc = new StyleContext(undefined, { labelColumns: { sm: 3 } });

        const lines = Options.onGetDynamicLineForPanel.map(f => f(sc));
        const lineContainer = React.cloneElement(<div />, undefined, ...lines);

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
                <br />
                <div className={`alert alert-${errors.length == 0 ? "success" : "danger"}`} role="alert">
                    <strong>{errors.length} Errors!</strong> {errors.length == 0 ?
                        "The dynamic code compiled successfully" :
                        "Please fix this errors in the dynamic entities"}
                </div>
                <br />
                {errors.length > 0 && this.renderErrorTable(errors)}
            </div>
        );
    }

    renderErrorTable(errors: CompilationError[]) {
        var err = this.state.selectedErrorIndex == null ? undefined : errors[this.state.selectedErrorIndex]

        return (
            <div>
                <table className="table table-sm">
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
                                    onClick={() => this.setState({ selectedErrorIndex: i })}
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

interface RestartServerAppStepProps {
    setStartErrors: (startErrors?: WebApiHttpError[]) => void;
    startErrors?: WebApiHttpError[];
}

interface RestartServerAppStepState {
    serverRestarting?: moment.Moment;
}

export class RestartServerAppStep extends React.Component<RestartServerAppStepProps, RestartServerAppStepState>{

    constructor(props: any) {
        super(props);
        this.state = {};
    }

    handleRestartApplication = (e: React.MouseEvent<any>) => {
        e.preventDefault();

        API.restartServer()
            .then(() => {
                this.setState({ serverRestarting: moment() });
                this.props.setStartErrors(undefined);
                API.getStartErrors()
                    .then(errors => {
                        this.props.setStartErrors(errors);
                        this.setState({ serverRestarting: undefined });
                    })
                    .catch(error => {
                        this.setState({ serverRestarting: undefined });
                        throw error;
                    })
                    .done();
            })
            .done();
    }

    render() {

        if (this.state.serverRestarting)
            return this.renderProgress(this.state.serverRestarting);

        return (
            <div>
                {
                    AuthClient.isPermissionAuthorized(DynamicPanelPermission.RestartApplication) &&
                    <a href="#" className="sf-button btn btn-danger" onClick={this.handleRestartApplication}>Restart Server Application</a>
                }
                {this.props.startErrors && this.props.startErrors.map((e, i) => <ErrorBlock key={i} error={e} />)}
            </div>
        );
    }

    renderProgress(since: moment.Moment) {

        return (
            <div className="progress">
                <div className="progress-bar progress-bar-striped bg-warning active" role="progressbar" style={{ width: "100%" }}>
                    <span>Restarting...</span>
                </div>
            </div>
        );
    }

}

export class ErrorBlock extends React.Component<{ error: WebApiHttpError }, { showDetails: boolean }>{

    constructor(props: any) {
        super(props);

        this.state = {
            showDetails: false,
        };
    }

    handleShowStackTrace = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        this.setState({ showDetails: !this.state.showDetails });
    }
    render() {
        var he = this.props.error;
        return (
            <div className="alert alert-danger error-block" style={{ marginTop: "20px" }}>
                <div >
                    <h3>{he.ExceptionType}</h3>
                    {textDanger(he.ExceptionMessage)}
                </div >
                <div>
                    <a href="#" onClick={this.handleShowStackTrace}>StackTrace</a>
                    {this.state.showDetails && <pre>{he.StackTrace}</pre>}
                </div>
            </div>

        );
    }
}

function textDanger(message: string | null | undefined): React.ReactFragment | null | undefined {

    if (typeof message == "string")
        return message.split("\n").map((s, i) => <p key={i} className="text-danger">{s}</p>);

    return message;
}

interface RefreshClientsStepState {

}

export class RefreshClientsStep extends React.Component<{}, RefreshClientsStepState>{

    handleRefreshClient = (e: React.MouseEvent<any>) => {
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