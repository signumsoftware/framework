import * as React from 'react'
import { DomUtils } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { parseLite, is } from '@framework/Signum.Entities'
import { FilterOptionParsed, ColumnOption, hasAggregate } from '@framework/FindOptions'
import { ChartRequest } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'

import "../Chart.css"
import { toFilterOptions } from '@framework/Finder';
import { ChartScript, chartScripts } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';


export interface ChartRendererProps {
    data: ChartClient.ChartTable;
    chartRequest: ChartRequest;
    lastChartRequest: ChartRequest;
}

export interface ChartRendererState {
    chartScript?: ChartScript;
    chartComponent?: React.ComponentClass<{ data: ChartClient.ChartTable, onClick: (e: MouseEvent) => void }>;
}


export default class ChartRenderer extends React.Component<ChartRendererProps, ChartRendererState> {

    constructor(props: ChartRendererProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.requestAndRedraw().done();
    }

    lastChartRequestPath: string | undefined;
    shouldComponentUpdate(newProps: ChartRendererProps) {
        if (this.props.data != newProps.data)
            return true;

        if (this.lastChartRequestPath != ChartClient.Encoder.chartPath(newProps.chartRequest))
            return true;

        return false;
    }

    componentWillReceiveProps(newProps: ChartRendererProps) {
            this.requestAndRedraw().done();
    }

    async requestAndRedraw() {

        const chartScriptPromise = ChartClient.getChartScript(this.props.chartRequest.chartScript);
        const chartComponentModulePromise = ChartClient.getRegisteredChartScriptComponent(this.props.chartRequest.chartScript);

        const chartScript = await chartScriptPromise;
        const chartComponentModule = await chartComponentModulePromise();


        const data = this.props.data;
        data.parameters = this.props.chartRequest.parameters.map(mle => mle.element).toObject(a => a.name!, a => a.value);

        chartScript.columns.map((cc, i) => {
            if (!(data.columns as any)["c" + i])
                (data.columns as any)["c" + i] = { name: "c" + 1 };
        });

        this.setState({ chartComponent: chartComponentModule.default, chartScript });
    }

    //redraw(chartScript: ChartScript, component: React.ComponentClass<any>) {
    //    const node = ReactDOM.findDOMNode(this) as SVGElement;
    //    while (node.firstChild) {
    //        node.removeChild(node.firstChild);
    //    }
    //    const rect = node.getBoundingClientRect();

    //    const data = this.props.data;

    //    data.parameters = this.props.chartRequest.parameters.map(mle => mle.element).toObject(a => a.name!, a => a.value);

    //    chartScript.columns.map((cc, i) => {
    //        if (!(data.columns as any)["c" + i])
    //            (data.columns as any)["c" + i] = { name: "c" + 1 };
    //    });

    //    const chart = D3.select(node)
    //        .append('svg:svg').attr("direction", "ltr").attr('width', rect.width).attr('height', rect.height);

    //    node.addEventListener("click", this.handleOnClick);

    //    let func: (chart: D3.Selection<any, any, any, any>, data: ChartClient.ChartTable) => void;
    //    const d3 = D3;
    //    const ChartUtils = ChartUtils_Mod;
    //    const width = rect.width;
    //    const height = rect.height;
    //    const getClickKeys = ChartUtils.getClickKeys;
    //    const translate = ChartUtils.translate;
    //    const scale = ChartUtils.scale;
    //    const rotate = ChartUtils.rotate;
    //    const skewX = ChartUtils.skewX;
    //    const skewY = ChartUtils.skewY;
    //    const matrix = ChartUtils.matrix;
    //    const scaleFor = ChartUtils.scaleFor;
    //    const rule = ChartUtils.rule;
    //    const ellipsis = ChartUtils.ellipsis;
    //    this.lastChartRequestPath = ChartClient.Encoder.chartPath(this.props.chartRequest);
    //    func = eval("(" + this.props.chartRequest.chartScript.script + ")");


    //    if (this.props.data.rows.length == 0) {
    //        const height = parseInt(chart.attr("height"));
    //        const width = parseInt(chart.attr("width"));

    //        chart.select(".sf-chart-error").remove();
    //        chart.append('svg:rect').attr('class', 'sf-chart-error').attr("x", width / 4).attr("y", (height / 2) - 10).attr("fill", "#EFF4FB").attr("stroke", "#FAC0DB").attr("width", width / 2).attr("height", 20);
    //        chart.append('svg:text').attr('class', 'sf-chart-error').attr("x", width / 4).attr("y", height / 2).attr("fill", "#0066ff").attr("dy", 5).attr("dx", 4).text(SearchMessage.NoResultsFound.niceToString());
    //    } else {
    //        try {
    //            func(chart, this.props.data);
    //            //this.bindMouseClick($chartContainer);
    //        } catch (e) {
    //            this.showError(e, __baseLineNumber__, chart);
    //        }
    //    }
    //}

    handleOnClick = (e: MouseEvent) => {
        const element = DomUtils.closest(e.target as HTMLElement, "[data-click]", e.currentTarget as Node);
        if (element) {
            const val = element.getAttribute("data-click");

            const obj = val!.split("&").filter(a => !!a).toObject(a => a.before("="), a => a.after("="));

            const cr = this.props.lastChartRequest!;

            if (cr.groupResults == false) {

                var lite = parseLite(obj["entity"]);

                window.open(Navigator.navigateRoute(lite));

            } else {

                const filters = cr.filterOptions.filter(a => !hasAggregate(a.token));
                const columns: ColumnOption[] = [];

                cr.columns.map((a, i) => {

                    const t = a.element.token;

                    if (obj.hasOwnProperty("c" + i)) {
                        filters.push({
                            token: t!.token!,
                            operation: "EqualTo",
                            value: obj["c" + i] == "null" ? null : obj["c" + i],
                            frozen: false
                        } as FilterOptionParsed);
                    }

                    if (t && t.token && t.token.parent != undefined) //Avoid Count and simple Columns that are already added
                    {
                        var col = t.token.queryTokenType == "Aggregate" ? t.token.parent : t.token

                        if (col.parent)
                            columns.push({
                                token: col.fullKey
                            });
                    }
                });

                window.open(Finder.findOptionsPath({
                    queryName: cr.queryKey,
                    filterOptions: toFilterOptions(filters),
                    columnOptions: columns,
                }));
            }
        }
    }


    render() {
        return (
            <ErrorBoundary>
                {this.state.chartComponent && React.createElement(this.state.chartComponent, { data: this.props.data, onClick: this.handleOnClick })}
            </ErrorBoundary>
        );
    }
}