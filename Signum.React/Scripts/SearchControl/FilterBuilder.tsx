
import * as React from 'react'
import * as numbro from 'numbro'
import * as moment from 'moment'
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

interface FilterBuilderProps {
    filterOptions: FilterOptionParsed[];
    subTokensOptions: SubTokensOptions;
    queryDescription: QueryDescription;
    onTokenChanged?: (token: QueryToken | undefined) => void;
    lastToken?: QueryToken;
    onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
    onHeightChanged?: () => void;
    readOnly?: boolean;
    title?: React.ReactNode;
}

export default class FilterBuilder extends React.Component<FilterBuilderProps>{

    handlerNewFilter = (e: React.MouseEvent<any>) => {

        e.preventDefault();

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
            <fieldset className="form-xs">
                {this.props.title && <legend>{this.props.title}</legend>}
                <div className="sf-filters-list table-responsive" style={{ overflowX: "visible" }}>
                    <table className="table-sm">
                        <thead>
                            <tr>
                                <th style={{ minWidth: "24px" }}></th>
                                <th>{SearchMessage.Field.niceToString()}</th>
                                <th>{SearchMessage.Operation.niceToString()}</th>
                                <th style={{ paddingRight: "20px" }}>{SearchMessage.Value.niceToString()}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {this.props.filterOptions.map((f, i) => <FilterComponent filter={f} readOnly={Boolean(this.props.readOnly)} key={i}
                                onDeleteFilter={this.handlerDeleteFilter}
                                subTokenOptions={this.props.subTokensOptions}
                                queryDescription={this.props.queryDescription}
                                onTokenChanged={this.props.onTokenChanged}
                                onFilterChanged={this.handleFilterChanged}
                            />)}
                            {!this.props.readOnly &&
                                <tr >
                                    <td colSpan={4}>
                                        <a href="#" title={SearchMessage.AddFilter.niceToString()}
                                            className="sf-line-button sf-create"
                                            onClick={this.handlerNewFilter}>
                                            <span className="fa fa-plus sf-create" />&nbsp;{SearchMessage.AddFilter.niceToString()}
                                        </a>
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </fieldset>
        );
    }
}


export interface FilterComponentProps extends React.Props<FilterComponent> {
    filter: FilterOptionParsed;
    readOnly: boolean;
    onDeleteFilter: (fo: FilterOptionParsed) => void;
    queryDescription: QueryDescription;
    subTokenOptions: SubTokensOptions;
    onTokenChanged?: (token: QueryToken | undefined) => void;
    onFilterChanged: (filter: FilterOptionParsed) => void;

}

export class FilterComponent extends React.Component<FilterComponentProps>{

    handleDeleteFilter = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        this.props.onDeleteFilter(this.props.filter);
    }

    handleTokenChanged = (newToken: QueryToken | null | undefined) => {

        const f = this.props.filter;

        if (newToken == undefined) {
            f.operation = undefined;
            f.value = undefined;
        }
        else {

            if (!areEqual(f.token, newToken, a => a.filterType) || !areEqual(f.token, newToken, a => a.preferEquals)) {
                f.operation = newToken.preferEquals ? "EqualTo" : newToken.filterType && filterOperations[newToken.filterType].first();
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

        const formatted = moment(date).format(momentFormat);
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

        const readOnly = f.frozen || this.props.readOnly;

        return (
            <tr>
                <td>
                    {!readOnly &&
                        <a href="#" title={SearchMessage.DeleteFilter.niceToString()}
                            className="sf-line-button sf-remove"
                            onClick={this.handleDeleteFilter}>
                            <span className="fa fa-remove" />
                        </a>}
                </td>
                <td>
                    <div className="rw-widget-xs">
                        <QueryTokenBuilder
                            queryToken={f.token}
                            onTokenChange={this.handleTokenChanged}
                            queryKey={this.props.queryDescription.queryKey}
                            subTokenOptions={this.props.subTokenOptions}
                            readOnly={readOnly} />
                    </div>
                </td>
                <td className="sf-filter-operation">
                    {f.token && f.token.filterType && f.operation &&
                        <select className="form-control form-control-xs" value={f.operation as any} disabled={readOnly} onChange={this.handleChangeOperation}>
                            {f.token.filterType && filterOperations[f.token.filterType!]
                                .map((ft, i) => <option key={i} value={ft as any}>{FilterOperation.niceToString(ft)}</option>)}
                        </select>}
                </td>

                <td className="sf-filter-value">
                    {f.token && f.token.filterType && f.operation && this.renderValue()}
                </td>
            </tr>
        );
    }

    renderValue() {
        const f = this.props.filter;

        const readOnly = this.props.readOnly || f.frozen;

        if (isList(f.operation!))
            return <MultiValue values={f.value} onRenderItem={this.handleCreateAppropiateControl} readOnly={readOnly} onChange={this.handleValueChange} />;

        const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(f, a => a.value));

        return this.handleCreateAppropiateControl(ctx);
    }

    handleCreateAppropiateControl = (ctx: TypeContext<any>): React.ReactElement<any> => {

        const token = this.props.filter.token!;

        switch (token.filterType) {
            case "Lite":
                if (token.type.name == IsByAll || getTypeInfos(token.type).some(ti => !ti.isLowPopulation))
                    return <EntityLine ctx={ctx} type={token.type} create={false} onChange={this.handleValueChange} />;
                else
                    return <EntityCombo ctx={ctx} type={token.type} create={false} onChange={this.handleValueChange} />
            case "Embedded":
                return <EntityLine ctx={ctx} type={token.type} create={false} autoComplete={null} onChange={this.handleValueChange} />;
            case "Enum":
                const ti = getTypeInfos(token.type).single();
                if (!ti)
                    throw new Error(`EnumType ${token.type.name} not found`);
                const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} comboBoxItems={members} onChange={this.handleValueChange} />;
            default:
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} onChange={this.handleValueChange} />;
        }
    }

    handleValueChange = () => {
        this.props.onFilterChanged(this.props.filter);
    }
}


export interface MultiValueProps {
    values: any[],
    onRenderItem: (ctx: TypeContext<any>) => React.ReactElement<any>;
    readOnly: boolean;
    onChange: () => void;
}

export class MultiValue extends React.Component<MultiValueProps> {

    handleDeleteValue = (e: React.MouseEvent<any>, index: number) => {
        e.preventDefault();
        this.props.values.removeAt(index);
        this.props.onChange();
        this.forceUpdate();
    }

    handleAddValue = (e: React.MouseEvent<any>) => {

        e.preventDefault();

        this.props.values.push(undefined);
        this.props.onChange();
        this.forceUpdate();
    }

    render() {
        return (
            <table style={{ marginBottom: "0px" }} className="sf-multi-value">
                <tbody>
                    {
                        this.props.values.map((v, i) =>
                            <tr key={i}>
                                <td>
                                    {!this.props.readOnly &&
                                        <a href="#" title={SearchMessage.DeleteFilter.niceToString()}
                                            className="sf-line-button sf-remove"
                                            onClick={e => this.handleDeleteValue(e, i)}>
                                            <span className="fa fa-remove" />
                                        </a>}
                                </td>
                                <td>
                                    {
                                        this.props.onRenderItem(new TypeContext<any>(undefined,
                                            {
                                                formGroupStyle: "None",
                                                formSize: "ExtraSmall",
                                                readOnly: this.props.readOnly
                                            }, undefined as any, new Binding<any>(this.props.values, i)))
                                    }
                                </td>
                            </tr>)
                    }
                    <tr >
                        <td colSpan={4}>
                            {!this.props.readOnly &&
                                <a href="#" title={SearchMessage.AddValue.niceToString()}
                                    className="sf-line-button sf-create"
                                    onClick={this.handleAddValue}>
                                    <span className="fa fa-plus sf-create" />&nbsp;{SearchMessage.AddValue.niceToString()}
                            </a>}
                        </td>
                    </tr>
                </tbody>
            </table>
        );

    }
}



