import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import { getTypeInfo, Binding, PropertyRoute } from '@framework/Reflection'
import { DynamicTypeDesignContext } from './DynamicTypeDefinitionComponent'


export interface ValueComponentProps {
  binding: Binding<any>;
  dc: DynamicTypeDesignContext;
  type: "number" | "string" | "boolean" | "textArea" | null;
  options?: (string | number)[];
  labelClass?: string;
  defaultValue: number | string | boolean | null;
  avoidDelete?: boolean;
  hideLabel?: boolean;
  labelColumns?: number;
  autoOpacity?: boolean;
  onBlur?: () => void;
  onChange?: () => void;
}

export default function ValueComponent(p : ValueComponentProps){
  function updateValue(value: string | boolean | undefined) {

    var parsedValue = p.type != "number" ? value : (isNaN(parseFloat(value as string)) ? null : parseFloat(value as string));

    if (parsedValue === "")
      parsedValue = null;

    if (parsedValue == p.defaultValue && !p.avoidDelete)
      p.binding.deleteValue();
    else
      p.binding.setValue(parsedValue);

    if (p.onChange)
      p.onChange();

    p.dc.refreshView();
  }

  function handleChangeCheckbox(e: React.ChangeEvent<any>) {
    var sender = (e.currentTarget as HTMLInputElement);
    updateValue(sender.checked);
  }

  function handleChangeSelectOrInput(e: React.ChangeEvent<any>) {
    var sender = (e.currentTarget as HTMLSelectElement | HTMLInputElement);
    updateValue(sender.value);
  }



  function renderValue(value: number | string | boolean | null | undefined) {

    const val = value === undefined ? p.defaultValue : value;

    const style = p.hideLabel ? { display: "inline-block" } as React.CSSProperties : undefined;

    if (p.options) {
      return (
        <select className="form-control form-control-sm" style={style} onBlur={p.onBlur}
          value={val == null ? "" : val.toString()} onChange={handleChangeSelectOrInput} >
          {val == null && <option value="">{" - "}</option>}
          {p.options.map((o, i) =>
            <option key={i} value={o.toString()}>{o.toString()}</option>)
          }
        </select>);
    }
    else {

      if (p.type == "boolean") {
        return (<input
          type="checkbox" onBlur={p.onBlur}
          className="form-control"
          checked={value == undefined ? p.defaultValue as boolean : value as boolean}
          onChange={handleChangeCheckbox} />
        );
      }

      if (p.type == "textArea") {
        return (<textarea className="form-control form-control-sm" style={style} onBlur={p.onBlur}
          value={val == null ? "" : val.toString()}
          onChange={handleChangeSelectOrInput} />);
      }

      return (<input className="form-control form-control-sm" style={style} onBlur={p.onBlur}
        type="text"
        value={val == null ? "" : val.toString()}
        onChange={handleChangeSelectOrInput} />);
    }
  }
  const value = p.binding.getValue();


  var opacity = p.autoOpacity && value == null ? { opacity: 0.5 } as React.CSSProperties : undefined;

  if (p.hideLabel) {
    return (
      <div className="form-inline form-sm" style={opacity}>
        {renderValue(value)}
      </div>
    );
  }

  const lc = p.labelColumns;

  return (
    <div className="form-group form-group-sm row" style={opacity}>
      <label className={classes("col-form-label col-form-label-sm", p.labelClass, "col-sm-" + (lc == null ? 2 : lc))}>
        {p.binding.member}
      </label>
      <div className={"col-sm-" + (lc == null ? 10 : 12 - lc)}>
        {renderValue(value)}
      </div>
    </div>
  );
}
