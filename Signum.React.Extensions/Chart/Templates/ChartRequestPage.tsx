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

interface ChartRequestPageState {
  chartRequest?: ChartRequestModel;
  userChart?: Lite<UserChartEntity>;
}

export default class ChartRequestPage extends React.Component<ChartRequestPageProps, ChartRequestPageState> {

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

  shouldComponentUpdate(nextProps: ChartRequestPageProps, nextState: ChartRequestPageState) {

    if (this.state.chartRequest != nextState.chartRequest || this.state.userChart != nextState.userChart)
      return true;
    
    if ((nextProps.location.pathname + nextProps.location.search) == this.justReplacedPath) {
      this.justReplacedPath = undefined;
      return false;
    }

    return true
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

    if (this.state.userChart != uc)
      this.setState({ userChart: uc }, () => this.changeUrl(cr, uc));
    else
      this.changeUrl(cr, uc);
  }


  justReplacedPath?: string;
  changeUrl(cr: ChartRequestModel, uc?: Lite<UserChartEntity>) {
    ChartClient.Encoder.chartPathPromise(cr, uc)
      .then(path => {
        this.justReplacedPath = path;
        Navigator.history.replace(path);
      })
      .done()
  }

  render() {
    return <ChartRequestView
      chartRequest={this.state.chartRequest}
      userChart={this.state.userChart}
      onChange={(cr, uc) => this.handleOnChange(cr, uc)} />;
  }
}


