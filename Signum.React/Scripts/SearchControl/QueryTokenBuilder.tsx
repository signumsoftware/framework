import * as React from 'react'
import 'react-widgets/dist/css/react-widgets.css';
import { areEqual, classes } from '../Globals'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import { FilterOperation, FilterOption, QueryDescription, QueryToken, SubTokensOptions, getTokenParents } from '../FindOptions'
import { SearchMessage, JavascriptMessage } from '../Signum.Entities'
import * as Reflection from '../Reflection'
import { default as SearchControl, SearchControlProps} from './SearchControl'
import * as PropTypes from "prop-types";


import "./QueryTokenBuilder.css"
import * as DropdownList from 'react-widgets/lib/DropdownList'


interface QueryTokenBuilderProps extends React.Props<QueryTokenBuilder> {
    queryToken: QueryToken | undefined | null;
    onTokenChange: (newToken: QueryToken | undefined) => void;
    queryKey: string;
    subTokenOptions: SubTokensOptions;
    readOnly: boolean;
    className?: string;
}

export default class QueryTokenBuilder extends React.Component<QueryTokenBuilderProps>{

    lastTokenChanged: string | undefined;

    static copiedToken: { fullKey: string, queryKey: string } | undefined; 

    render() {
        const tokenList: (QueryToken | undefined)[] = [...getTokenParents(this.props.queryToken)];

        if (!this.props.readOnly)
            tokenList.push(undefined);

        return (
            <div className={classes("sf-query-token-builder", this.props.className)} onKeyDown={this.handleKeyDown}>
                {tokenList.map((a, i) => <QueryTokenPart key={i == 0 ? "__first__" : tokenList[i - 1]!.fullKey }
                    queryKey={this.props.queryKey}
                    readOnly={this.props.readOnly}
                    onTokenSelected={qt => {
                        this.lastTokenChanged = qt && qt.fullKey;
                        this.props.onTokenChange && this.props.onTokenChange(qt);
                    } }
                    defaultOpen={this.lastTokenChanged && i > 0 && this.lastTokenChanged == tokenList[i - 1]!.fullKey ? true : false}
                    subTokenOptions={this.props.subTokenOptions}
                    parentToken={i == 0 ? undefined : tokenList[i - 1]}
                    selectedToken={a} />)}
            </div>
        );
    }

    handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {

        if (e.ctrlKey) {
            if (e.key == "c") {
                QueryTokenBuilder.copiedToken = this.props.queryToken ? {
                    fullKey: this.props.queryToken.fullKey,
                    queryKey: this.props.queryKey
                } : undefined;
            }
            else if (e.key == "v" && QueryTokenBuilder.copiedToken && QueryTokenBuilder.copiedToken.queryKey == this.props.queryKey) {
                Finder.parseSingleToken(this.props.queryKey, QueryTokenBuilder.copiedToken.fullKey, this.props.subTokenOptions)
                    .then(a => this.props.onTokenChange(a))
                    .done();
            }

        }

    }
}


interface QueryTokenPartProps extends React.Props<QueryTokenPart> {
    parentToken: QueryToken | undefined;
    selectedToken: QueryToken | undefined;
    onTokenSelected: (newToken: QueryToken | undefined) => void;
    queryKey: string;
    subTokenOptions: SubTokensOptions;
    readOnly: boolean;
    defaultOpen: boolean;
}

export class QueryTokenPart extends React.Component<QueryTokenPartProps, { subTokens?: (QueryToken | null)[] }>
{
    constructor(props: QueryTokenPartProps) {
        super(props);

        this.state = { subTokens: undefined };      
    }

    componentWillMount() {
        if (!this.props.readOnly)
            this.requestSubTokens(this.props);
    }

    componentWillReceiveProps(newProps: QueryTokenPartProps) {
        if ((newProps.readOnly == false && this.props.readOnly == true) ||
            !newProps.readOnly && (!areEqual(this.props.parentToken, newProps.parentToken, a => a.fullKey) ||
            this.props.subTokenOptions != newProps.subTokenOptions)) {
            this.setState({ subTokens: undefined });
            this.requestSubTokens(newProps);
        }
    }

    requestSubTokens(props: QueryTokenPartProps) {
        Finder.API.getSubTokens(props.queryKey, props.parentToken, props.subTokenOptions).then(tokens =>
            this.setState({ subTokens: tokens.length == 0 ? tokens : [null, ...tokens] })
        ).done();
    }

    getChildContext() {
        return { parentToken: this.props.parentToken };
    }

    static childContextTypes = { "parentToken": PropTypes.object };

    handleOnChange = (value: any) => {
        this.props.onTokenSelected(value || this.props.parentToken);
    }

    handleKeyUp = (e: React.KeyboardEvent<any>) => {
        if (e.key == "Enter") {
            e.preventDefault();
            e.stopPropagation();
        }
    }

    render() {
        
        if (this.state.subTokens != undefined && this.state.subTokens.length == 0)
            return null;
        
        return (
            <div className="sf-query-token-part" onKeyUp={this.handleKeyUp} onKeyDown={this.handleKeyUp}>
                <DropdownList
                    disabled={this.props.readOnly}
                    filter="contains"
                    data={this.state.subTokens || []}
                    value={this.props.selectedToken}
                    onChange={this.handleOnChange}
                    valueField="fullKey"
                    textField="toString"
                    valueComponent={QueryTokenItem}
                    itemComponent={QueryTokenOptionalItem}
                    defaultOpen={this.props.defaultOpen}
                    busy={!this.props.readOnly && this.state.subTokens == undefined}
                    />
            </div>
        );
    }
}

export class QueryTokenItem extends React.Component<{ item: QueryToken | null }> {
    render() {

        const item = this.props.item;

        if (item == null)
            return null;

        return (
            <span
                style= {{ color: item.typeColor }}
                title={item.niceTypeName}>
                { item.toString }
            </span>
        );
    }
}
  

export class QueryTokenOptionalItem extends React.Component<{ item: QueryToken | null }> {

    static contextTypes = { "parentToken": PropTypes.object };


    render() {


        const item = this.props.item;

        if (item == null)
            return <span> - </span>;

        const parentToken = (this.context as any).parentToken;

        return (
            <span data-token={item.key}
                style= {{ color: item.typeColor }}
                title={item.niceTypeName}>
                { ((item.parent && !parentToken) ? " > " : "") + item.toString }
            </span>
        );

    }
}