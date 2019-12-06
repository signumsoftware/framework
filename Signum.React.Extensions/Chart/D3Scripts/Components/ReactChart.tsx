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

export interface ReactChartProps {
  data?: ChartClient.ChartTable;
  parameters: { [parameter: string]: string }; 
  loading: boolean;
  onDrillDown: (e: ChartRow) => void;
  onRenderChart: (data: ChartClient.ChartScriptProps) => React.ReactNode;
}


export default function ReactChart(p: ReactChartProps) {

  const initalLoadEnabled = p.data == null || p.data.rows.length < ReactChart.maxRowsForAnimation;
  const oldData = useThrottle(p.data, 200, { enabled: initalLoadEnabled});
  const initialLoad = oldData == null && p.data != null && initalLoadEnabled;

  const { size, setContainer } = useSize();

  var animated = p.data == null || p.data.rows.length < ReactChart.maxRowsForAnimation;
  return (
    <div className={classes("sf-chart-container", animated ? "sf-chart-animable" : "")} ref={setContainer} >
      {size &&
        p.onRenderChart({
          data: p.data,
          parameters: p.parameters,
          loading: p.loading,
          onDrillDown: p.onDrillDown,
          height: size.height,
          width: size.width,
          initialLoad: initialLoad,
        })
      }
    </div>
  );
}


ReactChart.maxRowsForAnimation = 500;
