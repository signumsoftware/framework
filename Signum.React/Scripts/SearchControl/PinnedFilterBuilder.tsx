import * as React from 'react'
import {
  FilterOptionParsed, QueryDescription, QueryToken, SubTokensOptions,
  isList, isFilterGroupOptionParsed, isCheckBox
} from '../FindOptions'
import { ValueLine, FormGroup } from '../Lines'
import { Binding, IsByAll, tryGetTypeInfos, toLuxonFormat } from '../Reflection'
import { TypeContext } from '../TypeContext'
import "./FilterBuilder.css"
import { createFilterValueControl, MultiValue } from './FilterBuilder';
import { SearchMessage } from '../Signum.Entities';
import { classes } from '../Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'

interface PinnedFilterBuilderProps {
  filterOptions: FilterOptionParsed[];
  onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
  pinnedFilterVisible?: (fo: FilterOptionParsed) => boolean 
  onSearch?: () => void;
  showSearchButton?: boolean;
  extraSmall?: boolean;
}
export default function PinnedFilterBuilder(p: PinnedFilterBuilderProps) {

  const timeoutWriteText = React.useRef<number | null>(null);

  var allPinned = getAllPinned(p.filterOptions).filter(fop => p.pinnedFilterVisible == null || p.pinnedFilterVisible(fop));

  if (allPinned.length == 0)
    return null;

  return (
    <div onKeyUp={handleFiltersKeyUp }>
      <div className={classes("row", p.extraSmall ? "" : "mt-3 mb-3")}>
        {
          allPinned
            .groupBy(fo => (fo.pinned!.column ?? 0).toString())
            .orderBy(gr => parseInt(gr.key))
            .map(gr => <div className="col-sm-3" key={gr.key}>
              {gr.elements.orderBy(a => a.pinned!.row).map((f, i) => <div key={i}>{renderValue(f)}</div>)}
            </div>)
        }
      </div>
      {p.showSearchButton &&
        <button className={classes("sf-query-button sf-search btn btn-primary")} onClick={() => p.onSearch && p.onSearch()} title="Enter">
          <FontAwesomeIcon icon={"magnifying-glass"} />&nbsp;{SearchMessage.Search.niceToString()}
        </button>}

    </div>
  );

  function renderValue(filter: FilterOptionParsed) {

    const f = filter;
    const readOnly = f.frozen;
    var label = f.pinned!.label || f.token?.niceName;

    if (f.pinned && (isCheckBox(f.pinned.active))) {
      return (
        <div className="checkbox mt-4">
          <label>
            <input type="checkbox" className="form-check-input me-1" checked={f.pinned.active == "Checkbox_StartChecked" || f.pinned.active == "NotCheckbox_StartChecked"} readOnly={readOnly} onChange={() => {
              f.pinned!.active =
                f.pinned!.active == "Checkbox_StartChecked" ? "Checkbox_StartUnchecked" :
                  f.pinned!.active == "Checkbox_StartUnchecked" ? "Checkbox_StartChecked" :
                    f.pinned!.active == "NotCheckbox_StartChecked" ? "NotCheckbox_StartUnchecked" :
                      f.pinned!.active == "NotCheckbox_StartUnchecked" ? "NotCheckbox_StartChecked" : undefined!;
            p.onFiltersChanged && p.onFiltersChanged(p.filterOptions);
          }} />{label}</label>
        </div>
      );
    }

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "Basic", readOnly: readOnly, formSize: p.extraSmall ? "xs" : "sm" }, undefined as any, Binding.create(f, a => a.value));


    if (isFilterGroupOptionParsed(f)) {
      return <ValueLine ctx={ctx} type={{ name: "string" }} onChange={() => handleValueChange(f)} label={label || SearchMessage.Search.niceToString()} />
    }

    if (isList(f.operation!))
      return (
        <FormGroup ctx={ctx} label={label}>
          <MultiValue values={f.value} readOnly={readOnly} onChange={() => handleValueChange(f)}
            onRenderItem={ctx => createFilterValueControl(ctx, f.token!, () => handleValueChange(f))} />
        </FormGroup>
      );

    return createFilterValueControl(ctx, f.token!, () => handleValueChange(f), label, f.pinned!.active == "WhenHasValue");
  }


  function handleValueChange(f: FilterOptionParsed) {

    if (isFilterGroupOptionParsed(f) || f.token && f.token.filterType == "String") {

      if (timeoutWriteText.current)
        clearTimeout(timeoutWriteText.current);

      timeoutWriteText.current = setTimeout(() => {
        p.onFiltersChanged && p.onFiltersChanged(p.filterOptions);
        timeoutWriteText.current = null;
      }, 200);

    } else {
      p.onFiltersChanged && p.onFiltersChanged(p.filterOptions);
    }
  }

  function handleFiltersKeyUp(e: React.KeyboardEvent<HTMLDivElement>) {
    if (p.onSearch && e.keyCode == 13) {
      setTimeout(() => {
        p.onSearch!();
      }, 200);
    }
  }

}

function getAllPinned(filterOptions: FilterOptionParsed[]): FilterOptionParsed[] {
  var direct = filterOptions.filter(a => a.pinned != null);

  var recursive = filterOptions
    .flatMap(f => f.pinned == null && isFilterGroupOptionParsed(f) ? getAllPinned(f.filters) : []);

  return direct.concat(recursive);
}
