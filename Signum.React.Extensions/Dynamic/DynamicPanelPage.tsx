import * as React from 'react'
import * as moment from 'moment'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { StyleContext } from '@framework/TypeContext'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { WebApiHttpError } from '@framework/Services'
import { ValueSearchControl, FindOptions } from '@framework/Search'
import EntityLink from '@framework/SearchControl/EntityLink'
import { QueryEntitiesRequest } from '@framework/FindOptions'
import { getQueryNiceName, QueryTokenString } from '@framework/Reflection'
import { API, CompilationError, EvalEntityError, DynamicPanelInformation } from './DynamicClient'
import { Options } from './DynamicClientOptions'
import CSharpCodeMirror from '../Codemirror/CSharpCodeMirror'
import * as AuthClient from '../Authorization/AuthClient'
import { DynamicPanelPermission } from './Signum.Entities.Dynamic'
import { RouteComponentProps } from "react-router";
import * as QueryString from 'query-string';
import { Tab, Tabs } from '@framework/Components/Tabs';
import { FormGroup } from '@framework/Lines';
import { toFilterRequests } from '@framework/Finder';
import "./DynamicPanelPage.css"
import { validate } from './View/NodeUtils';
import { JavascriptMessage } from '@framework/Signum.Entities';

interface DynamicPanelProps extends RouteComponentProps<{}> {
}

interface DynamicPanelState {
  startErrors?: WebApiHttpError[];
  panelInformation?: DynamicPanelInformation;
}

type DynamicPanelTab = "compile" | "restartServerApp" | "migrations" | "checkEvals" | "refreshClients";

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
    this.loadData();
  }

  loadData() {
    API.getStartErrors()
      .then(errors => this.setState({ startErrors: errors }))
      .then(() => API.getPanelInformation())
      .then(info => this.setState({ panelInformation: info }))
      .done();
  }

  render() {
    AuthClient.asserPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel);

    let step = QueryString.parse(this.props.location.search).step as DynamicPanelTab | undefined;

    const errors = this.state.startErrors
    return (
      <div>
        {errors && errors.length > 0 &&
          <div role="alert" className="alert alert-danger" style={{ marginTop: "20px" }}>
            <FontAwesomeIcon icon="exclamation-triangle" />
            {" "}The server started, but there {errors.length > 1 ? "are" : "is"} <a href="#" onClick={this.handleErrorClick}>{errors.length} {errors.length > 1 ? "errors" : "error"}</a>.
                    </div>
        }
        {this.state.panelInformation ? this.renderPanelInformation() : JavascriptMessage.loading.niceToString()}
        <Tabs activeEventKey={step || "compile"} id="dynamicPanelTabs" style={{ marginTop: "20px" }} toggle={this.handleSelect}>
          <Tab eventKey="compile" title="1. Edit and Compile">
            <CompileStep refreshView={() => this.loadData()} />
          </Tab>

          <Tab eventKey="restartServerApp" title="2. Restart Server Application">
            <RestartServerAppStep
              startErrors={this.state.startErrors}
              setStartErrors={errors => this.setState({ startErrors: errors })}
              refreshView={() => this.loadData()} />
          </Tab>

          {Options.getDynaicMigrationsStep &&

            <Tab eventKey="migrations" title="3. Sql Migrations">
              {Options.getDynaicMigrationsStep()}
            </Tab>
          }
          <Tab eventKey="checkEvals" title={(Options.getDynaicMigrationsStep ? "4." : "3.") + " Check Evals"}>
            <CheckEvalsStep />
          </Tab>

          <Tab eventKey="refreshClients" title={(Options.getDynaicMigrationsStep ? "5." : "6.") + " Refresh Clients"}>
            <RefreshClientsStep />
          </Tab>
        </Tabs>
      </div>
    );
  }

  renderPanelInformation() {
    const lastCompile = this.state.panelInformation && this.state.panelInformation.lastDynamicCompilationDateTime;
    const lastChange = this.state.panelInformation && this.state.panelInformation.lastDynamicChangeDateTime;
    const loadedAssembly = this.state.panelInformation && this.state.panelInformation.loadedCodeGenAssemblyDateTime;

    const validStyle = { color: "green" } as React.CSSProperties;
    const invalidStyle = { color: "red", fontWeight: "bold" } as React.CSSProperties;

    const isValidCompile = lastChange && lastCompile && moment(lastCompile).isBefore(moment(lastChange)) ? false : true;
    const isValidAssembly = lastChange && loadedAssembly && moment(loadedAssembly).isBefore(moment(lastChange)) ? false : true;

    return (
      <table className="table table-condensed form-vertical" style={{ width: "30%" }}>
        <tr>
          <th>Last Dynamic Change</th>
          <td>{lastChange ? moment(lastChange).format("L LT") : "-"}</td>
        </tr>
        <tr>
          <th>Last Dynamic Compilation</th>
          <td style={isValidCompile ? validStyle : invalidStyle}>{lastCompile ? moment(lastCompile).format("L LT") : "-"}</td>
        </tr>
        <tr>
          <th>Loaded CodeGen Assembly</th>
          <td style={isValidAssembly ? validStyle : invalidStyle}>{loadedAssembly ? moment(loadedAssembly).format("L LT") : "-"}</td>
        </tr>
      </table>
    );
  }
}

interface DynamicCompileStepProps {
  refreshView?: () => void;
}

interface DynamicCompileStepState {
  complationErrors?: CompilationError[];
  selectedErrorIndex?: number;
  applicationRestarting?: moment.Moment;
}

export class CompileStep extends React.Component<DynamicCompileStepProps, DynamicCompileStepState>{

  constructor(props: any) {
    super(props);
    this.state = { };
  }

  handleCompile = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    API.compile()
      .then(errors => {
        this.setState({ complationErrors: errors, selectedErrorIndex: undefined });
        this.props.refreshView && this.props.refreshView();
      }).done();
  }

  handleCheck = (e: React.MouseEvent<any>) => {
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
          {<a href="#" className="sf-button btn btn-warning" onClick={this.handleCheck}>Check</a>}&nbsp;
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
  refreshView?: () => void;
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
        return Promise.all([this.refreshScreen(), this.reconnectWithServer()]);
      })
      .done();
  }

  refreshScreen = async () => {
    while (this.state.serverRestarting) {
      await new Promise(resolve => setTimeout(resolve, 1000));
      this.forceUpdate();
    }
  }

  reconnectWithServer = async () => {
    while (true) {
      try {
        var errors = await API.getStartErrors();
        this.props.setStartErrors(errors);
        this.setState({ serverRestarting: undefined });
        this.props.refreshView && this.props.refreshView();
        return;
      } catch (e) {
        if (e instanceof SyntaxError) {
          await new Promise(resolve => setTimeout(resolve, 500));
        }
        else {
          throw e;
        }
      }
    }
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
          <span>Restarting...({moment().diff(since, "s")}s)</span>
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
          <h3>{he.exceptionType}</h3>
          {textDanger(he.exceptionMessage)}
        </div >
        <div>
          <a href="#" onClick={this.handleShowStackTrace}>StackTrace</a>
          {this.state.showDetails && <pre>{he.stackTrace}</pre>}
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



interface CheckEvalsStepState {
  autoStart: number | undefined;
}

export class CheckEvalsStep extends React.Component<{}, CheckEvalsStepState>{

  constructor(props: CheckEvalsStepState) {
    super(props);
    this.state = { autoStart: undefined };
  }

  handleOnClick = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    this.setState(s => ({ autoStart: (s.autoStart || 0) + 1 }));
  }


  render() {
    var ctx = new StyleContext(undefined, {});
    return (
      <div>
        {Options.checkEvalFindOptions.map((fo, i) => <CheckEvalType key={i} ctx={ctx} findOptions={fo} autoStart={this.state.autoStart} />)}
        <button className="btn btn-success" onClick={this.handleOnClick}><FontAwesomeIcon icon="sync" /> Refresh all</button>
      </div>
    );
  }
}


interface CheckEvalTypeProps {
  findOptions: FindOptions;
  autoStart?: number;
  ctx: StyleContext;
}

interface CheckEvalTypeState {
  state: "initial" | "loading" | "success" | "failed";
  errors?: EvalEntityError[];
}


export class CheckEvalType extends React.Component<CheckEvalTypeProps, CheckEvalTypeState> {

  constructor(props: CheckEvalTypeProps) {
    super(props);
    this.state = { state: "initial" };
  }

  componentWillMount() {
    if (this.props.autoStart != null)
      this.loadData(this.props);
  }

  componentWillReceiveProps(newProps: CheckEvalTypeProps) {
    if (newProps.autoStart != null && newProps.autoStart != this.props.autoStart)
      this.loadData(newProps);
  }

  loadData(props: CheckEvalTypeProps) {
    this.setState({ state: "loading" }, () => {

      const fo = this.props.findOptions;
      Finder.getQueryDescription(fo.queryName)
        .then(qd => Finder.parseFindOptions(fo, qd))
        .then(fop => {
          var request = {
            queryKey: fop.queryKey,
            filters: toFilterRequests(fop.filterOptions || []),
            orders: [{ token: QueryTokenString.entity().append(e => e.id).toString(), orderType: "Ascending" }],
            count: 10000,
          } as QueryEntitiesRequest;
          API.getEvalErrors(request)
            .then(errors => this.setState({ state: "success", errors: errors }),
              e => {
                this.setState({ state: "failed", errors: undefined });
                throw e;
              }).done();
        });
    });
  }

  render() {
    return (
      <FormGroup ctx={this.props.ctx} labelText={getQueryNiceName(this.props.findOptions.queryName)}>
        <ValueSearchControl findOptions={this.props.findOptions} isLink={true} />
        {
          this.state.state == "loading" ?
            <FontAwesomeIcon icon="sync" spin={true} /> :
            <span onClick={e => { e.preventDefault(); this.loadData(this.props); }} style={{ cursor: "pointer" }}><FontAwesomeIcon icon="sync" className="sf-line-button" /></span>
        }

        {
          this.state.state == "failed" ? <span className="mini-alert alert-danger" role="alert"><FontAwesomeIcon icon="exclamation-triangle" /> Exception checking {getQueryNiceName(this.props.findOptions.queryName)}</span> :
            this.state.errors && this.state.errors.length > 0 ? <span className="mini-alert alert-danger" role="alert"><strong>{this.state.errors.length}</strong> {this.state.errors.length == 1 ? "Error" : "Errors"} found</span> :
              this.state.errors && this.state.errors.length == 0 ? <span className="mini-alert alert-success" role="alert">No errors found!</span> :
                undefined
        }
        {
          this.state.errors && this.state.errors.length > 0 &&
          <div className="table-responsive">
            <table className="table table-sm">
              <tbody>
                {this.state.errors.map((e, i) => <tr key={i}>
                  <td><EntityLink lite={e.lite} /></td>
                  <td className="text-danger">{e.error.split("\n").map((line, i) => <p key={i}>{line}</p>)}</td>
                </tr>
                )}
              </tbody>
            </table>
          </div>

        }
      </FormGroup>

    );
  }
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
