import * as React from 'react'
import { DropdownButton, MenuItem, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { Dic, DomUtils } from '../Globals'
import * as Finder from '../Finder'
import { ResultTable, ResultRow, FindOptions, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken, expandParentColumn, CountQueryRequest, QueryRequest } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, is } from '../Signum.Entities'
import { getTypeInfos, IsByAll, getQueryKey, TypeInfo, EntityData, getQueryNiceName} from '../Reflection'
import * as Navigator from '../Navigator'
import { StyleContext } from '../Typecontext'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'

export interface CountSearchControlProps extends React.Props<CountSearchControl> {
    ctx: StyleContext;
    findOptions: FindOptions;
    labelText?: React.ReactChild;
    labelProps?: React.HTMLAttributes;
    style?: "View" | "Link" | "Badge" | "Span";
    formGroupHtmlProps?: React.HTMLProps<HTMLDivElement>;
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

    getQueryRequest(fo: FindOptions): CountQueryRequest {

        return {
            queryKey: getQueryKey(fo.queryName),
            filters: (fo.filterOptions || []).map(fo => ({ token: fo.token!.fullKey, operation: fo.operation!, value: fo.value })),
        };
    }

    componentDidMount() {

        if (!Finder.isFindable(this.props.findOptions.queryName))
            return;

        const newFindOptions = expandParentColumn(this.props.findOptions);

        Finder.parseTokens(newFindOptions)
            .then(fo => Finder.API.queryCount(this.getQueryRequest(fo)))
            .then(count => {
                this.setState({ count });
                this.forceUpdate();
            }).done();
    }

    render() {
        if (!Finder.isFindable(this.props.findOptions.queryName))
            return null;

        if (this.props.style == "Badge")
            return this.renderBadget();

        if (this.props.style == "Span")
            return this.renderSpan();

        return (
            <FormGroup ctx={this.props.ctx} labelText={this.props.labelText || getQueryNiceName(this.props.findOptions.queryName) } labelProps={this.props.labelProps} htmlProps={this.props.formGroupHtmlProps}>
                {this.props.style == "Link" ? this.renderAsLink() : this.renderAsView() }
            </FormGroup>
        );
    }

    handleClick = (e: React.MouseEvent) => {
        Finder.exploreWindowsOpen(this.props.findOptions, e);
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