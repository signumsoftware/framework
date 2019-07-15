import * as React from 'react'
import {
  FilterOptionParsed, QueryDescription, QueryToken, SubTokensOptions,
  isList, isFilterGroupOptionParsed
} from '../FindOptions'
import { ValueLine, FormGroup } from '../Lines'
import { Binding, IsByAll, getTypeInfos, toMomentFormat } from '../Reflection'
import { TypeContext } from '../TypeContext'
import "./FilterBuilder.css"
import { createFilterValueControl, MultiValue } from './FilterBuilder';
import { SearchMessage } from '../Signum.Entities';
import { classes } from '../Globals';

interface PinnedFilterBuilderProps {
  filterOptions: FilterOptionParsed[];
  onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
  extraSmall?: boolean;
}
export default class PinnedFilterBuilder extends React.Component<PinnedFilterBuilderProps>{

  render() {
    var allPinned = getAllPinned(this.props.filterOptions);

    if (allPinned.length == 0)
      return null;

    return (
      <div className={classes("row", this.props.extraSmall ? "" : "mt-3 mb-3")}>
        {
          allPinned
            .groupBy(a => (a.pinned!.column || 0).toString())
            .orderBy(gr => parseInt(gr.key))
            .map(gr => <div className="col-sm-3" key={gr.key}>
              {gr.elements.orderBy(a => a.pinned!.row).map((f, i) => <div key={i}>{this.renderValue(f)}</div>)}
            </div>)
        }
      </div>
    );
  }

  renderValue(filter: FilterOptionParsed) {

    const f = filter;
    const readOnly = f.frozen;
    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "Basic", readOnly: readOnly, formSize: this.props.extraSmall ? "ExtraSmall" : "Small" }, undefined as any, Binding.create(f, a => a.value));

    var labelText = f.pinned!.label || f.token && f.token.niceName;

    if (isFilterGroupOptionParsed(f)) {
      return <ValueLine ctx={ctx} type={{ name: "string" }} onChange={() => this.handleValueChange(f)} labelText={labelText || SearchMessage.Search.niceToString()} />
    }

    if (isList(f.operation!))
      return (
        <FormGroup ctx={ctx} labelText={labelText}>
          <MultiValue values={f.value} readOnly={readOnly} onChange={() => this.handleValueChange(f)}
            onRenderItem={ctx => createFilterValueControl(ctx, f.token!, () => this.handleValueChange(f))} />
        </FormGroup>
      );

    return createFilterValueControl(ctx, f.token!, () => this.handleValueChange(f), labelText, f.pinned!.disableOnNull);
  }

  timeoutWriteText?: number | null;

  handleValueChange = (f: FilterOptionParsed) => {
    if (isFilterGroupOptionParsed(f) || f.token && f.token.filterType == "String") {

      if (this.timeoutWriteText)
        clearTimeout(this.timeoutWriteText);

      this.timeoutWriteText = setTimeout(() => {
        this.props.onFiltersChanged && this.props.onFiltersChanged(this.props.filterOptions);
        this.timeoutWriteText = null;
      }, 200);

    } else {
      this.props.onFiltersChanged && this.props.onFiltersChanged(this.props.filterOptions);
    }
  }
}

function getAllPinned(filterOptions: FilterOptionParsed[]): FilterOptionParsed[] {
  var direct = filterOptions.filter(a => a.pinned != null);

  var recursive = filterOptions
    .flatMap(f => f.pinned == null && isFilterGroupOptionParsed(f) ? getAllPinned(f.filters) : []);

  return direct.concat(recursive);
}
