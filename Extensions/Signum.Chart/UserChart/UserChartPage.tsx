import * as React from 'react'
import { toLite } from '@framework/Signum.Entities'
import { JavascriptMessage, parseLite } from '@framework/Signum.Entities'
import { Navigator } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as ChartClient from '../ChartClient'
import * as UserChartClient from './UserChartClient'
import { useLocation, useParams } from "react-router";
import { useForceUpdate } from '@framework/Hooks'
import { UserChartEntity } from './Signum.Chart.UserChart'



export default function UserChartPage() {
  const params = useParams() as { userChartId: string; entity?: string };

  React.useEffect(() => {
    const { userChartId, entity } = params;

    const lite = entity == undefined ? undefined : parseLite(entity);

    Navigator.API.fillLiteModels(lite)
      .then(() => Navigator.API.fetchEntity(UserChartEntity, userChartId))
      .then(uc => UserChartClient.Converter.toChartRequest(uc, lite)
        .then(cr => ChartClient.Encoder.chartPathPromise(cr, toLite(uc))))
      .then(path => AppContext.navigate(path, { replace : true }));
  }, []);

  return <span>{JavascriptMessage.loading.niceToString()}</span>;
}


