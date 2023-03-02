import * as React from 'react'
import { DateTime } from 'luxon'
import { Dic, areEqual, classes, KeyGenerator } from '../Globals'
import {
  FilterOptionParsed, QueryDescription, QueryToken, SubTokensOptions, filterOperations, isList, FilterOperation, FilterConditionOptionParsed, FilterGroupOptionParsed,
  isFilterGroupOptionParsed, hasAnyOrAll, getTokenParents, isPrefix, FilterConditionOption, PinnedFilter, PinnedFilterParsed, isCheckBox
} from '../FindOptions'
import { SearchMessage, Lite } from '../Signum.Entities'
import { isNumber, trimDateToFormat } from '../Lines/ValueLine'
import { ValueLine, EntityLine, EntityCombo, StyleContext, FormControlReadonly } from '../Lines'
import { Binding, IsByAll, tryGetTypeInfos, toLuxonFormat, getTypeInfos, toNumberFormat } from '../Reflection'
import { TypeContext } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { DashboardBehaviour, FilterGroupOperation, PinnedFilterActive } from '../Signum.Entities.DynamicQuery';
import "./FilterBuilder.css"
import { NumericTextBox } from '../Lines/ValueLine';
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
  showPinnedFiltersOptions?: boolean;
  showPinnedFiltersOptionsButton?: boolean;
  showDashboardBehaviour?: boolean;
}

export default function FilterBuilder(p: FilterBuilderProps) {

  const [showPinnedFiltersOptions, setShowPinnedFiltersOptions] = React.useState<boolean>(p.showPinnedFiltersOptions ?? false)

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

    forceUpdate().then(handleHeightChanged);
  };

  function handlerDeleteFilter(filter: FilterOptionParsed) {
    p.filterOptions.remove(filter);
    if (p.onFiltersChanged)
      p.onFiltersChanged(p.filterOptions);
    forceUpdate().then(handleHeightChanged);
  };

  function handleDeleteAllFilters(e: React.MouseEvent) {
    e.preventDefault();

    var filtersCount = p.filterOptions.length;
    p.filterOptions.filter(fo => !fo.frozen).forEach(fo => p.filterOptions.remove(fo));
    if (p.filterOptions.length == filtersCount)
      return;

    if (p.onFiltersChanged)
      p.onFiltersChanged(p.filterOptions);
    forceUpdate().then(handleHeightChanged);
  };

  function handleFilterChanged() {
    if (p.onFiltersChanged)
      p.onFiltersChanged(p.filterOptions);
  };

  function handleHeightChanged() {
    if (p.onHeightChanged)
      p.onHeightChanged();
  }


  var keyGenerator = React.useMemo(() => new KeyGenerator(), []);

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
                  <FontAwesomeIcon icon="xmark" />
                </a>}</th>
              <th>{SearchMessage.Field.niceToString()}</th>
              <th>{SearchMessage.Operation.niceToString()}</th>
              <th style={{ paddingRight: "20px" }}>{SearchMessage.Value.niceToString()}</th>
              {showPinnedFiltersOptions && <th></th>}
            </tr>
          </thead>
          <tbody>
            {p.filterOptions.map((f) => isFilterGroupOptionParsed(f) ?
              <FilterGroupComponent key={keyGenerator.getKey(f)} filterGroup={f} readOnly={Boolean(p.readOnly)} onDeleteFilter={handlerDeleteFilter}
                prefixToken={undefined}
                subTokensOptions={p.subTokensOptions} queryDescription={p.queryDescription}
                onTokenChanged={p.onTokenChanged} onFilterChanged={handleFilterChanged}
                lastToken={p.lastToken} onHeightChanged={handleHeightChanged} renderValue={p.renderValue}
                showPinnedFiltersOptions={showPinnedFiltersOptions}
                showDashboardBehaviour={(p.showDashboardBehaviour ?? true) && showPinnedFiltersOptions}
                disableValue={false} /> :
              <FilterConditionComponent key={keyGenerator.getKey(f)} filter={f} readOnly={Boolean(p.readOnly)} onDeleteFilter={handlerDeleteFilter}
                prefixToken={undefined}
                subTokensOptions={p.subTokensOptions} queryDescription={p.queryDescription}
                onTokenChanged={p.onTokenChanged} onFilterChanged={handleFilterChanged} renderValue={p.renderValue}
                showPinnedFiltersOptions={showPinnedFiltersOptions}
                showDashboardBehaviour={(p.showDashboardBehaviour ?? true) && showPinnedFiltersOptions}
                disableValue={false} />
            )}
            {!p.readOnly &&
              <tr className="sf-filter-create">
                <td colSpan={4}>
                  <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.AddFilter.niceToString() : undefined}
                    className="sf-line-button sf-create sf-create-condition"
                    onClick={e => handlerNewFilter(e, false)}>
                    <FontAwesomeIcon icon="plus" className="sf-create me-1" />{SearchMessage.AddFilter.niceToString()}
                  </a>
                  <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.AddGroup.niceToString() : undefined}
                    className="sf-line-button sf-create sf-create-group ms-3"
                    onClick={e => handlerNewFilter(e, true)}>
                    <FontAwesomeIcon icon="plus" className="sf-create me-1" />{SearchMessage.AddGroup.niceToString()}
                  </a>

                  {p.showPinnedFiltersOptionsButton && <a href="#" title={StyleContext.default.titleLabels ? (showPinnedFiltersOptions ? SearchMessage.HidePinnedFiltersOptions : SearchMessage.ShowPinnedFiltersOptions).niceToString() : undefined}
                    className="sf-line-button ms-3"
                    onClick={e => { e.preventDefault(); setShowPinnedFiltersOptions(!showPinnedFiltersOptions); }}>
                    <FontAwesomeIcon color="orange" icon={[showPinnedFiltersOptions ? "fas" : "far", "star"]} className="me-1" />{(showPinnedFiltersOptions ? SearchMessage.HidePinnedFiltersOptions : SearchMessage.ShowPinnedFiltersOptions).niceToString()}
                  </a>
                  }
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
  showPinnedFiltersOptions: boolean;
  showDashboardBehaviour: boolean;
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
    forceUpdatePromise().then(() => p.onHeightChanged());
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

    forceUpdatePromise().then(() => p.onHeightChanged());
  };

  function handleExpandCollapse(e: React.MouseEvent<any>) {
    e.preventDefault();
    const fg = p.filterGroup;
    fg.expanded = !fg.expanded;

    forceUpdatePromise().then(() => p.onHeightChanged());
  }

  const fg = p.filterGroup;

  var keyGenerator = React.useMemo(() => new KeyGenerator(), []);

  if (!p.showPinnedFiltersOptions && !isFilterActive(fg))
    return null;

  const readOnly = fg.frozen || p.readOnly;

  return (
    <tr className="sf-filter-group">
      <td style={{ verticalAlign: "top" }}>
        {!readOnly &&
          <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
            className="sf-line-button sf-remove"
            onClick={handleDeleteFilter}>
            <FontAwesomeIcon icon="xmark" />
          </a>}
      </td>
      <td colSpan={3 + (p.showPinnedFiltersOptions ? 1 : 0) + (p.showDashboardBehaviour ? 1 : 0)} style={{ backgroundColor: fg.groupOperation == "Or" ? "#eee" : "#fff", border: "1px solid #ddd" }}>
        <div className="justify-content-between d-flex" >
          <div className="row gx-1">
            <div className="col-auto">
              <a href="#" onClick={handleExpandCollapse} className={classes(fg.expanded ? "sf-hide-group-button" : "sf-show-group-button", "mx-2")} >
                <FontAwesomeIcon icon={fg.expanded ? ["far", "square-minus"] : ["far", "square-plus"]} className="me-2" />
              </a>
            </div>
            <div className="col-auto">
              <label>Group:</label>
            </div>
            <div className="col-auto">
              <select className="form-select form-select-xs sf-group-selector mx-2" value={fg.groupOperation as any} disabled={readOnly} onChange={handleChangeOperation}>
                {FilterGroupOperation.values().map((ft, i) => <option key={i} value={ft as any}>{FilterGroupOperation.niceToString(ft)}</option>)}
              </select>
            </div>
          </div>

          <div className="row gx-1">
            <div className="col-auto">
              <label>Prefix:</label>
            </div>
            <div className="col-auto">
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
          </div>
          {fg.pinned &&
            <div>
              {renderValue()}
            </div>
          }
          <div>
            {p.showPinnedFiltersOptions &&
              <button className={classes("btn", "btn-link", "btn-sm", "sf-user-filter", fg.pinned && "active")} onClick={e => {
                fg.pinned = fg.pinned ? undefined : {};
                fixDashboardBehaviour(fg);
                changeFilter();
              }} disabled={p.readOnly}>
                <FontAwesomeIcon color="orange" icon={[fg.pinned ? "fas" : "far", "star"]} />
              </button>
            }
          </div>
          {p.showDashboardBehaviour && <div>
            <DashboardBehaviourComponent filter={fg} readonly={readOnly} onChange={() => changeFilter()} />
          </div>}
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
                {fg.filters.map((f) => isFilterGroupOptionParsed(f) ?

                  <FilterGroupComponent key={keyGenerator.getKey(f)} filterGroup={f} readOnly={Boolean(p.readOnly)} onDeleteFilter={handlerDeleteFilter}
                    prefixToken={fg.token}
                    subTokensOptions={p.subTokensOptions} queryDescription={p.queryDescription}
                    onTokenChanged={p.onTokenChanged} onFilterChanged={p.onFilterChanged}
                    lastToken={p.lastToken} onHeightChanged={p.onHeightChanged} renderValue={p.renderValue}
                    showPinnedFiltersOptions={p.showPinnedFiltersOptions}
                    showDashboardBehaviour={p.showDashboardBehaviour}
                    disableValue={p.disableValue || fg.pinned != null && !isCheckBox(fg.pinned.active)}
                  /> :

                  <FilterConditionComponent key={keyGenerator.getKey(f)} filter={f} readOnly={Boolean(p.readOnly)} onDeleteFilter={handlerDeleteFilter}
                    prefixToken={fg.token}
                    subTokensOptions={p.subTokensOptions} queryDescription={p.queryDescription}
                    onTokenChanged={p.onTokenChanged} onFilterChanged={p.onFilterChanged} renderValue={p.renderValue}
                    showPinnedFiltersOptions={p.showPinnedFiltersOptions}
                    showDashboardBehaviour={p.showDashboardBehaviour}
                    disableValue={p.disableValue || fg.pinned != null && !isCheckBox(fg.pinned.active)}
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
                        className="sf-line-button sf-create ms-3"
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

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "xs" }, undefined as any, Binding.create(f, a => a.value));

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
  if (fo.pinned == null)
    return true;

  if (fo.pinned.splitText && (fo.value == null || fo.value == ""))
    return false;

  return fo.pinned.active == null /*Always*/ ||
    fo.pinned.active == "Always" ||
    fo.pinned.active == "Checkbox_StartChecked" ||
    fo.pinned.active == "NotCheckbox_StartUnchecked" ||
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
  showPinnedFiltersOptions: boolean;
  showDashboardBehaviour: boolean;
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

      if (!areEqual(f.token, newToken, a => a.filterType) ||
        !areEqual(f.token, newToken, a => a.preferEquals) ||
        newToken.filterType == "Lite" && f.value != null && newToken.type.name != IsByAll && !getTypeInfos(newToken.type.name).some(t => t.name == (f.value as Lite<any>).EntityType)) {
        f.operation = newToken.preferEquals ? "EqualTo" : newToken.filterType && filterOperations[newToken.filterType].first();
        f.value = f.operation && isList(f.operation) ? [undefined] : undefined;
      }
      else if (f.token && f.token.filterType == "DateTime" && newToken.filterType == "DateTime") {
        if (f.value) {
          const type = newToken.type.name as "DateOnly" | "DateTime";

          function convertDateToNewFormat(val: string) {
            var date = DateTime.fromISO(val);
            if (!date.isValid)
              return val;

            const trimmed = trimDateToFormat(date, type, newToken!.format);
            return type == "DateOnly" ? trimmed.toISODate() : trimmed.toISO();
          }

          if (f.operation && isList(f.operation)) {
            f.value = (f.value as string[]).map(v => convertDateToNewFormat(v));
          } else {
            f.value = convertDateToNewFormat(f.value);
          }
        }
      }
    }
    f.token = newToken ?? undefined;

    if (p.onTokenChanged)
      p.onTokenChanged(newToken ?? undefined);

    p.onFilterChanged();

    forceUpdate();
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

  if (!p.showPinnedFiltersOptions && !isFilterActive(f))
    return null;

  return (
    <>
      <tr className="sf-filter-condition">
        <td>
          {!readOnly &&
            <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
              className="sf-line-button sf-remove"
              onClick={handleDeleteFilter}>
              <FontAwesomeIcon icon="xmark" />
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
            <select className="form-select form-select-xs" value={f.operation} disabled={readOnly} onChange={handleChangeOperation}>
              {f.token.filterType && filterOperations[f.token.filterType!]
                .map((ft, i) => <option key={i} value={ft as any}>{FilterOperation.niceToString(ft)}</option>)}
            </select>}
        </td>

        <td className="sf-filter-value">
          {p.disableValue ? <small className="text-muted">{SearchMessage.ParentValue.niceToString()}</small> :
            f.token && f.token.filterType && f.operation && renderValue()}
        </td>
        {p.showPinnedFiltersOptions &&
          <td>
            {f.token && f.token.filterType && f.operation && <button className={classes("btn", "btn-link", "btn-sm", "sf-user-filter", f.pinned && "active")} onClick={e => {
              f.pinned = f.pinned ? undefined : {};
              fixDashboardBehaviour(f);
              changeFilter();
            }} disabled={p.readOnly}>
              <FontAwesomeIcon color="orange" icon={[f.pinned ? "fas" : "far", "star"]} />
            </button>
            }
          </td>
        }
        {p.showDashboardBehaviour && <td>
          <DashboardBehaviourComponent filter={f} readonly={readOnly} onChange={()=> changeFilter()}/>
        </td>}
      </tr>
      {p.showPinnedFiltersOptions && f.pinned && <PinnedFilterEditor pinned={f.pinned} onChange={() => changeFilter()} readonly={readOnly} />}
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

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "xs" }, undefined as any, Binding.create(f, a => a.value));

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
}

export function PinnedFilterEditor(p: PinnedFilterEditorProps) {
  return (
    <tr className="sf-pinned-filter" style={{ backgroundColor: "#fff6e6", verticalAlign: "top" }}>
      <td></td>
      <td>
        <div>
          <input type="text" className="form-control form-control-xs" placeholder={SearchMessage.Label.niceToString()} readOnly={p.readonly}
            value={p.pinned.label ?? ""}
            onChange={e => { p.pinned.label = e.currentTarget.value; p.onChange(); }} />
        </div>
      </td>
      <td>
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
    var numberFormat = toNumberFormat("0");

    return (
      <NumericTextBox readonly={p.readonly} value={val == undefined ? null : val} format={numberFormat} onChange={n => { binding.setValue(n == null ? undefined : n); p.onChange(); }}
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

function DashboardBehaviourComponent(p: { filter: FilterOptionParsed, readonly: boolean, onChange: ()=> void }) {
  return (
    <Dropdown>
      <Dropdown.Toggle variant={p.filter.dashboardBehaviour ? "info" : "light"} id="dropdown-basic" disabled={p.readonly} size={"xs" as any} className={classes("px-1", p.filter.dashboardBehaviour ? "text-light" : "text-info")}
        title={StyleContext.default.titleLabels ? "Behaviour of the filter when used inside of a Dashboard" : undefined}>
        {<FontAwesomeIcon icon="gauge" className={classes("icon", p.filter.dashboardBehaviour ? "text-light" : "text-info")} />}{p.filter.dashboardBehaviour ? " " + DashboardBehaviour.niceToString(p.filter.dashboardBehaviour) : ""}
      </Dropdown.Toggle>

      <Dropdown.Menu>
        {[undefined, ...DashboardBehaviour.values()].map(v =>
          <Dropdown.Item key={v} active={v == p.filter.dashboardBehaviour} onClick={() => {

            p.filter.dashboardBehaviour = v;
            if (v == "PromoteToDasboardPinnedFilter" && p.filter.pinned == null)
              p.filter.pinned = {};
            else if ((v == "UseAsInitialSelection" || v == "UseWhenNoFilters") && p.filter.pinned != null)
              p.filter.pinned = undefined;

            p.onChange();
          }}>
            {v == null? " - " :  DashboardBehaviour.niceToString(v)}
          </Dropdown.Item>)
        }
      </Dropdown.Menu>
    </Dropdown>
  );
}

export function createFilterValueControl(ctx: TypeContext<any>, token: QueryToken, handleValueChange: () => void, label?: string, forceNullable?: boolean): React.ReactElement<any> {

  var tokenType = token.type;
  if (forceNullable)
    tokenType = { ...tokenType, isNotNullable: false };

  switch (token.filterType) {
    case "Lite":
      if (tokenType.name == IsByAll || getTypeInfos(tokenType).some(ti => !ti.isLowPopulation))
        return <EntityLine ctx={ctx} type={tokenType} create={false} onChange={handleValueChange} label={label} />;
      else
        return <EntityCombo ctx={ctx} type={tokenType} create={false} onChange={handleValueChange} label={label} />
    case "Embedded":
      return <EntityLine ctx={ctx} type={tokenType} create={false} autocomplete={null} onChange={handleValueChange} label={label} />;
    case "Enum":
      const ti = tryGetTypeInfos(tokenType).single();
      if (!ti)
        throw new Error(`EnumType ${tokenType.name} not found`);
      const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
      return <ValueLine ctx={ctx} type={tokenType} format={token.format} unit={token.unit} optionItems={members} onChange={handleValueChange} label={label} />;
    default:
      return <ValueLine ctx={ctx} type={tokenType} format={token.format} unit={token.unit} onChange={handleValueChange} label={label} />;
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
                    <FontAwesomeIcon icon="xmark" />
                  </a>}
              </td>
              <td>
                {
                  p.onRenderItem(new TypeContext<any>(undefined,
                    {
                      formGroupStyle: "None",
                      formSize: "xs",
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


function fixDashboardBehaviour(fop: FilterOptionParsed) {
  if (fop.dashboardBehaviour == "PromoteToDasboardPinnedFilter" && fop.pinned == null)
    fop.dashboardBehaviour = undefined;

  if ((fop.dashboardBehaviour == "UseWhenNoFilters" || fop.dashboardBehaviour == "UseAsInitialSelection") && fop.pinned != null)
    fop.dashboardBehaviour = undefined;

}
