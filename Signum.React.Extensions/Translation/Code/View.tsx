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
import { ModifiableEntity, EntityControlMessage, Entity, Lite, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as CultureClient from '../CultureClient'
import { API, AssemblyResult, LocalizedType, LocalizableType } from '../TranslationClient'
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics'
import { TranslationMessage } from '../Signum.Entities.Translation'

require("../Translation.css");

interface TranslationCodeViewProps extends ReactRouter.RouteComponentProps<{}, { culture: string; assembly: string }> {

}

export default class TranslationCodeView extends React.Component<TranslationCodeViewProps, { result?: AssemblyResult; filter?: string; cultures?: { [name: string]: Lite<CultureInfoEntity> } }> {

    constructor(props) {
        super(props);
        this.state = { filter: "" };
    }

    componentWillMount() {
        CultureClient.getCultures().then(cultures => this.setState({ cultures }));
    }

    render() {

        var {routeParams } = this.props;

        var message = TranslationMessage.View0In1.niceToString(routeParams.assembly,
            routeParams.culture == null ? TranslationMessage.AllLanguages.niceToString() :
                this.state.cultures ? this.state.cultures[routeParams.culture].toStr :
                    routeParams.culture);

        return (
            <div>
                <h2>{message}</h2>
                { this.renderSearch() }
                <em> {TranslationMessage.PressSearchForResults.niceToString() }</em>
                <br/>
                { this.renderTable() }
            </div>
        );
    }

    handeFilterChange = (e: React.FormEvent) => {
        var filter = (e.currentTarget as HTMLInputElement).value;
        this.setState({ filter });
    }

    handleSearch = (e: React.FormEvent) => {

        e.preventDefault();

        var params = this.props.routeParams;

        return API.retrieve(params.assembly, params.culture || "", this.state.filter)
            .then(result => this.setState({ result: result }))
            .done();
    }

    renderSearch() {

        return (
            <form onSubmit={this.handleSearch} className="input-group">
                <input type="text" className="form-control"
                    placeholder={ TranslationMessage.Search.niceToString() }  value={ this.state.filter} onChange={this.handeFilterChange}/>
                <div className="input-group-btn">
                    <button className="btn btn-default" type="submit" title={ TranslationMessage.Search.niceToString() }>
                        <i className="glyphicon glyphicon-search"></i>
                    </button>
                </div>
            </form>
        );
    }

    renderTable() {

        if (this.state.result == null)
            return null;


        if (Dic.getKeys(this.state.result).length == 0)
            return <strong> {TranslationMessage.NoResultsFound.niceToString() }</strong>;

        return (
            <div>
                { Dic.getValues(this.state.result.types).map(type => <TranslationTypeTable type={type} result={this.state.result} culture={this.props.routeParams.culture} />) }
                <input type="submit" value={ TranslationMessage.Save.niceToString() } className="btn btn-primary" onClick={this.handleSave}/>
            </div>
        );
    }

    handleSave = (e: React.FormEvent) => {
        e.preventDefault();
        var params = this.props.routeParams;
        API.save(params.assembly, params.culture || "", this.state.result).done();
    }
}

export class TranslationTypeTable extends React.Component<{ type: LocalizableType, result?: AssemblyResult, culture: string }, void>{
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
                                    type.hasDescription ? "Sigular" : null,
                                    type.hasPluralDescription ? "Plural" : null,
                                    type.hasGender ? "Gender" : null,
                                    type.hasMembers ? "Members" : null
                                ].filter(a => !!a).join("/") })
                        </th>
                    </tr>
                </thead>
                <tbody>
                    {!type.hasDescription ? [] : Dic.getValues(type.cultures).map(loc =>
                        <TranslationTypeDescription key={loc.culture } edit={this.editCulture(loc) } loc={loc} result={this.props.result} type={type} />) }
                    {this.renderMembers(type) }
                </tbody>
            </table>
        );
    }

    renderMembers(type: LocalizableType): React.ReactElement<any>[] {
        if (!type.hasMembers)
            return [];

        var members = Dic.getKeys(Dic.getValues(type.cultures).first().members);

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
                    <TranslationMember key={me + "-" + loc.culture} loc={loc} edit={this.editCulture(loc)} member={me}/>
                ))
        );

    }

    editCulture(loc: LocalizedType) {
        return this.props.culture == null || this.props.culture == loc.culture;
    }
}

export class TranslationMember extends React.Component<{ loc: LocalizedType; member: string; edit: boolean }, void>{

    render() {

        var {member, loc, edit} = this.props;

        return (
            <tr >
                <td className="leftCell">{loc.culture}</td>
                <td colSpan={4} className="monospaceCell">
                    { edit ?
                        <textarea style={{ height: "24px", width: "90%" }} value={loc.members[member] || ""}
                            onChange={(e) => { loc.members[member] = (e.currentTarget as HTMLSelectElement).value; this.forceUpdate(); } } /> :
                        loc.members[member]
                    }
                </td>
            </tr>    
        );
    }
}

export class TranslationTypeDescription extends React.Component<{ type: LocalizableType, loc: LocalizedType, edit: boolean, result?: AssemblyResult }, void>{

    handleDescriptionChange = (e: React.FormEvent) => {
        var loc = this.props.loc;
        loc.description = (e.currentTarget as HTMLSelectElement).value;

        API.pluralize(loc.culture, loc.description).then(plural => {
            loc.pluralDescription = plural;
            this.forceUpdate();
        }).done();

        API.gender(loc.culture, loc.description).then(gender => {
            loc.gender = gender;
            this.forceUpdate();
        }).done();

        this.forceUpdate();
    }

    render() {

        var {type, loc, edit } = this.props;

        var pronoms = this.props.result.cultures[loc.culture].pronoms || [];

        return (
            <tr>
                <th className="leftCell">{ loc.culture }</th>
                <th className="smallCell monospaceCell">
                    {type.hasGender && (edit ?
                        <select value={loc.gender || ""} onChange={(e) => loc.gender = (e.currentTarget as HTMLSelectElement).value }>
                            { pronoms.concat(loc.gender == null ? [{ Gender: "", Singular: " - ", Plural: " - " }] : []).map(a => <option key={a.Gender}>{a.Singular}</option>) }
                        </select> :
                        (pronoms.filter(a => a.Gender == loc.gender).map(a => a.Singular).singleOrNull()))
                    }
                </th>
                <th className="monospaceCell">
                    { edit ? <textarea style={{ height: "24px", width: "90%" }} value={loc.description || ""} onChange={this.handleDescriptionChange} /> :
                        loc.description
                    }
                </th>
                <th className="smallCell">
                    { type.hasPluralDescription && type.hasGender &&
                        pronoms.filter(a => a.Gender == loc.gender).map(a => a.Plural).singleOrNull()
                    }
                </th>
                <th className="monospaceCell">
                    {
                        type.hasPluralDescription && (edit ?
                            <textarea style={{ height: "24px", width: "90%" }} value={loc.pluralDescription || ""} onChange={(e) => { loc.pluralDescription = (e.currentTarget as HTMLSelectElement).value; this.forceUpdate(); } } /> :
                            loc.pluralDescription)
                    }
                </th>
            </tr>
        );
    }
}



