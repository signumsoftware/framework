import * as React from 'react'
import { DropdownButton, MenuItem, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { Dic, DomUtils, classes } from '../Globals'
import * as Finder from '../Finder'
import {
    ResultTable, ResultRow, FindOptions, FindOptionsParsed, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken, CountQueryRequest, QueryRequest
} from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, is } from '../Signum.Entities'
import { getTypeInfos, IsByAll, getQueryKey, TypeInfo, EntityData, getQueryNiceName } from '../Reflection'
import * as Navigator from '../Navigator'
import { StyleContext } from '../Typecontext'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks } from '../Lines/LineBase'

export interface CountSearchControlProps extends React.Props<CountSearchControl> {
    findOptions: FindOptions;
    isLink?: boolean;
    isBadge?: boolean | "MoreThanZero";
    avoidAutoRefresh?: boolean;
    onCountChange?: (count: number) => void;
   
}

export interface CountSearchControlState {
    count?: number;
}

export default class CountSearchControl extends React.Component<CountSearchControlProps, CountSearchControlState> {

    static defaultProps = {
        isLink: false,
        isBadge: "MoreThanZero"
    }

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
        this.refreshCount(this.props);
    }

    componentWillReceiveProps(newProps: CountSearchControlProps) {
        if (Finder.findOptionsPath(this.props.findOptions) == Finder.findOptionsPath(newProps.findOptions))
            return;

        this.refreshCount(newProps);
    }

    refreshCount(props: CountSearchControlProps) {
        var fo = props.findOptions;

        if (!Finder.isFindable(fo.queryName))
            return;

        Finder.getQueryDescription(fo.queryName)
            .then(qd => Finder.parseFindOptions(fo, qd))
            .then(fo => Finder.API.queryCount(this.getQueryRequest(fo)))
            .then(count => {
                this.setState({ count });
                this.props.onCountChange && this.props.onCountChange(count);
            })
            .done();
    }

    render() {
        if (!Finder.isFindable(this.props.findOptions.queryName))
            return null;

        let className = classes(
            "count-search",
            this.props.isBadge == true || (this.props.isBadge == "MoreThanZero" && this.state.count > 0) ? "badge" : "",
            this.state.count > 0 ? "count-with-results" : "count-no-results"
        );

        if (this.props.isLink) {
            return (
                <a className={className} onClick={this.handleClick} href="">
                    {this.state.count == undefined ? "…" : this.state.count}
                </a>
            );
        }

        return <span className={className}>{this.state.count == undefined ? "…" : this.state.count}</span> 
    }

    handleClick = (e: React.MouseEvent) => {
        e.preventDefault();

        if (e.ctrlKey || e.button == 2)
            window.open(Finder.findOptionsPath(this.props.findOptions));
        else
            Finder.explore(this.props.findOptions).then(() => {
                if (!this.props.avoidAutoRefresh)
                    this.refreshCount(this.props);
            }).done();
    }
}