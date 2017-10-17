import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { notifySuccess } from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { ValueSearchControl, SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, Lite, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as CultureClient from '../CultureClient'
import { API, AssemblyResult, LocalizedType, LocalizableType } from '../TranslationClient'
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics'
import { TranslationMessage } from '../Signum.Entities.Translation'
import { TranslationTypeTable } from './TranslationTypeTable'
import { Link } from "react-router-dom";

import "../Translation.css"

interface TranslationCodeSyncProps extends RouteComponentProps<{ culture: string; assembly: string; namespace?: string; }> {

}

export default class TranslationCodeSync extends React.Component<TranslationCodeSyncProps, { result?: AssemblyResult; cultures?: { [name: string]: Lite<CultureInfoEntity> } }> {

    constructor(props: TranslationCodeSyncProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        CultureClient.getCultures(true).then(cultures => this.setState({ cultures })).done();

        this.loadSync().done();
    }

    loadSync() {
        const { assembly, culture, namespace } = this.props.match.params;
        return API.sync(assembly, culture, namespace).then(result => this.setState({ result }))
    }

    render() {

        const { assembly, culture, namespace } = this.props.match.params;


        if (this.state.result && this.state.result.totalTypes == 0) {
            return (
                <div>
                    <h2>{TranslationMessage._0AlreadySynchronized.niceToString(this.props.match.params.assembly)}</h2>
                    <Link to={`~/translation/status`}>
                        {TranslationMessage.BackToTranslationStatus.niceToString()}
                    </Link>
                </div>
            );
        }

        let message = TranslationMessage.Synchronize0In1.niceToString(namespace || assembly,
            this.state.cultures ? this.state.cultures[culture].toStr : culture);

        if (this.state.result) {
            message += ` [${Dic.getKeys(this.state.result.types).length}/${this.state.result.totalTypes}]`;
        }

        return (
            <div>
                <h2>{message}</h2>
                <br />
                {this.renderTable()}
            </div>
        );
    }



    handleSearch = (filter: string) => {
        const { assembly, culture } = this.props.match.params;

        return API.retrieve(assembly, culture || "", filter)
            .then(result => this.setState({ result: result }))
            .done();
    }

    renderTable() {

        if (this.state.result == undefined)
            return undefined;


        if (Dic.getKeys(this.state.result).length == 0)
            return <strong> {TranslationMessage.NoResultsFound.niceToString()}</strong>;

        return (
            <div>
                {Dic.getValues(this.state.result.types).map(type => <TranslationTypeTable key={type.type} type={type} result={this.state.result!} currentCulture={this.props.match.params.culture} />)}
                <input type="submit" value={TranslationMessage.Save.niceToString()} className="btn btn-primary" onClick={this.handleSave} />
            </div>
        );
    }

    handleSave = (e: React.FormEvent<any>) => {
        e.preventDefault();
        const params = this.props.match.params;
        API.save(params.assembly, params.culture || "", this.state.result!)
            .then(() => notifySuccess())
            .then(() => this.loadSync())
            .done();
    }
}
