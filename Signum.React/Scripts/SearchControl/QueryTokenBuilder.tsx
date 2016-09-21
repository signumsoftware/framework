import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import { DropdownList } from 'react-widgets';
import 'react-widgets/dist/css/react-widgets.css';
import { areEqual, classes } from '../Globals'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import { FilterOperation, FilterOption, QueryDescription, QueryToken, SubTokensOptions, getTokenParents } from '../FindOptions'
import { SearchMessage, JavascriptMessage } from '../Signum.Entities'
import * as Reflection from '../Reflection'
import { default as SearchControl, SearchControlProps} from './SearchControl'


require("!style!css!./QueryTokenBuilder.css");

interface QueryTokenBuilderProps extends React.Props<QueryTokenBuilder> {
    queryToken: QueryToken | undefined | null;
    onTokenChange: (newToken: QueryToken) => void;
    queryKey: string;
    subTokenOptions: SubTokensOptions;
    readOnly: boolean;
    className?: string;
}

export default class QueryTokenBuilder extends React.Component<QueryTokenBuilderProps, {}>  {
    render() {

        const tokenList = [...getTokenParents(this.props.queryToken), undefined];

        return (
            <div className={classes("sf-query-token-builder", this.props.className) }>
                {tokenList.map((a, i) => <QueryTokenPart key={i}
                    queryKey={this.props.queryKey}
                    readOnly={this.props.readOnly}
                    onTokenSelected={this.props.onTokenChange}
                    subTokenOptions={this.props.subTokenOptions}
                    parentToken={i == 0 ? undefined : tokenList[i - 1]}
                    selectedToken={a} />) }
            </div>
        );
    }
}


interface QueryTokenPartProps extends React.Props<QueryTokenPart> {
    parentToken: QueryToken | undefined;
    selectedToken: QueryToken | undefined;
    onTokenSelected: (newToken: QueryToken) => void;
    queryKey: string;
    subTokenOptions: SubTokensOptions;
    readOnly: boolean;
}

export class QueryTokenPart extends React.Component<QueryTokenPartProps, { data?: (QueryToken | null)[] }>
{
    constructor(props: QueryTokenPartProps) {
        super(props);

        this.state = { data: undefined };      
    }

    componentWillMount() {
        if (!this.props.readOnly)
            this.requestSubTokens(this.props);
    }

    componentWillReceiveProps(newProps: QueryTokenPartProps) {
        if (!newProps.readOnly && (!areEqual(this.props.parentToken, newProps.parentToken, a => a.fullKey) || this.props.subTokenOptions != newProps.subTokenOptions)) {
            this.setState({ data: undefined });
            this.requestSubTokens(newProps);
        }
    }

    requestSubTokens(props: QueryTokenPartProps) {
        Finder.API.subTokens(props.queryKey, props.parentToken, props.subTokenOptions).then(tokens =>
            this.setState({ data: tokens.length == 0 ? tokens : [null, ...tokens] })
        ).done();
    }

    getChildContext() {
        return { parentToken: this.props.parentToken };
    }

    static childContextTypes: React.ValidationMap<QueryTokenOptionalItem> = { "parentToken": React.PropTypes.object };

    handleOnChange = (value: any) => {
        this.props.onTokenSelected(value || this.props.parentToken);
    }

    render() {

        if (this.state.data != undefined && this.state.data.length == 0)
            return null;
        
        return (
            <div className="sf-query-token-part">
                <DropdownList
                    disabled={this.props.readOnly}
                    filter="contains"
                    data={this.state.data || []}
                    value={this.props.selectedToken}
                    onChange={this.handleOnChange}
                    valueField="fullKey"
                    textField="toString"
                    valueComponent={QueryTokenItem}
                    itemComponent={QueryTokenOptionalItem}
                    busy={!this.props.readOnly && this.state.data == undefined}
                    />
            </div>
        );
    }
}

export class QueryTokenItem extends React.Component<{ item: QueryToken | null }, {}> {
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
  

export class QueryTokenOptionalItem extends React.Component<{ item: QueryToken | null }, {}> {

    static contextTypes: React.ValidationMap<QueryTokenOptionalItem> = { "parentToken": React.PropTypes.object };


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