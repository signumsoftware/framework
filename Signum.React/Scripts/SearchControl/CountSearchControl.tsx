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
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'

export type CountSearchControlLayout = "View" | "Link" | "Badge" | "Span";

export interface CountSearchControlProps extends React.Props<CountSearchControl> {
    ctx: StyleContext;
    findOptions: FindOptions;
    labelText?: React.ReactChild;
    labelProps?: React.HTMLAttributes;
    layout?: CountSearchControlLayout;
    formGroupHtmlProps?: React.HTMLAttributes;
    avoidAutoRefresh? : boolean;
}

export interface CountSearchControlState {
    count?: number;
}

export default class CountSearchControl extends React.Component<CountSearchControlProps, CountSearchControlState> {

    constructor(props: CountSearchControlProps) {
        super(props);
        this.state = {
            count: undefined,
        };
    }

    getQueryRequest(fo: FindOptionsParsed): CountQueryRequest {

        return {
            queryKey: fo.queryKey,
            filters: fo.filterOptions.map(fo => ({ token: fo.token!.fullKey, operation: fo.operation!, value: fo.value })),
        };
    }

    componentDidMount() {
        this.refreshCount();
    }

    refreshCount() {
        var fo = this.props.findOptions;

        if (!Finder.isFindable(fo.queryName))
            return;

        Finder.getQueryDescription(fo.queryName)
            .then(qd => Finder.parseFindOptions(fo, qd))
            .then(fo => Finder.API.queryCount(this.getQueryRequest(fo)))
            .then(count => this.setState({ count }))
            .done();
    }

    render() {
        if (!Finder.isFindable(this.props.findOptions.queryName))
            return null;

        if (this.props.layout == "Badge")
            return this.renderBadget();

        if (this.props.layout == "Span")
            return this.renderSpan();

        return (
            <FormGroup ctx={this.props.ctx}
                labelText={this.props.labelText || getQueryNiceName(this.props.findOptions.queryName)}
                labelProps={this.props.labelProps}
                htmlProps={this.props.formGroupHtmlProps}>
                {this.props.layout == "Link" ? this.renderAsLink() : this.renderAsView() }
            </FormGroup>
        );
    }

    handleClick = (e: React.MouseEvent) => {
        if (e.ctrlKey || e.button == 2)
            window.open(Finder.findOptionsPath(this.props.findOptions));
        else
            Finder.explore(this.props.findOptions).then(() => {
                if (!this.props.avoidAutoRefresh)
                    this.refreshCount();
            }).done();
    }

    renderAsView() {
        return (
            <div>
                <span className={this.state.count > 0 ? "count-search count-with-results badge" : "count-search count-no-results"}>
                    {this.state.count == undefined ? "…" : this.state.count}
                </span>
                {this.state.count != undefined &&
                    <a className="sf-line-button sf-view" onClick={this.handleClick}>
                        <span className={"glyphicon glyphicon-arrow-right"}>
                        </span>
                    </a>
                }
            </div>
        );
    }

    renderAsLink() {
        return (<a onClick={this.handleClick}>
            {this.state.count == undefined ? "…" : this.state.count}
        </a>);
    }


    renderBadget() {
        return <a className={this.state.count > 0 ? "count-search count-with-results badge" : "count-search count-no-results"}  onClick={this.handleClick}>
            {this.state.count == undefined ? "…" : this.state.count}
        </a>;
    }

    renderSpan() {
        return <span>
            {this.state.count == undefined ? "…" : this.state.count}
        </span>;
    }
}