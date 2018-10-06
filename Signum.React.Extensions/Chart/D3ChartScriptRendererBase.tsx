import * as React from 'react'
import * as D3 from 'd3'
import * as ChartClient from './ChartClient'

export default class D3ChartScriptRendererBase extends React.Component<{ data: ChartClient.ChartTable }> {

    drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, HTMLDivElement, unknown>) {
        
    }

    render() {
        return (
            <div>

            </div>
        );
    }
}
