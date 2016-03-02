
import * as React from 'react'
import { DropdownButton, MenuItem, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { Dic, DomUtils } from '../Globals'
import * as Finder from '../Finder'
import { ResultTable, ResultRow, FindOptions, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken, expandSimpleColumnName, CountQueryRequest, QueryRequest } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, IEntity, liteKey, is } from '../Signum.Entities'
import { getTypeInfos, IsByAll, getQueryKey, TypeInfo, EntityData, getQueryNiceName} from '../Reflection'
import * as Navigator from '../Navigator'
import { StyleContext } from '../Typecontext'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'

export interface CountSearchControlProps extends React.Props<CountSearchControl> {
    ctx: StyleContext;
    findOptions: FindOptions;
    title?: string;
    showAsLink?: boolean;
}

export interface CountSearchControlState {
    count?: number;
}

export default class CountSearchControl extends React.Component<CountSearchControlProps, CountSearchControlState> {

    constructor(props: CountSearchControlProps) {
        super(props);
        this.state = {
            count: null,
        };
    }

    getQueryKey(): string {
        return getQueryKey(this.props.findOptions.queryName);
    }

    getQueryRequest(): CountQueryRequest {
        return {
            queryKey: this.getQueryKey(),
            filters: (this.props.findOptions.filterOptions||[]).map(fo => ({ token: fo.token.fullKey, operation: fo.operation, value: fo.value })),
        };
    }

    componentDidMount() {
        Finder.API.queryCount(this.getQueryRequest()).then(count => {
            this.setState({ count });
            this.forceUpdate();
        }).done();
    }

    render() {
        return (
            <FormGroup ctx={this.props.ctx} title={this.props.title || getQueryNiceName(this.props.findOptions.queryName) }>
                <FormControlStatic ctx={this.props.ctx}>
                    <span className={this.state.count > 0 ? "count-search count-with-results badge" : "count-search count-no-results"}>
                        {this.state.count == null ? "…" : this.state.count}
                    </span>
                </FormControlStatic>
            </FormGroup>
        );
    }
}