import * as React from 'react'
import { Dic, classes, ifError } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ValidationError, AbortableRequest } from '../../../../Framework/Signum.React/Scripts/Services'
import { Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, FormGroupStyle, StyleOptions, StyleContext, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SearchMessage, JavascriptMessage, parseLite, is, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PropertyRoute, getQueryNiceName, getTypeInfo, ReadonlyBinding, GraphExplorer }  from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import FilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import { ValueLine, FormGroup, ValueLineProps, ValueLineType } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ChartColumnEmbedded, ChartScriptColumnEmbedded, ChartScriptParameterEmbedded, ChartRequest, GroupByChart, ChartMessage,
    ChartColorEntity, ChartScriptEntity, ChartParameterEmbedded, ChartParameterType, UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { ChartColumn, ChartColumnInfo }from './ChartColumn'
import ChartBuilder from './ChartBuilder'
import ChartTable from './ChartTable'
import ChartRenderer from './ChartRenderer'


import "../Chart.css"
import "../../../../Framework/Signum.React/Scripts/SearchControl/Search.css"
import { Tab, Tabs, UncontrolledTabs } from '../../../../Framework/Signum.React/Scripts/Components/Tabs';


interface ChartRequestViewProps {
    chartRequest?: ChartRequest;
    userChart?: Lite<UserChartEntity>;
    onChange: (newChartRequest: ChartRequest, userChart?: Lite<UserChartEntity>) => void;
    title?: string;
}

interface ChartRequestViewState {
    queryDescription?: QueryDescription;
    lastChartRequest?: ChartRequest;
    chartResult?: ChartClient.API.ExecuteChartResult;
}

export default class ChartRequestView extends React.Component<ChartRequestViewProps, ChartRequestViewState> {

    lastToken: QueryToken | undefined;

    constructor(props: ChartRequestViewProps) {
        super(props);
        this.state = { };
   
    }

    componentWillMount() {
        this.loadQueryDescription(this.props);
    }

    componentWillReceiveProps(nextProps: ChartRequestViewProps) {

        var oldPath = this.props.chartRequest && ChartClient.Encoder.chartPath(this.props.chartRequest, this.props.userChart);
        var newPath = nextProps.chartRequest && ChartClient.Encoder.chartPath(nextProps.chartRequest, nextProps.userChart);

        if (oldPath == newPath)
            return;
        
        this.setState({ chartResult: undefined, lastChartRequest: undefined });
        this.loadQueryDescription(nextProps);
    }

    loadQueryDescription(props: ChartRequestViewProps) {
        if (props.chartRequest) {
            Finder.getQueryDescription(props.chartRequest.queryKey).then(qd => {
                this.setState({ queryDescription: qd });
            }).done();
        }
    }

    handleTokenChange = () => {
        this.removeObsoleteOrders();
    }

    handleInvalidate = () => {
        this.removeObsoleteOrders();
        this.setState({ chartResult: undefined, lastChartRequest: undefined });
    }

    removeObsoleteOrders() {
        var cr = this.props.chartRequest;
        if (cr && cr.groupResults) {
            var oldOrders = cr.orderOptions.filter(o =>
                o.token.queryTokenType != "Aggregate" &&
                !cr!.columns.some(mle2 => !!mle2.element.token && !!mle2.element.token.token && mle2.element.token.token.fullKey == o.token.fullKey));

            oldOrders.forEach(o => cr!.orderOptions.remove(o));
        }
    }

    handleOnRedraw = () => {
        this.forceUpdate();
        this.props.onChange(this.props.chartRequest!, this.props.userChart);
    }
    
    componentWillUnmount() {
        this.abortableQuery.abort();
    }

    abortableQuery = new AbortableRequest<ChartRequest, ChartClient.API.ExecuteChartResult>((abortController, request) => ChartClient.API.executeChart(request, abortController))

    handleOnDrawClick = () => {

        this.setState({ chartResult: undefined, lastChartRequest: undefined });

        var cr = this.props.chartRequest!;

        this.abortableQuery.getData(cr)
            .then(
            rt => {
                this.setState({ chartResult: rt, lastChartRequest: JSON.parse(JSON.stringify(this.props.chartRequest)) });
                this.props.onChange(cr, this.props.userChart);
            },
            ifError(ValidationError, e => {
                GraphExplorer.setModelState(this.props.chartRequest!, e.modelState, "request");
                this.forceUpdate();
            }))
            .done();
    }

    handleOnFullScreen = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        Navigator.history.push(ChartClient.Encoder.chartPath(this.props.chartRequest!));
    }

    handleEditScript = (e: React.MouseEvent<any>) => {
        window.open(Navigator.navigateRoute(this.props.chartRequest!.chartScript));
    }

    render() {
        const cr = this.props.chartRequest;
        const qd = this.state.queryDescription;
        const s = this.state;

        if (cr == undefined || qd == undefined)
            return null;

        const tc = new TypeContext<ChartRequest>(undefined, undefined, PropertyRoute.root(getTypeInfo(cr.Type)), new ReadonlyBinding(this.props.chartRequest!, ""));

        return (
            <div>
                <h2>
                    <span className="sf-entity-title">{getQueryNiceName(cr.queryKey) }</span>&nbsp;
                    <a className ="sf-popup-fullscreen" href="#" onClick={this.handleOnFullScreen}>
                        <span className="fa fa-external-link"></span>
                    </a>
                </h2 >
                <ValidationErrors entity={cr}/>
                <div className="sf-chart-control SF-control-container" >
                    <div>
                        <FilterBuilder filterOptions={cr.filterOptions} queryDescription={this.state.queryDescription!}
                            subTokensOptions={SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement}
                            lastToken={this.lastToken} onTokenChanged={t => this.lastToken = t} />

                    </div>
                    <div className="SF-control-container">
                        <ChartBuilder queryKey={cr.queryKey} ctx={tc}
                            onInvalidate={this.handleInvalidate}
                            onRedraw={this.handleOnRedraw}
                            onTokenChange={this.handleTokenChange}
                            />
                    </div >
                    <div className="sf-query-button-bar btn-toolbar">
                        <button type="submit" className="sf-query-button sf-chart-draw btn btn-primary" onClick={this.handleOnDrawClick}>{ChartMessage.DrawChart.niceToString()}</button>
                        <button className="sf-query-button sf-chart-script-edit btn btn-light" onClick={this.handleEditScript}><i className="fa fa-pencil" aria-hidden="true"/> &nbsp; {ChartMessage.EditScript.niceToString()}</button>
                        {ChartClient.ButtonBarChart.getButtonBarElements({ chartRequest: cr, chartRequestView: this }).map((a, i) => React.cloneElement(a, { key: i }))}
                        <button className="btn btn-light" onMouseUp={this.handleExplore} ><i className="fa fa-search"></i> &nbsp; {SearchMessage.Explore.niceToString()}</button>
                    </div>
                    <br />
                    <div className="sf-search-results-container" >
                        {!s.chartResult || !s.lastChartRequest ? JavascriptMessage.searchForResults.niceToString() :
                            <UncontrolledTabs id="chartResultTabs">
                                <Tab eventKey="chart" title={ChartMessage.Chart.niceToString()}>
                                    <ChartRenderer chartRequest={cr} lastChartRequest={s.lastChartRequest} data={s.chartResult.chartTable} />
                                </Tab>

                                <Tab eventKey="data" title={<span>{ChartMessage.Data.niceToString()} ({(s.chartResult.resultTable.rows.length)})</span> as any}>
                                    <ChartTable chartRequest={cr} lastChartRequest={s.lastChartRequest} resultTable={s.chartResult.resultTable} onRedraw={this.handleOnDrawClick} />
                                </Tab>
                            </UncontrolledTabs>
                        }
                    </div>
                </div>
            </div>
        );
    }


    handleExplore = (e: React.MouseEvent<any>) => {
        const cr = this.props.chartRequest!;

        var path = Finder.findOptionsPath({
            queryName: cr.queryKey,
            filterOptions: Finder.toFilterOptions(cr.filterOptions),
        });

        Navigator.pushOrOpenInTab(path, e);
    }
}







