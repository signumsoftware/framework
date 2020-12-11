import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as D3 from 'd3'
import * as ChartClient from '../../ChartClient'
import * as Navigator from '@framework/Navigator';
import { ColumnOption, FilterOptionParsed } from '@framework/Search';
import { hasAggregate } from '@framework/FindOptions';
import { DomUtils, classes } from '@framework/Globals';
import { parseLite, SearchMessage } from '@framework/Signum.Entities';
import { ChartRow } from '../../ChartClient';
import { Rectangle } from '../../../Map/Utils';
import { useThrottle, useSize, useAPI } from '@framework/Hooks';
import { ChartRequestModel } from '../../Signum.Entities.Chart';

export interface ReactChartProps {
  chartRequest: ChartRequestModel,
  data?: ChartClient.ChartTable;
  parameters: { [parameter: string]: string }; 
  loading: boolean;
  onReload: (() => void) | undefined;
  onDrillDown: (row: ChartRow, e: React.MouseEvent | MouseEvent) => void;
  onRenderChart: (data: ChartClient.ChartScriptProps) => React.ReactNode;
}


export default function ReactChart(p: ReactChartProps) {

  const isSimple = p.data == null || p.data.rows.length < ReactChart.maxRowsForAnimation;
  const oldData = useThrottle(p.data, 200, { enabled: isSimple});
  const initialLoad = oldData == null && p.data != null && isSimple;

  const { size, setContainer } = useSize();

  return (
    <div className={classes("sf-chart-container", isSimple ? "sf-chart-animable" : "")} ref={setContainer} >
      {size &&
        p.onRenderChart({
          chartRequest: p.chartRequest,
          data: p.data,
          parameters: p.parameters,
          loading: p.loading,
          onDrillDown: p.onDrillDown,
          onReload: p.onReload,
          height: size.height,
          width: size.width,
          initialLoad: initialLoad,
        })
      }
    </div>
  );
}

ReactChart.maxRowsForAnimation = 500;



