import * as React from 'react'
import { Link } from 'react-router'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { CountSearchControl, SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, LocalizedType, TranslationFileStatus } from '../TranslationClient'
import { TranslationMessage } from '../Signum.Entities.Translation'

require("../Translation.css");

interface TranslationCodeStatusProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

export default class TranslationCodeStatus extends React.Component<TranslationCodeStatusProps, { result: TranslationFileStatus[] }> {

    constructor(props: TranslationCodeStatusProps) {
        super(props);
        this.state = { result: null };
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
        if (this.state.result == null)
            return <strong>{JavascriptMessage.loading.niceToString() }</strong>;

        var tree = this.state.result.groupBy(a => a.assembly)
            .toObject(gr => gr.key, gr => gr.elements.toObject(a => a.culture));

        var assemblies = Dic.getKeys(tree);
        var cultures = Dic.getKeys(tree[assemblies.first()]);


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
                                        <Link to={`~/translation/sync/${assembly}/${culture}`} className={"status-" + tree[assembly][culture].status}>
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



