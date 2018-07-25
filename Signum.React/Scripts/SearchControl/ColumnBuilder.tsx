
import * as React from 'react'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Finder from '../Finder'
import { Dic, areEqual } from '../Globals'
import { openModal, IModalProps } from '../Modals';
import { ColumnOptionParsed, QueryDescription, QueryToken, SubTokensOptions } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { ValueLine, EntityLine, EntityCombo } from '../Lines'
import { Binding, IsByAll, getTypeInfos, toNumbroFormat, toMomentFormat } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'

import "./ColumnBuilder.css"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export interface ColumnsBuilderProps {
    queryDescription: QueryDescription;
    columnOptions: ColumnOptionParsed[];
    subTokensOptions: SubTokensOptions;
    onColumnsChanged?: (columns: ColumnOptionParsed[]) => void;
    title?: React.ReactNode;
    readonly?: boolean;
}

export default class ColumnsBuilder extends React.Component<ColumnsBuilderProps> {

    handlerNewColumn = () => {

        this.props.columnOptions.push({
            token: undefined,
            displayName: undefined,
        });

        if (this.props.onColumnsChanged)
            this.props.onColumnsChanged(this.props.columnOptions);

        this.forceUpdate();
    };

    handlerDeleteColumn = (column: ColumnOptionParsed) => {
        this.props.columnOptions.remove(column);
        if (this.props.onColumnsChanged)
            this.props.onColumnsChanged(this.props.columnOptions);

        this.forceUpdate();
    };

    handleColumnChanged = (column: ColumnOptionParsed) => {
        if (this.props.onColumnsChanged)
            this.props.onColumnsChanged(this.props.columnOptions);

        this.forceUpdate();
    };

    render() {


        return (
            <fieldset className="form-xs">
                {this.props.title && <legend>{this.props.title}</legend>}
                <div className="sf-columns-list table-responsive" style={{ overflowX: "visible" }}>
                    <table className="table table-condensed">
                        <thead>
                            <tr>
                                <th style={{ minWidth: "24px" }}></th>
                                <th>{SearchMessage.Field.niceToString()}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {this.props.columnOptions.map((c, i) => <ColumnComponent column={c} key={i} readonly={Boolean(this.props.readonly)}
                                onDeleteColumn={this.handlerDeleteColumn}
                                subTokenOptions={this.props.subTokensOptions}
                                queryDescription={this.props.queryDescription}
                                onColumnChanged={this.handleColumnChanged}
                            />)}
                            {!this.props.readonly &&
                                <tr>
                                    <td colSpan={4}>
                                        <a title={SearchMessage.AddColumn.niceToString()}
                                            className="sf-line-button sf-create"
                                            onClick={this.handlerNewColumn}>
                                            <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{SearchMessage.AddColumn.niceToString()}
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



export interface ColumnComponentProps {
    column: ColumnOptionParsed;
    onDeleteColumn: (fo: ColumnOptionParsed) => void;
    queryDescription: QueryDescription;
    subTokenOptions: SubTokensOptions;
    onTokenChanged?: (token: QueryToken | undefined) => void;
    onColumnChanged: (column: ColumnOptionParsed) => void;
    readonly: boolean;
}

export class ColumnComponent extends React.Component<ColumnComponentProps>{

    handleDeleteColumn = () => {
        this.props.onDeleteColumn(this.props.column);
    }

    handleTokenChanged = (newToken: QueryToken | null | undefined) => {

        const c = this.props.column;
        c.displayName = undefined;
        c.token = newToken || undefined;

        if (this.props.onTokenChanged)
            this.props.onTokenChanged(newToken || undefined);

        this.props.onColumnChanged(this.props.column);

        this.forceUpdate();
    }


    render() {
        const c = this.props.column;
        const readonly = this.props.readonly;
        return (
            <tr>
                <td>
                    {!readonly &&
                        <a title={JavascriptMessage.removeColumn.niceToString()}
                            className="sf-line-button sf-remove"
                        onClick={this.handleDeleteColumn}>
                        <FontAwesomeIcon icon="times" />
                        </a>}
                </td>
                <td>
                    <QueryTokenBuilder
                        queryToken={c.token}
                        onTokenChange={this.handleTokenChanged}
                        queryKey={this.props.queryDescription.queryKey}
                        subTokenOptions={this.props.subTokenOptions}
                        readOnly={readonly} />
                </td>
            </tr>
        );
    }
}

