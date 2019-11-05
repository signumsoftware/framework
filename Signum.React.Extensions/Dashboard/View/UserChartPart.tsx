
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
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'
import { PanelPartContentProps } from '../DashboardClient'

export default function UserChartPart(p: PanelPartContentProps<UserChartPartEntity>) {

  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<any | undefined>(undefined);
  const [result, setResult] = React.useState<ChartClient.API.ExecuteChartResult | undefined>(undefined);
  const [showData, setShowData] = React.useState(p.part.showData);
  const chartRequest = useAPI(() => UserChartClient.Converter.toChartRequest(p.part.userChart, p.entity), [p.part.userChart, p.entity]);

  function makeQuery() {
    ChartClient.getChartScript(chartRequest!.chartScript)
      .then(cs => ChartClient.API.executeChart(chartRequest!, cs))
      .then(rt => { setResult(rt); setLoading(false); setError(undefined); })
      .catch(e => { setError(e); })
      .done();
  }


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
  if (error) {
    return (
      <div>
        <h4>Error!</h4>
        {renderError(error)}
      </div>
    );
  }

  if (!chartRequest || !result)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  return (
    <div>
      <PinnedFilterBuilder filterOptions={chartRequest.filterOptions} onFiltersChanged={() => makeQuery()} extraSmall={true} />
      {p.part.allowChangeShowData &&
        <label>
          <input type="checkbox" checked={showData} onChange={e => setShowData(e.currentTarget.checked)} />
          {" "}{UserChartPartEntity.nicePropertyName(a => a.showData)}
        </label>}
      {showData ?
        <ChartTableComponent chartRequest={chartRequest} lastChartRequest={chartRequest}
          resultTable={result.resultTable} onOrderChanged={() => makeQuery()} /> :
        <ChartRenderer chartRequest={chartRequest} lastChartRequest={chartRequest}
          data={result.chartTable} loading={loading} />
      }
    </div>
  );
}
