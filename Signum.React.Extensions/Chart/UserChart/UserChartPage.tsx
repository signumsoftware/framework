import * as React from 'react'
import { toLite } from '@framework/Signum.Entities'
import { JavascriptMessage, parseLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import * as UserChartClient from './UserChartClient'
import { RouteComponentProps } from "react-router";
import { useForceUpdate } from '@framework/Hooks'

interface UserChartPageProps extends RouteComponentProps<{ userChartId: string; entity?: string }> {

}

export default function UserChartPage(p : UserChartPageProps){

  React.useEffect(() => {
    const { userChartId, entity } = p.match.params;

    const lite = entity == undefined ? undefined : parseLite(entity);

    Navigator.API.fillToStrings(lite)
      .then(() => Navigator.API.fetchEntity(UserChartEntity, userChartId))
      .then(uc => UserChartClient.Converter.toChartRequest(uc, lite)
        .then(cr => ChartClient.Encoder.chartPathPromise(cr, toLite(uc))))
      .then(path => Navigator.history.replace(path))
      .done();
  }, []);

  return <span>{JavascriptMessage.loading.niceToString()}</span>;
}


