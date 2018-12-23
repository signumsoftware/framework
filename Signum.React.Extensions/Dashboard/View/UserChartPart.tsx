
import * as React from 'react'
import { ServiceError } from '@framework/Services'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
import * as UserChartClient from '../../Chart/UserChart/UserChartClient'
import * as ChartClient from '../../Chart/ChartClient'
import { ChartRequestModel } from '../../Chart/Signum.Entities.Chart'
import ChartRenderer from '../../Chart/Templates/ChartRenderer'
import ChartTableComponent from '../../Chart/Templates/ChartTable'
import { UserChartPartEntity } from '../Signum.Entities.Dashboard'
import PinnedFilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/PinnedFilterBuilder';

export interface UserChartPartProps {
  part: UserChartPartEntity
  entity?: Lite<Entity>;
}

export interface UserChartPartState {
  chartRequest?: ChartRequestModel;
  result?: ChartClient.API.ExecuteChartResult;
  loading: boolean;
  error?: any;
  showData?: boolean;
}

export default class UserChartPart extends React.Component<UserChartPartProps, UserChartPartState> {
  constructor(props: UserChartPartProps) {
    super(props);
    this.state = { showData: props.part.showData, loading: false };
  }

  componentWillMount() {
    this.loadChartRequest(this.props);
  }

  componentWillReceiveProps(newProps: UserChartPartProps) {

    if (is(this.props.part.userChart, newProps.part.userChart) &&
      is(this.props.entity, newProps.entity))
      return;

    this.loadChartRequest(newProps);
  }

  loadChartRequest(props: UserChartPartProps) {
    this.setState({ chartRequest: undefined, result: undefined, error: undefined }, () =>
      UserChartClient.Converter.toChartRequest(props.part.userChart!, props.entity)
        .then(cr => this.setState({ chartRequest: cr, result: undefined, error: undefined, loading: true }, () => this.makeQuery()))
        .done());
  }

  makeQuery() {
    ChartClient.getChartScript(this.state.chartRequest!.chartScript)
      .then(cs => ChartClient.API.executeChart(this.state.chartRequest!, cs))
      .then(rt => this.setState({ result: rt, loading: false, error: undefined }))
      .catch(e => { this.setState({ error: e }); })
      .done();
  }

  render() {

    const s = this.state;
    if (s.error) {
      return (
        <div>
          <h4>Error!</h4>
          {this.renderError(s.error)}
        </div>
      );
    }

    if (!s.chartRequest || !s.result)
      return <span>{JavascriptMessage.loading.niceToString()}</span>;

    return (
      <div>
        <PinnedFilterBuilder filterOptions={s.chartRequest.filterOptions} onFiltersChanged={() => this.makeQuery()} extraSmall={true} />
        {this.props.part.allowChangeShowData &&
          <label>
            <input type="checkbox" checked={this.state.showData} onChange={e => this.setState({ showData: e.currentTarget.checked })} />
            {" "}{UserChartPartEntity.nicePropertyName(a => a.showData)}
          </label>}
        {this.state.showData ?
          <ChartTableComponent chartRequest={s.chartRequest} lastChartRequest={s.chartRequest}
            resultTable={s.result.resultTable} onOrderChanged={() => this.makeQuery()} /> :
          <ChartRenderer chartRequest={s.chartRequest} lastChartRequest={s.chartRequest}
            data={s.result.chartTable} loading={s.loading} />
        }
      </div>
    );
  }

  renderError(e: any) {

    const se = e instanceof ServiceError ? (e as ServiceError) : undefined;

    if (se == undefined)
      return <p className="text-danger"> {e.message ? e.message : e}</p>;

    return (
      <div>
        {se.httpError.exceptionMessage && <p className="text-danger">{se.httpError.exceptionMessage}</p>}
      </div>
    );

  }
}
