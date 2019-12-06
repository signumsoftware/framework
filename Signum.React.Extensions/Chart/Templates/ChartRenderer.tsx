import * as React from 'react'
import { DomUtils, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { parseLite, is } from '@framework/Signum.Entities'
import { FilterOptionParsed, ColumnOption, hasAggregate, withoutAggregate } from '@framework/FindOptions'
import { ChartRequestModel } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';

import "../Chart.css"
import { ChartScript, chartScripts, ChartRow } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import ReactChart from '../D3Scripts/Components/ReactChart';
import { useAppRelativeBasename } from '../../../../Framework/Signum.React/Scripts/AppRelativeRoutes'
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'


export interface ChartRendererProps {
  chartRequest: ChartRequestModel;
  loading: boolean;
  data?: ChartClient.ChartTable;
  lastChartRequest?: ChartRequestModel;
}

export default function ChartRenderer(p: ChartRendererProps) {
  const cs = useAPI(async signal => {
    const chartScriptPromise = ChartClient.getChartScript(p.chartRequest.chartScript);
    const chartComponentModulePromise = ChartClient.getRegisteredChartScriptComponent(p.chartRequest.chartScript);

    const chartScript = await chartScriptPromise;
    const chartComponentModule = await chartComponentModulePromise();

    return { chartComponent: chartComponentModule.default, chartScript };
  }, [p.chartRequest.chartScript]);

  var parameters = cs && ChartClient.API.getParameterWithDefault(p.chartRequest, cs.chartScript)

  function handleDrillDown(r: ChartRow) {
    const cr = p.lastChartRequest!;

    if (r.entity) {
      window.open(Navigator.navigateRoute(r.entity!));
    } else {
      const filters = cr.filterOptions.map(f => withoutAggregate(f)!).filter(Boolean);

      const columns: ColumnOption[] = [];

      cr.columns.map((a, i) => {

        const t = a.element.token;

        if (t?.token && !hasAggregate(t!.token!) && r.hasOwnProperty("c" + i)) {
          filters.push({
            token: t!.token!,
            operation: "EqualTo",
            value: (r as any)["c" + i],
            frozen: false
          } as FilterOptionParsed);
        }

        if (t?.token && t.token.parent != undefined) //Avoid Count and simple Columns that are already added
        {
          var col = t.token.queryTokenType == "Aggregate" ? t.token.parent : t.token

          if (col.parent)
            columns.push({
              token: col.fullKey
            });
        }
      });

      window.open(Finder.findOptionsPath({
        queryName: cr.queryKey,
        filterOptions: toFilterOptions(filters),
        columnOptions: columns,
      }));
    }
  }

  return (
    <FullscreenComponent>
      <ErrorBoundary>
        {cs && parameters &&
          (cs.chartComponent.prototype instanceof React.Component ?
            React.createElement(cs.chartComponent as React.ComponentClass<ChartClient.ChartComponentProps>, {
              data: p.data,
              loading: p.loading,
              onDrillDown: handleDrillDown,
              parameters: parameters
            }) :
            <ReactChart data={p.data}
              loading={p.loading}
              onDrillDown={handleDrillDown}
              parameters={parameters}
              onRenderChart={cs.chartComponent as ((p: ChartClient.ChartScriptProps) => React.ReactNode)} />)
        }
      </ErrorBoundary>
    </FullscreenComponent>
  );
}

interface FullscreenComponentProps {
  children: React.ReactNode
}

export function FullscreenComponent(p: FullscreenComponentProps) {

  const [isFullScreen, setIsFullScreen] = React.useState(false);

  function handleExpandToggle(e: React.MouseEvent<any>) {
    e.preventDefault();
    setIsFullScreen(!isFullScreen);
  }

  return (
    <div style={!isFullScreen ? { display: "flex" } : ({
      display: "flex",
      position: "fixed",
      background: "white",
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      height: "auto",
      zIndex: 9,
    })}>
      <a onClick={handleExpandToggle} style={{ color: "gray", order: 2, cursor: "pointer" }} >
        <FontAwesomeIcon icon={isFullScreen ? "compress" : "expand"} />
      </a>
      <div key={isFullScreen ? "A" : "B"} style={{ width: "100%", display: "flex" }}> 
        {p.children}
      </div>
    </div>
  );
}

