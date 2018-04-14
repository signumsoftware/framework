import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { notifySuccess } from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { ValueSearchControl, SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, Lite, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as CultureClient from '../CultureClient'
import { API, AssemblyResult, LocalizedType, LocalizableType, LocalizedMember } from '../TranslationClient'
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics'
import { TranslationMessage } from '../Signum.Entities.Translation'
import { RouteComponentProps } from "react-router";
import { TranslationTypeTable } from './TranslationTypeTable'

import "../Translation.css"

interface TranslationCodeViewProps extends RouteComponentProps<{ culture: string; assembly: string }> {

}

export default class TranslationCodeView extends React.Component<TranslationCodeViewProps, { result?: AssemblyResult; cultures?: { [name: string]: Lite<CultureInfoEntity> } }> {

    constructor(props: TranslationCodeViewProps) {
        super(props);
        this.state = { };
    }

    componentWillMount() {
        CultureClient.getCultures(true).then(cultures => this.setState({ cultures })).done();
    }

    render() {

        const {assembly, culture } = this.props.match.params;

        const message = TranslationMessage.View0In1.niceToString(assembly,
            culture == undefined ? TranslationMessage.AllLanguages.niceToString() :
                this.state.cultures ? this.state.cultures[culture].toStr :
                    culture);

        return (
            <div>
                <h2>{message}</h2>
                <TranslateSearchBox search={this.handleSearch} />
                <em> {TranslationMessage.PressSearchForResults.niceToString() }</em>
                <br/>
                { this.renderTable() }
            </div>
        );
    }

    handleSearch = (filter: string) => {
        const {assembly, culture} = this.props.match.params;

        return API.retrieve(assembly, culture || "", filter)
            .then(result => this.setState({ result: result }))
            .done();
    }

    renderTable() {

        if (this.state.result == undefined)
            return undefined;


        const result = this.state.result;

        if (Dic.getKeys(result).length == 0)
            return <strong> {TranslationMessage.NoResultsFound.niceToString() }</strong>;
        
        return (
            <div>
                { Dic.getValues(this.state.result.types).map(type => <TranslationTypeTable key={type.type} type={type} result={result} currentCulture={this.props.match.params.culture} />) }
                <input type="submit" value={ TranslationMessage.Save.niceToString() } className="btn btn-primary" onClick={this.handleSave}/>
            </div>
        );
    }

    handleSave = (e: React.FormEvent<any>) => {
        e.preventDefault();
        const params = this.props.match.params;
        API.save(params.assembly, params.culture || "", this.state.result!).then(() => notifySuccess()).done();
    }
}

export class TranslateSearchBox extends React.Component<{ search: (newValue: string) => void }, { filter: string }>
{
    state = { filter: "" };

    handleChange = (e: React.FormEvent<any>) => {
        e.preventDefault();
        this.setState({ filter: (e.currentTarget as HTMLInputElement).value });
    }

    handleSearch = (e: React.FormEvent<any>) => {
        e.preventDefault();
        this.props.search(this.state.filter);
    }

    render() {

        return (
            <form onSubmit={this.handleSearch} className="input-group">
                <input type="text" className="form-control"
                    placeholder={ TranslationMessage.Search.niceToString() }  value={ this.state.filter} onChange={this.handleChange}/>
                <div className="input-group-append">
                    <button className="btn btn-light" type="submit" title={ TranslationMessage.Search.niceToString() }>
                        <i className="fa fa-search"></i>
                    </button>
                </div>
            </form>
        );
    }
}
