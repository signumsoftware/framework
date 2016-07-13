import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as d3 from 'd3'
import { DomUtils } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { is, SearchMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as ChartUtils from "./ChartUtils"
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption, hasAggregate } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { ChartColumnEntity, ChartScriptColumnEntity, ChartScriptParameterEntity, ChartRequest, GroupByChart, ChartMessage,
   ChartColorEntity, ChartScriptEntity, ChartParameterEntity, ChartParameterType } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'

const colorbrewer = require("colorbrewer");

require("!style!css!../Chart.css");

declare global {
    interface Error {
        lineNumber: number;
    }

    interface Window {
        changeScript(chartScript: ChartScriptEntity): void;
        getExceptionNumber(): number;
    }
}

export default class ChartRenderer extends React.Component<{ data: ChartClient.ChartTable; chartRequest: ChartRequest }, void> {

    exceptionLine: number;

    componentWillMount(){

        window.changeScript = (chartScript) => {
          if(!is(chartScript, this.props.chartRequest.chartScript))
              return;

            this.props.chartRequest.chartScript = chartScript;
            this.forceUpdate();
        };
        window.getExceptionNumber = () => {
            if (this.exceptionLine == undefined || this.exceptionLine == undefined)
                return undefined;

            const temp = this.exceptionLine;
            this.exceptionLine = undefined;
            return temp;
        };

    }

    componentDidMount() {
        this.redraw();
    }

    componentDidUpdate() {
        this.redraw();
    }

    redraw() {

        const node = ReactDOM.findDOMNode(this);
        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }
        const rect = node.getBoundingClientRect();

        const data = this.props.data;

        ChartUtils.fillAllTokenValueFuntions(data);

        data.parameters = this.props.chartRequest.parameters.map(mle => mle.element).toObject(a => a.name, a => a.value);

        this.props.chartRequest.chartScript.columns.map(a => a.element).map((cc, i) => {
            if (!data.columns["c" + i])
                data.columns["c" + i] = {};
        }); 

        const chart = d3.select(node)
            .append('svg:svg').attr('width', rect.width).attr('height', rect.height);

        node.addEventListener("click", this.handleOnClick);

        let func: (chart: d3.Selection<any>, data: ChartClient.ChartTable) => void;
        let __baseLineNumber__: number;
        try {
            const width = rect.width;
            const height = rect.height;
            const getClickKeys = ChartUtils.getClickKeys;
            const translate = ChartUtils.translate;
            const scale = ChartUtils.scale;
            const rotate = ChartUtils.rotate;
            const skewX = ChartUtils.skewX;
            const skewY = ChartUtils.skewY;
            const matrix = ChartUtils.matrix;
            const scaleFor = ChartUtils.scaleFor;
            const rule = ChartUtils.rule;
            const ellipsis = ChartUtils.ellipsis;
            __baseLineNumber__ = new Error().lineNumber;
            func = eval("(" + this.props.chartRequest.chartScript.script + ")");
        } catch (e) {
            this.showError(e, __baseLineNumber__, chart);
            return;
        }

        if (this.props.data.rows.length == 0) {
            const height = parseInt(chart.attr("height"));
            const width = parseInt(chart.attr("width"));

            chart.select(".sf-chart-error").remove();
            chart.append('svg:rect').attr('class', 'sf-chart-error').attr("x", width / 4).attr("y", (height / 2) - 10).attr("fill", "#EFF4FB").attr("stroke", "#FAC0DB").attr("width", width / 2).attr("height", 20);
            chart.append('svg:text').attr('class', 'sf-chart-error').attr("x", width / 4).attr("y", height / 2).attr("fill", "#0066ff").attr("dy", 5).attr("dx", 4).text(SearchMessage.NoResultsFound.niceToString());
        } else {
            try {
                func(chart, this.props.data);
                //this.bindMouseClick($chartContainer);
            } catch (e) {
                this.showError(e, __baseLineNumber__, chart);
            }
        }
    }

    handleOnClick = (e: MouseEvent) => {
        const element = DomUtils.closest(e.target as HTMLElement, "[data-click]", e.currentTarget as Node);
        if (element)
        {
            const val = element.getAttribute("data-click");

            const cr = this.props.chartRequest;

            const filters = cr.filterOptions.filter(a => !hasAggregate(a.token));

            const obj = val.split("&").filter(a => !!a).toObject(a => a.before("="), a => a.after("="));

            cr.columns.map((a, i) => {
                if (obj.hasOwnProperty("c" + i))
                    filters.push({
                        columnName: a.element.token.token.fullKey,
                        token: a.element.token.token,
                        operation: "EqualTo",
                        value: obj["c" + i]
                    } as FilterOption);
            });

            window.open(Finder.findOptionsPath({
                queryName: cr.queryKey,
                filterOptions: filters
            }));
        }
    }

    showError(e: any, __baseLineNumber__: number, chart: d3.Selection<any>) {
        let message = e.toString();

        const regex = /(DrawChart.*@.*:(.*))|(DrawChart .*:(.*):.*\)\))|(DrawChart .*:(.*):.*\))/;
        let match: RegExpExecArray;
        if (e.stack != undefined && (match = regex.exec(e.stack)) != undefined) {
            let lineNumber = parseInt(match[2] || match[4] || match[6]) - (__baseLineNumber__ || 0);
            if (isNaN(lineNumber))
                lineNumber = 1;
            this.exceptionLine = lineNumber;
            message = "Line " + lineNumber + ": " + message;
        } else {
            this.exceptionLine = 1;
        }

        const height = parseInt(chart.attr("height"));
        const width = parseInt(chart.attr("width"));

        chart.select(".sf-chart-error").remove();
        chart.append('svg:rect').attr('class', 'sf-chart-error').attr("y", (height / 2) - 10).attr("fill", "#FBEFFB").attr("stroke", "#FAC0DB").attr("width", width - 1).attr("height", 20);
        chart.append('svg:text').attr('class', 'sf-chart-error').attr("y", height / 2).attr("fill", "red").attr("dy", 5).attr("dx", 4).text(message);
    }


    render() {
        return (
            <div className="sf-chart-container"></div>
        );
    }
}




