
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from '../Finder'
import { Dic, areEqual } from '../Globals'
import { openModal, IModalProps } from '../Modals';
import { FilterOptionParsed, QueryDescription, QueryToken, SubTokensOptions, filterOperations, FilterType, isList, FilterOperation } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { ValueLine, EntityLine, EntityCombo } from '../Lines'
import { Binding, IsByAll, getTypeInfos } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'

require("!style!css!./FilterBuilder.css");

interface FilterBuilderProps extends React.Props<FilterBuilder> {
    filterOptions: FilterOptionParsed[];
    subTokensOptions: SubTokensOptions;
    queryDescription: QueryDescription;
    onTokenChanged: (token: QueryToken) => void;
    lastToken?: QueryToken;
    onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
}

export default class FilterBuilder extends React.Component<FilterBuilderProps, {}>  {

    handlerNewFilter = () => {

        this.props.filterOptions.push({
            token: this.props.lastToken,
            operation: !this.props.lastToken ? undefined : (filterOperations[this.props.lastToken.filterType] || []).firstOrNull(),
            value: undefined,
            frozen: false
        });


        if (this.props.onFiltersChanged)
            this.props.onFiltersChanged(this.props.filterOptions);

        this.forceUpdate();
    };

    handlerDeleteFilter = (filter: FilterOptionParsed) => {
        this.props.filterOptions.remove(filter);
        if (this.props.onFiltersChanged)
            this.props.onFiltersChanged(this.props.filterOptions);
        this.forceUpdate();
    };

    handleFilterChanged = (filter: FilterOptionParsed) => {
        if (this.props.onFiltersChanged)
            this.props.onFiltersChanged(this.props.filterOptions);
    };

    render() {


        return (
            <div className="panel panel-default sf-filters form-xs">
                <div className="panel-body sf-filters-list table-responsive" style={{ overflowX: "visible" }}>
                    {
                        <table className="table table-condensed sf-filter-table">
                            <thead>
                                <tr>
                                    <th style={{ minWidth: "24px" }}></th>
                                    <th className="sf-filter-field-header">{ SearchMessage.Field.niceToString() }</th>
                                    <th>{ SearchMessage.Operation.niceToString() }</th>
                                    <th>{ SearchMessage.Value.niceToString() }</th>
                                </tr>
                            </thead>
                            <tbody>
                                {this.props.filterOptions.map((f, i) => <FilterComponent filter={f} key={i}
                                    onDeleteFilter={this.handlerDeleteFilter}
                                    subTokenOptions={this.props.subTokensOptions}
                                    queryDescription={this.props.queryDescription}
                                    onTokenChanged ={this.props.onTokenChanged}
                                    onFilterChanged={this.handleFilterChanged}
                                     />) }
                                <tr >
                                    <td colSpan={4}>
                                        <a title={SearchMessage.AddFilter.niceToString() }
                                            className="sf-line-button sf-create"
                                            onClick={this.handlerNewFilter}>
                                            <span className="glyphicon glyphicon-plus"/><span style={{ marginLeft: "5px", marginRight: "5px" }}> {SearchMessage.AddFilter.niceToString() } </span>
                                        </a>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    }
                </div>

            </div>
        );
    }
}


export interface FilterComponentProps extends React.Props<FilterComponent> {
    filter: FilterOptionParsed;
    onDeleteFilter: (fo: FilterOptionParsed) => void;
    queryDescription: QueryDescription;
    subTokenOptions: SubTokensOptions;
    onTokenChanged: (token: QueryToken) => void;
    onFilterChanged: (filter: FilterOptionParsed) => void;
}

export class FilterComponent extends React.Component<FilterComponentProps, {}>{

    handleDeleteFilter = () => {
        this.props.onDeleteFilter(this.props.filter);
    }

    handleTokenChanged = (newToken: QueryToken) => {

        const f = this.props.filter;

        if (newToken == undefined) {
            f.operation = undefined;
            f.value = undefined;
        }
        else {

            if (!areEqual(f.token, newToken, a => a.filterType)) {
                const operations = filterOperations[newToken.filterType];
                f.operation = operations && operations.firstOrNull();
                f.value = isList(f.operation) ? [undefined] : undefined;
            }
        }
        f.token = newToken;

        this.props.onTokenChanged(newToken);

        this.props.onFilterChanged(this.props.filter);

        this.forceUpdate();
    }
    

    handleChangeOperation = (event: React.FormEvent) => {
        const operation = (event.currentTarget as HTMLSelectElement).value as any;
        if (isList(operation) != isList(this.props.filter.operation!))
            this.props.filter.value = isList(operation) ? [this.props.filter.value] : this.props.filter.value[0];
        
        this.props.filter.operation = operation;

        this.props.onFilterChanged(this.props.filter);

        this.forceUpdate();
    }

    render() {
        const f = this.props.filter;

        return (
            <tr>
                <td>
                    {!f.frozen &&
                        <a title={SearchMessage.DeleteFilter.niceToString() }
                            className="sf-line-button sf-remove"
                            onClick={this.handleDeleteFilter}>
                            <span className="glyphicon glyphicon-remove"/>
                        </a>}
                </td>
                <td>
                    <QueryTokenBuilder
                        queryToken={f.token!}
                        onTokenChange={this.handleTokenChanged}
                        queryKey={ this.props.queryDescription.queryKey }
                        subTokenOptions={this.props.subTokenOptions}
                        readOnly={!!f.frozen}/></td>
                <td className="sf-filter-operation">
                    {f.token && f.operation &&
                        <select className="form-control" value={f.operation as any} disabled={f.frozen} onChange={this.handleChangeOperation}>
                            { filterOperations[f.token.filterType]
                                .map((ft, i) => <option key={i} value={ft as any}>{ FilterOperation.niceName(ft) }</option>) }
                        </select> }
                </td>

                <td className="sf-filter-value">
                    {f.token && f.operation && this.renderValue() }
                </td>
            </tr>
        );
    }

    renderValue() {
        const f = this.props.filter;

        if (isList(f.operation!))
            return <MultiValue values={f.value} createAppropiateControl={this.handleCreateAppropiateControl} frozen={!!this.props.filter.frozen} onChange={this.handleValueChange}/>;

        const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: f.frozen }, undefined as any, Binding.create(f, a => a.value));

        return this.handleCreateAppropiateControl(ctx);
    }

    handleCreateAppropiateControl = (ctx: TypeContext<any>): React.ReactElement<any> => {

        const token = this.props.filter.token!;

        switch (token.filterType) {
            case "Lite":
                if (token.type.name == IsByAll || getTypeInfos(token.type).some(ti => !ti.isLowPopupation))
                    return <EntityLine ctx={ctx} type={token.type} create={false} onChange={this.handleValueChange} />;
                else
                    return <EntityCombo ctx={ctx} type={token.type} create={false} onChange={this.handleValueChange}/>
            case "Embedded":
                return <EntityLine ctx={ctx} type={token.type} create={false} autoComplete={false} onChange={this.handleValueChange}/>;
            case "Enum":
                const ti = getTypeInfos(token.type).single();
                if (!ti)
                    throw new Error(`EnumType ${token.type.name} not found`);
                const members = Dic.getValues(ti.members).filter(a => !a.isIgnored);
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} comboBoxItems={members} onChange={this.handleValueChange}/>;
            default:
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} onChange={this.handleValueChange}/>;
        }
    }

    handleValueChange = () => {
        this.props.onFilterChanged(this.props.filter);
    }
}


export interface MultiValueProps {
    values: any[],
    createAppropiateControl: (ctx: TypeContext<any>) => React.ReactElement<any>;
    frozen: boolean;
    onChange: () => void;
}

export class MultiValue extends React.Component<MultiValueProps, void> {

    handleDeleteValue = (index: number) => {

        this.props.values.removeAt(index);
        this.props.onChange();
        this.forceUpdate();

    }

    handleAddValue = () => {
        this.props.values.push(undefined);
        this.props.onChange();
        this.forceUpdate();
    }

    render() {
        return (
            <table style={{ marginBottom: "0px" }}>
                <tbody>
                    {
                        this.props.values.map((v, i) =>
                            <tr key={i}>
                                <td>
                                    {!this.props.frozen &&
                                        <a title={SearchMessage.DeleteFilter.niceToString() }
                                            className="sf-line-button sf-remove"
                                            onClick={() => this.handleDeleteValue(i) }>
                                            <span className="glyphicon glyphicon-remove"/>
                                        </a>}
                                </td>
                                <td>
                                    {
                                        this.props.createAppropiateControl(new TypeContext<any>(undefined,
                                        {
                                            formGroupStyle: "None",
                                            readOnly: this.props.frozen
                                        }, undefined as any, new Binding<any>(this.props.values, i)))
                                    }
                                </td>
                            </tr>)
                    }
                    <tr >
                        <td colSpan={4}>
                            <a title={SearchMessage.AddValue.niceToString() }
                                className="sf-line-button sf-create"
                                onClick={this.handleAddValue}>
                                <span className="glyphicon glyphicon-plus" style={{ marginRight: "5px" }}/>{SearchMessage.AddValue.niceToString() }
                            </a>
                        </td>
                    </tr>
                </tbody>
            </table>

        );

    }

}



