import * as React from 'react'
import { DateTime } from 'luxon'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { StyleContext } from '@framework/TypeContext'
import * as Finder from '@framework/Finder'
import * as AppContext from '@framework/AppContext'
import { WebApiHttpError } from '@framework/Services'
import { SearchValue, FindOptions, SearchValueLine } from '@framework/Search'
import EntityLink from '@framework/SearchControl/EntityLink'
import { QueryEntitiesRequest } from '@framework/FindOptions'
import { getQueryNiceName, QueryTokenString } from '@framework/Reflection'
import { API, CompilationError, EvalEntityError, DynamicPanelInformation } from './DynamicClient'
import { Options } from './DynamicClientOptions'
import CSharpCodeMirror from '../Codemirror/CSharpCodeMirror'
import * as AuthClient from '../Authorization/AuthClient'
import { DynamicPanelPermission } from './Signum.Entities.Dynamic'
import { RouteComponentProps } from "react-router";
import { Tab, Tabs } from 'react-bootstrap';
import { FormGroup } from '@framework/Lines';
import { toFilterRequests } from '@framework/Finder';
import "./DynamicPanelPage.css"
import { JavascriptMessage } from '@framework/Signum.Entities';
import { useForceUpdate, useAPI, useInterval } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { CheckEvalsStep, SearchPanel } from './DynamicPanelSimplePage'


type DynamicPanelTab = "compile" | "restartServerApp" | "migrations" | "checkEvals" | "refreshClients";

export default function DynamicPanelPage(p: RouteComponentProps<{}>) {

  const [refreshKey, setRefreshKey] = React.useState(0);

  const startErrors = useAPI(() => API.getStartErrors(), [refreshKey]);
  const panelInformation = useAPI(() => API.getPanelInformation(), [refreshKey]);
  const [restarting, setRestarting] = React.useState<DateTime | null>(null);


  function handleSelect(key: any /*string*/) {
    AppContext.history.push("~/dynamic/panel?step=" + key);
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
      {restarting ? undefined :
        startErrors?.length ?
          <div role="alert" className="alert alert-danger" style={{ marginTop: "20px" }}>
            <FontAwesomeIcon icon="triangle-exclamation" />
            {" "}The server started, but there {startErrors.length > 1 ? "are" : "is"} <a href="#" onClick={handleErrorClick}>{startErrors.length} {startErrors.length > 1 ? "errors" : "error"}</a>.
        </div> :
          <div role="alert" className="alert alert-success">
            <FontAwesomeIcon icon="circle-check" />
            {" "}The server is started successfully.
        </div>
      }
      <Tabs activeKey={step ?? "search"} id="dynamicPanelTabs" style={{ marginTop: "20px" }} onSelect={handleSelect}>
        <Tab eventKey="search" title="Search">
          <SearchPanel />
        </Tab>

        <Tab eventKey="compile" title="1. Edit and Compile">
          <CompileStep refreshView={() => setRefreshKey(refreshKey + 1)} panelInformation={panelInformation} />
        </Tab>

        <Tab eventKey="restartServerApp" title="2. Restart Server Application">
          <RestartServerAppStep
            startErrors={startErrors}
            restarting={restarting}
            setRestarting={setRestarting}
            refreshView={() => setRefreshKey(refreshKey + 1)} />
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
      });
  }

  function handleCheck(e: React.MouseEvent<any>) {
    e.preventDefault();
    API.getCompilationErrors()
      .then(errors => {
        setSelectedErrorIndex(undefined);
        setCompilationErrors(errors);
      });
  }


  function renderPanelInformation() {
    var pi = p.panelInformation;
    const lastCompile = pi?.lastDynamicCompilationDateTime;
    const lastChange = pi?.lastDynamicChangeDateTime;
    const loadedAssembly = pi?.loadedCodeGenAssemblyDateTime;
    const loadedControllerAssembly = pi?.loadedCodeGenControllerAssemblyDateTime;

    const validStyle = { color: "green" } as React.CSSProperties;
    const invalidStyle = { color: "red", fontWeight: "bold" } as React.CSSProperties;

    const isValidCompile = lastChange && lastCompile && DateTime.fromISO(lastCompile) < DateTime.fromISO(lastChange) ? false : true;
    const isValidAssembly = lastChange && loadedAssembly && DateTime.fromISO(loadedAssembly) < DateTime.fromISO(lastChange) ? false : true;
    const isValidControllerAssembly = lastChange && loadedControllerAssembly && DateTime.fromISO(loadedControllerAssembly) < DateTime.fromISO(lastChange) ? false : true;

    return (
      <table className="table table-condensed form-vertical table-sm">
        <tbody>
          <tr>
            <th>Last Dynamic Change</th>
            <td>{lastChange ? DateTime.fromISO(lastChange).toFormat("FFF") : "-"}</td>
          </tr>
          <tr>
            <th>Last Dynamic Compilation</th>
            <td style={isValidCompile ? validStyle : invalidStyle}>{lastCompile ? DateTime.fromISO(lastCompile).toFormat("FFF") : "-"}</td>
          </tr>
          <tr>
            <th>Loaded CodeGen Assembly</th>
            <td style={isValidAssembly ? validStyle : invalidStyle}>{loadedAssembly ? DateTime.fromISO(loadedAssembly).toFormat("FFF") : "-"}</td>
          </tr>
          <tr>
            <th>Loaded CodeGen Controller Assembly</th>
            <td style={isValidControllerAssembly ? validStyle : invalidStyle}>{loadedControllerAssembly ? DateTime.fromISO(loadedControllerAssembly).toFormat("FFF") : "-"}</td>
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
  setRestarting: (time: DateTime | null) => void;
  restarting: DateTime | null;
}


export function RestartServerAppStep(p: RestartServerAppStepProps) {
  const forceUpdate = useForceUpdate();

  function handleRestartApplication(e: React.MouseEvent<any>) {
    e.preventDefault();

    API.restartServer()
      .then(() => {
        p.setRestarting(DateTime.local());
        return Promise.all([refreshScreen(), reconnectWithServer()]);
      });
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
          <span>Restarting...({DateTime.local().diff(p.restarting, "second").as("second")}s)</span>
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


export function RefreshClientsStep() {
  function handleRefreshClient(e: React.MouseEvent<any>) {
    e.preventDefault();
    window.location.reload();
  }

  return (
    <div>
      <p>Now you need to refresh the clients manually (i.e. pressing F5).</p>
      <a href="#" className="sf-button btn btn-warning" onClick={handleRefreshClient}>Refresh this client</a>
    </div>
  );
}
