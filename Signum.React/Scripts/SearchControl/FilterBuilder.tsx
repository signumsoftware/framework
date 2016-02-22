
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from '../Finder'
import { Dic, areEqual } from '../Globals'
import { openModal, IModalProps } from '../Modals';
import { FilterOperation, FilterOption, QueryDescription, QueryToken, SubTokensOptions, filterOperations, FilterType, isList } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { FilterOperation_Type } from '../Signum.Entities.DynamicQuery' 
import { ValueLine, EntityLine, EntityCombo } from '../Lines'
import { Binding, IsByAll, getTypeInfos } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'


interface FilterBuilderProps extends React.Props<FilterBuilder> {
    filterOptions: FilterOption[];
    subTokensOptions: SubTokensOptions;
    queryDescription: QueryDescription;
    tokenChanged: (token: QueryToken) => void;
    lastToken: QueryToken;
}

export default class FilterBuilder extends React.Component<FilterBuilderProps, {}>  {

    handlerNewFilter = () => {

        this.props.filterOptions.push({
            token: this.props.lastToken,
            columnName: null,
            operation: !this.props.lastToken ? null : (filterOperations[this.props.lastToken.filterType] || []).firstOrNull(),
            value: null,
        });
        this.forceUpdate();
    };

    handlerDeleteFilter = (filter: FilterOption) => {
        this.props.filterOptions.remove(filter);
        this.forceUpdate();
    };

    render() {


        return (
            <div className="panel panel-default sf-filters form-xs">
                <div className="panel-body sf-filters-list table-responsive" style={{ overflowX: "visible" }}>
                    {
                        <table className="table table-condensed">
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
                                    tokenChanged ={this.props.tokenChanged} />) }
                                <tr >
                                    <td colSpan={4}>
                                        <a title={SearchMessage.AddFilter.niceToString() }
                                            className="sf-line-button sf-create"
                                            onClick={this.handlerNewFilter}>
                                            <span className="glyphicon glyphicon-plus" style={{ marginRight: "5px" }}/>{SearchMessage.AddFilter.niceToString() }
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
    filter: FilterOption;
    onDeleteFilter: (fo: FilterOption) => void;
    queryDescription: QueryDescription;
    subTokenOptions: SubTokensOptions;
    tokenChanged: (token: QueryToken) => void;
}

export class FilterComponent extends React.Component<FilterComponentProps, {}>{

    handleDeleteFilter = () => {
        this.props.onDeleteFilter(this.props.filter);
    }

    handleTokenChanged = (newToken: QueryToken) => {

        const f = this.props.filter;

        if (newToken == null) {
            f.operation = null;
            f.value = null;
        }
        else {

            if (!areEqual(f.token, newToken, a => a.filterType)) {
                const operations = filterOperations[newToken.filterType];
                f.operation = operations && operations.firstOrNull();
                f.value = isList(f.operation) ? [null] : null;
            }
        }
        f.token = newToken;

        this.props.tokenChanged(newToken);

        this.forceUpdate();
    }



    handleChangeOperation = (event: React.FormEvent) => {
        var operation = (event.currentTarget as HTMLSelectElement).value as any;
        if (isList(operation) != isList(this.props.filter.operation))
            this.props.filter.value = isList(operation) ? [this.props.filter.value] : this.props.filter.value[0];
        
        this.props.filter.operation = operation;

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
                        readOnly={f.frozen}/></td>
                <td>
                    {f.token && f.operation &&
                        <select className="form-control" value={f.operation as any} disabled={f.frozen} onChange={this.handleChangeOperation}>
                            { filterOperations[f.token.filterType]
                                .map((ft, i) => <option key={i} value={ft as any}>{ FilterOperation_Type.niceName(ft) }</option>) }
                        </select> }
                </td>

                <td>
                    {f.token && f.operation && this.renderValue() }
                </td>
            </tr>
        );
    }

    renderValue() {
        const f = this.props.filter;

        if (isList(f.operation))
            return <MultiValue values={f.value} createAppropiateControl={this.handleCreateAppropiateControl} frozen={this.props.filter.frozen}/>;

        const ctx = new TypeContext<any>(null, { formGroupStyle: FormGroupStyle.None, readOnly: f.frozen }, null, new Binding<any>("value", f));

        return this.handleCreateAppropiateControl(ctx);
    }

    handleCreateAppropiateControl = (ctx: TypeContext<any>): React.ReactElement<any> => {

        var token = this.props.filter.token;

        switch (token.filterType) {
            case FilterType.Lite:
                if (token.type.name == IsByAll || getTypeInfos(token.type).some(ti => !ti.isLowPopupation))
                    return <EntityLine ctx={ctx} type={token.type} create={false} />;
                else
                    return <EntityCombo ctx={ctx} type={token.type} create={false}/>
            case FilterType.Embedded:
                return <EntityLine ctx={ctx} type={token.type} create={false} autoComplete={false} />;
            case FilterType.Enum:
                const ti = getTypeInfos(token.type).single();
                if (!ti)
                    throw new Error(`EnumType ${token.type.name} not found`);
                const members = Dic.getValues(ti.members).filter(a => !a.isIgnored);
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit} comboBoxItems={members}/>;
            default:
                return <ValueLine ctx={ctx} type={token.type} formatText={token.format} unitText={token.unit}/>;
        }
    }
}


export interface MultiValueProps {
    values: any[],
    createAppropiateControl: (ctx: TypeContext<any>) => React.ReactElement<any>;
    frozen: boolean;
}

export class MultiValue extends React.Component<MultiValueProps, void> {

    handleDeleteValue = (index: number) => {

        this.props.values.removeAt(index);
        this.forceUpdate();

    }

    handleAddValue = () => {
        this.props.values.push(null);
        this.forceUpdate();
    }

    render() {
        return (
            <table className="table table-condensed" style={{ marginBottom: "0px" }}>
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
                                    {this.props.createAppropiateControl(new TypeContext<any>(null,
                                        {
                                            formGroupStyle: FormGroupStyle.None,
                                            readOnly: this.props.frozen
                                        }, null, new Binding<any>(i, this.props.values))) }
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



