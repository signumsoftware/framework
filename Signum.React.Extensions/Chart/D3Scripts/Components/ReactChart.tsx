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


export interface ReactChartProps {
  data?: ChartClient.ChartTable;
  parameters: { [parameter: string]: string }; 
  loading: boolean;
  onDrillDown: (e: ChartRow) => void;
  onRenderChart: (data: ChartClient.ChartScriptProps) => React.ReactNode;
}

export default class ReactChart extends React.Component<ReactChartProps, { width: number | null, height: number | null, initialLoad: boolean }> {

  static maxRowsForAnimation = 500;

  constructor(props: ReactChartProps) {
    super(props);
    this.state = { width: null, height: null, initialLoad: true };
  }

  divElement?: HTMLDivElement | null;

  setDivElement(div?: HTMLDivElement | null) {
    if (this.divElement == null && div != null) {
      const rect = div.getBoundingClientRect();
      if (this.state.width != rect.width && this.state.height != rect.height) {
        this.setState({ width: rect.width, height: rect.height });
      }

    }
    this.divElement = div;
  }

  componentWillMount() {
    window.addEventListener('resize', this.onResize);
    if (this.props.data) {
      this.setInitialTimer();
    }
  }

  resizeHandle?: number;
  onResize = () => {
    if (this.resizeHandle != null)
      clearTimeout(this.resizeHandle);

    this.resizeHandle = setTimeout(this.onResizeTimeout, 300);
  }

  onResizeTimeout = () => {
    if (this.divElement) {
      const rect = this.divElement.getBoundingClientRect();
      if (this.state.width != rect.width || this.state.height != rect.height) {
        this.setState({ width: rect.width, height: rect.height });
      }
    }
  }

  initialLoadTimeoutHandle?: number;
  componentWillReceiveProps(newProps: ReactChartProps) {
    if (this.props.data == null && newProps.data != null) {
      if (newProps.data.rows.length < ReactChart.maxRowsForAnimation)
        this.setInitialTimer();
      else
        this.state.initialLoad = false; //To use the same rendering loop
    }
  }

  setInitialTimer() {
    this.initialLoadTimeoutHandle = setTimeout(() => {
      this.initialLoadTimeoutHandle = undefined;
      this.setState({ initialLoad: false });
    }, 500);
  }

  componentWillUnmount() {
    window.removeEventListener('resize', this.onResize);
    if (this.initialLoadTimeoutHandle != null) {
      clearTimeout(this.initialLoadTimeoutHandle);
      this.initialLoadTimeoutHandle = undefined;
    }
  }

  render() {
    var animated = this.props.data == null || this.props.data.rows.length < ReactChart.maxRowsForAnimation;
    return (
      <div className={classes("sf-chart-container", animated ? "sf-chart-animable" : "")} ref={d => this.setDivElement(d)} >
        {this.state.width != null && this.state.height != null &&
          this.props.onRenderChart({
            data: this.props.data,
            parameters: this.props.parameters,
            loading: this.props.loading,
            onDrillDown: this.props.onDrillDown,
            height: this.state.height,
            width: this.state.width,
            initialLoad: this.state.initialLoad,
          })
        }
      </div>
    );
  }
}
