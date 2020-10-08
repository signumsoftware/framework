import * as React from 'react'
import { DateTime } from 'luxon'
import { Dic, areEqual, classes } from '../Globals'
import { FilterOptionParsed, QueryDescription, QueryToken, SubTokensOptions, filterOperations, isList, FilterOperation, FilterConditionOptionParsed, FilterGroupOptionParsed, isFilterGroupOptionParsed, hasAnyOrAll, getTokenParents, isPrefix, FilterConditionOption, PinnedFilter, PinnedFilterParsed } from '../FindOptions'
import { SearchMessage } from '../Signum.Entities'
import { isNumber } from '../Lines/ValueLine'
import { ValueLine, EntityLine, EntityCombo, StyleContext, FormControlReadonly } from '../Lines'
import { Binding, IsByAll, tryGetTypeInfos, toLuxonFormat, getTypeInfos, toNumberFormat } from '../Reflection'
import { TypeContext } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { FilterGroupOperation, PinnedFilterActive } from '../Signum.Entities.DynamicQuery';
import "./FilterBuilder.css"
import { NumericTextBox } from '../Lines/ValueLine';
import PinnedFilterBuilder from './PinnedFilterBuilder';
import { useStateWithPromise, useForceUpdate, useForceUpdatePromise } from '../Hooks'
import { Dropdown } from 'react-bootstrap'

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
  renderValue?: (rvc: RenderValueContext) => React.ReactElement<any> | undefined;
  showPinnedFilters?: boolean;
}

export default function FilterBuilder(p: FilterBuilderProps) {

  const forceUpdate = useForceUpdatePromise();

  function handlerNewFilter(e: React.MouseEvent<any>, isGroup: boolean) {
    e.preventDefault();
    var lastToken = p.lastToken;

    p.filterOptions.push(isGroup ?
      {
        groupOperation: "Or",
        token: lastToken && hasAnyOrAll(lastToken) ? getTokenParents(lastToken).filter(a => a.queryTokenType == "AnyOrAll").lastOrNull() : undefined,
        filters: [],
        frozen: false,
        expanded: true,
      } as FilterGroupOptionParsed :
      {
        token: p.lastToken,
        operation: (lastToken && (filterOperations[lastToken.filterType!] ?? []).firstOrNull()) ?? undefined,
        value: undefined,
        frozen: false
      } as FilterConditionOptionParsed);

    if (p.onFiltersChanged)
      p.onFiltersChanged(p.filterOptions);

    forceUpdate().then(handleHeightChanged).done();
  };

  function handlerDeleteFilter(filter: FilterOptionParsed) {
    p.filterOptions.remove(filter);
    if (p.onFiltersChanged)
      p.onFiltersChanged(p.filterOptions);
    forceUpdate().then(handleHeightChanged).done();
  };

  function handleDeleteAllFilters(e: React.MouseEvent) {
    e.preventDefault();
    p.filterOptions.clear();
    if (p.onFiltersChanged)
      p.onFiltersChanged(p.filterOptions);
    forceUpdate().then(handleHeightChanged).done();
  };

  function handleFilterChanged() {
    if (p.onFiltersChanged)
      p.onFiltersChanged(p.filterOptions);
  };

  function handleHeightChanged() {
    if (p.onHeightChanged)
      p.onHeightChanged();
  }


  return (
    <fieldset className="form-xs">
      {p.title && <legend>{p.title}</legend>}
      <div className="sf-filters-list table-responsive" style={{ overflowX: "visible" }}>
        <table className="table-sm">
          <thead>
            <tr>
              <th style={{ minWidth: "24px" }}>{!p.readOnly && p.filterOptions.length > 0 &&
                <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.DeleteAllFilter.niceToString() : undefined}
                  className="sf-line-button sf-remove"
                  onClick={handleDeleteAllFilters}>
                  <FontAwesomeIcon icon="times" />
                </a>}</th>
              <th>{SearchMessage.Field.niceToString()}</th>
              <th>{SearchMessage.Operation.niceToString()}</th>
              <th style={{ paddingRight: "20px" }}>{SearchMessage.Value.niceToString()}</th>
              {p.showPinnedFilters && <th></th>}
            </tr>
          </thead>
          <tbody>
            {p.filterOptions.map((f, i) => isFilterGroupOptionParsed(f) ?
              <FilterGroupComponent key={i} filterGroup={f} readOnly={Boolean(p.readOnly)} onDeleteFilter={handlerDeleteFilter}
                prefixToken={undefined}
                subTokensOptions={p.subTokensOptions} queryDescription={p.queryDescription}
                onTokenChanged={p.onTokenChanged} onFilterChanged={handleFilterChanged}
                lastToken={p.lastToken} onHeightChanged={handleHeightChanged} renderValue={p.renderValue}
                showPinnedFilters={p.showPinnedFilters || false} disableValue={false} /> :
              <FilterConditionComponent key={i} filter={f} readOnly={Boolean(p.readOnly)} onDeleteFilter={handlerDeleteFilter}
                prefixToken={undefined}
                subTokensOptions={p.subTokensOptions} queryDescription={p.queryDescription}
                onTokenChanged={p.onTokenChanged} onFilterChanged={handleFilterChanged} renderValue={p.renderValue}
                showPinnedFilters={p.showPinnedFilters || false} disableValue={false} />
            )}
            {!p.readOnly &&
              <tr className="sf-filter-create">
                <td colSpan={4}>
                  <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.AddFilter.niceToString() : undefined}
                    className="sf-line-button sf-create sf-create-condition"
                    onClick={e => handlerNewFilter(e, false)}>
                    <FontAwesomeIcon icon="plus" className="sf-create mr-1" />{SearchMessage.AddFilter.niceToString()}
                  </a>
                  <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.AddGroup.niceToString() : undefined}
                    className="sf-line-button sf-create sf-create-group ml-3"
                    onClick={e => handlerNewFilter(e, true)}>
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

export interface RenderValueContext {
  filter: FilterConditionOptionParsed | FilterGroupOptionParsed;
  readonly: boolean;
  handleValueChange: () => void;
}

export interface FilterGroupComponentsProps {
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
  renderValue?: (rvc: RenderValueContext) => React.ReactElement<any> | undefined;
  showPinnedFilters: boolean;
  disableValue: boolean;
}

export function FilterGroupComponent(p: FilterGroupComponentsProps) {

  const forceUpdate = useForceUpdate();
  const forceUpdatePromise = useForceUpdatePromise();

  function handleDeleteFilter(e: React.MouseEvent<any>) {
    e.preventDefault();
    p.onDeleteFilter(p.filterGroup);
  }

  function handleTokenChanged(newToken: QueryToken | null | undefined) {

    const f = p.filterGroup;
    f.token = newToken ?? undefined;
    if (p.onTokenChanged)
      p.onTokenChanged(newToken ?? undefined);
    p.onFilterChanged();
    forceUpdate();
  }

  function handleChangeOperation(e: React.FormEvent<HTMLSelectElement>) {
    const operation = (e.currentTarget as HTMLSelectElement).value as FilterGroupOperation;

    p.filterGroup.groupOperation = operation;

    p.onFilterChanged();

    forceUpdate();
  }

  function handlerDeleteFilter(filter: FilterOptionParsed) {
    p.filterGroup.filters.remove(filter);
    if (p.onFilterChanged)
      p.onFilterChanged();
    forceUpdatePromise().then(() => p.onHeightChanged()).done();
  };



  function handlerNewFilter(e: React.MouseEvent<any>, isGroup: boolean) {

    e.preventDefault();

    let lastToken = p.lastToken;
    if (!lastToken || p.filterGroup.token && !isPrefix(p.filterGroup.token, lastToken))
      lastToken = p.filterGroup.token;

    p.filterGroup.filters.push(isGroup ?
      {
        groupOperation: p.filterGroup.groupOperation == "And" ? "Or" : "And",
        token: lastToken && hasAnyOrAll(lastToken) ? getTokenParents(lastToken).filter(a => a.queryTokenType == "AnyOrAll").lastOrNull() : p.prefixToken,
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


    p.onFilterChanged();

    forceUpdatePromise().then(() => p.onHeightChanged()).done();
  };

  function handleExpandCollapse(e: React.MouseEvent<any>) {
    e.preventDefault();
    const fg = p.filterGroup;
    fg.expanded = !fg.expanded;

    forceUpdatePromise().then(() => p.onHeightChanged()).done();
  }

  const fg = p.filterGroup;

  const opacity = isFilterActive(fg) ? undefined : 0.4;

  const readOnly = fg.frozen || p.readOnly;

  return (
    <tr className="sf-filter-group" style={{ opacity: opacity}}>
      <td style={{ verticalAlign: "top" }}>
        {!readOnly &&
          <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
            className="sf-line-button sf-remove"
            onClick={handleDeleteFilter}>
            <FontAwesomeIcon icon="times" />
          </a>}
      </td>
      <td colSpan={3} style={{ backgroundColor: fg.groupOperation == "Or" ? "#eee" : "#fff", border: "1px solid #ddd" }}>
        <div className="justify-content-between d-flex" >
          <div className="form-inline">
            <a href="#" onClick={handleExpandCollapse} className={classes(fg.expanded ? "sf-hide-group-button" : "sf-show-group-button", "mx-2")} >
              <FontAwesomeIcon icon={fg.expanded ? ["far", "minus-square"] : ["far", "plus-square"]} className="mr-2" />
            </a>
            <label>Group:</label>
            <select className="form-control form-control-xs sf-group-selector mx-2" value={fg.groupOperation as any} disabled={readOnly} onChange={handleChangeOperation}>
              {FilterGroupOperation.values().map((ft, i) => <option key={i} value={ft as any}>{FilterGroupOperation.niceToString(ft)}</option>)}
            </select>
          </div>

          <div className="form-inline">
            <label>Prefix:</label>
            <div className={classes("rw-widget-xs mx-2", fg.token == null ? "hidden" : undefined)}>
              <QueryTokenBuilder
                prefixQueryToken={p.prefixToken}
                queryToken={fg.token}
                onTokenChange={handleTokenChanged}
                queryKey={p.queryDescription.queryKey}
                subTokenOptions={p.subTokensOptions}
                readOnly={readOnly} />
            </div>
          </div>
          {fg.pinned &&
            <div>
              {renderValue()}
            </div>
          }
          <div>
            {p.showPinnedFilters &&
              <button className={classes("btn", "btn-link", "btn-sm", "sf-user-filter", fg.pinned && "active")} onClick={e => { fg.pinned = fg.pinned ? undefined : {}; changeFilter(); }} disabled={p.readOnly}>
                <FontAwesomeIcon color="orange" icon={[fg.pinned ? "fas" : "far", "star"]} />
              </button>
            }
          </div>
        </div>
        <div className="sf-filters-list table-responsive" style={{ overflowX: "visible" }}>
          <table className="table-sm" style={{ width: "100%" }}>
            <thead>
              {fg.pinned && <PinnedFilterEditor pinned={fg.pinned} onChange={() => changeFilter()} readonly={readOnly} />}
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

                  <FilterGroupComponent key={i} filterGroup={f} readOnly={Boolean(p.readOnly)} onDeleteFilter={handlerDeleteFilter}
                    prefixToken={fg.token}
                    subTokensOptions={p.subTokensOptions} queryDescription={p.queryDescription}
                    onTokenChanged={p.onTokenChanged} onFilterChanged={p.onFilterChanged}
                    lastToken={p.lastToken} onHeightChanged={p.onHeightChanged} renderValue={p.renderValue}
                    showPinnedFilters={p.showPinnedFilters}
                    disableValue={p.disableValue || fg.pinned != null && fg.pinned.active != "Checkbox_StartChecked" && fg.pinned.active != "Checkbox_StartUnchecked"}
                  /> :

                  <FilterConditionComponent key={i} filter={f} readOnly={Boolean(p.readOnly)} onDeleteFilter={handlerDeleteFilter}
                    prefixToken={fg.token}
                    subTokensOptions={p.subTokensOptions} queryDescription={p.queryDescription}
                    onTokenChanged={p.onTokenChanged} onFilterChanged={p.onFilterChanged} renderValue={p.renderValue}
                    showPinnedFilters={p.showPinnedFilters}
                    disableValue={p.disableValue || fg.pinned != null && fg.pinned.active != "Checkbox_StartChecked" && fg.pinned.active != "Checkbox_StartUnchecked"}
                  />
                )}
                {!p.readOnly &&
                  <tr className="sf-filter-create">
                    <td colSpan={4}>
                      <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.AddFilter.niceToString() : undefined}
                        className="sf-line-button sf-create"
                        onClick={e => handlerNewFilter(e, false)}>
                        <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{SearchMessage.AddFilter.niceToString()}
                      </a>

                      <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.AddGroup.niceToString() : undefined}
                        className="sf-line-button sf-create ml-3"
                        onClick={e => handlerNewFilter(e, true)}>
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

  function renderValue() {

    if (p.renderValue)
      return p.renderValue({ filter: p.filterGroup, handleValueChange, readonly: p.readOnly });

    const f = p.filterGroup;

    const readOnly = p.readOnly || f.frozen;

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(f, a => a.value));

    return <ValueLine ctx={ctx} type={{ name: "string" }} onChange={() => handleValueChange()} />

  }

  function handleValueChange() {
    forceUpdate();
    p.onFilterChanged();
  }

  function changeFilter() {
    forceUpdate();
    p.onFilterChanged();
  }
}

function isFilterActive(fo: FilterOptionParsed) {
  return fo.pinned == null ||
    fo.pinned.active == null /*Always*/ ||
    fo.pinned.active == "Always" ||
    fo.pinned.active == "Checkbox_StartChecked" ||
    fo.pinned.active == "WhenHasValue" && !(fo.value == null || fo.value == "");
}

export interface FilterConditionComponentProps {
  filter: FilterConditionOptionParsed;
  prefixToken: QueryToken | undefined;
  readOnly: boolean;
  onDeleteFilter: (fo: FilterConditionOptionParsed) => void;
  queryDescription: QueryDescription;
  subTokensOptions: SubTokensOptions;
  onTokenChanged?: (token: QueryToken | undefined) => void;
  onFilterChanged: () => void;
  renderValue?: (rvc: RenderValueContext) => React.ReactElement<any> | undefined;
  showPinnedFilters: boolean;
  disableValue: boolean;
}

export function FilterConditionComponent(p: FilterConditionComponentProps) {

  const forceUpdate = useForceUpdate();

  function handleDeleteFilter(e: React.MouseEvent<any>) {
    e.preventDefault();
    p.onDeleteFilter(p.filter);
  }

  function handleTokenChanged(newToken: QueryToken | null | undefined) {

    const f = p.filter;

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
        f.value = f.value && trimDateToFormat(f.value, toLuxonFormat(newToken.format));
      }
    }
    f.token = newToken ?? undefined;

    if (p.onTokenChanged)
      p.onTokenChanged(newToken ?? undefined);

    p.onFilterChanged();

    forceUpdate();
  }

  function trimDateToFormat(date: string, momentFormat: string | undefined) {

    if (!momentFormat)
      return date;

    const formatted = DateTime.fromISO(date).toFormat(momentFormat);
    return DateTime.fromFormat(formatted, momentFormat).toISO();
  }


  function handleChangeOperation(event: React.FormEvent<HTMLSelectElement>) {
    const operation = (event.currentTarget as HTMLSelectElement).value as FilterOperation;
    if (isList(operation) != isList(p.filter.operation!))
      p.filter.value = isList(operation) ? [p.filter.value] : p.filter.value[0];

    p.filter.operation = operation;

    p.onFilterChanged();

    forceUpdate();
  }

  const f = p.filter;

  const readOnly = f.frozen || p.readOnly;

  const opacity = isFilterActive(f) ? undefined : 0.4;

  return (
    <>
      <tr className="sf-filter-condition" style={{ opacity: opacity }}>
        <td>
          {!readOnly &&
            <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
              className="sf-line-button sf-remove"
              onClick={handleDeleteFilter}>
              <FontAwesomeIcon icon="times" />
            </a>}
        </td>
        <td>
          <div className="rw-widget-xs">
            <QueryTokenBuilder
              prefixQueryToken={p.prefixToken}
              queryToken={f.token}
              onTokenChange={handleTokenChanged}
              queryKey={p.queryDescription.queryKey}
              subTokenOptions={p.subTokensOptions}
              readOnly={readOnly} />
          </div>
        </td>
        <td className="sf-filter-operation">
          {f.token && f.token.filterType && f.operation &&
            <select className="form-control form-control-xs" value={f.operation} disabled={readOnly} onChange={handleChangeOperation}>
              {f.token.filterType && filterOperations[f.token.filterType!]
                .map((ft, i) => <option key={i} value={ft as any}>{FilterOperation.niceToString(ft)}</option>)}
            </select>}
        </td>

        <td className="sf-filter-value">
          {p.disableValue ? <small className="text-muted">{SearchMessage.ParentValue.niceToString()}</small> :
            f.token && f.token.filterType && f.operation && renderValue()}
        </td>
        {f.token && f.token.filterType && f.operation && p.showPinnedFilters &&
          <td>
            <button className={classes("btn", "btn-link", "btn-sm", "sf-user-filter", f.pinned && "active")} onClick={e => { f.pinned = f.pinned ? undefined : {}; changeFilter(); }} disabled={p.readOnly}>
              <FontAwesomeIcon color="orange" icon={[f.pinned ? "fas" : "far", "star"]} />
            </button>
          </td>
        }
      </tr>
      {p.showPinnedFilters && f.pinned && <PinnedFilterEditor pinned={f.pinned} opacity={opacity} onChange={() => changeFilter()} readonly={readOnly} />}
    </>
  );

  function changeFilter() {
    forceUpdate();
    p.onFilterChanged();
  }

  function renderValue() {

    if (p.renderValue)
      return p.renderValue({ filter: p.filter, handleValueChange, readonly: p.readOnly });

    const f = p.filter;

    const readOnly = p.readOnly || f.frozen;

    if (isList(f.operation!))
      return <MultiValue values={f.value} onRenderItem={ctx => createFilterValueControl(ctx, f.token!, handleValueChange)} readOnly={readOnly} onChange={handleValueChange} />;

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(f, a => a.value));

    return createFilterValueControl(ctx, f.token!, handleValueChange);
  }

  function handleValueChange() {
    forceUpdate();
    p.onFilterChanged();
  }
}


interface PinnedFilterEditorProps {
  pinned: PinnedFilterParsed;
  readonly: boolean;
  onChange: () => void;
  opacity?: number
}

export function PinnedFilterEditor(p: PinnedFilterEditorProps) {
  return (
    <tr className="sf-pinned-filter" style={{ backgroundColor: "#fff6e6", verticalAlign: "top" }}>
      <td></td>
      <td style={{ opacity: p.opacity }}>
        <div>
          <input type="text" className="form-control form-control-xs" placeholder={SearchMessage.Label.niceToString()} readOnly={p.readonly}
            value={p.pinned.label ?? ""}
            onChange={e => { p.pinned.label = e.currentTarget.value; p.onChange(); }} />
        </div>
      </td>
      <td style={{ opacity: p.opacity }}>
        <div className="input-group input-group-xs">
          {numericTextBox(Binding.create(p.pinned, _ => _.column), SearchMessage.Column.niceToString())}
          {numericTextBox(Binding.create(p.pinned, _ => _.row), SearchMessage.Row.niceToString())}
        </div>
      </td>
      <td colSpan={2}>
        <div className="btn-group btn-group-xs" role="group" aria-label="Basic example" style={{ verticalAlign: "unset" }}>
          {renderActiveDropdown(Binding.create(p.pinned, a => a.active), "Select when the filter will take effect")}
          {renderButton(Binding.create(p.pinned, a => a.splitText), "SplitText", "To enable google-like search")}
        </div>
      </td>
    </tr>
  );

  function numericTextBox(binding: Binding<number | undefined>, title: string) {

    var val = binding.getValue();
    if (p.readonly)
      return <span className="numeric form-control form-control-xs" style={{ width: "60px" }}>{val}</span>;

    var numberFormat = toNumberFormat("0");

    return (
      <NumericTextBox value={val == undefined ? null : val} format={numberFormat} onChange={n => { binding.setValue(n == null ? undefined : n); p.onChange(); }}
        validateKey={isNumber} formControlClass="form-control form-control-xs" htmlAttributes={{ placeholder: title, style: { width: "60px" } }} />
    );
  }

  function renderButton(binding: Binding<boolean | undefined>, label: string, title: string) {
    return (
      <button type="button" className={classes("px-1 btn btn-light", binding.getValue() && "active")} disabled={p.readonly}
        onClick={e => { binding.setValue(binding.getValue() ? undefined : true); p.onChange(); }}
        title={StyleContext.default.titleLabels ? title : undefined}>
        {label}
      </button>
    );
  }

  function renderActiveDropdown(binding: Binding<PinnedFilterActive | undefined>, title: string) {
    var value = binding.getValue() ?? "Always";
    return (
      <Dropdown>
        <Dropdown.Toggle variant="light" id="dropdown-basic" disabled={p.readonly} size={"xs" as any} className="px-1"
          title={StyleContext.default.titleLabels ? title : undefined}>
          Active: {PinnedFilterActive.niceToString(value)}
        </Dropdown.Toggle>

        <Dropdown.Menu>
          {PinnedFilterActive.values().map(v =>
            <Dropdown.Item key={v} active={v == value} onClick={() => { binding.setValue(v == "Always" ? undefined : v); p.onChange(); }}>
              {PinnedFilterActive.niceToString(v)}
            </Dropdown.Item>)
          }
        </Dropdown.Menu>
      </Dropdown>
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
      const ti = tryGetTypeInfos(tokenType).single();
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

export function MultiValue(p: MultiValueProps) {

  const forceUpdate = useForceUpdate();

  function handleDeleteValue(e: React.MouseEvent<any>, index: number) {
    e.preventDefault();
    p.values.removeAt(index);
    p.onChange();
    forceUpdate();
  }

  function handleAddValue(e: React.MouseEvent<any>) {
    e.preventDefault();
    p.values.push(undefined);
    p.onChange();
    forceUpdate();
  }

  return (
    <table style={{ marginBottom: "0px" }} className="sf-multi-value">
      <tbody>
        {
          p.values.map((v, i) =>
            <tr key={i}>
              <td>
                {!p.readOnly &&
                  <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
                    className="sf-line-button sf-remove"
                    onClick={e => handleDeleteValue(e, i)}>
                    <FontAwesomeIcon icon="times" />
                  </a>}
              </td>
              <td>
                {
                  p.onRenderItem(new TypeContext<any>(undefined,
                    {
                      formGroupStyle: "None",
                      formSize: "ExtraSmall",
                      readOnly: p.readOnly
                    }, undefined as any, new Binding<any>(p.values, i)))
                }
              </td>
            </tr>)
        }
        <tr >
          <td colSpan={4}>
            {!p.readOnly &&
              <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.AddValue.niceToString() : undefined}
                className="sf-line-button sf-create"
                onClick={handleAddValue}>
                <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{SearchMessage.AddValue.niceToString()}
              </a>}
          </td>
        </tr>
      </tbody>
    </table>
  );
}

