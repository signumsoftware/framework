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


type DynamicPanelTab = "compile" | "restartServerApp" | "migrations" | "checkEvals" | "refreshClients";

export default function DynamicPanelSimplePage(p: RouteComponentProps<{}>) {

  function handleSelect(key: any /*string*/) {
    AppContext.history.push("~/dynamic/panel?step=" + key);
  }

  AuthClient.assertPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel);

  let step = QueryString.parse(p.location.search).step as DynamicPanelTab | undefined;

  return (
    <div>
      <h2>Dynamic Panel</h2>
   
      <Tabs activeKey={step ?? "search"} id="dynamicPanelTabs" style={{ marginTop: "20px" }} onSelect={handleSelect}>
        <Tab eventKey="search" title="Search">
          <SearchPanel />
        </Tab>
     
        <Tab eventKey="checkEvals" title={"Check Evals"}>
          <CheckEvalsStep />
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
            <span className="form-control-feedback"><FontAwesomeIcon icon="magnifying-glass" /></span>
            <input type="text" className="form-control" value={search} onChange={e => setSearch(e.currentTarget.value)} />
          </div>
          {React.cloneElement(<div />, undefined, ...elements)}
        </div>
      </div>
    </div>
  );
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
      <button className="btn btn-success" onClick={handleOnClick}><FontAwesomeIcon icon="arrows-rotate" /> Refresh all</button>
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
            });
      });
  }

  return (
    <FormGroup ctx={p.ctx} label={getQueryNiceName(p.findOptions.queryName)}>
      <SearchValue findOptions={p.findOptions} isLink={true} />
      {
        state == "loading" ?
          <FontAwesomeIcon icon="arrows-rotate" spin={true} /> :
          <span onClick={e => { e.preventDefault(); loadData(p); }} style={{ cursor: "pointer" }}><FontAwesomeIcon icon="arrows-rotate" className="sf-line-button" /></span>
      }
      {
        state == "failed" ? <span className="mini-alert alert-danger" role="alert"><FontAwesomeIcon icon="triangle-exclamation" /> Exception checking {getQueryNiceName(p.findOptions.queryName)}</span> :
          errors && errors.length > 0 ? <span className="mini-alert alert-danger" role="alert"><strong>{errors.length}</strong> {errors.length == 1 ? "Error" : "Errors"} found</span> :
            errors && errors?.length == 0 ? <span className="mini-alert alert-success" role="alert">No errors found!</span> :
              undefined
      }
      {
        errors != null && errors.length > 0 &&
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
    window.location.reload();
  }

  return (
    <div>
      <p>Now you need to refresh the clients manually (i.e. pressing F5).</p>
      <a href="#" className="sf-button btn btn-warning" onClick={handleRefreshClient}>Refresh this client</a>
    </div>
  );
}
