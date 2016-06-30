import * as React from 'react'
import { DropdownButton, MenuItem, Tabs, Tab} from 'react-bootstrap'
import { Dic, classes, ifError } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, FormGroupSize, FormGroupStyle, StyleOptions, StyleContext, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SearchMessage, JavascriptMessage, parseLite, is, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PropertyRoute, getQueryNiceName, getTypeInfo, Binding, GraphExplorer }  from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ChartColumnEntity, ChartScriptColumnEntity, ChartScriptParameterEntity, ChartRequest, GroupByChart, ChartMessage,
    ChartColorEntity, ChartScriptEntity, ChartParameterEntity, ChartParameterType } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import ChartRequestView from './ChartRequestView'

interface ChartRequestPageProps extends ReactRouter.RouteComponentProps<{}, { queryName: string }> {

}

export default class ChartRequestPage extends React.Component<ChartRequestPageProps, { chartRequest?: ChartRequest }> {
    
    constructor(props) {
        super(props);
        this.state = {};
    }
    
    componentWillMount() {
        this.load(this.props);
    }

    componentWillReceiveProps(nextProps: ChartRequestPageProps) {
        this.state = {};
        this.forceUpdate();
        this.load(nextProps);
    }

    load(props: ChartRequestPageProps) {
        ChartClient.Decoder.parseChartRequest(props.routeParams.queryName, props.location.query).then(cr => {
            this.setState({ chartRequest: cr });
        }).done();
    }


    render() {
        return <ChartRequestView
            chartRequest={this.state.chartRequest}
            onChange={cr => this.setState({ chartRequest: cr }) }/>;
    }
}


