import * as React from 'react'
import {
  FilterOptionParsed, QueryDescription, QueryToken, SubTokensOptions,
  isList, isFilterGroupOptionParsed
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
  onSearch?: () => void;
  showSearchButton?: boolean;
  extraSmall?: boolean;
}
export default function PinnedFilterBuilder(p: PinnedFilterBuilderProps) {

  const timeoutWriteText = React.useRef<number | null>(null);

  var allPinned = getAllPinned(p.filterOptions);

  if (allPinned.length == 0)
    return null;

  return (
    <div onKeyUp={handleFiltersKeyUp }>
      <div className={classes("row", p.extraSmall ? "" : "mt-3 mb-3")}>
        {
          allPinned
            .groupBy(a => (a.pinned!.column ?? 0).toString())
            .orderBy(gr => parseInt(gr.key))
            .map(gr => <div className="col-sm-3" key={gr.key}>
              {gr.elements.orderBy(a => a.pinned!.row).map((f, i) => <div key={i}>{renderValue(f)}</div>)}
            </div>)
        }
      </div>
      {p.showSearchButton &&
        <button className={classes("sf-query-button sf-search btn btn-primary")} onClick={() => p.onSearch && p.onSearch()} title="Enter">
          <FontAwesomeIcon icon={"search"} />&nbsp;{SearchMessage.Search.niceToString()}
        </button>}

    </div>
  );

  function renderValue(filter: FilterOptionParsed) {

    const f = filter;
    const readOnly = f.frozen;
    var labelText = f.pinned!.label || f.token?.niceName;

    if (f.pinned && (f.pinned.active == "Checkbox_StartChecked" || f.pinned.active == "Checkbox_StartUnchecked")) {
      return (
        <div className="checkbox mt-4">
          <label><input type="checkbox" className="mr-1" checked={f.pinned.active == "Checkbox_StartChecked"} readOnly={readOnly} onChange={() => {
            f.pinned!.active = f.pinned!.active == "Checkbox_StartChecked" ? "Checkbox_StartUnchecked" : "Checkbox_StartChecked";
            p.onFiltersChanged && p.onFiltersChanged(p.filterOptions);
          }} />{labelText}</label>
        </div>
      );
    }

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "Basic", readOnly: readOnly, formSize: p.extraSmall ? "ExtraSmall" : "Small" }, undefined as any, Binding.create(f, a => a.value));


    if (isFilterGroupOptionParsed(f)) {
      return <ValueLine ctx={ctx} type={{ name: "string" }} onChange={() => handleValueChange(f)} labelText={labelText || SearchMessage.Search.niceToString()} />
    }

    if (isList(f.operation!))
      return (
        <FormGroup ctx={ctx} labelText={labelText}>
          <MultiValue values={f.value} readOnly={readOnly} onChange={() => handleValueChange(f)}
            onRenderItem={ctx => createFilterValueControl(ctx, f.token!, () => handleValueChange(f))} />
        </FormGroup>
      );

    return createFilterValueControl(ctx, f.token!, () => handleValueChange(f), labelText, f.pinned!.active == "WhenHasValue");
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
