import * as React from 'react'
import { Lite, JavascriptMessage } from '@framework/Signum.Entities'
import { parseLite } from '@framework/Signum.Entities'
import * as AppContext from '@framework/AppContext'
import { ChartRequestModel, UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import ChartRequestView from './ChartRequestView'
import { RouteComponentProps } from 'react-router'
import { useStateWithPromise } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { getQueryNiceName } from '@framework/Reflection'

interface ChartRequestPageProps extends RouteComponentProps<{ queryName: string; }> {

}


export default React.memo(function ChartRequestPage(p: ChartRequestPageProps) {
  const [pair, setPair] = useStateWithPromise<{ chartRequest: ChartRequestModel; userChart?: Lite<UserChartEntity>; } | undefined>(undefined);

  React.useEffect(() => {
    var newPath = p.location.pathname + p.location.search;
    var oldPathPromise: Promise<string | undefined> = pair ? ChartClient.Encoder.chartPathPromise(pair.chartRequest, pair.userChart) : Promise.resolve(undefined);
    oldPathPromise.then(oldPath => {
      if (oldPath != newPath) {
        var query = QueryString.parse(p.location.search);
        var uc = query.userChart == null ? undefined : (parseLite(query.userChart) as Lite<UserChartEntity>);
        ChartClient.Decoder.parseChartRequest(p.match.params.queryName, query)
          .then(cr => setPair({ chartRequest: cr, userChart: uc }));
      }
    });
  }, [p.location.pathname, p.location.search, p.match.params.queryName])

  AppContext.useTitle(getQueryNiceName(p.match.params.queryName));

  function handleOnChange(cr: ChartRequestModel, uc?: Lite<UserChartEntity>) {
    if (pair!.userChart != uc)
      setPair({ userChart: uc, chartRequest: pair!.chartRequest }).then(() => changeUrl(cr, uc));
    else
      changeUrl(cr, uc);
  }

  function changeUrl(cr: ChartRequestModel, uc?: Lite<UserChartEntity>) {
    ChartClient.Encoder.chartPathPromise(cr, uc)
      .then(path => AppContext.history.replace(path));
  }

  if (pair == null)
    return null;

  const isEmpty = pair.chartRequest.columns.every(a => a.element.token == null)

  return (
    <div style={{ display: "flex" }}>
      {pair == null ? <h2>
        <span className="sf-entity-title">{JavascriptMessage.loading.niceToString()}</span>
      </h2 > :
        <ChartRequestView
          chartRequest={pair.chartRequest}
          userChart={pair.userChart}
          showChartSettings={isEmpty}
          searchOnLoad={!isEmpty}
          onChange={(cr, uc) => handleOnChange(cr, uc)} />
      }
    </div>
  );
}, (prev, next) => (prev.location.pathname + prev.location.search) == (next.location.pathname + next.location.search));



