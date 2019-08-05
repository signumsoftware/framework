import * as React from 'react'
import * as moment from 'moment'
import { Dic, areEqual, classes } from '../Globals'
import { FilterOptionParsed, QueryDescription, QueryToken, SubTokensOptions, filterOperations, isList, FilterOperation, FilterConditionOptionParsed, FilterGroupOptionParsed, isFilterGroupOptionParsed, hasAnyOrAll, getTokenParents, isPrefix, FilterConditionOption, PinnedFilter } from '../FindOptions'
import { SearchMessage } from '../Signum.Entities'
import { ValueLine, EntityLine, EntityCombo, StyleContext, FormControlReadonly } from '../Lines'
import { Binding, IsByAll, getTypeInfos, toMomentFormat } from '../Reflection'
import { TypeContext } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { FilterGroupOperation } from '../Signum.Entities.DynamicQuery';
import "./FilterBuilder.css"
import { NumericTextBox } from '../Lines/ValueLine';
import PinnedFilterBuilder from './PinnedFilterBuilder';
import { TitleManager } from '../Lines/EntityBase';

interface FilterBuilderProps {
  filterOptions: FilterOptionParsed[];
  subTokensOptions: SubTokensOptions;
  queryDescription: QueryDescription;
  onTokenChanged?: (token: QueryToken | undefined) => void;
  lastToken?: QueryToken;
  onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
  onHeightChanged?: () => void;
  readOnly?: boolean;
  title?: React.ReactNode;
  renderValue?: (fc: FilterConditionComponent | FilterGroupComponent) => React.ReactElement<any> | undefined;
  showPinnedFilters?: boolean;
}

export default class FilterBuilder extends React.Component<FilterBuilderProps>{
  handlerNewFilter = (e: React.MouseEvent<any>, isGroup: boolean) => {
    e.preventDefault();
    var lastToken = this.props.lastToken;

    this.props.filterOptions.push(isGroup ?
      {
        groupOperation: "Or",
        token: lastToken && hasAnyOrAll(lastToken) ? getTokenParents(lastToken).filter(a => a.queryTokenType == "AnyOrAll").lastOrNull() : undefined,
        filters: [],
        frozen: false,
        expanded: true,
      } as FilterGroupOptionParsed :
      {
        token: this.props.lastToken,
        operation: lastToken && (filterOperations[lastToken.filterType!] || []).firstOrNull() || undefined,
        value: undefined,
        frozen: false
      } as FilterConditionOptionParsed);

    if (this.props.onFiltersChanged)
      this.props.onFiltersChanged(this.props.filterOptions);

    this.forceUpdate(() => this.handleHeightChanged());
  };

  handlerDeleteFilter = (filter: FilterOptionParsed) => {
    this.props.filterOptions.remove(filter);
    if (this.props.onFiltersChanged)
      this.props.onFiltersChanged(this.props.filterOptions);
    this.forceUpdate(() => this.handleHeightChanged());
  };

  handleFilterChanged = () => {
    if (this.props.onFiltersChanged)
      this.props.onFiltersChanged(this.props.filterOptions);
  };

  handleHeightChanged = () => {
    if (this.props.onHeightChanged)
      this.props.onHeightChanged();
  }

  render() {


    return (
      <fieldset className="form-xs">
        {this.props.title && <legend>{this.props.title}</legend>}
        <div className="sf-filters-list table-responsive" style={{ overflowX: "visible" }}>
          <table className="table-sm">
            <thead>
              <tr>
                <th style={{ minWidth: "24px" }}></th>
                <th>{SearchMessage.Field.niceToString()}</th>
                <th>{SearchMessage.Operation.niceToString()}</th>
                <th style={{ paddingRight: "20px" }}>{SearchMessage.Value.niceToString()}</th>
                {this.props.showPinnedFilters && <th></th>}
              </tr>
            </thead>
            <tbody>
              {this.props.filterOptions.map((f, i) => isFilterGroupOptionParsed(f) ?
                <FilterGroupComponent key={i} filterGroup={f} readOnly={Boolean(this.props.readOnly)} onDeleteFilter={this.handlerDeleteFilter}
                  prefixToken={undefined}
                  subTokensOptions={this.props.subTokensOptions} queryDescription={this.props.queryDescription}
                  onTokenChanged={this.props.onTokenChanged} onFilterChanged={this.handleFilterChanged}
                  lastToken={this.props.lastToken} onHeightChanged={this.handleHeightChanged} renderValue={this.props.renderValue}
                  showPinnedFilters={this.props.showPinnedFilters || false} disableValue={false} /> :
                <FilterConditionComponent key={i} filter={f} readOnly={Boolean(this.props.readOnly)} onDeleteFilter={this.handlerDeleteFilter}
                  prefixToken={undefined}
                  subTokensOptions={this.props.subTokensOptions} queryDescription={this.props.queryDescription}
                  onTokenChanged={this.props.onTokenChanged} onFilterChanged={this.handleFilterChanged} renderValue={this.props.renderValue}
                  showPinnedFilters={this.props.showPinnedFilters || false} disableValue={false} />
              )}
              {!this.props.readOnly &&
                <tr className="sf-filter-create">
                <td colSpan={4}>
                  <a href="#" title={TitleManager.useTitle ? SearchMessage.AddFilter.niceToString() : undefined}
                    className="sf-line-button sf-create sf-create-condition"
                      onClick={e => this.handlerNewFilter(e, false)}>
                      <FontAwesomeIcon icon="plus" className="sf-create mr-1" />{SearchMessage.AddFilter.niceToString()}
                  </a>
                  <a href="#" title={TitleManager.useTitle ? SearchMessage.AddGroup.niceToString() : undefined}
                      className="sf-line-button sf-create sf-create-group ml-3"
                      onClick={e => this.handlerNewFilter(e, true)}>
                      <FontAwesomeIcon icon="plus" className="sf-create mr-1" />{SearchMessage.AddGroup.niceToString()}
                    </a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </fieldset>
    );
  }
}

export interface FilterGroupComponentsProps extends React.Props<FilterConditionComponent> {
  prefixToken: QueryToken | undefined;
  filterGroup: FilterGroupOptionParsed;
  readOnly: boolean;
  onDeleteFilter: (fo: FilterGroupOptionParsed) => void;
  queryDescription: QueryDescription;
  subTokensOptions: SubTokensOptions;
  onTokenChanged?: (token: QueryToken | undefined) => void;
  onFilterChanged: () => void;
  onHeightChanged: () => void;
  lastToken: QueryToken | undefined;
  renderValue?: (fc: FilterConditionComponent | FilterGroupComponent) => React.ReactElement<any> | undefined;
  showPinnedFilters: boolean;
  disableValue: boolean;
}


export class FilterGroupComponent extends React.Component<FilterGroupComponentsProps>{

  handleDeleteFilter = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    this.props.onDeleteFilter(this.props.filterGroup);
  }

  handleTokenChanged = (newToken: QueryToken | null | undefined) => {

    const f = this.props.filterGroup;
    f.token = newToken || undefined;
    if (this.props.onTokenChanged)
      this.props.onTokenChanged(newToken || undefined);
    this.props.onFilterChanged();
    this.forceUpdate();
  }

  handleChangeOperation = (event: React.FormEvent<HTMLSelectElement>) => {
    const operation = (event.currentTarget as HTMLSelectElement).value as FilterGroupOperation;

    this.props.filterGroup.groupOperation = operation;

    this.props.onFilterChanged();

    this.forceUpdate();
  }

  handlerDeleteFilter = (filter: FilterOptionParsed) => {
    this.props.filterGroup.filters.remove(filter);
    if (this.props.onFilterChanged)
      this.props.onFilterChanged();
    this.forceUpdate(() => this.props.onHeightChanged());
  };



  handlerNewFilter = (e: React.MouseEvent<any>, isGroup: boolean) => {

    e.preventDefault();

    let lastToken = this.props.lastToken;
    if (!lastToken || this.props.filterGroup.token && !isPrefix(this.props.filterGroup.token, lastToken))
      lastToken = this.props.filterGroup.token;

    this.props.filterGroup.filters.push(isGroup ?
      {
        groupOperation: this.props.filterGroup.groupOperation == "And" ? "Or" : "And",
        token: lastToken && hasAnyOrAll(lastToken) ? getTokenParents(lastToken).filter(a => a.queryTokenType == "AnyOrAll").lastOrNull() : this.props.prefixToken,
        filters: [],
        frozen: false,
        expanded: true,
      } as FilterGroupOptionParsed :
      {
        token: lastToken,
        operation: lastToken && (filterOperations[lastToken.filterType!] || []).firstOrNull() || undefined,
        value: undefined,
        frozen: false
      } as FilterConditionOptionParsed);


    this.props.onFilterChanged();

    this.forceUpdate(() => this.props.onHeightChanged());
  };

  handleExpandCollapse = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    const fg = this.props.filterGroup;
    fg.expanded = !fg.expanded;

    this.forceUpdate(() => this.props.onHeightChanged());
  }

  render() {
    const fg = this.props.filterGroup;

    const readOnly = fg.frozen || this.props.readOnly;

    return (
      <tr className="sf-filter-group">
        <td style={{ verticalAlign: "top" }}>
          {!readOnly &&
            <a href="#" title={TitleManager.useTitle ? SearchMessage.DeleteFilter.niceToString() : undefined}
              className="sf-line-button sf-remove"
              onClick={this.handleDeleteFilter}>
              <FontAwesomeIcon icon="times" />
            </a>}
        </td>
        <td colSpan={3} style={{ backgroundColor: fg.groupOperation == "Or" ? "#eee" : "#fff", border: "1px solid #ddd" }}>
          <div className="justify-content-between d-flex" >
            <div className="form-inline">
              <a href="#" onClick={this.handleExpandCollapse} className={classes(fg.expanded ? "sf-hide-group-button" : "sf-show-group-button", "mx-2")} >
                <FontAwesomeIcon icon={fg.expanded ? ["far", "minus-square"] : ["far", "plus-square"]} className="mr-2" />
              </a>
              <label>Group:</label>
              <select className="form-control form-control-xs sf-group-selector mx-2" value={fg.groupOperation as any} disabled={readOnly} onChange={this.handleChangeOperation}>
                {FilterGroupOperation.values().map((ft, i) => <option key={i} value={ft as any}>{FilterGroupOperation.niceToString(ft)}</option>)}
              </select>
            </div>

            <div className="form-inline">
              <label>Prefix:</label>
              <div className={classes("rw-widget-xs mx-2", fg.token == null ? "hidden" : undefined)}>
                <QueryTokenBuilder
                  prefixQueryToken={this.props.prefixToken}
                  queryToken={fg.token}
                  onTokenChange={this.handleTokenChanged}
                  queryKey={this.props.queryDescription.queryKey}
                  subTokenOptions={this.props.subTokensOptions}
                  readOnly={readOnly} />
              </div>
            </div>
            {fg.pinned &&
              <div>
                {(this.props.renderValue ? this.props.renderValue(this) : this.renderValue())}
              </div>
            }
            <div>
              {this.props.showPinnedFilters &&
                <button className={classes("btn", "btn-link", "btn-sm", "sf-user-filter", fg.pinned && "active")} onClick={e => { fg.pinned = fg.pinned ? undefined : {}; this.changeFilter(); }} disabled={this.props.readOnly}>
                  <FontAwesomeIcon color="orange" icon={[fg.pinned ? "fas" : "far", "star"]} />
                </button>
              }
            </div>
          </div>
          <div className="sf-filters-list table-responsive" style={{ overflowX: "visible" }}>
            <table className="table-sm" style={{ width: "100%" }}>
              <thead>
                {fg.pinned && <PinnedFilterEditor pinned={fg.pinned} onChange={() => this.changeFilter()} readonly={readOnly} />}
                {fg.expanded && <tr>
                  <th style={{ minWidth: "24px" }}></th>
                  <th>{SearchMessage.Field.niceToString()}</th>
                  <th>{SearchMessage.Operation.niceToString()}</th>
                  <th style={{ paddingRight: "20px" }}>{SearchMessage.Value.niceToString()}</th>
                </tr>
                }
              </thead>
              {fg.expanded ?
                <tbody>
                  {fg.filters.map((f, i) => isFilterGroupOptionParsed(f) ?

                    <FilterGroupComponent key={i} filterGroup={f} readOnly={Boolean(this.props.readOnly)} onDeleteFilter={this.handlerDeleteFilter}
                      prefixToken={fg.token}
                      subTokensOptions={this.props.subTokensOptions} queryDescription={this.props.queryDescription}
                      onTokenChanged={this.props.onTokenChanged} onFilterChanged={this.props.onFilterChanged}
                      lastToken={this.props.lastToken} onHeightChanged={this.props.onHeightChanged} renderValue={this.props.renderValue}
                      showPinnedFilters={this.props.showPinnedFilters}
                      disableValue={this.props.disableValue || Boolean(fg.pinned)}
                    /> :

                    <FilterConditionComponent key={i} filter={f} readOnly={Boolean(this.props.readOnly)} onDeleteFilter={this.handlerDeleteFilter}
                      prefixToken={fg.token}
                      subTokensOptions={this.props.subTokensOptions} queryDescription={this.props.queryDescription}
                      onTokenChanged={this.props.onTokenChanged} onFilterChanged={this.props.onFilterChanged} renderValue={this.props.renderValue}
                      showPinnedFilters={this.props.showPinnedFilters}
                      disableValue={this.props.disableValue || Boolean(fg.pinned)}
                    />
                  )}
                  {!this.props.readOnly &&
                    <tr className="sf-filter-create">
                      <td colSpan={4}>
                        <a href="#" title={TitleManager.useTitle ? SearchMessage.AddFilter.niceToString() : undefined}
                          className="sf-line-button sf-create"
                          onClick={e => this.handlerNewFilter(e, false)}>
                          <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{SearchMessage.AddFilter.niceToString()}
                        </a>

                        <a href="#" title={TitleManager.useTitle ? SearchMessage.AddGroup.niceToString() : undefined}
                          className="sf-line-button sf-create ml-3"
                          onClick={e => this.handlerNewFilter(e, true)}>
                          <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{SearchMessage.AddGroup.niceToString()}
                        </a>
                      </td>
                    </tr>
                  }
                </tbody> :
                <tbody>
                  <tr>
                    <td colSpan={4} style={{ color: "#aaa", textAlign: "center", fontSize: "smaller" }}> {SearchMessage._0FiltersCollapsed.niceToString(fg.filters.length)}</td>
                  </tr>
                </tbody>
              }
            </table>
          </div>
        </td>
      </tr>
    );
  }

  renderValue() {

    const f = this.props.filterGroup;

    const readOnly = this.props.readOnly || f.frozen;
    
    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(f, a => a.value));

    return <ValueLine ctx={ctx} type={{ name: "string" }} onChange={() => this.handleValueChange()} />

  }

  handleValueChange = () => {
    this.props.onFilterChanged();
  }

  changeFilter() {
    this.forceUpdate();
    this.props.onFilterChanged();
  }
}

export interface FilterConditionComponentProps extends React.Props<FilterConditionComponent> {
  filter: FilterConditionOptionParsed;
  prefixToken: QueryToken | undefined;
  readOnly: boolean;
  onDeleteFilter: (fo: FilterConditionOptionParsed) => void;
  queryDescription: QueryDescription;
  subTokensOptions: SubTokensOptions;
  onTokenChanged?: (token: QueryToken | undefined) => void;
  onFilterChanged: () => void;
  renderValue?: (fc: FilterConditionComponent | FilterGroupComponent) => React.ReactElement<any> | undefined;
  showPinnedFilters: boolean;
  disableValue: boolean;
}

export class FilterConditionComponent extends React.Component<FilterConditionComponentProps>{

  handleDeleteFilter = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    this.props.onDeleteFilter(this.props.filter);
  }

  handleTokenChanged = (newToken: QueryToken | null | undefined) => {

    const f = this.props.filter;

    if (newToken == undefined) {
      f.operation = undefined;
      f.value = undefined;
    }
    else {

      if (!areEqual(f.token, newToken, a => a.filterType) || !areEqual(f.token, newToken, a => a.preferEquals)) {
        f.operation = newToken.preferEquals ? "EqualTo" : newToken.filterType && filterOperations[newToken.filterType].first();
        f.value = f.operation && isList(f.operation) ? [undefined] : undefined;
      }
      else if (f.token && f.token.filterType == "DateTime" && newToken.filterType == "DateTime" && newToken.format && f.token.format != newToken.format) {
        f.value = f.value && this.trimDateToFormat(f.value, toMomentFormat(newToken.format));
      }
    }
    f.token = newToken || undefined;

    if (this.props.onTokenChanged)
      this.props.onTokenChanged(newToken || undefined);

    this.props.onFilterChanged();

    this.forceUpdate();
  }

  trimDateToFormat(date: string, momentFormat: string | undefined) {

    if (!momentFormat)
      return date;

    const formatted = moment(date).format(momentFormat);
    return moment(formatted, momentFormat).format();
  }


  handleChangeOperation = (event: React.FormEvent<HTMLSelectElement>) => {
    const operation = (event.currentTarget as HTMLSelectElement).value as FilterOperation;
    if (isList(operation) != isList(this.props.filter.operation!))
      this.props.filter.value = isList(operation) ? [this.props.filter.value] : this.props.filter.value[0];

    this.props.filter.operation = operation;

    this.props.onFilterChanged();

    this.forceUpdate();
  }

  render() {
    const f = this.props.filter;

    const readOnly = f.frozen || this.props.readOnly;

    return (
      <>
        <tr className="sf-filter-condition">
          <td>
            {!readOnly &&
              <a href="#" title={TitleManager.useTitle ? SearchMessage.DeleteFilter.niceToString() : undefined}
                className="sf-line-button sf-remove"
                onClick={this.handleDeleteFilter}>
                <FontAwesomeIcon icon="times" />
              </a>}
          </td>
          <td>
            <div className="rw-widget-xs">
              <QueryTokenBuilder
                prefixQueryToken={this.props.prefixToken}
                queryToken={f.token}
                onTokenChange={this.handleTokenChanged}
                queryKey={this.props.queryDescription.queryKey}
                subTokenOptions={this.props.subTokensOptions}
                readOnly={readOnly} />
            </div>
          </td>
          <td className="sf-filter-operation">
            {f.token && f.token.filterType && f.operation &&
              <select className="form-control form-control-xs" value={f.operation} disabled={readOnly} onChange={this.handleChangeOperation}>
                {f.token.filterType && filterOperations[f.token.filterType!]
                  .map((ft, i) => <option key={i} value={ft as any}>{FilterOperation.niceToString(ft)}</option>)}
              </select>}
          </td>

          <td className="sf-filter-value">
            {this.props.disableValue ? <small className="text-muted">{SearchMessage.ParentValue.niceToString()}</small> :
              f.token && f.token.filterType && f.operation && (this.props.renderValue ? this.props.renderValue(this) : this.renderValue())}
          </td>
          {f.token && f.token.filterType && f.operation && this.props.showPinnedFilters &&
            <td>
            <button className={classes("btn", "btn-link", "btn-sm", "sf-user-filter", f.pinned && "active")} onClick={e => { f.pinned = f.pinned ? undefined : {}; this.changeFilter(); }} disabled={this.props.readOnly}>
                <FontAwesomeIcon color="orange" icon={[f.pinned ? "fas" : "far", "star"]} />
              </button>
            </td>
          }
        </tr>
        {this.props.showPinnedFilters && f.pinned && <PinnedFilterEditor pinned={f.pinned} onChange={() => this.changeFilter()} readonly={readOnly} />}
      </>
    );
  }



  changeFilter() {
    this.forceUpdate();
    this.props.onFilterChanged();
  }


  renderValue() {

    const f = this.props.filter;

    const readOnly = this.props.readOnly || f.frozen;

    if (isList(f.operation!))
      return <MultiValue values={f.value} onRenderItem={ctx => createFilterValueControl(ctx, f.token!, this.handleValueChange)} readOnly={readOnly} onChange={this.handleValueChange} />;

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(f, a => a.value));

    return createFilterValueControl(ctx, f.token!, this.handleValueChange);
  }


  handleValueChange = () => {
    this.props.onFilterChanged();
  }
}


interface PinnedFilterEditorProps {
  pinned: PinnedFilter;
  readonly: boolean;
  onChange: () => void;
}

export class PinnedFilterEditor extends React.Component<PinnedFilterEditorProps> {
  render() {
    var p = this.props.pinned;
    return (
      <tr className="sf-pinned-filter" style={{ backgroundColor: "#fff6e6", verticalAlign: "top" }}>
        <td></td>
        <td>
          <div>
            <input type="text" className="form-control form-control-xs" placeholder={SearchMessage.Label.niceToString()} readOnly={this.props.readonly}
              value={p.label || ""}
              onChange={e => { p!.label = e.currentTarget.value; this.props.onChange(); }} />
          </div>
        </td>
        <td>
          <div className="input-group input-group-xs">
            {this.numericTextBox(Binding.create(p, _ => _.column), SearchMessage.Column.niceToString())}
            {this.numericTextBox(Binding.create(p, _ => _.row), SearchMessage.Row.niceToString())}
          </div>
        </td>
        <td colSpan={2}>
          <div className="btn-group btn-group-xs" role="group" aria-label="Basic example" style={{ verticalAlign: "unset" }}>
            {this.renderButton(Binding.create(p, a => a.splitText), "SplitText", "To enable google-like search")}
            {this.renderButton(Binding.create(p, a => a.disableOnNull), "DisableNull", "Disables the filter when no value is selected")}
          </div>
        </td>
      </tr>
    );
  }

  numericTextBox(binding: Binding<number | undefined>, title: string) {

    var val = binding.getValue();
    if (this.props.readonly)
      return <span className="numeric form-control form-control-xs" style={{ width: "60px" }}>{val}</span>;

    return (
      <NumericTextBox value={val == undefined ? null : val} onChange={n => { binding.setValue(n == null ? undefined : n); this.props.onChange(); }}
        validateKey={ValueLine.isNumber} formControlClass="form-control form-control-xs" htmlAttributes={{ placeholder: title, style: { width: "60px" } }} />
    );
  }

  renderButton(binding: Binding<boolean | undefined>, label: string, title: string) {
    return (
      <button type="button" className={classes("btn btn-light", binding.getValue() && "active")} disabled={this.props.readonly}
        onClick={e => { binding.setValue(!binding.getValue()); this.props.onChange(); }}
        title={TitleManager.useTitle ? title : undefined}>
        {label}
      </button>
    );
  }
}

export function createFilterValueControl(ctx: TypeContext<any>, token: QueryToken, handleValueChange: () => void, labelText?: string, forceNullable?: boolean): React.ReactElement<any> {

  var tokenType = token.type;
  if (forceNullable)
    tokenType = { ...tokenType, isNotNullable: false };

  switch (token.filterType) {
    case "Lite":
      if (tokenType.name == IsByAll || getTypeInfos(tokenType).some(ti => !ti.isLowPopulation))
        return <EntityLine ctx={ctx} type={tokenType} create={false} onChange={handleValueChange} labelText={labelText} />;
      else
        return <EntityCombo ctx={ctx} type={tokenType} create={false} onChange={handleValueChange} labelText={labelText} />
    case "Embedded":
      return <EntityLine ctx={ctx} type={tokenType} create={false} autocomplete={null} onChange={handleValueChange} labelText={labelText} />;
    case "Enum":
      const ti = getTypeInfos(tokenType).single();
      if (!ti)
        throw new Error(`EnumType ${tokenType.name} not found`);
      const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
      return <ValueLine ctx={ctx} type={tokenType} formatText={token.format} unitText={token.unit} comboBoxItems={members} onChange={handleValueChange} labelText={labelText} />;
    default:
      return <ValueLine ctx={ctx} type={tokenType} formatText={token.format} unitText={token.unit} onChange={handleValueChange} labelText={labelText} />;
  }
}



export interface MultiValueProps {
  values: any[],
  onRenderItem: (ctx: TypeContext<any>) => React.ReactElement<any>;
  readOnly: boolean;
  onChange: () => void;
}

export class MultiValue extends React.Component<MultiValueProps> {

  handleDeleteValue = (e: React.MouseEvent<any>, index: number) => {
    e.preventDefault();
    this.props.values.removeAt(index);
    this.props.onChange();
    this.forceUpdate();
  }

  handleAddValue = (e: React.MouseEvent<any>) => {

    e.preventDefault();

    this.props.values.push(undefined);
    this.props.onChange();
    this.forceUpdate();
  }

  render() {
    return (
      <table style={{ marginBottom: "0px" }} className="sf-multi-value">
        <tbody>
          {
            this.props.values.map((v, i) =>
              <tr key={i}>
                <td>
                  {!this.props.readOnly &&
                    <a href="#" title={TitleManager.useTitle ? SearchMessage.DeleteFilter.niceToString() : undefined}
                      className="sf-line-button sf-remove"
                      onClick={e => this.handleDeleteValue(e, i)}>
                      <FontAwesomeIcon icon="times" />
                    </a>}
                </td>
                <td>
                  {
                    this.props.onRenderItem(new TypeContext<any>(undefined,
                      {
                        formGroupStyle: "None",
                        formSize: "ExtraSmall",
                        readOnly: this.props.readOnly
                      }, undefined as any, new Binding<any>(this.props.values, i)))
                  }
                </td>
              </tr>)
          }
          <tr >
            <td colSpan={4}>
              {!this.props.readOnly &&
                <a href="#" title={TitleManager.useTitle ? SearchMessage.AddValue.niceToString() : undefined}
                  className="sf-line-button sf-create"
                  onClick={this.handleAddValue}>
                  <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{SearchMessage.AddValue.niceToString()}
                </a>}
            </td>
          </tr>
        </tbody>
      </table>
    );
  }
}

