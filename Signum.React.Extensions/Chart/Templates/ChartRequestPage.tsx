import * as React from 'react'
import * as QueryString from "query-string"
import { Lite } from '@framework/Signum.Entities'
import { parseLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { ChartRequestModel, UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import ChartRequestView from './ChartRequestView'
import { RouteComponentProps } from 'react-router'

interface ChartRequestPageProps extends RouteComponentProps<{ queryName: string; }> {

}

export default class ChartRequestPage extends React.Component<ChartRequestPageProps, { chartRequest?: ChartRequestModel; userChart?: Lite<UserChartEntity> }> {

  constructor(props: ChartRequestPageProps) {
    super(props);
    this.state = {};
  }

  componentWillMount() {
    this.load(this.props);
  }

  componentWillReceiveProps(nextProps: ChartRequestPageProps) {
    this.load(nextProps);
  }

  load(props: ChartRequestPageProps) {
    
    var newPath = props.location.pathname + props.location.search;
    var oldPathPromise: Promise<string | undefined> = this.state.chartRequest ? ChartClient.Encoder.chartPathPromise(this.state.chartRequest, this.state.userChart) : Promise.resolve(undefined);
    oldPathPromise.then(oldPath => {
      if (oldPath != newPath) {
        var query = QueryString.parse(props.location.search);
        var uc = query.userChart == null ? undefined : (parseLite(query.userChart) as Lite<UserChartEntity>);
        ChartClient.Decoder.parseChartRequest(props.match.params.queryName, query)
          .then(cr => this.setState({ chartRequest: cr, userChart: uc }))
          .done();
      }
    }).done();
  }

  handleOnChange = (cr: ChartRequestModel, uc?: Lite<UserChartEntity>) => {
    this.setState({ userChart: uc }, () =>
      ChartClient.Encoder.chartPathPromise(cr, uc)
        .then(path => Navigator.history.replace(path))
        .done()
    );
  }

  render() {
    return <ChartRequestView
      chartRequest={this.state.chartRequest}
      userChart={this.state.userChart}
      onChange={(cr, uc) => this.handleOnChange(cr, uc)} />;
  }
}


