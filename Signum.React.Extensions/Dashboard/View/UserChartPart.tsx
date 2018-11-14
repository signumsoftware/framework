
import * as React from 'react'
import { ServiceError } from '@framework/Services'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
import * as UserChartClient from '../../Chart/UserChart/UserChartClient'
import * as ChartClient from '../../Chart/ChartClient'
import { ChartRequest } from '../../Chart/Signum.Entities.Chart'
import ChartRenderer from '../../Chart/Templates/ChartRenderer'
import ChartTable from '../../Chart/Templates/ChartTable'
import { UserChartPartEntity } from '../Signum.Entities.Dashboard'

export interface UserChartPartProps {
  part: UserChartPartEntity
  entity?: Lite<Entity>;
}

export interface UserChartPartState {
  chartRequest?: ChartRequest;
  result?: ChartClient.API.ExecuteChartResult;
  error?: any;
  showData?: boolean;
}

export default class UserChartPart extends React.Component<UserChartPartProps, UserChartPartState> {
  constructor(props: UserChartPartProps) {
    super(props);
    this.state = { showData: props.part.showData };
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
        .then(cr => this.setState({ chartRequest: cr, result: undefined }, () => this.makeQuery()))
        .done());
  }

  makeQuery() {
    this.setState({ result: undefined, error: undefined }, () =>
      ChartClient.API.executeChart(this.state.chartRequest!)
        .then(rt => this.setState({ result: rt }))
        .catch(e => { this.setState({ error: e }); })
        .done());
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
        {this.props.part.allowChangeShowData &&
          <label>
            <input type="checkbox" checked={this.state.showData} onChange={e => this.setState({ showData: e.currentTarget.checked })} />
            {" "}{UserChartPartEntity.nicePropertyName(a => a.showData)}
          </label>}
        {this.state.showData ?
          <ChartTable chartRequest={s.chartRequest} lastChartRequest={s.chartRequest} resultTable={s.result.resultTable} onRedraw={() => this.makeQuery()} /> :
          <ChartRenderer chartRequest={s.chartRequest} lastChartRequest={s.chartRequest} data={s.result.chartTable} />
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



