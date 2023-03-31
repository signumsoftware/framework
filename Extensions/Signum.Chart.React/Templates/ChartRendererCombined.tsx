import * as React from 'react'
import { DomUtils, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { FilterOptionParsed, ColumnOption, hasAggregate, withoutAggregate, FilterOption, FindOptions, withoutPinned } from '@framework/FindOptions'
import { ChartRequestModel, ChartMessage, UserChartEntity } from '../Signum.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';

import "../Chart.css"
import { ChartScript, ChartRow } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';

import ReactChart, { MemoRepository } from '../D3Scripts/Components/ReactChart';
import { useAPI } from '@framework/Hooks'
import { TypeInfo } from '@framework/Reflection'
import { FullscreenComponent } from './FullscreenComponent'
import { handleDrillDown } from './ChartRenderer'
import { ReactChartCombined } from '../D3Scripts/Components/ReactChartCombined'
import { Lite } from '@framework/Signum.Entities'



export interface ChartRendererCombinedProps {
  infos: ChartRendererCombinedInfo[]
  onReload?: (e: React.MouseEvent<any>) => void;
  useSameScale: boolean;
  minHeigh: number | null;
}

export interface ChartRendererCombinedInfo {
  userChart: Lite<UserChartEntity>;
  chartRequest: ChartRequestModel;
  chartScript: ChartScript;
  data?: ChartClient.ChartTable;
  memo: MemoRepository;
} 

export default function ChartRendererCombined(p: ChartRendererCombinedProps) {

  return (
    <FullscreenComponent onReload={p.onReload} >
      <ErrorBoundary deps={p.infos.map(a => a.data)}>
        <ReactChartCombined useSameScale={p.useSameScale} minHeigh={p.minHeigh} infos={p.infos.map(info => ({
          chartRequest: info.chartRequest,
          onDrillDown: (r, e) => handleDrillDown(r, e, info.chartRequest, info.userChart),
          parameters: ChartClient.API.getParameterWithDefault(info.chartRequest, info.chartScript),
          data: info.data,
          memo: info.memo
        }))} />
      </ErrorBoundary>
    </FullscreenComponent>
  );
}

/*
 *  <div>
      <FullscreenComponent onReload={handleReload}>
        <ErrorBoundary deps={infos.current}>

          {infos.current && infos.current. cs && parameters &&
            <CombinedReactChart
              chartRequest={p.chartRequest}
              data={p.data}
              loading={p.loading}
              onDrillDown={(r, e) => handleDrillDown(r, e, p.lastChartRequest!)}
              parameters={parameters}
              onRenderChart={cs.chartComponent as ((p: ChartClient.ChartScriptProps) => React.ReactNode)} />
          }
        </ErrorBoundary>
      </FullscreenComponent>
    </div>
 * 
 * */
