import * as React from 'react'
import { toLite } from '@framework/Signum.Entities'
import { JavascriptMessage, parseLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import * as UserChartClient from './UserChartClient'
import { RouteComponentProps } from "react-router";

interface UserChartPageProps extends RouteComponentProps<{ userChartId: string; entity?: string }> {

}

export default class UserChartPage extends React.Component<UserChartPageProps> {

  constructor(props: UserChartPageProps) {
    super(props);
    this.state = {};
  }

  componentWillMount() {
    this.load(this.props);
  }

  componentWillReceiveProps(nextProps: UserChartPageProps) {
    this.state = {};
    this.forceUpdate();
    this.load(nextProps);
  }

  load(props: UserChartPageProps) {

    const { userChartId, entity } = props.match.params;

    const lite = entity == undefined ? undefined : parseLite(entity);

    Navigator.API.fillToStrings(lite)
      .then(() => Navigator.API.fetchEntity(UserChartEntity, userChartId))
      .then(uc => UserChartClient.Converter.toChartRequest(uc, lite)
        .then(cr => ChartClient.Encoder.chartPathPromise(cr, toLite(uc))))
      .then(path => Navigator.history.replace(path))
      .done();
  }

  render() {
    return <span>{JavascriptMessage.loading.niceToString()}</span>;
  }
}


