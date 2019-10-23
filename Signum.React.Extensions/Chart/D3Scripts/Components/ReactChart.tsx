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
import { useThrottle } from '../../../../../Framework/Signum.React/Scripts/Hooks';


export interface ReactChartProps {
  data?: ChartClient.ChartTable;
  parameters: { [parameter: string]: string }; 
  loading: boolean;
  onDrillDown: (e: ChartRow) => void;
  onRenderChart: (data: ChartClient.ChartScriptProps) => React.ReactNode;
}

export default function ReactChart(p: ReactChartProps) {

  const [initialLoad, setInitialLoad] = React.useState<boolean>(p.data != null && p.data.rows.length < ReactChart.maxRowsForAnimation);
  const initialLoadHandler = React.useRef<number | null>(null);
  React.useEffect(() => {
    if (p.data != null && p.data.rows.length < ReactChart.maxRowsForAnimation) {
      setInitialLoad(true);
      if (initialLoadHandler.current != null) {
        clearTimeout(initialLoadHandler.current);
        initialLoadHandler.current = null;
      }
      initialLoadHandler.current = setTimeout(() => {
        setInitialLoad(false);
      }, 500);

      return () => {
        if (initialLoadHandler.current != null) {
          clearTimeout(initialLoadHandler.current);
          initialLoadHandler.current = null;
        }
      };
    }

  }, [p.data != null]);

  const [size, setSize] = React.useState<{ width: number, height: number } | undefined>();
  const divElement = React.useRef<HTMLDivElement | null>(null);
  function setNewSize() {
    const rect = divElement.current!.getBoundingClientRect();
    if (size == null || size.width != rect.width || size.height != rect.height)
      setSize({ width: rect.width, height: rect.height });
  }

  function setDivElement(div: HTMLDivElement | null) {
    if (divElement.current = div) {
      setNewSize();
    }
  }

  React.useEffect(() => {
    const handler = React.useRef<number | null>(null);
    function onResize() {
      if (handler.current != null)
        clearTimeout(handler.current);

      handler.current = setTimeout(() => {
        if (divElement.current) {
          setNewSize()
        }
      }, 300);
    }

    window.addEventListener('resize', onResize);

    return () => {
      if (handler.current)
        clearTimeout(handler.current);

      window.removeEventListener("resize", onResize);
    };
  }, []);


  var animated = p.data == null || p.data.rows.length < ReactChart.maxRowsForAnimation;
  return (
    <div className={classes("sf-chart-container", animated ? "sf-chart-animable" : "")} ref={d => setDivElement(d)} >
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
