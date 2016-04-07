import * as React from 'react'
import { DropdownButton, MenuItem, Tabs, Tab} from 'react-bootstrap'
import { Dic, classes, ifError } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ValidationError } from '../../../../Framework/Signum.React/Scripts/Services'
import { Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, FormGroupSize, FormGroupStyle, StyleOptions, StyleContext, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SearchMessage, JavascriptMessage, parseLite, is, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PropertyRoute, getQueryNiceName, getTypeInfo, Binding, GraphExplorer }  from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import FilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import { ValueLine, FormGroup, ValueLineProps, ValueLineType } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ChartColumnEntity, ChartScriptColumnEntity, ChartScriptParameterEntity, ChartRequest, GroupByChart, ChartMessage,
    ChartColorEntity, ChartScriptEntity, ChartParameterEntity, ChartParameterType } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { ChartColumn, ChartColumnInfo }from './ChartColumn'
import ChartBuilder from './ChartBuilder'
import ChartTable from './ChartTable'
import ChartRenderer from './ChartRenderer'


require("!style!css!../Chart.css");

interface ChartRequestViewProps extends ReactRouter.RouteComponentProps<{}, { queryName: string }> {

}


interface ChartRequestViewState {
    chartRequest?: ChartRequest;
    queryDescription?: QueryDescription;
    chartResult?: ChartClient.API.ExecuteChartResult;
}

export default class ChartRequestView extends React.Component<ChartRequestViewProps, ChartRequestViewState> {

    lastToken: QueryToken;

    constructor(props) {
        super(props);
        this.state = { chartRequest: null };
   
    }

    componentWillMount() {
        this.load(this.props);
    }

    componentWillReceiveProps(nextProps: ChartRequestViewProps) {
        this.setState({ chartRequest: null });
        this.load(nextProps);
    }

    load(props: ChartRequestViewProps) {

        Finder.getQueryDescription(props.routeParams.queryName).then(qd => {
            this.setState({ queryDescription: qd });
        }).done();

        ChartClient.Decoder.parseChartRequest(props.routeParams.queryName, props.location.query).then(cr => {
            this.setState({ chartRequest: cr });
        }).done();
    }

    handleOnInvalidate = () => {
        this.setState({ chartResult: null });
    }

    handleOnRedraw = () => {
        this.forceUpdate();
    }

    handleOnDrawClick = () => {

        this.setState({ chartResult: null });

        ChartClient.API.executeChart(this.state.chartRequest)
            .then(rt => this.setState({ chartResult: rt }),
            ifError(ValidationError, e => {
                GraphExplorer.setModelState(this.state.chartRequest, e.modelState, "request");
                this.forceUpdate();
            }))
            .done();
    }

    handleOnFullScreen = (e: React.MouseEvent) => {
        e.preventDefault();
        Navigator.currentHistory.push(ChartClient.Encoder.chartRequestPath(this.state.chartRequest));
    }

    handleEditScript = (e: React.MouseEvent) => {
        window.open(Navigator.navigateRoute(this.state.chartRequest.chartScript));
    }

    render() {

        const cr = this.state.chartRequest;
        const qd = this.state.queryDescription;

        if (cr == null || qd == null)
            return null;

        var tc = new TypeContext<ChartRequest>(null, null, PropertyRoute.root(getTypeInfo(cr.Type)), new Binding<ChartRequest>("chartRequest", this.state));

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
                        <FilterBuilder filterOptions={cr.filterOptions} queryDescription={this.state.queryDescription}
                            subTokensOptions={SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement}
                            lastToken={this.lastToken} tokenChanged={t => this.lastToken = t} />

                    </div>
                    <div className="SF-control-container">
                        <ChartBuilder queryKey={cr.queryKey} ctx={tc} onInvalidate={this.handleOnInvalidate} onRedraw={this.handleOnRedraw}/>
                    </div >
                    <div className="sf-query-button-bar btn-toolbar">
                        <button type="submit" className="sf-query-button sf-chart-draw btn btn-primary" onClick={this.handleOnDrawClick}>{ ChartMessage.Chart_Draw.niceToString() }</button>
                        <button className="sf-query-button sf-chart-script-edit btn btn-default" onClick={this.handleEditScript}>{ ChartMessage.EditScript.niceToString() }</button>
                        { ChartClient.ButtonBarChart.getButtonBarElements({ chartRequest: cr, chartRequestView: this }).map((a, i) => React.cloneElement(a, { key: i })) }
                    </div>
                    <br />
                    <div className="sf-search-results-container" >
                        {!this.state.chartResult ? JavascriptMessage.searchForResults.niceToString() :

                            <Tabs>
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




