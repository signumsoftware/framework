import { QueryDescription } from '@framework/FindOptions';
import * as React from 'react'

import ReactChart, { MemoRepository } from './ReactChart';
import { ChartRequestModel } from '../../Signum.Chart';
import { ChartClient, ChartRow, ChartTable } from '../../ChartClient';
import { useSize, useThrottle } from '@framework/Hooks';
import { classes } from '@framework/Globals';
import { renderCombinedLinesAndColumns } from '../CombinedLinesAndColumns';

export interface ReactChartCombinedInfo {
  chartRequest: ChartRequestModel;
  data?: ChartTable;
  parameters: { [parameter: string]: string };
  memo: MemoRepository;
  onDrillDown: (row: ChartRow, e: React.MouseEvent | MouseEvent) => void;
}

export function ReactChartCombined(p: { infos: ReactChartCombinedInfo[], useSameScale: boolean, minHeigh: number | null, sizeDeps?: React.DependencyList }): React.JSX.Element {

  const isSimple = p.infos.every(a => a.data == null || a.data.rows.length < ReactChart.maxRowsForAnimation);
  const allData = p.infos.every(a => a.data != null);
  const oldAllData = useThrottle(allData, 200, { enabled: isSimple });
  const initialLoad = oldAllData == false && allData && isSimple;

  const { size, setContainer } = useSize({deps: p.sizeDeps});

  return (
    <div className={classes("sf-chart-container", isSimple ? "sf-chart-animable" : "")}
      style={{ minHeight: (p.minHeigh ?? 400) + "px" }}
      ref={setContainer} >
      {size &&
        renderCombinedLinesAndColumns({
          infos: p.infos,
          width: size.width,
          height: size.height,
          initialLoad: initialLoad,
          useSameScale: p.useSameScale,
        })
      }
    </div>
  );
}
