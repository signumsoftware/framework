import * as React from 'react'
import { Link, RouteComponentProps } from 'react-router-dom'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ValueSearchControl, SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, LocalizedType, NamespaceSyncStats } from '../TranslationClient'
import { TranslationMessage } from '../Signum.Entities.Translation'

import "../Translation.css"

interface TranslationCodeStatusProps extends RouteComponentProps<{ culture: string; assembly: string; }> {

}

export default class TranslationCodeSyncNamespaces extends React.Component<TranslationCodeStatusProps, { result?: NamespaceSyncStats[] }> {

    constructor(props: TranslationCodeStatusProps) {
        super(props);
        this.state = { result: undefined };
    }

    componentWillMount() {
        this.loadState().done();
    }

    componentWillReceiveProps() {
        this.loadState().done();
    }

    loadState() {
        var p = this.props.match.params;
        return API.namespaceStatus(p.assembly, p.culture)
            .then(result => this.setState({ result }));
    }

    render() {

        if (this.state.result && this.state.result.length == 0) {
            return (
                <div>
                    <h2>{TranslationMessage._0AlreadySynchronized.niceToString(this.props.match.params.assembly)}</h2>
                    <Link to={`~/translation/status`}>
                        {TranslationMessage.BackToTranslationStatus.niceToString()}
                    </Link>
                </div>
            );
        }

        var p = this.props.match.params;

        return (
            <div>
                <h2>{TranslationMessage.Synchronize0In1.niceToString(p.assembly, p.culture)}</h2>
                {this.renderTable() }
            </div>
        );
    }

    renderTable() {
        if (this.state.result == undefined)
            return <strong>{JavascriptMessage.loading.niceToString()}</strong>;

        

        var p = this.props.match.params;
        
        return (
            <table className="st">
                <thead>
                    <tr>
                        <th> {TranslationMessage.Namespace.niceToString()} </th>
                        <th> {TranslationMessage.NewTypes.niceToString()} </th>
                        <th> {TranslationMessage.NewTranslations.niceToString()} </th>
                    </tr>
                </thead>
                <tbody>
                    <tr key={"All"}>
                        <th>
                            <Link to={`~/translation/sync/${p.assembly}/${p.culture}`}>
                                {TranslationMessage.All.niceToString()}
                            </Link>
                        </th>
                        <th> {this.state.result.sum(a => a.types)}</th>
                        <th> {this.state.result.sum(a => a.translations)}</th>
                    </tr>

                    {this.state.result.map(stats =>
                        <tr key={stats.namespace}>
                            <td>
                                <Link to={`~/translation/sync/${p.assembly}/${p.culture}/${stats.namespace}`}>
                                    {stats.namespace}
                                </Link>
                            </td>
                            <th> {stats.types}</th>
                            <th> {stats.translations}</th>
                        </tr>
                    ) }
                </tbody>
            </table>
        );
    }
}



