import * as React from 'react'
import { DropdownButton, MenuItem, Tabs, Tab} from 'react-bootstrap'
import { Dic, classes, ifError } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ValidationError } from '../../../../Framework/Signum.React/Scripts/Services'
import { Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, FormGroupSize, FormGroupStyle, StyleOptions, StyleContext, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SearchMessage, JavascriptMessage, parseLite, is, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PropertyRoute, getQueryNiceName, getTypeInfo, ReadonlyBinding, GraphExplorer }  from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import FilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import { ValueLine, FormGroup, ValueLineProps, ValueLineType } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ChartColumnEntity, ChartScriptColumnEntity, ChartScriptParameterEntity, ChartRequest, GroupByChart, ChartMessage,
    ChartColorEntity, ChartScriptEntity, ChartParameterEntity, ChartParameterType, UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { ChartColumn, ChartColumnInfo }from './ChartColumn'
import ChartBuilder from './ChartBuilder'
import ChartTable from './ChartTable'
import ChartRenderer from './ChartRenderer'


require("../Chart.css");
require("../../../../Framework/Signum.React/Scripts/SearchControl/Search.css");


interface ChartRequestViewProps {
    chartRequest?: ChartRequest;
    userChart?: UserChartEntity;
    onChange: (newChartRequest: ChartRequest) => void;
    title?: string;
}

interface ChartRequestViewState {
    queryDescription?: QueryDescription;
    chartResult?: ChartClient.API.ExecuteChartResult;
}

export default class ChartRequestView extends React.Component<ChartRequestViewProps, ChartRequestViewState> {

    lastToken: QueryToken;

    constructor(props: ChartRequestViewProps) {
        super(props);
        this.state = { };
   
    }

    componentWillMount() {
        this.loadQueryDescription(this.props);
    }

    componentWillReceiveProps(nextProps: ChartRequestViewProps) {
        this.state = {};
        this.forceUpdate();
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
        this.setState({ chartResult: undefined });
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
    }

    handleOnDrawClick = () => {

        this.setState({ chartResult: undefined });

        ChartClient.API.executeChart(this.props.chartRequest!)
            .then(rt => this.setState({ chartResult: rt }),
            ifError(ValidationError, e => {
                GraphExplorer.setModelState(this.props.chartRequest!, e.modelState, "request");
                this.forceUpdate();
            }))
            .done();
    }

    handleOnFullScreen = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        Navigator.currentHistory.push(ChartClient.Encoder.chartRequestPath(this.props.chartRequest!));
    }

    handleEditScript = (e: React.MouseEvent<any>) => {
        window.open(Navigator.navigateRoute(this.props.chartRequest!.chartScript));
    }

    render() {

        const cr = this.props.chartRequest;
        const qd = this.state.queryDescription;

        if (cr == undefined || qd == undefined)
            return null;

        const tc = new TypeContext<ChartRequest>(undefined, undefined, PropertyRoute.root(getTypeInfo(cr.Type)), new ReadonlyBinding(this.props.chartRequest!, ""));

        return (
            <div>
                <h2>
                    <span className="sf-entity-title">{getQueryNiceName(cr.queryKey) }</span>&nbsp;
                    <a className ="sf-popup-fullscreen" href="#" onClick={this.handleOnFullScreen}>
                        <span className="glyphicon glyphicon-new-window"></span>
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
                        <button type="submit" className="sf-query-button sf-chart-draw btn btn-primary" onClick={this.handleOnDrawClick}>{ ChartMessage.Chart_Draw.niceToString() }</button>
                        <button className="sf-query-button sf-chart-script-edit btn btn-default" onClick={this.handleEditScript}>{ ChartMessage.EditScript.niceToString() }</button>
                        { ChartClient.ButtonBarChart.getButtonBarElements({ chartRequest: cr, chartRequestView: this }).map((a, i) => React.cloneElement(a, { key: i })) }
                    </div>
                    <br />
                    <div className="sf-search-results-container" >
                        {!this.state.chartResult ? JavascriptMessage.searchForResults.niceToString() :

                            <Tabs id="chartResultTabs" animation={false}>
                                <Tab eventKey="chart" title={ChartMessage.Chart.niceToString() }>
                                    <ChartRenderer  chartRequest={cr} data={this.state.chartResult.chartTable}/>
                                </Tab>

                                <Tab eventKey="data" title={ChartMessage.Data.niceToString() }>
                                    <ChartTable chartRequest={cr} resultTable={this.state.chartResult.resultTable} onRedraw={this.handleOnDrawClick} />
                                </Tab>
                            </Tabs>
                        }
                    </div>
                </div>
            </div>
        );
    }

}







