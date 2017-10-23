
import * as React from 'react'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from '../Finder'
import { Dic, areEqual } from '../Globals'
import { openModal, IModalProps } from '../Modals';
import { FilterOptionParsed, QueryDescription, QueryToken, SubTokensOptions, filterOperations, FilterType, isList, FilterOperation } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { ValueLine, EntityLine, EntityCombo } from '../Lines'
import { Binding, IsByAll, getTypeInfos, toNumbroFormat, toMomentFormat } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'

import "./FilterBuilder.css"

interface FilterBuilderProps extends React.Props<FilterBuilder> {
    filterOptions: FilterOptionParsed[];
    subTokensOptions: SubTokensOptions;
    queryDescription: QueryDescription;
    onTokenChanged?: (token: QueryToken) => void;
    lastToken?: QueryToken;
    onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
    onHeightChanged?: () => void;
}

export default class FilterBuilder extends React.Component<FilterBuilderProps>{

    handlerNewFilter = () => {

        this.props.filterOptions.push({
            token: this.props.lastToken,
            operation: this.props.lastToken && (filterOperations[this.props.lastToken.filterType!] || []).firstOrNull() || undefined,
            value: undefined,
            frozen: false
        });


        if (this.props.onFiltersChanged)
            this.props.onFiltersChanged(this.props.filterOptions);

        this.forceUpdate(() => this.handleHeightChanged());
    };

    handlerDeleteFilter = (filter: FilterOptionParsed) => {
        this.props.filterOptions.remove(filter);
        if (this.props.onFiltersChanged)
            this.props.onFiltersChanged(this.props.filterOptions);
        this.forceUpdate(() => this.handleHeightChanged());
    };

    handleFilterChanged = (filter: FilterOptionParsed) => {
        if (this.props.onFiltersChanged)
            this.props.onFiltersChanged(this.props.filterOptions);
    };

    handleHeightChanged = () => {
        if (this.props.onHeightChanged)
            this.props.onHeightChanged();
    }

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
                                            <span className="glyphicon glyphicon-plus sf-create sf-create-label" />{SearchMessage.AddFilter.niceToString()}
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
    onTokenChanged?: (token: QueryToken | undefined) => void;
    onFilterChanged: (filter: FilterOptionParsed) => void;
}

export class FilterComponent extends React.Component<FilterComponentProps>{

    handleDeleteFilter = () => {
        this.props.onDeleteFilter(this.props.filter);
    }

    handleTokenChanged = (newToken: QueryToken | null | undefined) => {

        const f = this.props.filter;

        if (newToken == undefined) {
            f.operation = undefined;
            f.value = undefined;
        }
        else {

            if (!areEqual(f.token, newToken, a => a.filterType)) {
                f.operation = newToken.filterType && filterOperations[newToken.filterType].first();
                f.value = f.operation && isList(f.operation) ? [undefined] : undefined;
            }
            else if (f.token && f.token.filterType == "DateTime" && newToken.filterType == "DateTime" && newToken.format && f.token.format != newToken.format) {
                f.value = f.value && this.trimDateToFormat(f.value, toMomentFormat(newToken.format));
            }
        }
        f.token = newToken || undefined;

        if (this.props.onTokenChanged)
            this.props.onTokenChanged(newToken || undefined);

        this.props.onFilterChanged(this.props.filter);

        this.forceUpdate();
    }

    trimDateToFormat(date: string, momentFormat: string | undefined) {

        if (!momentFormat)
            return date;

        const formatted = moment(date).format( momentFormat);
        return moment(formatted, momentFormat).format();
    }
    

    handleChangeOperation = (event: React.FormEvent<HTMLSelectElement>) => {
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
                        queryToken={f.token}
                        onTokenChange={this.handleTokenChanged}
                        queryKey={ this.props.queryDescription.queryKey }
                        subTokenOptions={this.props.subTokenOptions}
                        readOnly={!!f.frozen}/></td>
                <td className="sf-filter-operation">
                    {f.token && f.token.filterType && f.operation &&
                        <select className="form-control" value={f.operation as any} disabled={f.frozen} onChange={this.handleChangeOperation}>
                        {f.token.filterType && filterOperations[f.token.filterType!]
                                .map((ft, i) => <option key={i} value={ft as any}>{ FilterOperation.niceName(ft) }</option>) }
                        </select> }
                </td>

                <td className="sf-filter-value">
                    {f.token && f.token.filterType && f.operation && this.renderValue() }
                </td>
            </tr>
        );
    }

    renderValue() {
        const f = this.props.filter;

        if (isList(f.operation!))
            return <MultiValue values={f.value} onRenderItem={this.handleCreateAppropiateControl} frozen={!!this.props.filter.frozen} onChange={this.handleValueChange}/>;

        const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: f.frozen, formGroupSize: "ExtraSmall" }, undefined as any, Binding.create(f, a => a.value));

        return this.handleCreateAppropiateControl(ctx);
    }

    handleCreateAppropiateControl = (ctx: TypeContext<any>): React.ReactElement<any> => {

        const token = this.props.filter.token!;

        switch (token.filterType) {
            case "Lite":
                if (token.type.name == IsByAll || getTypeInfos(token.type).some(ti => !ti.isLowPopulation))
                    return <EntityLine ctx={ctx} type={token.type} create={false} onChange={this.handleValueChange} />;
                else
                    return <EntityCombo ctx={ctx} type={token.type} create={false} onChange={this.handleValueChange}/>
            case "Embedded":
                return <EntityLine ctx={ctx} type={token.type} create={false} autoComplete={null} onChange={this.handleValueChange}/>;
            case "Enum":
                const ti = getTypeInfos(token.type).single();
                if (!ti)
                    throw new Error(`EnumType ${token.type.name} not found`);
                const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
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
    onRenderItem: (ctx: TypeContext<any>) => React.ReactElement<any>;
    frozen: boolean;
    onChange: () => void;
}

export class MultiValue extends React.Component<MultiValueProps> {

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
                                        this.props.onRenderItem(new TypeContext<any>(undefined,
                                        {
                                            formGroupStyle: "None",
                                            formGroupSize: "ExtraSmall",
                                            readOnly: this.props.frozen
                                        }, undefined as any, new Binding<any>(this.props.values, i)))
                                    }
                                </td>
                            </tr>)
                    }
                    <tr >
                        <td colSpan={4}>
                            {!this.props.frozen &&
                                <a title={SearchMessage.AddValue.niceToString()}
                                    className="sf-line-button sf-create"
                                    onClick={this.handleAddValue}>
                                    <span className="glyphicon glyphicon-plus sf-create sf-create-label" />{SearchMessage.AddValue.niceToString()}
                                </a>}
                        </td>
                    </tr>
                </tbody>
            </table>

        );

    }

}



