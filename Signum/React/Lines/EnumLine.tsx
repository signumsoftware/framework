import * as React from 'react'
import { DropdownList, Combobox, Value } from 'react-widgets-up'
import { Dic, classes } from '../Globals'
import { MemberInfo, tryGetTypeInfo } from '../Reflection'
import { genericMemo, LineBaseController, LineBaseProps, setRefProp, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum } from '../Signum.Entities'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { ValueBaseController, ValueBaseProps } from './ValueBase'
import { JSX } from 'react/jsx-runtime'

export interface EnumLineProps<V extends string | number | boolean | null> extends ValueBaseProps<V> {
  lineType?:
  "DropDownList" | /*For Enums! (only values in optionItems can be selected)*/
  "ComboBoxText" | /*For Text! (with freedom to choose a different value not in optionItems)*/
  "RadioGroup";
  emptyLabel?: string;
  optionItems?: (OptionItem | MemberInfo | V)[];
  onRenderDropDownListItem?: (oi: OptionItem) => React.ReactNode;
  optionHtmlAttributes?: (oi: OptionItem) => React.OptionHTMLAttributes<HTMLOptionElement>;
  columnCount?: number;
  columnWidth?: number;
  ref?: React.Ref<EnumLineController<V>>;
}

export class EnumLineController<V extends string | number | boolean | null> extends ValueBaseController<EnumLineProps<V>, V> {

}

export interface OptionItem {
  value: any;
  label: string;
}

export const EnumLine: <V extends string | number | boolean | null>(props: EnumLineProps<V>) => React.ReactNode | null
  = genericMemo(function EnumLine<V extends string | number | boolean | null>(props: EnumLineProps<V>) {

    const c = useController(EnumLineController<V>, props);

    if (c.isHidden)
      return null;

    return props.lineType == 'ComboBoxText' ? internalComboBoxText(c) :
      props.lineType == 'RadioGroup' ? internalRadioGroup(c) :
        internalDropDownList(c);
  }, (prev, next) => {
    if (prev.optionHtmlAttributes || next.optionHtmlAttributes)
      return false;

    if (prev.onRenderDropDownListItem || next.onRenderDropDownListItem)
      return false;

    return LineBaseController.propEquals(prev, next);
  });

function internalDropDownList<V extends string | number | boolean | null>(c: EnumLineController<V>) {

  var optionItems = getOptionsItems(c);
  const p = c.props;
  if (!p.type!.isNotNullable || p.ctx.value == undefined)
    optionItems = [{ value: null, label: p.emptyLabel ?? " - " }].concat(optionItems);

  const isLabelVisible = !(p.ctx.formGroupStyle === "SrOnly" || "visually-hidden");
  var ariaAtts = p.ctx.readOnly ? c.baseAriaAttributes() : c.extendedAriaAttributes();
  if (!isLabelVisible && p.label) {
    ariaAtts = { ...ariaAtts, "aria-label": typeof p.label === "string" ? p.label : String(p.label) };
  }

  var htmlAtts = c.props.valueHtmlAttributes;
  var mergedHtml = { ...htmlAtts, ...ariaAtts };

  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  let niceValue: string | undefined = undefined;
  if (p.ctx.value != undefined) {

    var item = optionItems.filter(a => a.value == p.ctx.value).singleOrNull();

    niceValue = item ? item.label : p.ctx.value.toString();
  }

  if (p.ctx.readOnly) {

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
        {inputId =>
          c.withItemGroup(
            <FormControlReadonly
              id={inputId}
              htmlAttributes={{
                ...mergedHtml,
                ...({ 'data-value': p.ctx.value } as any), /*Testing*/
              }} ctx={p.ctx} innerRef={c.setRefs}>
              {c.props.onRenderDropDownListItem ? (p.ctx.value == undefined ? undefined : c.props.onRenderDropDownListItem({ label: niceValue!, value: p.ctx.value })) : niceValue}
            </FormControlReadonly>)
        }
      </FormGroup>
    );
  }

  if (c.props.onRenderDropDownListItem) {
    var oi = optionItems.singleOrNull(a => a.value == p.ctx.value) ?? {
      value: p.ctx.value,
      label: p.ctx.value,
    };

    function renderElement({ item }: any) {
      var result = c.props.onRenderDropDownListItem!(item) as React.ReactElement;
      return React.cloneElement(result, { 'data-value': item.value } as any);
    }

    //const renderElement = ({ item }: any) => (
    //  <div role="option" aria-selected={oi?.value === item.value}>
    //    {c.props.onRenderDropDownListItem ? c.props.onRenderDropDownListItem(item) : <span>{item.label}</span>}
    //    <span className="sr-only">{item.label}</span>
    //  </div>
    //);

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
        {inputId => c.withItemGroup(
          <DropdownList<OptionItem> className={classes(c.props.valueHtmlAttributes?.className, p.ctx.formControlClass, c.mandatoryClass, "p-0")} data={optionItems}
            id={inputId}
            onChange={(oe, md) => c.setValue(oe.value, md.originalEvent)}
            value={oi}
            autoComplete="off"
            dataKey="value"
            textField="label"
            renderValue={renderElement}
            renderListItem={renderElement}
            title={niceValue}
            inputProps={{
              value: oi?.label ?? "",
              role: "combobox",
              "aria-haspopup": "listbox",
              "aria-expanded": false,
              "aria-controls": `${inputId}_listbox`,
              "aria-label": p.label ?? "Auswahl"
            }}
            listProps={{
              role: "listbox",
              id: `${inputId}_listbox`,
            }}
            {...(p.valueHtmlAttributes as any)}
          />)
        }
      </FormGroup>
    );
  } else {

    const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      const option = optionItems.filter(a => toStr(a.value) == input.value).single();
      c.setValue(option.value, e);
    };

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
        {inputId => c.withItemGroup(
          <select id={inputId} title={niceValue} {...c.props.valueHtmlAttributes} value={toStr(p.ctx.value)} className={classes(c.props.valueHtmlAttributes?.className, p.ctx.formSelectClass, c.mandatoryClass)} onChange={handleEnumOnChange} >
            {!optionItems.some(a => toStr(a.value) == toStr(p.ctx.value)) && <option key={-1} value={toStr(p.ctx.value)}>{toStr(p.ctx.value)}</option>}
            {optionItems.map((oi, i) => <option key={i} value={toStr(oi.value)} {...p.optionHtmlAttributes?.(oi)}>{oi.label}</option>)}
          </select>)
        }
      </FormGroup>
    );
  }
}

function toStr(val: any) {
  return val == null ? "" :
    val === true ? "True" :
      val === false ? "False" :
        val.toString();
}

function internalComboBoxText<V extends string | number | boolean | null>(c: EnumLineController<V>) {

  var optionItems = getOptionsItems(c);

  const p = c.props;
  if (!p.type!.isNotNullable || p.ctx.value == undefined)
    optionItems = [{ value: null, label: " - " }].concat(optionItems);

  const isLabelVisible = !(p.ctx.formGroupStyle === "SrOnly" || "visually-hidden");
  var ariaAtts = p.ctx.readOnly ? c.baseAriaAttributes() : c.extendedAriaAttributes();
  if (!isLabelVisible && p.label) {
    ariaAtts = { ...ariaAtts, "aria-label": typeof p.label === "string" ? p.label : String(p.label) };
  }

  var htmlAtts = c.props.valueHtmlAttributes;
  var mergedHtmlReadOnly = { ...htmlAtts, ...ariaAtts };

  if (p.ctx.readOnly) {

    var label: string | null = null;
    if (p.ctx.value != undefined) {

      var item = optionItems.filter(a => a.value == p.ctx.value).singleOrNull();

      label = item ? item.label : p.ctx.value.toString();
    }

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
        {inputId => c.withItemGroup(
          <FormControlReadonly id={inputId} htmlAttributes={{
            ...mergedHtmlReadOnly,
            ...({ 'data-value': p.ctx.value } as any) /*Testing*/
          }} ctx={p.ctx} innerRef={c.setRefs}>
            {label}
          </FormControlReadonly>)}
      </FormGroup>
    );
  }

  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  var renderItem = c.props.onRenderDropDownListItem ? (a: any) => c.props.onRenderDropDownListItem!(a.item) : undefined;

  return (
    <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
      {inputId => c.withItemGroup(
        <Combobox<OptionItem>
          id={inputId}
          className={classes(c.props.valueHtmlAttributes?.className, p.ctx.formControlClass, c.mandatoryClass)} data={optionItems}
          onChange={(e: string | OptionItem, md) => {
            c.setValue(e == null ? null : typeof e == "string" ? e : e.value, md.originalEvent);
          }}
          value={p.ctx.value}
          dataKey="value"
          textField="label"
          focusFirstItem
          autoSelectMatches
          renderListItem={renderItem}
          {...(p.valueHtmlAttributes as any)}
        />
      )
      }
    </FormGroup>
  );
}

function internalRadioGroup<V extends string | number | boolean | null>(c: EnumLineController<V>) {

  var optionItems = getOptionsItems(c);
  const baseId = React.useId();

  const p = c.props;
  var ariaAtts = p.ctx.readOnly ? c.baseAriaAttributes() : c.extendedAriaAttributes();

  const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    const option = optionItems.filter(a => toStr(a.value).toLowerCase() == input.value.toLowerCase()).single();
    c.setValue(option.value, e);
  };

  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  return (
    <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
      {inputId => <>
        {getTimeMachineIcon({ ctx: p.ctx })}
        <div style={getColumnStyle()}>
          {optionItems.map((oi, i) =>
            <label key={i} htmlFor={baseId + "-" + i} {...c.props.valueHtmlAttributes} className={classes("sf-radio-element", c.getErrorClass())}>
              <input id={baseId + "-" + i} type="radio" value={oi.value} checked={p.ctx.value == oi.value} onChange={handleEnumOnChange} disabled={p.ctx.readOnly} />
              {" " + oi.label}
            </label>)}
        </div>
      </>}
    </FormGroup>
  );

  function getColumnStyle(): React.CSSProperties | undefined {

    const p = c.props;

    if (p.columnCount && p.columnWidth)
      return {
        columns: `${p.columnCount} ${p.columnWidth}px`,
      };

    if (p.columnCount)
      return {
        columnCount: p.columnCount,
      };

    if (p.columnWidth)
      return {
        columnWidth: p.columnWidth,
      };

    return undefined;
  }
}


function getOptionsItems(el: EnumLineController<any>): OptionItem[] {

  var ti = tryGetTypeInfo(el.props.type!.name);

  if (el.props.optionItems) {
    return el.props.optionItems
      .map(a => typeof a == "string" && ti != null && ti.kind == "Enum" ? toOptionItem(ti.members[a]) : toOptionItem(a))
      .filter(a => !!a);
  }

  if (el.props.type!.name == "boolean")
    return ([
      { label: BooleanEnum.niceToString("False")!, value: false },
      { label: BooleanEnum.niceToString("True")!, value: true }
    ]);

  if (ti != null && ti.kind == "Enum")
    return Dic.getValues(ti.members).map(m => toOptionItem(m));

  throw new Error("Unable to get Options from " + el.props.type!.name);
}

function toOptionItem(m: MemberInfo | OptionItem | string): OptionItem {

  if (typeof m == "string")
    return {
      value: m,
      label: m,
    }

  if ((m as MemberInfo).name)
    return {
      value: (m as MemberInfo).name,
      label: (m as MemberInfo).niceName,
    };

  return m as OptionItem;
}
