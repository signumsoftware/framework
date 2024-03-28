import * as React from 'react'
import { classes } from '@framework/Globals';
import { ChartClient, ChartRow, ChartScriptProps, ChartTable } from '../../ChartClient';
import { useThrottle, useSize, areEqualDeps } from '@framework/Hooks';
import { ChartRequestModel } from '../../Signum.Chart';
import { DashboardFilter } from '../../../Signum.Dashboard/View/DashboardFilterController';

export interface ReactChartProps {
  chartRequest: ChartRequestModel,
  data?: ChartTable;
  parameters: { [parameter: string]: string }; 
  loading: boolean;
  onReload: (() => void) | undefined;
  onDrillDown: (row: ChartRow, e: React.MouseEvent | MouseEvent) => void;
  onBackgroundClick?: (e: React.MouseEvent) => void;
  onRenderChart: (data: ChartScriptProps) => React.ReactNode;
  dashboardFilter?: DashboardFilter;
  minHeight: number | null;
}


export default function ReactChart(p: ReactChartProps) {

  const isSimple = p.data == null || p.data.rows.length < ReactChart.maxRowsForAnimation;
  const oldData = useThrottle(p.data, 200, { enabled: isSimple });
  const initialLoad = oldData == null && p.data != null && isSimple;

  const memo = React.useMemo(() => new MemoRepository(), [p.chartRequest, p.chartRequest.chartScript]);

  const { size, setContainer } = useSize();

  return (
    <div className={classes("sf-chart-container", isSimple ? "sf-chart-animable" : "")} style={{ minHeight: (p.minHeight ?? 400) + "px" }} ref={setContainer} onClick={p.onBackgroundClick}>
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
          memo: memo,
          dashboardFilter: p.dashboardFilter
        })
      }
    </div>
  );
}

ReactChart.maxRowsForAnimation = 500;


export class MemoRepository {
  cache: Map<string, { val: unknown, deps: unknown[] }> = new Map();

  memo<T>(name: string, deps: unknown[], factory: () => T): T {
    var box = this.cache.get(name);
    if (box == null || !areEqualDeps(box.deps, deps)) {
      box = {
        val: factory(),
        deps: deps,
      };
      this.cache.set(name, box);
    }

    return box.val as T;
  }
}


