import * as React from 'react'
import * as moment from 'moment'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { StyleContext } from '@framework/TypeContext'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { WebApiHttpError } from '@framework/Services'
import { ValueSearchControl, FindOptions, ValueSearchControlLine } from '@framework/Search'
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
import { Tab, Tabs } from 'react-bootstrap';
import { FormGroup } from '@framework/Lines';
import { toFilterRequests } from '@framework/Finder';
import "./DynamicPanelPage.css"
import { validate } from './View/NodeUtils';
import { JavascriptMessage } from '@framework/Signum.Entities';
import { useForceUpdate, useAPI, useInterval } from '@framework/Hooks'

interface DynamicPanelProps extends RouteComponentProps<{}> {
}

type DynamicPanelTab = "compile" | "restartServerApp" | "migrations" | "checkEvals" | "refreshClients";

export default function DynamicPanelPage(p: DynamicPanelProps) {

  const [count, setCount] = React.useState(0);

  const startErrors = useAPI(() => API.getStartErrors(), [count]);
  const panelInformation = useAPI(() => API.getPanelInformation(), [count]);
  const [restarting, setRestarting] = React.useState<moment.Moment | null>(null);


  function handleSelect(key: any /*string*/) {
    Navigator.history.push("~/dynamic/panel?step=" + key);
  }

  function handleErrorClick(e: React.MouseEvent<any>) {
    e.preventDefault();
    handleSelect("restartServerApp");
  }


  AuthClient.assertPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel);

  let step = QueryString.parse(p.location.search).step as DynamicPanelTab | undefined;

  return (
    <div>
      <h2>Dynamic Panel</h2>
      {startErrors?.length && !restarting &&
        <div role="alert" className="alert alert-danger" style={{ marginTop: "20px" }}>
          <FontAwesomeIcon icon="exclamation-triangle" />
          {" "}The server started, but there {startErrors.length > 1 ? "are" : "is"} <a href="#" onClick={handleErrorClick}>{startErrors.length} {startErrors.length > 1 ? "errors" : "error"}</a>.
                    </div>
      }
      <Tabs activeKey={step ?? "search"} id="dynamicPanelTabs" style={{ marginTop: "20px" }} onSelect={handleSelect}>
        <Tab eventKey="search" title="Search">
          <SearchPanel />
        </Tab>

        <Tab eventKey="compile" title="1. Edit and Compile">
          <CompileStep refreshView={() => setCount(count + 1)} panelInformation={panelInformation} />
        </Tab>

        <Tab eventKey="restartServerApp" title="2. Restart Server Application">
          <RestartServerAppStep
            startErrors={startErrors}
            restarting={restarting}
            setRestarting={setRestarting}
            refreshView={() => setCount(count + 1)} />
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

export function SearchPanel(props: {}) {


  const [search, setSearch] = React.useState("");
  var sc = new StyleContext(undefined, { labelColumns: 3 });

  const elements = Options.onGetDynamicPanelSearch.map(f => f(sc, search));

  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <div className="form-group has-search">
            <span className="form-control-feedback"><FontAwesomeIcon icon="search" /></span>
            <input type="text" className="form-control" value={search} onChange={e => setSearch(e.currentTarget.value)} />
          </div>
          {React.cloneElement(<div />, undefined, ...elements)}
        </div>
      </div>
    </div>
  );
}

interface DynamicCompileStepProps {
  refreshView?: () => void;
  panelInformation?: DynamicPanelInformation;
}

export function CompileStep(p: DynamicCompileStepProps) {

  const [compilationErrors, setCompilationErrors] = React.useState<CompilationError[] | undefined>(undefined);

  const [selectedErrorIndex, setSelectedErrorIndex] = React.useState<number | undefined>(undefined);


  function handleCompile(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.compile()
      .then(errors => {
        setSelectedErrorIndex(undefined);
        setCompilationErrors(errors);
        p.refreshView && p.refreshView();
      }).done();
  }

  function handleCheck(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.getCompilationErrors()
      .then(errors => {
        setSelectedErrorIndex(undefined);
        setCompilationErrors(errors);
      })
      .done();
  }


  function renderPanelInformation() {
    var pi = p.panelInformation;
    const lastCompile = pi?.lastDynamicCompilationDateTime;
    const lastChange = pi?.lastDynamicChangeDateTime;
    const loadedAssembly = pi?.loadedCodeGenAssemblyDateTime;
    const loadedControllerAssembly = pi?.loadedCodeGenControllerAssemblyDateTime;

    const validStyle = { color: "green" } as React.CSSProperties;
    const invalidStyle = { color: "red", fontWeight: "bold" } as React.CSSProperties;

    const isValidCompile = lastChange && lastCompile && moment(lastCompile).isBefore(moment(lastChange)) ? false : true;
    const isValidAssembly = lastChange && loadedAssembly && moment(loadedAssembly).isBefore(moment(lastChange)) ? false : true;
    const isValidControllerAssembly = lastChange && loadedControllerAssembly && moment(loadedControllerAssembly).isBefore(moment(lastChange)) ? false : true;

    return (
      <table className="table table-condensed form-vertical table-sm">
        <tbody>
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
          <tr>
            <th>Loaded CodeGen Controller Assembly</th>
            <td style={isValidControllerAssembly ? validStyle : invalidStyle}>{loadedControllerAssembly ? moment(loadedControllerAssembly).format("L LT") : "-"}</td>
          </tr>
        </tbody>
      </table>
    );
  }

  function renderCompileResult(errors: CompilationError[]) {
    return (
      <div>
        <br />
        <div className={`alert alert-${errors.length == 0 ? "success" : "danger"}`} role="alert">
          <strong>{errors.length} Errors!</strong> {errors.length == 0 ?
            "The dynamic code compiled successfully" :
            "Please fix this errors in the dynamic entities"}
        </div>
        <br />
        {errors.length > 0 && renderErrorTable(errors)}
      </div>
    );
  }

  function renderErrorTable(errors: CompilationError[]) {
    var err = selectedErrorIndex == null ? undefined : errors[selectedErrorIndex]

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
                  onClick={() => setSelectedErrorIndex(i)}
                  className={classes("dynamic-error-line", i == selectedErrorIndex ? "active" : undefined)}>
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
  var sc = new StyleContext(undefined, { labelColumns: { sm: 6 } });

  const lines = Options.onGetDynamicLineForPanel.map(f => f(sc));
  const lineContainer = React.cloneElement(<div />, undefined, ...lines);

  return (
    <div>

      <div className="row">
        <div className="col-sm-6">
          {lineContainer}
        </div>
        <div className="col-sm-6">
          {p.panelInformation ? renderPanelInformation() : JavascriptMessage.loading.niceToString()}
        </div>
      </div>


      <br />
      {<a href="#" className="sf-button btn btn-warning" onClick={handleCheck}>Check</a>}&nbsp;
      {<a href="#" className="sf-button btn btn-success" onClick={handleCompile}>Compile</a>}
      {compilationErrors && renderCompileResult(compilationErrors)}
    </div>
  );
}

interface RestartServerAppStepProps {
  startErrors?: WebApiHttpError[];
  refreshView: () => void;
  setRestarting: (time: moment.Moment | null) => void;
  restarting: moment.Moment | null;
}


export function RestartServerAppStep(p: RestartServerAppStepProps) {
  const forceUpdate = useForceUpdate();

  function handleRestartApplication(e: React.MouseEvent<any>) {
    e.preventDefault();

    API.restartServer()
      .then(() => {
        p.setRestarting(moment());
        return Promise.all([refreshScreen(), reconnectWithServer()]);
      })
      .done();
  }

  useInterval(p.restarting ? 1000 : null, 0, a => a + 1);

  async function refreshScreen() {
    while (p.restarting) {
      await new Promise(resolve => setTimeout(resolve, 1000));
      forceUpdate();
    }
  }

  async function reconnectWithServer() {
    while (true) {
      try {
        var errors = await API.getStartErrors();
        p.setRestarting(null);
        p.refreshView();
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

  if (p.restarting) {
    return (
      <div className="progress">
        <div className="progress-bar progress-bar-striped bg-warning active" role="progressbar" style={{ width: "100%" }}>
          <span>Restarting...({moment().diff(p.restarting, "s")}s)</span>
        </div>
      </div>
    );
  }

  return (
    <div>
      {
        AuthClient.isPermissionAuthorized(DynamicPanelPermission.RestartApplication) &&
        <a href="#" className="sf-button btn btn-danger" onClick={handleRestartApplication}>Restart Server Application</a>
      }
      {p.startErrors && p.startErrors.map((e, i) => <ErrorBlock key={i} error={e} />)}
    </div>
  );
}

export function ErrorBlock(p: { error: WebApiHttpError }) {

  const [showDetails, setShowDetails] = React.useState(false)

  function handleShowStackTrace(e: React.MouseEvent<any>) {
    e.preventDefault();
    setShowDetails(!showDetails);
  }

  var he = p.error;
  return (
    <div className="alert alert-danger error-block" style={{ marginTop: "20px" }}>
      <div >
        <h3>{he.exceptionType}</h3>
        {textDanger(he.exceptionMessage)}
      </div >
      <div>
        <a href="#" onClick={handleShowStackTrace}>StackTrace</a>
        {showDetails && <pre>{he.stackTrace}</pre>}
      </div>
    </div>
  );
}

function textDanger(message: string | null | undefined): React.ReactFragment | null | undefined {

  if (typeof message == "string")
    return message.split("\n").map((s, i) => <p key={i} className="text-danger">{s}</p>);

  return message;
}

export function CheckEvalsStep() {

  const [autoStart, setAutoStart] = React.useState<number | undefined>(undefined);

  function handleOnClick(e: React.MouseEvent<any>) {
    e.preventDefault();
    setAutoStart((autoStart ?? 0) + 1);
  }

  var ctx = new StyleContext(undefined, {});
  return (
    <div>
      {Options.checkEvalFindOptions.map((fo, i) => <CheckEvalType key={i} ctx={ctx} findOptions={fo} autoStart={autoStart} />)}
      <button className="btn btn-success" onClick={handleOnClick}><FontAwesomeIcon icon="sync" /> Refresh all</button>
    </div>
  );
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


export function CheckEvalType(p: CheckEvalTypeProps) {

  const [{ state, errors }, setState] = React.useState<CheckEvalTypeState>({ state: "initial", errors: undefined });

  React.useEffect(() => {
    if (p.autoStart != null)
      loadData(p);
  }, [p.autoStart]);


  function loadData(props: CheckEvalTypeProps) {
    setState({ state: "loading" });
    const fo = p.findOptions;
    Finder.getQueryDescription(fo.queryName)
      .then(qd => Finder.parseFindOptions(fo, qd, false))
      .then(fop => {
        var request = {
          queryKey: fop.queryKey,
          filters: toFilterRequests(fop.filterOptions),
          orders: [{ token: QueryTokenString.entity().append(e => e.id).toString(), orderType: "Ascending" }],
          count: 10000,
        } as QueryEntitiesRequest;
        API.getEvalErrors(request)
          .then(errors => setState({ state: "success", errors: errors }),
            e => {
              setState({ state: "failed", errors: undefined });
              throw e;
            }).done();
      });
  }

  return (
    <FormGroup ctx={p.ctx} labelText={getQueryNiceName(p.findOptions.queryName)}>
      <ValueSearchControl findOptions={p.findOptions} isLink={true} />
      {
        state == "loading" ?
          <FontAwesomeIcon icon="sync" spin={true} /> :
          <span onClick={e => { e.preventDefault(); loadData(p); }} style={{ cursor: "pointer" }}><FontAwesomeIcon icon="sync" className="sf-line-button" /></span>
      }
      {
        state == "failed" ? <span className="mini-alert alert-danger" role="alert"><FontAwesomeIcon icon="exclamation-triangle" /> Exception checking {getQueryNiceName(p.findOptions.queryName)}</span> :
          errors && errors.length > 0 ? <span className="mini-alert alert-danger" role="alert"><strong>{errors.length}</strong> {errors.length == 1 ? "Error" : "Errors"} found</span> :
            errors && errors?.length == 0 ? <span className="mini-alert alert-success" role="alert">No errors found!</span> :
              undefined
      }
      {
        errors?.length &&
        <div className="table-responsive">
          <table className="table table-sm">
            <tbody>
              {errors.map((e, i) => <tr key={i}>
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


export function RefreshClientsStep() {
  function handleRefreshClient(e: React.MouseEvent<any>) {
    e.preventDefault();
    window.location.reload(true);
  }

  return (
    <div>
      <p>Now you need to refresh the clients manually (i.e. pressing F5).</p>
      <a href="#" className="sf-button btn btn-warning" onClick={handleRefreshClient}>Refresh this client</a>
    </div>
  );
}
