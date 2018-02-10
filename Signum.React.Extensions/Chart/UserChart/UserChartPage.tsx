import * as React from 'react'
import { Dic, classes, ifError } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, FormGroupStyle, StyleOptions, StyleContext, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SearchMessage, JavascriptMessage, parseLite, is, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PropertyRoute, getQueryNiceName, getTypeInfo, Binding, GraphExplorer }  from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ChartColumnEmbedded, ChartScriptColumnEmbedded, ChartScriptParameterEmbedded, ChartRequest, GroupByChart, ChartMessage,
    ChartColorEntity, ChartScriptEntity, ChartParameterEmbedded, ChartParameterType, UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import * as UserChartClient from './UserChartClient'
import ChartRequestView from '../Templates/ChartRequestView'
import { RouteComponentProps } from "react-router";


interface UserChartPageProps extends RouteComponentProps<{ userChartId: string; entity?: string }> {

}


export default class UserChartPage extends React.Component<UserChartPageProps> {

    constructor(props: UserChartPageProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.load(this.props);
    }

    componentWillReceiveProps(nextProps: UserChartPageProps) {
        this.state = {};
        this.forceUpdate();
        this.load(nextProps);
    }

    load(props: UserChartPageProps) {

        const { userChartId, entity } = this.props.match.params;

        const lite = entity == undefined ? undefined : parseLite(entity);

        Navigator.API.fillToStrings(lite)
            .then(() => Navigator.API.fetchEntity(UserChartEntity, userChartId))
            .then(uc => UserChartClient.Converter.toChartRequest(uc, lite)
                .then(cr => Navigator.history.replace(ChartClient.Encoder.chartPath(cr, toLite(uc)))))
            .done();
    }

    render() {
        return <span>{JavascriptMessage.loading.niceToString()}</span>;
    }
}


