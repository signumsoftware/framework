import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as D3 from 'd3'
import * as ChartClient from '../ChartClient'
import * as Navigator from '@framework/Navigator';
import { ColumnOption, FilterOptionParsed } from '@framework/Search';
import { hasAggregate } from '@framework/FindOptions';
import { DomUtils } from '@framework/Globals';
import { parseLite, SearchMessage } from '@framework/Signum.Entities';
import { ChartRow } from '../ChartClient';


export interface ReactChartBaseProps {
  data: ChartClient.ChartTable;
  onDrillDown: (e: ChartRow) => void;
}

export default abstract class ReactChartBase extends React.Component<ReactChartBaseProps, { width: number | null, height: number | null }> {

  constructor(props: ReactChartBaseProps) {
    super(props);
    this.state = { width: null, height: null };
  }

  divElement?: HTMLDivElement | null;

  setDivElement(div?: HTMLDivElement | null) {
    if (this.divElement == null && div != null) {
      const rect = div.getBoundingClientRect();
      if (this.state.width != rect.width && this.state.height != rect.height) {
        this.setState({ width: rect.width, height: rect.height });
      }

    }
  }

  render() {
    return (
      <div className="sf-chart-container" ref={d => this.setDivElement(d)}>
        {this.state.width != null && this.state.height != null && this.renderChartOrMessage(this.state.width, this.state.height)}
      </div>
    );
  }

  renderChartOrMessage(width: number, height: number): React.ReactElement<any> {

    const data = this.props.data;

    if (this.props.data.rows.length == 0)
      return (
        <svg direction="rtl" width={width} height={height}>
          <rect className="sf-chart-error" x={width / 4} y={(height / 2) - 10} fill="#EFF4FB" stroke="#FAC0DB" width={width / 2} height={20} />
          <text className="sf-chart-error" x={width / 4} y={(height / 2)} fill="#EFF4FB" dy={4} dx={4}>{SearchMessage.NoResultsFound.niceToString()}</text>
        </svg>
      );
    else
      return this.renderChart(data, width, height);
  }

  abstract renderChart(data: ChartClient.ChartTable, width: number, height: number): React.ReactElement<any>;
}
