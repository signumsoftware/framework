import * as React from 'react'
import {
  FilterOptionParsed, 
  isCheckBox,
  isFilterGroup} from '../FindOptions'
import { Binding } from '../Reflection'
import { TypeContext } from '../TypeContext'
import "./FilterBuilder.css"
import { SearchMessage } from '../Signum.Entities';
import { classes } from '../Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Finder } from '../Finder'

interface PinnedFilterBuilderProps {
  filterOptions: FilterOptionParsed[];
  highlightFilter?: FilterOptionParsed;
  onFiltersChanged?: (filters: FilterOptionParsed[], avoidSearch?: boolean) => void;
  pinnedFilterVisible?: (fo: FilterOptionParsed) => boolean 
  onSearch?: () => void;
  showSearchButton?: boolean;
  extraSmall?: boolean;
  showGrid?: boolean;
}
export default function PinnedFilterBuilder(p: PinnedFilterBuilderProps): React.ReactElement | null {

  const timeoutWriteText = React.useRef<number | null>(null);

  var allPinned = getAllPinned(p.filterOptions).filter(fop => p.pinnedFilterVisible == null || p.pinnedFilterVisible(fop));

  if (allPinned.length == 0 && !p.showGrid)
    return null;

  function getColSpan(fo: FilterOptionParsed) {
    if (fo.pinned!.colSpan == null || fo.pinned!.colSpan < 0)
      return 1;

    return fo.pinned!.colSpan;
  }

  var maxColumns = allPinned.max(a => (a.pinned!.column ?? 0) + getColSpan(a))!;
  var maxRows = Math.max(allPinned.max(a => (a.pinned!.row ?? 0) + 1 )!, 1);

  maxColumns =
    maxColumns <= 4 ? 4 :
    maxColumns <= 6 ? 6 :
      12;

  var bsBase =
    maxColumns == 4 ? 3 :
    maxColumns == 6 ? 2 :
      1;

  var grid = <div className="row sf-rule">
    {Array.range(0, 12).map(i =>
      <div className="col-sm-1" key={i}>
        <div className="sf-rule-item" >Col {i}</div>
      </div>
    )}
  </div>;

  return (
    <div onKeyUp={handleFiltersKeyUp}>
      <div className={p.extraSmall ? "" : "sf-pinned-filters"}>
        {
          p.showGrid && grid
        }
        {
          Array.range(0, maxRows).map((r, i) => {
            var rowPinned = allPinned.filter(a => (a.pinned?.row ?? 0) == r);
            var hiddenColumns = rowPinned.filter(a => getColSpan(a) > 1)
              .flatMap(a => Array.range(0, a.pinned!.colSpan! - 1).map(i => (a.pinned!.column ?? 0) + i + 1))
              .distinctBy(a => a.toString());


            const allCheckBox = rowPinned.every(a => isCheckBox(a.pinned?.active));

            return (
              <div key={i} className={classes("row", p.showGrid  && "py-2")}>
                {Array.range(0, maxColumns).map((c, j) => {
                  var cellPinned = rowPinned.filter(a => (a.pinned!.column ?? 0) == c);
                  if (hiddenColumns.contains(c) && cellPinned.length == 0)
                    return null;

                  var colSpan = cellPinned.max(a => getColSpan(a)) ?? 1;


                  var error = cellPinned.some(a => a.pinned?.colSpan != null && a.pinned?.colSpan <= 0)
                    || hiddenColumns.contains(c);

                  return (<div key={j} className={classes("col-sm-" + (bsBase * colSpan), error && "border-danger")}>
                    {cellPinned.map((f, i) => <div key={i} className={f == p.highlightFilter ? "sf-filter-highlight" : undefined}>{renderValue(f, i == 0 && !allCheckBox)}</div>)}
                  </div>
                  );
                })}
              </div>
            );
          })
        }

        {
          p.showGrid && grid
        }

      </div>

      {p.showSearchButton &&
        <button className={classes("sf-query-button sf-search btn btn-primary")} onClick={() => p.onSearch && p.onSearch()} title="Enter">
          <FontAwesomeIcon aria-hidden={true} icon={"magnifying-glass"} />&nbsp;{SearchMessage.Search.niceToString()}
        </button>}

    </div>
  );

  function renderValue(filter: FilterOptionParsed, isFirst: boolean) {

    const f = filter;
    const readOnly = f.frozen;
    var label = f.pinned!.label || (f.token?.queryTokenType == "AnyOrAll" || f.token?.queryTokenType == "Element" ? f.token.parent?.niceName : f.token?.niceName);

    if (f.pinned && (isCheckBox(f.pinned.active))) {
      return (
        <div className={classes("checkbox", isFirst && "mt-4")}>
          <label>
            <input type="checkbox" className="form-check-input me-1" checked={f.pinned.active == "Checkbox_Checked" || f.pinned.active == "NotCheckbox_Checked"} readOnly={readOnly} onChange={() => {
              f.pinned!.active =
                f.pinned!.active == "Checkbox_Checked" ? "Checkbox_Unchecked" :
                  f.pinned!.active == "Checkbox_Unchecked" ? "Checkbox_Checked" :
                    f.pinned!.active == "NotCheckbox_Checked" ? "NotCheckbox_Unchecked" :
                      f.pinned!.active == "NotCheckbox_Unchecked" ? "NotCheckbox_Checked" : undefined!;
              p.onFiltersChanged && p.onFiltersChanged(p.filterOptions);
            }} />{label}</label>
        </div>
      );
    }

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "Basic", readOnly: readOnly, formSize: p.extraSmall ? "xs" : "sm" }, undefined as any, Binding.create(f, a => a.value));

    return Finder.renderFilterValue(f, {
      ctx,
      filterOptions: p.filterOptions,
      label: label,
      handleValueChange: handleValueChange,
      forceNullable: f.pinned!.active == "WhenHasValue",
      mandatory: undefined,
    });
  }

  function handleValueChange(f: FilterOptionParsed, avoidSearch?: boolean) {

    if (isFilterGroup(f) || f.token && f.token.filterType == "String") {

      if (timeoutWriteText.current)
        clearTimeout(timeoutWriteText.current);

      timeoutWriteText.current = window.setTimeout(() => {
        p.onFiltersChanged && p.onFiltersChanged(p.filterOptions, avoidSearch);
        timeoutWriteText.current = null;
      }, 200);

    } else {
      p.onFiltersChanged && p.onFiltersChanged(p.filterOptions, avoidSearch);
    }
  }

  function handleFiltersKeyUp(e: React.KeyboardEvent<HTMLDivElement>) {
    if (p.onSearch && e.key === 'Enter') {
      window.setTimeout(() => {
        p.onSearch!();
      }, 200);
    }
  }

}

export function getAllPinned(filterOptions: FilterOptionParsed[]): FilterOptionParsed[] {
  var direct = filterOptions.filter(a => a.pinned != null);

  var recursive = filterOptions
    .flatMap(f => f.pinned == null && isFilterGroup(f) ? getAllPinned(f.filters) : []);

  return direct.concat(recursive);
}
