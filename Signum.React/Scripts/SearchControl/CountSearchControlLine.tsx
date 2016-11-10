import * as React from 'react'
import { DropdownButton, MenuItem, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { Dic, DomUtils } from '../Globals'
import * as Finder from '../Finder'
import {
    ResultTable, ResultRow, FindOptions, FindOptionsParsed, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken, CountQueryRequest, QueryRequest } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, is } from '../Signum.Entities'
import { getTypeInfos, IsByAll, getQueryKey, TypeInfo, EntityData, getQueryNiceName} from '../Reflection'
import * as Navigator from '../Navigator'
import { StyleContext } from '../Typecontext'
import CountSearchControl from './CountSearchControl'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'

export interface CountSearchControlLineProps extends React.Props<CountSearchControlLine> {
    ctx: StyleContext;
    findOptions: FindOptions;
    labelText?: React.ReactChild;
    labelProps?: React.HTMLAttributes;
    formGroupHtmlProps?: React.HTMLAttributes;
    isLink?: boolean;
    isBadge?: boolean | "MoreThanZero";
    viewButton?: boolean;
    avoidAutoRefresh?: boolean;
    extraButtons?: (countSearchControl: CountSearchControl) => React.ReactNode
}

export default class CountSearchControlLine extends React.Component<CountSearchControlLineProps, void> {

    static defaultProps = {
        isLink: false,
        isBadge: "MoreThanZero",
        viewButton: true,
    }; 

    countSearchControl?: CountSearchControl;

    handleCountSearchControlLoaded = (csc: CountSearchControl | undefined) => {

        if (csc != this.countSearchControl)
            this.forceUpdate();

        this.countSearchControl = csc;
    }

    render() {
        if (!Finder.isFindable(this.props.findOptions.queryName))
            return null;
        
        return (
            <FormGroup ctx={this.props.ctx}
                labelText={this.props.labelText || getQueryNiceName(this.props.findOptions.queryName)}
                labelProps={this.props.labelProps}
                htmlProps={this.props.formGroupHtmlProps}>
                <div>
                    <CountSearchControl
                        ref={this.handleCountSearchControlLoaded}
                        findOptions={this.props.findOptions}
                        isBadge={this.props.isBadge}
                        isLink={this.props.isLink}
                        onCountChange={() => this.forceUpdate()}/>
                    {this.countSearchControl && this.countSearchControl.state.count != undefined &&
                        <a className="sf-line-button sf-view" onClick={this.countSearchControl.handleClick}> <span className={"glyphicon glyphicon-arrow-right"}> </span></a>
                    }
                    {this.countSearchControl && this.props.extraButtons && this.props.extraButtons(this.countSearchControl)}
                </div>
            </FormGroup>
        );
    }
}