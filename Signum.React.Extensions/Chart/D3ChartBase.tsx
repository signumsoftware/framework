import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as D3 from 'd3'
import * as ChartClient from './ChartClient'
import * as Navigator from '@framework/Navigator';
import { ColumnOption, FilterOptionParsed } from '@framework/Search';
import { hasAggregate } from '@framework/FindOptions';
import { DomUtils } from '@framework/Globals';
import { parseLite, SearchMessage } from '@framework/Signum.Entities';

export default abstract class D3ChartBase extends React.Component<{ data: ChartClient.ChartTable, onClick: (e: MouseEvent) => void }> {

    componentDidUpdate() {
        const node = ReactDOM.findDOMNode(this) as HTMLDivElement;
        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }
        const rect = node.getBoundingClientRect();

        const data = this.props.data;

        const chart = D3.select(node)
            .append<SVGElement>('svg:svg')
            .attr("direction", "ltr")
            .attr('width', rect.width)
            .attr('height', rect.height);

        if (this.props.data.rows.length == 0) {
            const height = parseInt(chart.attr("height"));
            const width = parseInt(chart.attr("width"));

            chart.select(".sf-chart-error").remove();
            chart.append('svg:rect').attr('class', 'sf-chart-error').attr("x", width / 4).attr("y", (height / 2) - 10).attr("fill", "#EFF4FB").attr("stroke", "#FAC0DB").attr("width", width / 2).attr("height", 20);
            chart.append('svg:text').attr('class', 'sf-chart-error').attr("x", width / 4).attr("y", height / 2).attr("fill", "#0066ff").attr("dy", 5).attr("dx", 4).text(SearchMessage.NoResultsFound.niceToString());

        } else {
            node.addEventListener("click", this.props.onClick);

            if (rect.width && rect.height)
                this.drawChart(this.props.data, chart, rect.width, rect.height);
        }
    }

    abstract drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, null, undefined>, width: number, height: number): void;

    divElement?: HTMLDivElement | null;

    render() {
        return (
            <div className="sf-chart-container" ref={d => this.divElement = d}>
            </div>
        );
    }
}
