import * as React from 'react'
import { Link, RouteComponentProps } from 'react-router-dom'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ValueSearchControl, SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, LocalizedType, TranslationFileStatus } from '../TranslationClient'
import { TranslationMessage } from '../Signum.Entities.Translation'

import "../Translation.css"

interface TranslationCodeStatusProps extends RouteComponentProps<{}> {

}

export default class TranslationCodeStatus extends React.Component<TranslationCodeStatusProps, { result?: TranslationFileStatus[] }> {

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
        return API.status()
            .then(result => this.setState({ result }));
    }

    render() {

        return (
            <div>
                <h2>{TranslationMessage.CodeTranslations.niceToString() }</h2>
                {this.renderTable() }
            </div>
        );
    }

    renderTable() {
        if (this.state.result == undefined)
            return <strong>{JavascriptMessage.loading.niceToString() }</strong>;

        const tree = this.state.result.groupBy(a => a.assembly)
            .toObject(gr => gr.key, gr => gr.elements.toObject(a => a.culture));

        const assemblies = Dic.getKeys(tree);
        const cultures = Dic.getKeys(tree[assemblies.first()]);


        return (
            <table className="st">
                <thead>
                    <tr>
                        <th></th>
                        <th> { TranslationMessage.All.niceToString() } </th>
                        {cultures.map(culture => <th key={culture}>{culture}</th>) }
                    </tr>
                </thead>
                <tbody>
                    {assemblies.map(assembly =>
                        <tr key={assembly}>
                            <th> {assembly}</th>
                            <td>
                                <Link to={`~/translation/view/${assembly}`}>{TranslationMessage.View.niceToString() }</Link>
                            </td>
                            {cultures.map(culture =>
                                <td key={culture}>
                                    <Link to={`~/translation/view/${assembly}/${culture}`}>{TranslationMessage.View.niceToString() }</Link>
                                    <br/>
                                    {
                                        !tree[assembly][culture].isDefault &&
                                        <Link to={`~/translation/syncNamespaces/${assembly}/${culture}`} className={"status-" + tree[assembly][culture].status}>
                                            {TranslationMessage.Sync.niceToString() }
                                        </Link>
                                    }
                                </td>
                            ) }
                        </tr>
                    ) }
                </tbody>
            </table>
        );
    }
}



