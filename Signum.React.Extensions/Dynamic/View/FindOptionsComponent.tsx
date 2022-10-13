import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '@framework/Lines'
import { classes, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { QueryDescription, SubTokensOptions, QueryToken, filterOperations, OrderType, ColumnOptionsMode } from '@framework/FindOptions'
import { getQueryNiceName, getTypeInfo, isTypeEntity, Binding, getTypeInfos } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { Typeahead } from '@framework/Components'
import QueryTokenBuilder from '@framework/SearchControl/QueryTokenBuilder'
import { ModifiableEntity, JavascriptMessage, EntityControlMessage, getToString } from '@framework/Signum.Entities'
import { QueryEntity } from '@framework/Signum.Entities.Basics'
import { FilterOperation, PaginationMode } from '@framework/Signum.Entities.DynamicQuery'
import { ExpressionOrValueComponent, FieldComponent, DesignerModal } from './Designer'
import * as Nodes from './Nodes'
import * as NodeUtils from './NodeUtils'
import { DesignerNode, Expression } from './NodeUtils'
import { BaseNode, SearchControlNode } from './Nodes'
import { FindOptionsExpr, FilterOptionExpr, OrderOptionExpr, ColumnOptionExpr } from './FindOptionsExpression'
import * as DynamicViewClient from '../DynamicViewClient'
import { DynamicViewMessage, DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import SelectorModal from '@framework/SelectorModal';
import { tryGetTypeInfos } from '@framework/Reflection';
import { TypeInfo } from '@framework/Reflection';
import { useForceUpdate, useAPI } from '@framework/Hooks'

interface FindOptionsLineProps {
  binding: Binding<FindOptionsExpr | undefined>;
  dn: DesignerNode<BaseNode>;
  avoidSuggestion?: boolean;
  onQueryChanged?: () => void;
}

export function FindOptionsLine(p : FindOptionsLineProps){
  function renderMember(fo: FindOptionsExpr | undefined): React.ReactNode {
    return (<span
      className={fo === undefined ? "design-default" : "design-changed"}>
      {p.binding.member}
    </span>);
  }

  function handleRemove(e: React.MouseEvent<any>) {
    e.preventDefault();
    p.binding.deleteValue();
    p.dn.context.refreshView();
  }

  function handleCreate(e: React.MouseEvent<any>) {
    e.preventDefault();
    const route = p.dn.route;
    const ti = route?.typeReferenceInfo();

    const promise = p.avoidSuggestion == true || !ti || !isTypeEntity(ti) ? Promise.resolve({} as FindOptionsExpr) :
      DynamicViewClient.API.getSuggestedFindOptions(ti.name)
        .then(sfos => SelectorModal.chooseElement(sfos, {
          title: DynamicViewMessage.SuggestedFindOptions.niceToString(),
          message: DynamicViewMessage.TheFollowingQueriesReference0.niceToString().formatHtml(<strong>{ti.niceName}</strong>),
          buttonDisplay: sfo => <div><strong>{sfo.queryKey}</strong><br /><small>(by <code>{sfo.parentToken}</code>)</small></div>
        }))
        .then(sfo => ({
          queryName: sfo?.queryKey,
          filterOptions: [{ token: sfo?.parentToken, value: sfo && { __code__: "ctx.value" } as Expression<ModifiableEntity> }]
        } as FindOptionsExpr));

    promise.then(fo => modifyFindOptions(fo));
  }

  function handleView(e: React.MouseEvent<any>) {
    e.preventDefault();
    var fo = JSON.parse(JSON.stringify(p.binding.getValue())) as FindOptionsExpr;
    modifyFindOptions(fo);
  }

  function modifyFindOptions(fo: FindOptionsExpr) {
    DesignerModal.show("FindOptions", () => <FindOptionsComponent findOptions={fo} dn={p.dn} />).then(result => {
      if (result) {
        var oldFo = p.binding.getValue();
        p.binding.setValue(clean(fo));
        if (oldFo?.queryName != p.binding.getValue()?.queryName) {
          if (p.onQueryChanged)
            p.onQueryChanged();
        }
      }

      p.dn.context.refreshView();
    });
  }

  function clean(fo: FindOptionsExpr) {
    if (fo.filterOptions) fo.filterOptions.forEach(f => delete f.parsedToken);
    if (fo.orderOptions) fo.orderOptions.forEach(o => delete o.parsedToken);
    if (fo.columnOptions) fo.columnOptions.forEach(c => delete c.parsedToken);
    return fo;
  }


  function getDescription(fo: FindOptionsExpr) {
    var filters = [
      fo.parentToken,
      fo.filterOptions && fo.filterOptions.length && fo.filterOptions.length + " filters"]
      .filter(a => !!a).join(", ");

    return `${fo.queryName} (${filters || "No filter"})`.trim();
  }
  const fo = p.binding.getValue();

    return (
      <div className="form-group">
        <label className="control-label">
        {renderMember(fo)}
        </label>
        <div>
          {fo ? <div>
          <a href="#" onClick={handleView}>{getDescription(fo)}</a>
            {" "}
            <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={handleRemove}
              title={EntityControlMessage.Remove.niceToString()}>
              <FontAwesomeIcon icon="xmark" />
            </a></div> :
            <a href="#" title={EntityControlMessage.Create.niceToString()}
              className="sf-line-button sf-create"
            onClick={handleCreate}>
              <FontAwesomeIcon icon="plus" className="sf-create sf-create-label" />{EntityControlMessage.Create.niceToString()}
            </a>}
        </div>
      </div>
    );
}


interface QueryTokenLineProps {
  binding: Binding<string | undefined>;
  dn: DesignerNode<BaseNode>;
  subTokenOptions: SubTokensOptions;
  queryKey: string;
}

export function QueryTokenLine(p: QueryTokenLineProps) {

  const [parsedToken, setParsedToken] = React.useState<QueryToken | undefined>(undefined);


  function renderMember(token: string | undefined): React.ReactNode {
    return (
      <span className={token === undefined ? "design-default" : "design-changed"}>
        {p.binding.member}
      </span>
    );
  }

  function handleChange(qt: QueryToken | undefined) {
    setParsedToken(qt);
    if (qt?.fullKey != p.binding.getValue()) {
      if (qt)
        p.binding.setValue(qt.fullKey);
      else
        p.binding.deleteValue();

      p.dn.context.refreshView();
    }
  }

  function getDescription(fo: FindOptionsExpr) {
    var filters = [
      fo.parentToken,
      fo.filterOptions && fo.filterOptions.length && fo.filterOptions.length + " filters"]
      .filter(a => !!a).join(", ");

    return `${fo.queryName} (${filters || "No filter"})`.trim();
  }
  const token = p.binding.getValue();

    return (
      <div className="form-group">
        <label className="control-label">
        {renderMember(token)} {p.queryKey && <small>({getQueryNiceName(p.queryKey)})</small>}
        </label>
        <div>
        <QueryTokenBuilderString key={p.queryKey}
          queryKey={p.queryKey}
            token={token}
          subTokenOptions={p.subTokenOptions}
          parsedToken={parsedToken}
            hideLabel={true}
          onChange={handleChange}
            label=""
          />
        </div>
      </div>
    );
}



interface FetchQueryDescriptionProps {
  queryName?: string;
  children: (qd?: QueryDescription) => React.ReactElement<any>;
}

interface FetchQueryDescriptionState {
  queryDescription?: QueryDescription
}

export function FetchQueryDescription(p: FetchQueryDescriptionProps) {
  const queryDescription = useAPI(() => !p.queryName ? Promise.resolve(undefined) : Finder.getQueryDescription(p.queryName), [p.queryName]);
  return p.children(queryDescription);
}

interface ViewNameComponentProps {
  binding: Binding<any>;
  dn: DesignerNode<BaseNode>;
  typeName?: string;
}

export function ViewNameComponent(p: ViewNameComponentProps) {

  const viewNames = useAPI(() => p.typeName && !p.typeName.contains(", ") && !isTypeEntity(p.typeName) ? Promise.resolve(undefined) :
    Promise.all(getTypeInfos(p.typeName ?? "").map(ti => Navigator.viewDispatcher.getViewNames(ti.name).then(array => [...array, (hastStaticView(ti) ? "STATIC" : undefined)])))
      .then(arrays => [...(arrays.flatMap(a => a).filter(a => a != null) as string[]), "NEW"]), [p.typeName]);

  return <ExpressionOrValueComponent dn={p.dn} binding={p.binding} type="string" defaultValue={null} options={viewNames}
    exampleExpression={"e => modules.Navigator.viewDispatcher.getViewPromiseWithName(e, \"View Name\").withProps({ ... })"} />;
}

function hastStaticView(t: TypeInfo) {
  var es = Navigator.getSettings(t);
  return es != null && es.getViewPromise != null;
}

interface FindOptionsComponentProps {
  dn: DesignerNode<BaseNode>;
  findOptions: FindOptionsExpr;
}

export function FindOptionsComponent(p : FindOptionsComponentProps){
  const forceUpdate = useForceUpdate();
  function handleChangeQueryKey(queryKey: string | undefined) {
    const fo = p.findOptions;
    fo.queryName = queryKey;
    delete fo.parentToken;
    delete fo.filterOptions;
    delete fo.columnOptions;
    delete fo.orderOptions;
    forceUpdate();
  }


  var dn = p.dn;
  const fo = p.findOptions;
    return (
      <div className="form-sm filter-options code-container">
      <QueryKeyLine queryKey={fo.queryName} label="queryKey" onChange={handleChangeQueryKey} />

        {fo.queryName &&
          <div>
            <FilterOptionsComponent dn={dn} binding={Binding.create(fo, f => f.filterOptions)} queryKey={fo.queryName} refreshView={() => forceUpdate()} extraButtons={() =>
              <ExpressionOrValueComponent dn={dn} binding={Binding.create(fo, f => f.includeDefaultFilters)} refreshView={() => forceUpdate()} type="boolean" defaultValue={null} />
            } />
            <ColumnOptionsComponent dn={dn} binding={Binding.create(fo, f => f.columnOptions)} queryKey={fo.queryName} refreshView={() => forceUpdate()} extraButtons={() =>
              <ExpressionOrValueComponent dn={dn} binding={Binding.create(fo, f => f.columnOptionsMode)} refreshView={() => forceUpdate()} type="string" options={ColumnOptionsMode.values()} defaultValue={"Add" as ColumnOptionsMode} />
            } />

            <OrderOptionsComponent dn={dn} binding={Binding.create(fo, f => f.orderOptions)} queryKey={fo.queryName} refreshView={() => forceUpdate()} />
            <PaginationComponent dn={dn} findOptions={fo} refreshView={() => forceUpdate()} />
          </div>
        }
      </div>
    );
}

export function QueryKeyLine(p : { queryKey: string | undefined, label: string; onChange: (queryKey: string | undefined) => void }){
  function handleGetItems(query: string) {
    return Finder.API.findLiteLike({ types: QueryEntity.typeName, subString: query, count: 5 })
      .then(lites => lites.map(a => getToString(a)));
  }


  function renderLink() {
    return (
      <div className="input-group">
        <span className="form-control btn-light sf-entity-line-entity">
          {p.queryKey}
        </span>
        <a href="#" className={classes("sf-line-button", "sf-remove btn btn-light")}
          onClick={() => p.onChange(undefined)}
          title={EntityControlMessage.Remove.niceToString()}>
          <FontAwesomeIcon icon="xmark" />
        </a>
      </div>
    );
  }
  return (
    <div className="form-group">
      <label className="control-label">
        {p.label}
      </label>
      <div style={{ position: "relative" }}>
        {
          p.queryKey ? renderLink() :
            <Typeahead
              inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
              getItems={handleGetItems}
              onSelect={item => { p.onChange(item as string); return ""; }} />

        }
      </div>
    </div>
  );
}

interface QueryTokenBuilderStringProps {
  queryKey: string;
  token: string | undefined;
  parsedToken: QueryToken | undefined;
  label: string;
  onChange: (newToken: QueryToken | undefined) => void;
  subTokenOptions: SubTokensOptions;
  hideLabel?: boolean;
}

function QueryTokenBuilderString(p: QueryTokenBuilderStringProps) {

  React.useEffect(() => {

    if (p.parsedToken?.fullKey != p.token) {
      var promise = p.token == null ? Promise.resolve<QueryToken | undefined>(undefined) :
        Finder.parseSingleToken(p.queryKey, p.token, p.subTokenOptions);

      promise
        .then(t => p.onChange(t));
    }
  }, [p.queryKey, p.token]);

  var qt = <QueryTokenBuilder
    queryToken={p.parsedToken}
    queryKey={p.queryKey}
    onTokenChange={p.onChange}
    readOnly={false}
    subTokenOptions={p.subTokenOptions} />;

  if (p.hideLabel)
    return qt;

  return (
    <div className="form-group">
      <label className="control-label">
        {p.label}
      </label>
      <div>
        {qt}
      </div>
    </div>
  );
}

interface BaseOptionsComponentProps<T> {
  binding: Binding<Array<T> | undefined>;
  dn: DesignerNode<BaseNode>;
  extraButtons?: () => React.ReactNode;
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
    const list = this.props.binding.getValue()!;
    list.moveUp(index);
    this.props.refreshView();
  }

  handleOnMoveDown = (event: React.MouseEvent<any>, index: number) => {
    event.preventDefault();
    const list = this.props.binding.getValue()!;
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
        <FontAwesomeIcon icon="xmark" />
      </a>

      <a href="#" className={classes("sf-line-button", "move-up")}
        onClick={e => this.handleOnMoveUp(e, index)}
        title={EntityControlMessage.MoveUp.niceToString()}>
        <FontAwesomeIcon icon="chevron-up" />
      </a>

      <a href="#" className={classes("sf-line-button", "move-down")}
        onClick={e => this.handleOnMoveDown(e, index)}
        title={EntityControlMessage.MoveDown.niceToString()}>
        <FontAwesomeIcon icon="chevron-down" />
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

    return (<fieldset className="sf-table-field">
      <legend>
        {this.renderTitle()}
      </legend>
      <table className="table table-sm code-container">
        <thead>
          {this.renderHeader()}
        </thead>
        <tbody>
          {array?.map((item, i) => this.renderItem(item, i))}
          <tr>
            <td colSpan={this.getNumColumns()}>
              <a title={EntityControlMessage.Create.niceToString()}
                className="sf-line-button sf-create"
                onClick={this.handleCreateClick}>
                <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{EntityControlMessage.Create.niceToString()}
              </a>
              {this.props.extraButtons && <div className="mt-2">{this.props.extraButtons()}</div>}
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
    item.token = newToken?.fullKey;
    item.parsedToken = newToken;
    this.props.refreshView();
  }

  renderItem(item: FilterOptionExpr, index: number) {
    const dn = this.props.dn;
    return (
      <tr key={index}>
        <td>{this.renderButtons(index)}</td>
        <td> <QueryTokenBuilderString label="columnName" token={item.token} parsedToken={item.parsedToken} onChange={newToken => this.handleColumnChange(item, newToken)}
          queryKey={this.props.queryKey} subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} hideLabel={true} /></td>
        <td> {item.parsedToken && <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.operation)} type="string" defaultValue={null} options={this.getOperations(item.parsedToken)} />}</td>
        <td> {item.parsedToken && <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.value)} type={FilterOptionsComponent.getValueType(item.parsedToken)} defaultValue={null} />}</td>
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

    if (tr.name == "string" || tr.name == "Guid" || tr.name == "DateTime")
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
    item.token = newToken?.fullKey;;
    item.parsedToken = newToken;
    this.props.refreshView();
  }

  renderItem(item: OrderOptionExpr, index: number) {
    const dn = this.props.dn;
    return (
      <tr key={index}>
        <td>{this.renderButtons(index)}</td>
        <td> <QueryTokenBuilderString label="columnName" parsedToken={item.parsedToken} token={item.token}
          onChange={newToken => this.handleColumnChange(item, newToken)} queryKey={this.props.queryKey} subTokenOptions={SubTokensOptions.CanElement} hideLabel={true} /></td>
        <td> {item.parsedToken && !item.parsedToken.type.isEmbedded && <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.orderType)} type="string" defaultValue={null} options={OrderType.values()} />}</td>
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
    item.token = newToken?.fullKey;
    item.parsedToken = newToken;
    this.props.refreshView();
  }

  renderItem(item: ColumnOptionExpr, index: number) {
    const dn = this.props.dn;
    return (
      <tr key={index}>
        <td>{this.renderButtons(index)}</td>
        <td>
          <QueryTokenBuilderString label="columnName" parsedToken={item.parsedToken} token={item.token}
            onChange={newToken => this.handleColumnChange(item, newToken)}
            queryKey={this.props.queryKey} subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanOperation} hideLabel={true} />
        </td>
        <td> {item.parsedToken && <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.displayName)} type="string" defaultValue={null} />}</td>
        <td> <ExpressionOrValueComponent dn={dn} hideLabel={true} refreshView={() => this.forceUpdate()} binding={Binding.create(item, f => f.applicable)} type="boolean" defaultValue={true} /></td>
      </tr>
    );
  }

  getNumColumns() { return 4; };

  newElement() {
    return {} as ColumnOptionExpr;
  }
}

function PaginationComponent(p : { findOptions: FindOptionsExpr, dn: DesignerNode<BaseNode>; refreshView: () => void }){
  const fo = p.findOptions;
  const dn = p.dn;
    const mode = fo.paginationMode;

    return (
      <fieldset>
        <legend>Pagination</legend>
      <ExpressionOrValueComponent dn={dn} refreshView={p.refreshView} binding={Binding.create(fo, f => f.paginationMode)} type="string" options={PaginationMode.values()} defaultValue={null} allowsExpression={false} />
        {(mode == "Firsts" || mode == "Paginate") &&
        <ExpressionOrValueComponent dn={dn} refreshView={p.refreshView} binding={Binding.create(fo, f => f.elementsPerPage)} type="number" defaultValue={null} />}
        {(mode == "Paginate") &&
        <ExpressionOrValueComponent dn={dn} refreshView={p.refreshView} binding={Binding.create(fo, f => f.currentPage)} type="number" defaultValue={null} />}
      </fieldset>
    );
}
