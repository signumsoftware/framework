import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions, QueryToken, filterOperations, OrderType, ColumnOptionsMode } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, getTypeInfo, isTypeEntity } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import Typeahead from '../../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import QueryTokenBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/QueryTokenBuilder'
import { ModifiableEntity, JavascriptMessage, EntityControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { QueryEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { FilterOperation, PaginationMode } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.DynamicQuery'
import { ExpressionOrValueComponent, FieldComponent } from './Designer'
import * as Nodes from './Nodes'
import * as NodeUtils from './NodeUtils'
import { BaseNode, SearchControlNode } from './Nodes'
import { DesignerNode } from './NodeUtils'
import { FindOptionsExpr, FilterOptionExpr, OrderOptionExpr, ColumnOptionExpr } from './FindOptionsExpression'

interface FindOptionsComponentProps {
    dn: DesignerNode<BaseNode>;
    findOptions: FindOptionsExpr;
}

export class FindOptionsComponent extends React.Component<FindOptionsComponentProps, void> {


    handleChangeQueryKey = (queryKey: string) => {
        const fo = this.props.findOptions;
        fo.queryKey = queryKey;
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
            <div className="form-sm filter-options">
                <QueryKeyLine queryKey={fo.queryKey} label="queryKey" onChange={this.handleChangeQueryKey} />

                {fo.queryKey &&
                    <div>
                        <div className="row">
                            <div className="col-sm-6">
                            <QueryTokenBuilderString label="parentColumn" token={fo.parentToken} columnName={fo.parentColumn} 
                                    onChange={this.handleChangeParentColumn} queryKey={fo.queryKey} subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                            </div>
                            <div className="col-sm-6">
                                {fo.parentToken &&
                                    <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="parentValue"
                                type={FilterOptionsComponent.getValueType(fo.parentToken)} defaultValue={null} />}
                            </div>
                        </div>


                        <FilterOptionsComponent object={fo} dn={dn} member="filters" queryKey={fo.queryKey} refreshView={() => this.forceUpdate()} />

                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="columnOptionsMode" type="string" options={ColumnOptionsMode.values()} defaultValue={"Add" as ColumnOptionsMode} />
                        <ColumnOptionsComponent object={fo} dn={dn} member="columns" queryKey={fo.queryKey} refreshView={() => this.forceUpdate()} />

                        <OrderOptionsComponent object={fo} dn={dn} member="orders" queryKey={fo.queryKey} refreshView={() => this.forceUpdate()} />
                        <PaginationComponent dn={dn} findOptions={fo} refreshView={() => this.forceUpdate()} />

                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="searchOnLoad" type="boolean" defaultValue={null} />
                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="showHeader" type="boolean" defaultValue={null} />
                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="showFilters" type="boolean" defaultValue={null} />
                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="showFilterButton" type="boolean" defaultValue={null} />
                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="showFooter" type="boolean" defaultValue={null} />
                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="allowChangeColumns" type="boolean" defaultValue={null} />
                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="create" type="boolean" defaultValue={null} />
                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="navigate" type="boolean" defaultValue={null} />
                        <ExpressionOrValueComponent object={fo} dn={dn} refreshView={() => this.forceUpdate()} member="contextMenu" type="boolean" defaultValue={null} />
                    </div>
                }
            </div>
        );
    }
}

export class QueryKeyLine extends React.Component<{ queryKey: string | undefined, label: string; onChange: (queryKey: string | undefined) => void }, void>{

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
                <span className="form-control btn-default sf-entity-line-entity">
                    {this.props.queryKey}

                </span>
                <span className="input-group-btn">
                    <a className={classes("sf-line-button", "sf-remove btn btn-default")}
                        onClick={() => this.props.onChange(undefined)}
                        title={EntityControlMessage.Remove.niceToString()}>
                        <span className="glyphicon glyphicon-remove" />
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

class QueryTokenBuilderString extends React.Component<QueryTokenBuilderStringProps, void>{

    componentWillMount() {
        this.loadInitialToken(this.props);
    }

    componentWillReceiveProps(newProps: QueryTokenBuilderStringProps) {
        this.loadInitialToken(newProps);
    }

    loadInitialToken(props: QueryTokenBuilderStringProps) {
        if (props.columnName && !props.token)
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
            subTokenOptions={SubTokensOptions.CanElement} />;

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
    object: any;
    member: string;
    dn: DesignerNode<BaseNode>;
    refreshView: () => void;
    queryKey: string;
}

abstract class BaseOptionsComponent<T> extends React.Component<BaseOptionsComponentProps<T>, void>{

    getArray(): Array<T> | undefined {
        return (this.props.object as any)[this.props.member] as Array<T> | undefined;
    }

    setArray(newArray: Array<T> | undefined): Array<T> | undefined {
        return (this.props.object as any)[this.props.member] = newArray;
    }

    handleOnRemove = (event: React.MouseEvent, index: number) => {
        event.preventDefault();
        var array = this.getArray() !;
        array.removeAt(index);
        if (array.length == 0)
            this.setArray(undefined);

        this.props.refreshView();
    }

    handleOnMoveUp = (event: React.MouseEvent, index: number) => {
        event.preventDefault();
        const list = this.getArray() !;
        if (index == 0)
            return;

        const entity = list[index]
        list.removeAt(index);
        list.insertAt(index - 1, entity);
        this.props.refreshView();
    }

    handleOnMoveDown = (event: React.MouseEvent, index: number) => {
        event.preventDefault();
        const list = this.getArray() !;
        if (index == list.length - 1)
            return;

        const entity = list[index]
        list.removeAt(index);
        list.insertAt(index + 1, entity);
        this.props.refreshView();
    }


    handleCreateClick = (event: React.SyntheticEvent) => {
        var array = this.getArray() || this.setArray([]);
        array!.push(this.newElement());
        this.props.refreshView();
    }


    renderButtons(index: number) {
        return (<div className="item-group">
            <a className={classes("sf-line-button", "sf-remove")}
                onClick={e => this.handleOnRemove(e, index)}
                title={EntityControlMessage.Remove.niceToString()}>
                <span className="glyphicon glyphicon-remove" />
            </a>

            <a className={classes("sf-line-button", "move-up")}
                onClick={e => this.handleOnMoveUp(e, index)}
                title={EntityControlMessage.MoveUp.niceToString()}>
                <span className="glyphicon glyphicon-chevron-up" />
            </a>

            <a className={classes("sf-line-button", "move-down")}
                onClick={e => this.handleOnMoveDown(e, index)}
                title={EntityControlMessage.MoveDown.niceToString()}>
                <span className="glyphicon glyphicon-chevron-down" />
            </a>
        </div>);
    }

    abstract renderTitle(): React.ReactNode;
    abstract renderHeader(): React.ReactElement<any>;
    abstract renderItem(item: T, index: number): React.ReactElement<any>;
    abstract getNumColumns(): number;
    abstract newElement(): T;


    render() {

        const array = this.getArray();

        return (<fieldset className="SF-table-field">
            <legend>
                {this.renderTitle()}
            </legend>
            <table className="table table-condensed form-vertical">
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
                                <span className="glyphicon glyphicon-plus" style={{ marginRight: "5px" }} />{EntityControlMessage.Create.niceToString()}
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
                <td> {item.token && <ExpressionOrValueComponent object={item} dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} member="operation" type="string" defaultValue={null} options={this.getOperations(item.token)} />}</td>
                <td> {item.token && <ExpressionOrValueComponent object={item} dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} member="value" type={FilterOptionsComponent.getValueType(item.token)} defaultValue={null} />}</td>
                <td> <ExpressionOrValueComponent object={item} dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} member="frozen" type="boolean" defaultValue={null} /></td>
                <td> <ExpressionOrValueComponent object={item} dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} member="applicable" type="boolean" defaultValue={null} /></td>
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
                <td> {item.token && !item.token.type.isEmbedded && <ExpressionOrValueComponent object={item} dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} member="orderType" type="string" defaultValue={null} options={OrderType.values()} />}</td>
                <td> <ExpressionOrValueComponent object={item} dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} member="applicable" type="boolean" defaultValue={null} /></td>
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
                <td> {item.token && <ExpressionOrValueComponent object={item} dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} member="displayName" type="string" defaultValue={null} />}</td>
                <td> <ExpressionOrValueComponent object={item} dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} member="applicable" type="boolean" defaultValue={null} /></td>
            </tr>
        );
    }

    getNumColumns() { return 4; };

    newElement() {
        return {} as ColumnOptionExpr;
    }
}

class PaginationComponent extends React.Component<{ findOptions: FindOptionsExpr, dn: DesignerNode<BaseNode>; refreshView: () => void }, void> {

    render() {
        const fo = this.props.findOptions;
        const dn = this.props.dn;
        const mode = fo.paginationMode;

        return (
            <fieldset>
                <legend>Pagination</legend>
                <ExpressionOrValueComponent object={fo} dn={dn} refreshView={this.props.refreshView} member="paginationMode" type="string" options={PaginationMode.values()} defaultValue={null} allowsExpression={false} />
                {(mode == "Firsts" || mode == "Paginate") &&
                    <ExpressionOrValueComponent object={fo} dn={dn} refreshView={this.props.refreshView} member="elementsPerPage" type="number" defaultValue={null} />}
                {(mode == "Paginate") &&
                    <ExpressionOrValueComponent object={fo} dn={dn} refreshView={this.props.refreshView} member="currentPage" type="number" defaultValue={null} />}
            </fieldset>
        );
    }
}
