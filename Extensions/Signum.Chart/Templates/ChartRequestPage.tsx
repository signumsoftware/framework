import * as React from 'react'
import { Lite, JavascriptMessage } from '@framework/Signum.Entities'
import { parseLite } from '@framework/Signum.Entities'
import * as AppContext from '@framework/AppContext'
import { ChartRequestModel } from '../Signum.Chart'
import { ChartClient } from '../ChartClient'
import ChartRequestView from './ChartRequestView'
import { useLocation, useParams } from 'react-router'
import { useStateWithPromise } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { getQueryNiceName } from '@framework/Reflection'
import { UserChartEntity } from '../UserChart/Signum.Chart.UserChart'




export default function ChartRequestPage(): React.JSX.Element | null {
  const params = useParams() as { queryName: string; };
  const location = useLocation();
  const [pair, setPair] = useStateWithPromise<{ chartRequest: ChartRequestModel; userChart?: Lite<UserChartEntity>; } | undefined>(undefined);

  React.useEffect(() => {
    var newPath = location.pathname + location.search;
    var oldPathPromise: Promise<string | undefined> = pair ? ChartClient.Encoder.chartPathPromise(pair.chartRequest, pair.userChart) : Promise.resolve(undefined);
    oldPathPromise.then(oldPath => {
      if (oldPath != newPath) {
        var query = QueryString.parse(location.search);
        var uc = query.userChart == null ? undefined : (parseLite(query.userChart) as Lite<UserChartEntity>);
        ChartClient.Decoder.parseChartRequest(params.queryName, query)
          .then(cr => setPair({ chartRequest: cr, userChart: uc }));
      }
    });
  }, [location.pathname, location.search, params.queryName])

  AppContext.useTitle(getQueryNiceName(params.queryName));

  function handleOnChange(cr: ChartRequestModel, uc?: Lite<UserChartEntity>) {
    if (pair!.userChart != uc)
      setPair({ userChart: uc, chartRequest: pair!.chartRequest }).then(() => changeUrl(cr, uc));
    else
      changeUrl(cr, uc);
  }

  function changeUrl(cr: ChartRequestModel, uc?: Lite<UserChartEntity>) {
    ChartClient.Encoder.chartPathPromise(cr, uc)
      .then(path => AppContext.navigate(path, { replace : true }));
  }

  if (pair == null)
    return null;

  const isEmpty = pair.chartRequest.columns.every(a => a.element.token == null)

  return (
    <div style={{ display: "flex" }}>
      {pair == null ? <h1 className="h2">
        <span className="sf-entity-title">{JavascriptMessage.loading.niceToString()}</span>
      </h1 > :
        <ChartRequestView
          chartRequest={pair.chartRequest}
          userChart={pair.userChart}
          showChartSettings={isEmpty}
          searchOnLoad={!isEmpty}
          onChange={(cr, uc) => handleOnChange(cr, uc)} />
      }
    </div>
  );
}



