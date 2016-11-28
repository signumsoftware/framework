import * as React from 'react'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { DropdownButton, MenuItem, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { Dic, DomUtils, classes } from '../Globals'
import * as Finder from '../Finder'
import {
    ResultTable, ResultRow, FindOptions, FindOptionsParsed, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken, QueryCountRequest, QueryRequest
} from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, is, Entity, getToString, EmbeddedEntity } from '../Signum.Entities'
import { getTypeInfos, IsByAll, getQueryKey, TypeInfo, EntityData, getQueryNiceName, toNumbroFormat, toMomentFormat, getEnumInfo } from '../Reflection'
import * as Navigator from '../Navigator'
import { StyleContext } from '../Typecontext'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks } from '../Lines/LineBase'



export interface ValueSearchControlProps extends React.Props<ValueSearchControl> {
    valueToken?: string; 
    findOptions: FindOptions;
    isLink?: boolean;
    isBadge?: boolean | "MoreThanZero";
    formControlClass?: string;
    avoidAutoRefresh?: boolean;
    onValueChange?: (value: any) => void;
    onTokenLoaded?: () => void;
    initialValue?: any;
    customClass?: string;
    customStyle?: React.CSSProperties;
}

export interface ValueSearchControlState {
    value?: any;
    token?: QueryToken;
}

export default class ValueSearchControl extends React.Component<ValueSearchControlProps, ValueSearchControlState> {

    static defaultProps = {
        isLink: false,
        isBadge: "MoreThanZero"
    }

    constructor(props: ValueSearchControlProps) {
        super(props);
        this.state = { };
    }

    getQueryRequest(fo: FindOptionsParsed): QueryCountRequest {

        return {
            queryKey: fo.queryKey,
            filters: fo.filterOptions.map(fo => ({ token: fo.token!.fullKey, operation: fo.operation!, value: fo.value })),
            valueToken: this.props.valueToken
        };
    }

    componentDidMount() {
        if(this.props.initialValue !== undefined)
              this.setState({ value: this.props.initialValue });
        else{
            this.loadToken(this.props);
            this.refreshCount(this.props);
        }
    }

    componentWillReceiveProps(newProps: ValueSearchControlProps) {
        if (Finder.findOptionsPath(this.props.findOptions) == Finder.findOptionsPath(newProps.findOptions) &&
            this.props.valueToken == newProps.valueToken)
            return;

        this.loadToken(newProps);
        this.refreshCount(newProps);
    }

    loadToken(props: ValueSearchControlProps) {

        if (props.valueToken == (this.state.token && this.state.token.fullKey))
            return; 

        this.setState({ token: undefined, value: undefined });
        if (props.valueToken)
            Finder.parseSingleToken(props.findOptions.queryName, props.valueToken, SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll)
                .then(st => {
                    this.setState({ token: st });
                    this.props.onTokenLoaded && this.props.onTokenLoaded();
                })
                .done();
    }

    refreshCount(props: ValueSearchControlProps) {
        var fo = props.findOptions;

        if (!Finder.isFindable(fo.queryName))
            return;

        Finder.getQueryDescription(fo.queryName)
            .then(qd => Finder.parseFindOptions(fo, qd))
            .then(fo => Finder.API.queryCount(this.getQueryRequest(fo)))
            .then(value => {
                this.setState({ value });
                this.props.onValueChange && this.props.onValueChange(value);
            })
            .done();
    }

    isNumeric() {
        let token = this.state.token;
        return token && (token.filterType == "Integer" || token.filterType == "Decimal");
    }

    render() {

        const p = this.props;

        if (!Finder.isFindable(p.findOptions.queryName))
            return null;

        let className = classes(
           p.valueToken == undefined && "count-search",
           p.valueToken == undefined && this.state.value > 0 ? "count-with-results" : "count-no-results",
           p.formControlClass,
           p.formControlClass && this.isNumeric() && "numeric",
           p.isBadge == true || (p.isBadge == "MoreThanZero" && this.state.value > 0) ? "badge" : "",
           p.customClass
        );

        if (p.formControlClass)
            return <p className={className} style={p.customStyle}>{this.renderValue()}</p> 

        if (p.isLink) {
            return (
                <a className={className} onClick={this.handleClick} href="" style={p.customStyle}>
                    {this.renderValue()}
                </a>
            );
        }

        return <span className={className} style={p.customStyle}>{this.renderValue()}</span> 
    }

    renderValue() {

        let value = this.state.value;

        if (value === undefined)
            return "…";

        if (value === null)
            return null;

        if (!this.props.valueToken)
            return this.state.value;

        let token = this.state.token;
        if (!token)
            return null;

        switch (token.filterType) {
            case "Integer":
            case "Decimal": 
                const numbroFormat = toNumbroFormat(token.format);
                return numbro(value).format(numbroFormat);


            case "String": return value;
            case "DateTime":  
                const momentFormat = toMomentFormat(token!.format);
                return moment(value).format(momentFormat)
            case "Lite": return (value as Lite<Entity>).toStr;
            case "Embedded": return getToString(value as EmbeddedEntity);
            case "Boolean": return <input type="checkbox" disabled={true} checked={value} />
            case "Enum": return getEnumInfo(token!.type.name, value).niceName;
            case "Guid":
                let str = value as string;
                return <span className="guid">{str.substr(0, 4) + "…" + str.substring(str.length - 4)}</span>;
        }

        return value;
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