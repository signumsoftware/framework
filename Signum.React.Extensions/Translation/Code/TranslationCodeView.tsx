import * as React from 'react'
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
import { API, AssemblyResult, LocalizedType, LocalizableType, LocalizedMember } from '../TranslationClient'
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics'
import { TranslationMessage } from '../Signum.Entities.Translation'
import { RouteComponentProps } from "react-router";

import "../Translation.css"

interface TranslationCodeViewProps extends RouteComponentProps<{ culture: string; assembly: string }> {

}

export default class TranslationCodeView extends React.Component<TranslationCodeViewProps, { result?: AssemblyResult; cultures?: { [name: string]: Lite<CultureInfoEntity> } }> {

    constructor(props: TranslationCodeViewProps) {
        super(props);
        this.state = { };
    }

    componentWillMount() {
        CultureClient.getCultures().then(cultures => this.setState({ cultures })).done();
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
                <div className="input-group-btn">
                    <button className="btn btn-default" type="submit" title={ TranslationMessage.Search.niceToString() }>
                        <i className="glyphicon glyphicon-search"></i>
                    </button>
                </div>
            </form>
        );
    }
}

export class TranslationTypeTable extends React.Component<{ type: LocalizableType, result: AssemblyResult, currentCulture: string }>{
    render() {

        let {type, result} = this.props;

        return (
            <table style={{ width: "100%", margin: "10px 0" }} className="st" key={type.type}>
                <thead>
                    <tr>
                        <th className="leftCell"> {TranslationMessage.Type.niceToString() } </th>
                        <th colSpan={4} className="titleCell">
                            {type.type} ({
                                [
                                    type.hasDescription ? "Sigular" : undefined,
                                    type.hasPluralDescription ? "Plural" : undefined,
                                    type.hasGender ? "Gender" : undefined,
                                    type.hasMembers ? "Members" : undefined
                                ].filter(a => !!a).join("/") })
                        </th>
                    </tr>
                </thead>
                <tbody>
                    {type.hasDescription && Dic.getValues(type.cultures).filter(loc => !!loc.typeDescription)
                        .map(loc => <TranslationTypeDescription key={loc.culture} edit={this.editCulture(loc)} loc={loc} result={this.props.result} type={type} />)}
                    {type.hasMembers && this.renderMembers(type) }
                </tbody>
            </table>
        );
    }

    renderMembers(type: LocalizableType): React.ReactElement<any>[] {

        const members = Dic.getKeys(Dic.getValues(type.cultures).first().members);

        return members.flatMap(me =>
            [<tr key={me}>
                <th className="leftCell">
                    {TranslationMessage.Member.niceToString() }
                </th>
                <th colSpan={4}>
                    {me}
                </th>
            </tr>]
                .concat(Dic.getValues(type.cultures).map(loc =>
                    <TranslationMember key={me + "-" + loc.culture} type={type} loc={loc} edit={this.editCulture(loc) } member={loc.members[me]}/>
                ))
        );

    }

    editCulture(loc: LocalizedType) {
        return this.props.currentCulture == undefined || this.props.currentCulture == loc.culture;
    }
}

export class TranslationMember extends React.Component<{ type: LocalizableType, loc: LocalizedType; member: LocalizedMember; edit: boolean }, { avoidCombo?: boolean }>{
    

    constructor(props: any) {
        super(props);
        this.state = {};
    }

    render() {
        const {member, loc, edit} = this.props;

        return (
            <tr >
                <td className="leftCell">{loc.culture}</td>
                <td colSpan={4} className="monospaceCell">
                    { edit ? this.renderEdit() : member.description }
                </td>
            </tr>
        );
    }

    handleOnChange = (e: React.FormEvent<any>) => {
        this.props.member.description = (e.currentTarget as HTMLSelectElement).value;
        this.forceUpdate();
    }

    handleAvoidCombo = (e: React.FormEvent<any>) => {
        e.preventDefault();
        this.setState({ avoidCombo: true });
    }
    handleKeyDown = (e: React.KeyboardEvent<any>) => {
        if (e.keyCode == 32 || e.keyCode == 113) { //SPACE OR F2
            e.preventDefault();
            this.setState({ avoidCombo: true });
        }
    }

    renderEdit() {
        const { member } = this.props;

        const translatedMembers = Dic.getValues(this.props.type.cultures).map(lt => ({ culture: lt.culture, member: lt.members[member.name] })).filter(a => !!a.member.translatedDescription);
        if (!translatedMembers.length || this.state.avoidCombo)
            return (<textarea style={{ height: "24px", width: "90%" }} value={member.description || ""} onChange={this.handleOnChange} ref={(ta) => ta && ta.focus()}/>);

        return (
            <span>
                <select value={member.description || ""} onChange={this.handleOnChange} onKeyDown={this.handleKeyDown}>
                    { initialElementIf(member.description == undefined).concat(
                        translatedMembers.map(a => <option key={a.culture} value={a.member.translatedDescription}>{a.member.translatedDescription}</option>)) }
                </select>
                &nbsp;
                <a href="#" onClick={this.handleAvoidCombo}>{TranslationMessage.Edit.niceToString() }</a>
            </span>
        );
    }
}


function initialElementIf(condition: boolean) {
    return condition ? [<option key={""} value={""}>{" - "}</option>] : []
}

                

export class TranslationTypeDescription extends React.Component<{ type: LocalizableType, loc: LocalizedType, edit: boolean, result: AssemblyResult }, { avoidCombo?: boolean }>{

    constructor(props: any) {
        super(props);
        this.state = {};
    }

    render() {

        const { type, loc, edit } = this.props;

        const td = loc.typeDescription!;

        const pronoms = this.props.result.cultures[loc.culture].pronoms || [];

        return (
            <tr>
                <th className="leftCell">{ loc.culture }</th>
                <th className="smallCell monospaceCell">
                    {type.hasGender && (edit ?
                        <select value={td.gender || ""} onChange={(e) => { td.gender = e.currentTarget.value; this.forceUpdate(); }}>
                            { initialElementIf(td.gender == undefined).concat(
                                pronoms.map(a => <option key={a.Gender} value={a.Gender}>{a.Singular}</option>)) }
                        </select> :
                        (pronoms.filter(a => a.Gender == td.gender).map(a => a.Singular).singleOrNull()))
                    }
                </th>
                <th className="monospaceCell">
                    { edit ? this.renderEdit() : td.description
                    }
                </th>
                <th className="smallCell">
                    { type.hasPluralDescription && type.hasGender &&
                        pronoms.filter(a => a.Gender == td.gender).map(a => a.Plural).singleOrNull()
                    }
                </th>
                <th className="monospaceCell">
                    {
                        type.hasPluralDescription && (edit ?
                            <textarea style={{ height: "24px", width: "90%" }} value={td.pluralDescription || ""} onChange={e => { td.pluralDescription = e.currentTarget.value; this.forceUpdate(); } } /> :
                            td.pluralDescription)
                    }
                </th>
            </tr>
        );
    }

    handleOnChange = (e: React.ChangeEvent<HTMLTextAreaElement | HTMLSelectElement>) => {
        const { loc } = this.props;
        const td = loc.typeDescription!;
        td.description = e.currentTarget.value;

        API.pluralize(loc.culture, td.description).then(plural => {
            td.pluralDescription = plural;
            this.forceUpdate();
        }).done();

        API.gender(loc.culture, td.description).then(gender => {
            td.gender = gender;
            this.forceUpdate();
        }).done();

        this.forceUpdate();
    }

    handleAvoidCombo = (e: React.FormEvent<any>) => {
        e.preventDefault();
        this.setState({ avoidCombo: true });
    }
    handleKeyDown = (e: React.KeyboardEvent<any>) => {
        if (e.keyCode == 32 || e.keyCode == 113) { //SPACE OR F2
            e.preventDefault();
            this.setState({ avoidCombo: true });
        }
    }

    renderEdit() {
        const { loc } = this.props;
        const td = loc.typeDescription!;

        const translatedTypes = Dic.getValues(this.props.type.cultures).filter(a => !!a.typeDescription!.translatedDescription);
        if (!translatedTypes.length || this.state.avoidCombo)
            return (<textarea style={{ height: "24px", width: "90%" }} value={td.description || ""} onChange={this.handleOnChange} ref={(ta) => ta && ta.focus()} />);

        return (
            <span>
                <select value={td.description || ""} onChange={this.handleOnChange} onKeyDown={this.handleKeyDown}>
                    {  initialElementIf(td.description == undefined).concat(
                        translatedTypes.map(a => <option key={a.culture} value={a.typeDescription!.translatedDescription}>{a.typeDescription!.translatedDescription}</option>)) }
                </select>
                &nbsp;
                <a href="#" onClick={this.handleAvoidCombo}>{TranslationMessage.Edit.niceToString() }</a>
            </span>
        );
    }
}



