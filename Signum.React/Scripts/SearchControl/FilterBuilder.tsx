
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from '../Finder'
import { Dic, areEqual } from '../Globals'
import { openModal, IModalProps } from '../Modals';
import { FilterOperation, FilterOption, QueryDescription, QueryToken, SubTokensOptions, filterOperations, FilterType } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity, DynamicQuery } from '../Signum.Entities'
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

export default class FilterBuilder extends React.Component<FilterBuilderProps, { }>  {

    handlerNewFilter = () => {
        this.props.filterOptions.push({
            token: this.props.lastToken,
            columnName: null,
            operation: null,
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
                    { <table className="table table-condensed">
                        <thead>
                            <tr>
                                <th></th>
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
                                        <span className="glyphicon glyphicon-plus"/> {SearchMessage.AddFilter.niceToString() }
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

        var f = this.props.filter;
        
        if (newToken == null) {
            f.operation = null;
            f.value = null;
        }
        else {

            if (!areEqual(f.token, newToken, a=> a.filterType)) {
                var operations = filterOperations[newToken.filterType];
                f.operation = operations && operations.firstOrNull();
                f.value = null;
            }
        }
        f.token = newToken;

        this.props.tokenChanged(newToken);

        this.forceUpdate();
    }


    handleChangeOperation = (event: React.FormEvent) => {
        this.props.filter.operation = (event.currentTarget as HTMLSelectElement).value as any;
        this.forceUpdate();
    }

    render() {
        var f = this.props.filter;

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
                                .map((ft, i) => <option key={i} value={ft as any}>{ DynamicQuery.FilterOperation_Type.niceName(ft) }</option>) }
                        </select> }
                </td>

                <td>
                    {f.token && f.operation && this.renderValue() }
                </td>
            </tr>
        );
    }

    renderValue() {
        var f = this.props.filter;

        var ctx = new TypeContext<any>(null, { formGroupStyle: FormGroupStyle.None, readOnly: f.frozen }, null, new Binding<any>("value", f));

        switch (f.token.filterType) {
            case FilterType.Lite:
                if (f.token.type.name == IsByAll || getTypeInfos(f.token.type).some(ti=> !ti.isLowPopupation))
                    return <EntityLine ctx={ctx} type={f.token.type} create={false} />;
                else
                    return <EntityCombo ctx={ctx} type={f.token.type} create={false}/>
            case FilterType.Embedded:
                return <EntityLine ctx={ctx} type={f.token.type} create={false} autoComplete={false} />;
            case FilterType.Enum:
                var ti = getTypeInfos(f.token.type).single();
                if (!ti)
                    throw new Error(`EnumType ${f.token.type.name} not found`);
                var members = Dic.getValues(ti.members).filter(a=> !a.isIgnored);
                return <ValueLine ctx={ctx} type={f.token.type} formatText={f.token.format} unitText={f.token.unit} comboBoxItems={members}/>;
            default:
                return <ValueLine ctx={ctx} type={f.token.type} formatText={f.token.format} unitText={f.token.unit}/>;
        }
    }
}



