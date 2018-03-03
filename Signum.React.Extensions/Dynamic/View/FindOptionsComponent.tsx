import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions, QueryToken, filterOperations, OrderType, ColumnOptionsMode } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, getTypeInfo, isTypeEntity, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { Typeahead } from '../../../../Framework/Signum.React/Scripts/Components'
import QueryTokenBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/QueryTokenBuilder'
import { ModifiableEntity, JavascriptMessage, EntityControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { QueryEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { FilterOperation, PaginationMode } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.DynamicQuery'
import { ExpressionOrValueComponent, FieldComponent, DesignerModal } from './Designer'
import * as Nodes from './Nodes'
import * as NodeUtils from './NodeUtils'
import { DesignerNode, Expression } from './NodeUtils'
import { BaseNode, SearchControlNode } from './Nodes'
import { FindOptionsExpr, FilterOptionExpr, OrderOptionExpr, ColumnOptionExpr } from './FindOptionsExpression'
import * as DynamicViewClient from '../DynamicViewClient'
import { DynamicViewMessage, DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal';
import { getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection';
import { TypeInfo } from '../../../../Framework/Signum.React/Scripts/Reflection';


interface FindOptionsLineProps {
    binding: Binding<FindOptionsExpr | undefined>;
    dn: DesignerNode<BaseNode>;
    avoidSuggestion?: boolean;
}

export class FindOptionsLine extends React.Component<FindOptionsLineProps>{

    renderMember(fo: FindOptionsExpr | undefined): React.ReactNode {
        return (<span
            className={fo === undefined ? "design-default" : "design-changed"}>
            {this.props.binding.member}
        </span>);
    }

    handleRemove = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        this.props.binding.deleteValue();
        this.props.dn.context.refreshView();
    }

    handleCreate = (e: React.MouseEvent<any>) => {

        e.preventDefault();
        const route = this.props.dn.route;
        const ti = route && route.typeReferenceInfo();

        const promise = this.props.avoidSuggestion == true || !ti || !isTypeEntity(ti) ? Promise.resolve({} as FindOptionsExpr) :
            DynamicViewClient.API.getSuggestedFindOptions(ti.name)
                .then(sfos => SelectorModal.chooseElement(sfos, {
                    title: DynamicViewMessage.SuggestedFindOptions.niceToString(),
                    message: DynamicViewMessage.TheFollowingQueriesReference0.niceToString().formatHtml(<strong>{ti.niceName}</strong>),
                    buttonDisplay: sfo => <div><strong>{sfo.queryKey}</strong><br /><small>(by <code>{sfo.parentColumn}</code>)</small></div>
                }))
                .then(sfo => ({
                    queryName: sfo && sfo.queryKey,
                    parentColumn: sfo && sfo.parentColumn,
                    parentValue: sfo && { __code__: "ctx.value" } as Expression<ModifiableEntity>
                } as FindOptionsExpr));

        promise.then(fo => this.modifyFindOptions(fo)).done();
    }

    handleView = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        var fo = JSON.parse(JSON.stringify(this.props.binding.getValue())) as FindOptionsExpr;
        this.modifyFindOptions(fo);
    }

    modifyFindOptions(fo: FindOptionsExpr) {
        DesignerModal.show("FindOptions", () => <FindOptionsComponent findOptions={fo} dn={this.props.dn} />).then(result => {
            if (result)
                this.props.binding.setValue(this.clean(fo));

            this.props.dn.context.refreshView();
        }).done();
    }

    clean(fo: FindOptionsExpr) {
        delete fo.parentToken;
        if (fo.filterOptions) fo.filterOptions.forEach(f => delete f.token);
        if (fo.orderOptions) fo.orderOptions.forEach(o => delete o.token);
        if (fo.columnOptions) fo.columnOptions.forEach(c => delete c.token);
        return fo;
    }

    render() {
        const fo = this.props.binding.getValue();

        return (
            <div className="form-group">
                <label className="control-label">
                    {this.renderMember(fo)}
                </label>
                <div>
                    {fo ? <div>
                        <a href="#" onClick={this.handleView}>{this.getDescription(fo)}</a>
                        {" "}
                        <a href="#" className={classes("sf-line-button", "sf-remove")}
                            onClick={this.handleRemove}
                            title={EntityControlMessage.Remove.niceToString()}>
                            <span className="fa fa-remove" />
                        </a></div> :
                        <a href="#" title={EntityControlMessage.Create.niceToString()}
                            className="sf-line-button sf-create"
                            onClick={this.handleCreate}>
                            <span className="fa fa-plus sf-create sf-create-label" />{EntityControlMessage.Create.niceToString()}
                        </a>}
                </div>
            </div>
        );
    }

    getDescription(fo: FindOptionsExpr) {

        var filters = [
            fo.parentColumn,
            fo.filterOptions && fo.filterOptions.length && fo.filterOptions.length + " filters"]
            .filter(a => !!a).join(", ");

        return `${fo.queryName} (${filters || "No filter"})`.trim();
    }
}


interface QueryTokenLineProps {
    binding: Binding<string | undefined>;
    dn: DesignerNode<BaseNode>;
    subTokenOptions: SubTokensOptions;
    queryKey: string;
}

interface QueryTokenLineState {
    token?: QueryToken;
}

export class QueryTokenLine extends React.Component<QueryTokenLineProps, QueryTokenLineState>{

    state = {} as QueryTokenLineState;

    renderMember(columnName: string| undefined): React.ReactNode {
        return (<span
            className={columnName === undefined ? "design-default" : "design-changed"}>
            {this.props.binding.member}
        </span>);
    }

    handleChange = (qt: QueryToken | undefined) => {
        this.setState({ token: qt });
        if (qt)
            this.props.binding.setValue(qt.fullKey);
        else
            this.props.binding.deleteValue();

        this.props.dn.context.refreshView();
    }

    componentWillReceiveProps(newProps: QueryTokenLineProps) {
        if (this.props.queryKey != null && this.props.queryKey != newProps.queryKey)
            this.handleChange(undefined);
    }   

    render() {
        const columnName = this.props.binding.getValue();

        return (
            <div className="form-group">
                <label className="control-label">
                    {this.renderMember(columnName)} {this.props.queryKey && <small>({getQueryNiceName(this.props.queryKey)})</small>}
                </label>
                <div>
                    <QueryTokenBuilderString key={this.props.queryKey}
                        queryKey={this.props.queryKey}
                        columnName={columnName}
                        subTokenOptions={this.props.subTokenOptions}
                        token={this.state.token}
                        hideLabel={true}
                        onChange={this.handleChange}
                        label=""
                        />
                </div>
            </div>
        );
    }

    getDescription(fo: FindOptionsExpr) {

        var filters = [
            fo.parentColumn,
            fo.filterOptions && fo.filterOptions.length && fo.filterOptions.length + " filters"]
            .filter(a => !!a).join(", ");

        return `${fo.queryName} (${filters || "No filter"})`.trim();
    }
}



interface FetchQueryDescriptionProps {
    queryName?: string;
    children: (qd?: QueryDescription) => React.ReactElement<any>;
}

interface FetchQueryDescriptionState {
    queryDescription?: QueryDescription
}

export class FetchQueryDescription extends React.Component<FetchQueryDescriptionProps, FetchQueryDescriptionState> {

    constructor(props: FetchQueryDescriptionProps) {
        super(props);
        this.state = { queryDescription: undefined };
    }

    componentWillMount() {
        this.loadData(this.props);
    }

    componentWillReceiveProps(newProps: FetchQueryDescriptionProps) {
        if (newProps.queryName != this.props.queryName)
            this.loadData(newProps);
    }

    loadData(props: FetchQueryDescriptionProps) {

        if (!props.queryName)
            this.setState({ queryDescription: undefined });
        else
            Finder.getQueryDescription(props.queryName)
                .then(qd => this.setState({ queryDescription: qd }))
                .done();
    }

    render() {
        return this.props.children(this.state.queryDescription);
    }
}


interface ViewNameComponentProps {
    binding: Binding<any>;
    dn: DesignerNode<BaseNode>;
    typeName?: string;
}

interface ViewNameComponentState {
    viewNames?: string[];
}

export class ViewNameComponent extends React.Component<ViewNameComponentProps, ViewNameComponentState> {

    constructor(props: ViewNameComponentProps) {
        super(props);
        this.state = { viewNames: [] };
    }

    componentWillMount() {
        this.loadData(this.props);
    }

    componentWillReceiveProps(newProps: ViewNameComponentProps) {
        if (newProps.typeName != this.props.typeName)
            this.loadData(newProps);
    }

    loadData(props: ViewNameComponentProps) {
        if (props.typeName && !props.typeName.contains(", ") && !isTypeEntity(props.typeName))
            this.setState({ viewNames: [] });
        else
            Promise.all(getTypeInfos(props.typeName || "").map(ti => Navigator.viewDispatcher.getViewNames(ti.name).then(array => [...array, (hastStaticView(ti) ? "STATIC" : undefined)])))
                .then(arrays => this.setState({
                    viewNames: [...(arrays.flatMap(a => a).filter(a => a != null) as string[]), "NEW"]
                }))
                .done();
    }

    render() {
        return <ExpressionOrValueComponent dn={this.props.dn} binding={this.props.binding} type="string" defaultValue={null} options={this.state.viewNames} exampleExpression={"e => \"MyStaticOrDynamicViewName\""}/>;
    }
}

function hastStaticView(t: TypeInfo) {
    var es = Navigator.getSettings(t);
    return es != null && es.getViewPromise != null;
}

interface FindOptionsComponentProps {
    dn: DesignerNode<BaseNode>;
    findOptions: FindOptionsExpr;
}

export class FindOptionsComponent extends React.Component<FindOptionsComponentProps> {


    handleChangeQueryKey = (queryKey: string | undefined) => {
        const fo = this.props.findOptions;
        fo.queryName = queryKey;
        delete fo.parentColumn;
        delete fo.parentToken;
        delete fo.parentValue;
        delete fo.filterOptions;
        delete fo.columnOptions;
        delete fo.orderOptions;
        this.forceUpdate();
    }

    handleChangeParentColumn = (newToken: QueryToken | undefined) => {
        this.props.findOptions.parentColumn = newToken && newToken.fullKey;
        this.props.findOptions.parentToken = newToken;
        this.forceUpdate();
    }
    
    render() {
        var dn = this.props.dn;
        const fo = this.props.findOptions;
        return (
            <div className="form-sm filter-options code-container">
                <QueryKeyLine queryKey={fo.queryName} label="queryKey" onChange={this.handleChangeQueryKey} />

                {fo.queryName &&
                    <div>
                        <div className="row">
                            <div className="col-sm-6">
                            <QueryTokenBuilderString label="parentColumn" token={fo.parentToken} columnName={fo.parentColumn} 
                                    onChange={this.handleChangeParentColumn} queryKey={fo.queryName} subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                            </div>
                            <div className="col-sm-6">
                            {fo.parentToken &&
                                <ExpressionOrValueComponent dn={dn} binding={Binding.create(fo, f => f.parentValue)} refreshView={() => this.forceUpdate()}
                                    type={FilterOptionsComponent.getValueType(fo.parentToken)} defaultValue={null} />}
                            </div>
                        </div>


                        <FilterOptionsComponent dn={dn} binding={Binding.create(fo, f => f.filterOptions)} queryKey={fo.queryName} refreshView={() => this.forceUpdate()} />

                        <ExpressionOrValueComponent dn={dn} binding={Binding.create(fo, f => f.columnOptionsMode)} refreshView={() => this.forceUpdate()} type="string" options={ColumnOptionsMode.values()} defaultValue={"Add" as ColumnOptionsMode} />
                        <ColumnOptionsComponent dn={dn} binding={Binding.create(fo, f => f.columnOptions)} queryKey={fo.queryName} refreshView={() => this.forceUpdate()} />

                        <OrderOptionsComponent dn={dn} binding={Binding.create(fo, f => f.orderOptions)} queryKey={fo.queryName} refreshView={() => this.forceUpdate()} />
                        <PaginationComponent dn={dn} findOptions={fo} refreshView={() => this.forceUpdate()} />

                     
                    </div>
                }
            </div>
        );
    }
}

export class QueryKeyLine extends React.Component<{ queryKey: string | undefined, label: string; onChange: (queryKey: string | undefined) => void }>{

    handleGetItems = (query: string) => {
        return Finder.API.findLiteLike({ types: QueryEntity.typeName, subString: query, count: 5 })
            .then(lites => lites.map(a => a.toStr));
    }

    render() {
        return (
            <div className="form-group">
                <label className="control-label">
                    {this.props.label}
                </label>
                <div style={{ position: "relative" }}>
                    {
                        this.props.queryKey ? this.renderLink() :
                            <Typeahead
                                inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
                                getItems={this.handleGetItems}
                                onSelect={item => { this.props.onChange(item as string); return ""; } } />

                    }
                </div>
            </div>
        );
    }

    renderLink() {
        return (
            <div className="input-group">
                <span className="form-control btn-light sf-entity-line-entity">
                    {this.props.queryKey}
                </span>
                <span className="input-group-append">
                    <a href="#" className={classes("sf-line-button", "sf-remove btn btn-light")}
                        onClick={() => this.props.onChange(undefined)}
                        title={EntityControlMessage.Remove.niceToString()}>
                        <span className="fa fa-remove" />
                    </a>
                </span>
            </div>
        );
    }
}

interface QueryTokenBuilderStringProps {
    queryKey: string;
    columnName: string | undefined;
    token: QueryToken | undefined;
    label: string;
    onChange: (newToken: QueryToken | undefined) => void;
    subTokenOptions: SubTokensOptions;
    hideLabel?: boolean;
}

class QueryTokenBuilderString extends React.Component<QueryTokenBuilderStringProps>{

    componentWillMount() {
        this.loadInitialToken(this.props);
    }

    componentWillReceiveProps(newProps: QueryTokenBuilderStringProps) {
        this.loadInitialToken(newProps);
    }

    loadInitialToken(props: QueryTokenBuilderStringProps) {
        if (props.columnName == undefined) {
            if (this.props.columnName != props.columnName)
                props.onChange(undefined);
        }
        else if (!props.token || this.props.queryKey != props.queryKey || props.token.fullKey != props.columnName)
            return Finder.parseSingleToken(props.queryKey, props.columnName, props.subTokenOptions)
                .then(t => this.props.onChange(t))
                .done();
    }

    render() {

        var qt = <QueryTokenBuilder
            queryToken={this.props.token}
            queryKey={this.props.queryKey}
            onTokenChange={this.props.onChange}
            readOnly={false}
            subTokenOptions={this.props.subTokenOptions} />;

        if (this.props.hideLabel)
            return qt;

        return (
            <div className="form-group">
                <label className="control-label">
                    {this.props.label}
                </label>
                <div>
                    {qt}
                </div>
            </div>
        );
    }
}

interface BaseOptionsComponentProps<T> {
    binding: Binding<Array<T> | undefined>;
    dn: DesignerNode<BaseNode>;
    refreshView: () => void;
    queryKey: string;
}

abstract class BaseOptionsComponent<T> extends React.Component<BaseOptionsComponentProps<T>>{


    handleOnRemove = (event: React.MouseEvent<any>, index: number) => {
        event.preventDefault();
        var array = this.props.binding.getValue()!;
        array.removeAt(index);
        if (array.length == 0)
            this.props.binding.deleteValue();

        this.props.refreshView();
    }

    handleOnMoveUp = (event: React.MouseEvent<any>, index: number) => {
        event.preventDefault();
        const list = this.props.binding.getValue() !;
        list.moveUp(index);
        this.props.refreshView();
    }

    handleOnMoveDown = (event: React.MouseEvent<any>, index: number) => {
        event.preventDefault();
        const list = this.props.binding.getValue() !;
        list.moveDown(index);
        this.props.refreshView();
    }


    handleCreateClick = (event: React.SyntheticEvent<any>) => {
        var array = this.props.binding.getValue();
        if (array == undefined) {
            array = [];
            this.props.binding.setValue(array);
        }
        array!.push(this.newElement());
        this.props.refreshView();
    }


    renderButtons(index: number) {
        return (<div className="item-group">
            <a href="#" className={classes("sf-line-button", "sf-remove")}
                onClick={e => this.handleOnRemove(e, index)}
                title={EntityControlMessage.Remove.niceToString()}>
                <span className="fa fa-remove" />
            </a>

            <a href="#" className={classes("sf-line-button", "move-up")}
                onClick={e => this.handleOnMoveUp(e, index)}
                title={EntityControlMessage.MoveUp.niceToString()}>
                <span className="fa fa-chevron-up" />
            </a>

            <a href="#" className={classes("sf-line-button", "move-down")}
                onClick={e => this.handleOnMoveDown(e, index)}
                title={EntityControlMessage.MoveDown.niceToString()}>
                <span className="fa fa-chevron-down" />
            </a>
        </div>);
    }

    abstract renderTitle(): React.ReactNode;
    abstract renderHeader(): React.ReactElement<any>;
    abstract renderItem(item: T, index: number): React.ReactElement<any>;
    abstract getNumColumns(): number;
    abstract newElement(): T;


    render() {

        const array = this.props.binding.getValue();

        return (<fieldset className="SF-table-field">
            <legend>
                {this.renderTitle()}
            </legend>
            <table className="table table-sm code-container">
                <thead>
                    {this.renderHeader()}
                </thead>
                <tbody>
                    {array && array.map((item, i) => this.renderItem(item, i))}
                    <tr>
                        <td colSpan={this.getNumColumns()}>
                            <a title={EntityControlMessage.Create.niceToString()}
                                className="sf-line-button sf-create"
                                onClick={this.handleCreateClick}>
                                <span className="fa fa-plus sf-create" />&nbsp;{EntityControlMessage.Create.niceToString()}
                            </a>
                        </td>
                    </tr>
                </tbody>
            </table>
        </fieldset>);
    }
}

class FilterOptionsComponent extends BaseOptionsComponent<FilterOptionExpr> {

    renderTitle() {
        return "Filters";
    }

    renderHeader() {
        return (
            <tr>
                <th></th>
                <th>Column</th>
                <th>Operation</th>
                <th>Value</th>
                <th>Frozen</th>
                <th>Applicable</th>
            </tr>
        );
    }

    handleColumnChange = (item: FilterOptionExpr, newToken: QueryToken | undefined) => {
        item.columnName = newToken && newToken.fullKey;
        item.token = newToken;
        this.props.refreshView();
    }

    renderItem(item: FilterOptionExpr, index: number) {
        const dn = this.props.dn;
        return (
            <tr key={index}>
                <td>{this.renderButtons(index)}</td>
                <td> <QueryTokenBuilderString label="columnName" columnName={item.columnName} token={item.token} onChange={newToken => this.handleColumnChange(item, newToken)}
                    queryKey={this.props.queryKey} subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} hideLabel={true} /></td>
                <td> {item.token && <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.operation)} type="string" defaultValue={null} options={this.getOperations(item.token)} />}</td>
                <td> {item.token && <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.value)} type={FilterOptionsComponent.getValueType(item.token)} defaultValue={null} />}</td>
                <td> <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.frozen)} type="boolean" defaultValue={false} /></td>
                <td> <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.applicable)} type="boolean" defaultValue={true} /></td>
            </tr>
        );
    }

    getOperations(token: QueryToken): FilterOperation[] {
        var filterType = token.filterType;

        if (!filterType)
            return [];

        return filterOperations[filterType]
    }

    static getValueType(token: QueryToken): "string" | "boolean" | "number" | null {
        var tr = token.type;
        if (tr.isCollection || tr.isLite || tr.isEmbedded)
            return null;

        if (tr.name == "string" || tr.name == "Guid" || tr.name == "datetime")
            return "string";

        if (tr.name == "number")
            return "number";

        if (tr.name == "boolean")
            return "boolean";

        return null;
    }

    getNumColumns() { return 7; };

    newElement() {
        return {} as FilterOptionExpr;
    }
}

class OrderOptionsComponent extends BaseOptionsComponent<OrderOptionExpr> {

    renderTitle() {
        return "Orders";
    }

    renderHeader() {
        return (
            <tr>
                <th></th>
                <th>Column</th>
                <th>OrderType</th>
                <th>Applicable</th>
            </tr>
        );
    }

    handleColumnChange = (item: OrderOptionExpr, newToken: QueryToken | undefined) => {
        item.columnName = newToken && newToken.fullKey;;
        item.token = newToken;
        this.props.refreshView();
    }

    renderItem(item: OrderOptionExpr, index: number) {
        const dn = this.props.dn;
        return (
            <tr key={index}>
                <td>{this.renderButtons(index)}</td>
                <td> <QueryTokenBuilderString label="columnName" token={item.token} columnName={item.columnName}
                    onChange={newToken => this.handleColumnChange(item, newToken)} queryKey={this.props.queryKey} subTokenOptions={SubTokensOptions.CanElement} hideLabel={true} /></td>
                <td> {item.token && !item.token.type.isEmbedded && <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.orderType)} type="string" defaultValue={null} options={OrderType.values()} />}</td>
                <td> <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.applicable)} type="boolean" defaultValue={true} /></td>
            </tr>
        );
    }

    getNumColumns() { return 4; };

    newElement() {
        return {} as OrderOptionExpr;
    }
}

class ColumnOptionsComponent extends BaseOptionsComponent<ColumnOptionExpr> {

    renderTitle() {
        return "Columns";
    }

    renderHeader() {
        return (
            <tr>
                <th></th>
                <th>Column</th>
                <th>DisplayName</th>
                <th>Applicable</th>
            </tr>
        );
    }

    handleColumnChange = (item: ColumnOptionExpr, newToken: QueryToken | undefined) => {
        item.columnName = newToken && newToken.fullKey;
        item.token = newToken;
        this.props.refreshView();
    }
    
    renderItem(item: ColumnOptionExpr, index: number) {
        const dn = this.props.dn;
        return (
            <tr key={index}>
                <td>{this.renderButtons(index)}</td>
                <td> <QueryTokenBuilderString label="columnName" token={item.token} columnName={item.columnName}
                    onChange={newToken => this.handleColumnChange(item, newToken)}
                    queryKey={this.props.queryKey} subTokenOptions={SubTokensOptions.CanElement} hideLabel={true} /></td>
                <td> {item.token && <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.displayName)} type="string" defaultValue={null} />}</td>
                <td> <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.applicable)} type="boolean" defaultValue={true} /></td>
            </tr>
        );
    }

    getNumColumns() { return 4; };

    newElement() {
        return {} as ColumnOptionExpr;
    }
}

class PaginationComponent extends React.Component<{ findOptions: FindOptionsExpr, dn: DesignerNode<BaseNode>; refreshView: () => void }> {

    render() {
        const fo = this.props.findOptions;
        const dn = this.props.dn;
        const mode = fo.paginationMode;

        return (
            <fieldset>
                <legend>Pagination</legend>
                <ExpressionOrValueComponent dn={dn} refreshView={this.props.refreshView} binding={Binding.create(fo, f => f.paginationMode)} type="string" options={PaginationMode.values()} defaultValue={null} allowsExpression={false} />
                {(mode == "Firsts" || mode == "Paginate") &&
                    <ExpressionOrValueComponent dn={dn} refreshView={this.props.refreshView} binding={Binding.create(fo, f => f.elementsPerPage)} type="number" defaultValue={null} />}
                {(mode == "Paginate") &&
                    <ExpressionOrValueComponent dn={dn} refreshView={this.props.refreshView} binding={Binding.create(fo, f => f.currentPage)}  type="number" defaultValue={null} />}
            </fieldset>
        );
    }
}
