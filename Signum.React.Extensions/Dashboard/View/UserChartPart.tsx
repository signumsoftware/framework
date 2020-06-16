
import * as React from 'react'
import { ServiceError } from '@framework/Services'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
import * as UserChartClient from '../../Chart/UserChart/UserChartClient'
import * as ChartClient from '../../Chart/ChartClient'
import { ChartRequestModel } from '../../Chart/Signum.Entities.Chart'
import ChartRenderer from '../../Chart/Templates/ChartRenderer'
import ChartTableComponent from '../../Chart/Templates/ChartTable'
import { UserChartPartEntity } from '../Signum.Entities.Dashboard'
import PinnedFilterBuilder from '@framework/SearchControl/PinnedFilterBuilder';
import { useAPI, useAPIWithReload } from '../../../../Framework/Signum.React/Scripts/Hooks'
import { PanelPartContentProps } from '../DashboardClient'

interface ResultOrError {

}

export default function UserChartPart(p: PanelPartContentProps<UserChartPartEntity>) {

  const chartRequest = useAPI(() => UserChartClient.Converter.toChartRequest(p.part.userChart, p.entity), [p.part.userChart, p.entity]);

  const [resultOrError, makeQuery] = useAPIWithReload<undefined | { error?: any, result?: ChartClient.API.ExecuteChartResult }>(() => chartRequest == null ? Promise.resolve(undefined) :
    ChartClient.getChartScript(chartRequest!.chartScript)
      .then(cs => ChartClient.API.executeChart(chartRequest!, cs))
      .then(result => ({ result }))
      .catch(error => ({ error })), [chartRequest]);


  const [showData, setShowData] = React.useState(p.part.showData);
  
  function renderError(e: any) {
    const se = e instanceof ServiceError ? (e as ServiceError) : undefined;

    if (se == undefined)
      return <p className="text-danger"> {e.message ? e.message : e}</p>;

    return (
      <div>
        {se.httpError.exceptionMessage && <p className="text-danger">{se.httpError.exceptionMessage}</p>}
      </div>
    );

  }

  if (!chartRequest)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (resultOrError?.error) {
    return (
      <div>
        <h4>Error!</h4>
        {renderError(resultOrError.error)}
      </div>
    );
  }

  const result = resultOrError?.result!;

  return (
    <div>
      <PinnedFilterBuilder filterOptions={chartRequest.filterOptions} onFiltersChanged={() => makeQuery()} extraSmall={true} />
      {p.part.allowChangeShowData &&
        <label>
          <input type="checkbox" checked={showData} onChange={e => setShowData(e.currentTarget.checked)} />
          {" "}{UserChartPartEntity.nicePropertyName(a => a.showData)}
        </label>}
      {showData ?
        (!result ? <span>{JavascriptMessage.loading.niceToString()}</span> :
          <ChartTableComponent chartRequest={chartRequest} lastChartRequest={chartRequest}
          resultTable={result.resultTable!} onOrderChanged={() => makeQuery()} />) :
        <ChartRenderer chartRequest={chartRequest} lastChartRequest={chartRequest}
          data={result?.chartTable} loading={result == null} />
      }
    </div>
  );
}
