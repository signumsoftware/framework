import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as d3 from 'd3'
import { DomUtils } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as ChartUtils from "./ChartUtils"
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { ChartColumnEntity, ChartScriptColumnEntity, ChartScriptParameterEntity, ChartRequest, GroupByChart, ChartMessage,
   ChartColorEntity, ChartScriptEntity, ChartParameterEntity, ChartParameterType } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'

var colorbrewer = require("colorbrewer");

declare global {
    interface Error {
        lineNumber: number;
    }

    interface Window {       
        changeScript(chartScript: ChartScriptEntity);
        getExceptionNumber(): number;
    }
}

export default class ChartRenderer extends React.Component<{ data: ChartClient.API.ChartTable; chartRequest: ChartRequest }, void> {

    exceptionLine: number;

    componentWillMount(){

        window.changeScript = (chartScript) => {
          if(!is(chartScript, this.props.chartRequest.chartScript))
              return;

            this.props.chartRequest.chartScript = chartScript;
            this.forceUpdate();
        };
        window.getExceptionNumber = () => {
            if (this.exceptionLine == null || this.exceptionLine == undefined)
                return null;

            var temp = this.exceptionLine;
            this.exceptionLine = null;
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

        var node = ReactDOM.findDOMNode(this);
        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }
        var rect = node.getBoundingClientRect();

        var data = this.props.data;

        ChartUtils.fillAllTokenValueFuntions(data);

        data.parameters = this.props.chartRequest.parameters.map(mle => mle.element).toObject(a => a.name, a => a.value);

        this.props.chartRequest.chartScript.columns.map(a => a.element).map((cc, i) => {
            if (!data.columns["c" + i])
                data.columns["c" + i] = {};
        }); 

        var chart = d3.select(node)
            .append('svg:svg').attr('width', rect.width).attr('height', rect.height);

        node.addEventListener("click", this.handleOnClick);

        var func;
        var __baseLineNumber__: number;
        try {
            var width = rect.width;
            var height = rect.height;
            var getClickKeys = ChartUtils.getClickKeys;
            var translate = ChartUtils.translate;
            var scale = ChartUtils.scale;
            var rotate = ChartUtils.rotate;
            var skewX = ChartUtils.skewX;
            var skewY = ChartUtils.skewY;
            var matrix = ChartUtils.matrix;
            var scaleFor = ChartUtils.scaleFor;
            var rule = ChartUtils.rule;
            var ellipsis = ChartUtils.ellipsis;
            __baseLineNumber__ = new Error().lineNumber;
            func = eval("(" + this.props.chartRequest.chartScript.script + ")");
        } catch (e) {
            this.showError(e, __baseLineNumber__, chart);
            return;
        }

        try {
            func(chart, this.props.data);
            //this.bindMouseClick($chartContainer);
        } catch (e) {
            this.showError(e, __baseLineNumber__, chart);
        }
    }

    handleOnClick = (e: MouseEvent) => {
        var element = DomUtils.closest(e.target as HTMLElement, "[data-click]", e.currentTarget as Node);
        if (element)
        {
            var val = element.getAttribute("data-click");

            var cr = this.props.chartRequest;

            var filters = cr.filterOptions.clone();

            var obj = val.split("&").filter(a => !!a).toObject(a => a.before("="), a => a.after("="));

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

    showError(e, __baseLineNumber__, chart) {
        var message = e.toString();

        var regex = /(DrawChart.*@.*:(.*))|(DrawChart .*:(.*):.*\)\))|(DrawChart .*:(.*):.*\))/;
        var match;
        if (e.stack != undefined && (match = regex.exec(e.stack)) != null) {
            var lineNumber = parseInt(match[2] || match[4] || match[6]) - (__baseLineNumber__ || 0);
            if (isNaN(lineNumber))
                lineNumber = 1;
            this.exceptionLine = lineNumber;
            message = "Line " + lineNumber + ": " + message;
        } else {
            this.exceptionLine = 1;
        }

        chart.select(".sf-chart-error").remove();
        chart.append('svg:rect').attr('class', 'sf-chart-error').attr("y", (chart.attr("height") / 2) - 10).attr("fill", "#FBEFFB").attr("stroke", "#FAC0DB").attr("width", chart.attr("width") - 1).attr("height", 20);
        chart.append('svg:text').attr('class', 'sf-chart-error').attr("y", chart.attr("height") / 2).attr("fill", "red").attr("dy", 5).attr("dx", 4).text(message);
    }


    render() {
        return (
            <div className="sf-chart-container"></div>
        );
    }
}




