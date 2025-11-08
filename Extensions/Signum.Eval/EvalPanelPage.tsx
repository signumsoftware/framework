import * as React from 'react'
import { DateTime } from 'luxon'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { StyleContext } from '@framework/TypeContext'
import { Finder } from '@framework/Finder'
import * as AppContext from '@framework/AppContext'
import { WebApiHttpError } from '@framework/Services'
import { SearchValue, FindOptions, SearchValueLine } from '@framework/Search'
import EntityLink from '@framework/SearchControl/EntityLink'
import { QueryEntitiesRequest } from '@framework/FindOptions'
import { getQueryNiceName, QueryTokenString } from '@framework/Reflection'
import { EvalClient } from './EvalClient'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { useLocation, useParams } from "react-router";
import { Tab, Tabs } from 'react-bootstrap';
import { FormGroup } from '@framework/Lines';
import "./EvalPanelPage.css"
import { JavascriptMessage, SearchMessage } from '@framework/Signum.Entities';
import { useForceUpdate, useAPI, useInterval } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { assertPermissionAuthorized } from '@framework/AppContext'
import { EvalPanelMessage, EvalPanelPermission } from './Signum.Eval'
import { LinkButton } from '@framework/Basics/LinkButton'


type DynamicPanelTab = "search" | "checkEvals";

export default function DynamicPanelSimplePage(): React.JSX.Element {
  const location = useLocation();

  function handleSelect(key: any /*string*/) {
    AppContext.navigate("/dynamic/panel?step=" + key);
  }

  assertPermissionAuthorized(EvalPanelPermission.ViewDynamicPanel);

  let step = QueryString.parse(location.search).step as DynamicPanelTab | undefined;

  return (
    <div>
      <h2>{EvalPanelMessage.DynamicPanel.niceToString()}</h2>
   
      <Tabs activeKey={step ?? "search"} id="dynamicPanelTabs" style={{ marginTop: "20px" }} onSelect={handleSelect}>
        <Tab eventKey="search" title={EvalPanelMessage.Search.niceToString()}>
          <SearchPanel />
        </Tab>
     
        <Tab eventKey="checkEvals" title={EvalPanelMessage.CheckEvals.niceToString()}>
          <CheckEvalsStep />
        </Tab>

      </Tabs>
    </div>
  );
}



export function SearchPanel(props: {}): React.JSX.Element {


  const [search, setSearch] = React.useState("");
  var sc = new StyleContext(undefined, { labelColumns: 3 });

  const elements = EvalClient.Options.onGetDynamicPanelSearch.map(f => f(sc, search));

  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <div className="form-group has-search">
            <span className="form-control-feedback"><FontAwesomeIcon aria-hidden={true} icon="magnifying-glass" /></span>
            <input type="text" className="form-control" value={search} onChange={e => setSearch(e.currentTarget.value)} />
          </div>
          {React.cloneElement(<div />, undefined, ...elements)}
        </div>
      </div>
    </div>
  );
}

export function CheckEvalsStep(): React.JSX.Element {

  const [autoStart, setAutoStart] = React.useState<number | undefined>(undefined);

  function handleOnClick(e: React.MouseEvent<any>) {
    setAutoStart((autoStart ?? 0) + 1);
  }

  var ctx = new StyleContext(undefined, {});
  return (
    <div>
      {EvalClient.Options.checkEvalFindOptions.orderBy(fo => fo.queryName).map((fo, i) => <CheckEvalType key={i} ctx={ctx} findOptions={fo} autoStart={autoStart} />)}
      <button type="button" className="btn btn-success" onClick={handleOnClick}><FontAwesomeIcon aria-hidden={true} icon="arrows-rotate" /> {EvalPanelMessage.RefreshAll.niceToString()}</button>
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
  errors?: EvalClient.EvalEntityError[];
}


export function CheckEvalType(p: CheckEvalTypeProps): React.JSX.Element {

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
          filters: Finder.toFilterRequests(fop.filterOptions),
          orders: [{ token: QueryTokenString.entity().append(e => e.id).toString(), orderType: "Ascending" }],
          count: 10000,
        } as QueryEntitiesRequest;
        EvalClient.API.getEvalErrors(request)
          .then(errors => setState({ state: "success", errors: errors }),
            e => {
              setState({ state: "failed", errors: undefined });
              throw e;
            });
      });
  }

  return (
    <FormGroup ctx={p.ctx} label={getQueryNiceName(p.findOptions.queryName)}>
      {() => <>
        <SearchValue findOptions={p.findOptions} isLink={true} />
        {
          state == "loading" ?
            <FontAwesomeIcon icon="arrows-rotate" spin={true} /> :
            <LinkButton onClick={e => { loadData(p); }} title={EvalPanelMessage.OpenErrors.niceToString()}><FontAwesomeIcon icon="arrows-rotate" className="sf-line-button" /></LinkButton>
        }
        {
          state == "failed" ? <span className="mini-alert alert-danger" role="alert"><FontAwesomeIcon aria-hidden icon="triangle-exclamation" /> {EvalPanelMessage.ExceptionChecking0_.niceToString(getQueryNiceName(p.findOptions.queryName))}</span> :
            errors && errors.length > 0 ? <span className="mini-alert alert-danger" role="alert"><strong>{errors.length}</strong> {EvalPanelMessage._0Found.niceToString(errors.length == 1 ? "Error" : "Errors")}</span> :
              errors && errors?.length == 0 ? <span className="mini-alert alert-success" role="alert">{EvalPanelMessage.NoErrorsFound.niceToString()}</span> :
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
      </>}
    </FormGroup>
  );
}


export function RefreshClientsStep(): React.JSX.Element {
  function handleRefreshClient(e: React.MouseEvent<any>) {
    window.location.reload();
  }

  return (
    <div>
      <p>{EvalPanelMessage.YouNeedToRefreshManually.niceToString()}</p>
      <LinkButton title={undefined} className="sf-button btn btn-warning" onClick={handleRefreshClient}>{EvalPanelMessage.RefreshThisClient.niceToString()}</LinkButton>
    </div>
  );
}
