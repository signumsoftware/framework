import * as React from 'react'
import { DomUtils, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { parseLite, is, SearchMessage } from '@framework/Signum.Entities'
import { FilterOptionParsed, ColumnOption, hasAggregate, withoutAggregate, FilterOption, FindOptions, withoutPinned } from '@framework/FindOptions'
import { ChartRequestModel, ChartMessage } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';

import "../Chart.css"
import { ChartScript, chartScripts, ChartRow } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import ReactChart from '../D3Scripts/Components/ReactChart';
import { useAppRelativeBasename } from '../../../../Framework/Signum.React/Scripts/AppRelativeRoutes'
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'
import { TypeInfo } from '@framework/Reflection'
import { pushOrOpenInTab } from '../../../../Framework/Signum.React/Scripts/AppContext'


export interface ChartRendererProps {
  chartRequest: ChartRequestModel;
  loading: boolean;
  data?: ChartClient.ChartTable;
  lastChartRequest?: ChartRequestModel;
  onReload?: (e: React.MouseEvent<any>) => void;
  onCreateNew?: (e: React.MouseEvent<any>) => void;
  typeInfos?: TypeInfo[];
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

  function handleDrillDown(r: ChartRow, e: React.MouseEvent | MouseEvent) {
    const cr = p.lastChartRequest!;

    var newWindow = e.ctrlKey || e.button == 1;

    if (r.entity) {
      if (newWindow)
        window.open(Navigator.navigateRoute(r.entity));
      else
        Navigator.navigate(r.entity).done();
    } else {
      const filters = cr.filterOptions.map(f => withoutAggregate(withoutPinned(f))!).filter(Boolean);

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

      var fo: FindOptions = {
        queryName: cr.queryKey,
        filterOptions: toFilterOptions(filters),
        includeDefaultFilters: false,
        columnOptions: columns,
      };

      if (newWindow)
        window.open(Finder.findOptionsPath(fo));
      else
        Finder.explore(fo).done();
    }
  }

  return (
    <FullscreenComponent onReload={p.onReload} onCreateNew={p.onCreateNew} typeInfos={p.typeInfos}>
      <ErrorBoundary refreshKey={p.data}>
        {cs && parameters &&
          <ReactChart
            chartRequest={p.chartRequest}
            data={p.data}
            loading={p.loading}
            onDrillDown={handleDrillDown}
            parameters={parameters}
            onRenderChart={cs.chartComponent as ((p: ChartClient.ChartScriptProps) => React.ReactNode)} />
        }
      </ErrorBoundary>
    </FullscreenComponent>
  );
}

interface FullscreenComponentProps {
  children: React.ReactNode;
  onReload?: (e: React.MouseEvent<any>) => void;
  typeInfos?: TypeInfo[];
  onCreateNew?: (e: React.MouseEvent<any>) => void;
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

      <div key={isFullScreen ? "A" : "B"} style={{ width: "100%", display: "flex" }}>
        {p.children}
      </div>
      <div style={{ display: "flex", flexDirection: "column", marginLeft: "5px" }}>
        <a onClick={handleExpandToggle} href="#" className="sf-chart-mini-icon" title={isFullScreen ? ChartMessage.Minimize.niceToString() : ChartMessage.Maximize.niceToString()}  >
          <FontAwesomeIcon icon={isFullScreen ? "compress" : "expand"} />
        </a>
        {p.onReload &&
          <a onClick={p.onReload} href="#" className="sf-chart-mini-icon" title={ChartMessage.Reload.niceToString()} >
            <FontAwesomeIcon icon={"redo"} />
          </a>
        }
        {p.onCreateNew && p.typeInfos &&
          <a onClick={p.onCreateNew} href="#" className="sf-chart-mini-icon" title={createNewTitle(p.typeInfos)}>
            <FontAwesomeIcon icon={"plus"} />
          </a>
        }
      </div>

    </div>
  );
}

function createNewTitle(tis: TypeInfo[]) {

  const types = tis.map(ti => ti.niceName).join(", ");
  const gender = tis.first().gender;

  return SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(gender).formatWith(types);
}

