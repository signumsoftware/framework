/// <reference path="../ConfigureReactWidgets.ts" />
import * as React from 'react'
import * as moment from 'moment'
import * as numbro from 'numbro'

import { Dic, addClass, classes } from '../Globals'
import * as DateTimePicker from 'react-widgets/lib/DateTimePicker'
import 'react-widgets/dist/css/react-widgets.css';
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, TypeReference, toMomentFormat, toMomentDurationFormat, toNumbroFormat, isTypeEnum } from '../Reflection'
import { LineBase, LineBaseProps } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum } from '../Signum.Entities'


export interface ValueLineProps extends LineBaseProps, React.Props<ValueLine> {
    valueLineType?: ValueLineType;
    unitText?: React.ReactChild;
    formatText?: string;
    autoTrimString?: boolean;
    autoFixString?: boolean;
    inlineCheckbox?: boolean | "block";
    comboBoxItems?: (OptionItem | MemberInfo | string)[];
    onTextboxBlur?: (val: any) => void;
    valueHtmlAttributes?: React.AllHTMLAttributes<any>;
    extraButtons?: (vl: ValueLine) => React.ReactNode;
    initiallyFocused?: boolean;
}

export interface OptionItem {
    value: any;
    label: string;
}

export type ValueLineType =
    "Checkbox" |
    "ComboBox" |
    "DateTime" |
    "TextBox" |
    "TextArea" |
    "Number" |
    "Decimal" |
    "Color" |
    "TimeSpan";

export class ValueLine extends LineBase<ValueLineProps, ValueLineProps> {

    static autoFixString(str: string, autoTrim: boolean): string {

        if (autoTrim)
            return str && str.trim();

        return str;
    }

    calculateDefaultState(state: ValueLineProps) {
        state.valueLineType = ValueLine.getValueLineType(state.type!);

        if (state.valueLineType == undefined)
            throw new Error(`No ValueLineType found for type '${state.type!.name}' (property route = ${state.ctx.propertyRoute ? state.ctx.propertyRoute.propertyPath() : "??"})`);
    }

    inputElement?: HTMLElement | null;

    componentDidMount() {
        setTimeout(() => {
            let element = this.inputElement;
            if (this.props.initiallyFocused && element) {
                if (element instanceof HTMLInputElement)
                    element.setSelectionRange(0, element.value.length);
                else if (element instanceof HTMLTextAreaElement)
                    element.setSelectionRange(0, element.value.length);
                element.focus();
            }
        }, 0);
    }

    static getValueLineType(t: TypeReference): ValueLineType | undefined {

        if (t.isCollection || t.isLite)
            return undefined;

        if (isTypeEnum(t.name) || t.name == "boolean" && !t.isNotNullable)
            return "ComboBox";

        if (t.name == "boolean")
            return "Checkbox";

        if (t.name == "datetime")
            return "DateTime";

        if (t.name == "string" || t.name == "Guid")
            return "TextBox";

        if (t.name == "number")
            return "Number";

        if (t.name == "decimal")
            return "Decimal";

        if (t.name == "TimeSpan")
            return "TimeSpan";

        return undefined;
    }

    overrideProps(state: ValueLineProps, overridenProps: ValueLineProps) {

        const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
        super.overrideProps(state, overridenProps);
        state.valueHtmlAttributes = valueHtmlAttributes;
    }

    static renderers: {
        [valueLineType: string]: (vl: ValueLine) => JSX.Element;
    } = {};


    renderInternal() {

        if (this.state.visible == false || this.state.hideIfNull && this.state.ctx.value == undefined)
            return null;

        return ValueLine.renderers[this.state.valueLineType!](this);

    }

    static withItemGroup(vl: ValueLine, input: JSX.Element): JSX.Element {
        if (!vl.state.unitText && !vl.state.extraButtons)
            return input;

        return (
            <div className={vl.state.ctx.inputGroupClass}>
                {input}
                {
                    (vl.state.unitText != null || vl.state.extraButtons != null) && <div className="input-group-append">
                    {vl.state.unitText && <span className="input-group-text">{vl.state.unitText}</span>}
                    {vl.state.extraButtons && vl.state.extraButtons(vl)}
                </div>
                }
            </div>
        );
    }

    static isNumber(e: React.KeyboardEvent<any>) {
        const c = e.keyCode;
        return ((c >= 48 && c <= 57) /*0-9*/ ||
            (c >= 96 && c <= 105) /*NumPad 0-9*/ ||
            (c == 8) /*BackSpace*/ ||
            (c == 9) /*Tab*/ ||
            (c == 12) /*Clear*/ ||
            (c == 27) /*Escape*/ ||
            (c == 37) /*Left*/ ||
            (c == 39) /*Right*/ ||
            (c == 46) /*Delete*/ ||
            (c == 36) /*Home*/ ||
            (c == 35) /*End*/ ||
            (c == 109) /*NumPad -*/ ||
            (c == 189) /*-*/ ||
            (e.ctrlKey && c == 86) /*Ctrl + v*/ ||
            (e.ctrlKey && c == 88) /*Ctrl + x*/ ||
            (e.ctrlKey && c == 67) /*Ctrl + c*/);
    }

    static isDecimal(e: React.KeyboardEvent<any>) {
        const c = e.keyCode;
        return (ValueLine.isNumber(e) ||
            (c == 110) /*NumPad Decimal*/ ||
            (c == 190) /*.*/ ||
            (c == 188) /*,*/);
    }

    static isDuration(e: React.KeyboardEvent<any>) {
        const c = e.keyCode;
        return (ValueLine.isNumber(e) ||
            (c == 186) /*Colon*/);
    }
}

ValueLine.renderers["Checkbox" as ValueLineType] = (vl) => {
    const s = vl.state;

    const handleCheckboxOnChange = (e: React.SyntheticEvent<any>) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.checked);
    };

    if (s.inlineCheckbox) {
        return (
            <label className={vl.state.ctx.error} style={{ display: s.inlineCheckbox == "block" ? "block" : undefined }} {...vl.baseHtmlAttributes() } { ...s.formGroupHtmlAttributes }>
                <input type="checkbox" {...vl.state.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange} disabled={s.ctx.readOnly} />
                {" " + s.labelText}
            </label>
        );
    }
    else {
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }}>
                <input type="checkbox" {...vl.state.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange}
                    className={addClass(vl.state.valueHtmlAttributes, s.ctx.formControlClass)} disabled={s.ctx.readOnly} />
            </FormGroup>
        );
    }
};

ValueLine.renderers["ComboBox"] = (vl) => {
    return internalComboBox(vl);
};

function getOptionsItems(vl: ValueLine): OptionItem[]{

    var ti = getTypeInfo(vl.state.type!.name);
    if (vl.state.comboBoxItems)
        return vl.state.comboBoxItems.map(a =>
            typeof a == "string" ? toOptionItem(ti.members[a]) :
                toOptionItem(a));

    if (vl.state.type!.name == "boolean")
        return ([
            { label: BooleanEnum.niceToString("False")!, value: false },
            { label: BooleanEnum.niceToString("True")!, value: true }
        ]);

    return Dic.getValues(ti.members).map(m => toOptionItem(m));
}

function toOptionItem(m: MemberInfo | OptionItem): OptionItem {

    if ((m as MemberInfo).name)
        return {
            value: (m as MemberInfo).name,
            label: (m as MemberInfo).niceName,
        };

    return m as OptionItem;
}

function internalComboBox(vl: ValueLine) {

    var optionItems = getOptionsItems(vl);

    const s = vl.state;
    if (!s.type!.isNotNullable || s.ctx.value == undefined)
        optionItems = [{ value: null, label: " - " }].concat(optionItems);

    if (s.ctx.readOnly) {

        var label = null;
        if (s.ctx.value != undefined) {

            var item = optionItems.filter(a => a.value == s.ctx.value).singleOrNull();

            label = item ? item.label : s.ctx.value.toString();
        }

        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
                {ValueLine.withItemGroup(vl,
                    <FormControlReadonly htmlAttributes={{
                        ...vl.state.valueHtmlAttributes,
                        ...({ 'data-value': s.ctx.value } as any) /*Testing*/
                    }} ctx={s.ctx}>
                        {label}
                    </FormControlReadonly>)}
            </FormGroup>
        );
    }

    function toStr(val: any){
        return val == null ? "" :
            val === true ? "True" :
                val === false ? "False" :
                    val.toString();
    }

    const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
        const input = e.currentTarget as HTMLInputElement;
        const option = optionItems.filter(a => toStr(a.value) == input.value).single();
        vl.setValue(option.value);
    };

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
            {ValueLine.withItemGroup(vl,
                <select {...vl.state.valueHtmlAttributes} value={toStr(s.ctx.value)} className={addClass(vl.state.valueHtmlAttributes, s.ctx.formControlClass)} onChange={handleEnumOnChange} >
                    {optionItems.map((oi, i) => <option key={i} value={toStr(oi.value)}>{oi.label}</option>)}
                </select>)
            }
        </FormGroup>
    );

}

ValueLine.renderers["TextBox" as ValueLineType] = (vl) => {

    const s = vl.state;

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
                {ValueLine.withItemGroup(vl, <FormControlReadonly htmlAttributes={vl.state.valueHtmlAttributes} ctx={s.ctx}>{s.ctx.value}</FormControlReadonly>)}
            </FormGroup>
        );

    const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.value);
    };

    let handleBlur: ((e: React.SyntheticEvent<any>) => void) | undefined = undefined;
    if (s.autoFixString != false) {
        handleBlur = (e: React.SyntheticEvent<any>) => {
            const input = e.currentTarget as HTMLInputElement;
            var fixed = ValueLine.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : true);
            if (fixed != input.value)
                vl.setValue(fixed);

            if (vl.props.onTextboxBlur)
                vl.props.onTextboxBlur(fixed);
        };
    }


    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
            {ValueLine.withItemGroup(vl,
                <input type="text" {...vl.state.valueHtmlAttributes}
                    className={addClass(vl.state.valueHtmlAttributes, s.ctx.formControlClass)}
                    value={s.ctx.value || ""}
                    onBlur={handleBlur}
                    onChange={isIE11() ? undefined : handleTextOnChange} //https://github.com/facebook/react/issues/7211
                    onInput={isIE11() ? handleTextOnChange : undefined}
                    placeholder={s.ctx.placeholderLabels ? asString(s.labelText) : undefined}
                    ref={elment => vl.inputElement = elment} />)
            }
        </FormGroup>
    );
};

function asString(reactChild: React.ReactChild | undefined): string | undefined {
    if (typeof reactChild == "string")
        return reactChild as string;

    return undefined;
}

function isIE11(): boolean {
    return (!!(window as any).MSInputMethodContext && !!(document as any).documentMode);
}

ValueLine.renderers["TextArea" as ValueLineType] = (vl) => {

    const s = vl.state;

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
                <textarea {...vl.state.valueHtmlAttributes} className={addClass(vl.state.valueHtmlAttributes, s.ctx.formControlClass)} value={s.ctx.value || ""}
                    disabled={true} />
            </FormGroup>
        );

    const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.value);
    };

    let handleBlur: ((e: React.SyntheticEvent<any>) => void) | undefined = undefined;
    if (s.autoFixString != false) {
        handleBlur = (e: React.SyntheticEvent<any>) => {
            const input = e.currentTarget as HTMLInputElement;
            var fixed = ValueLine.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : false);
            if (fixed != input.value)
                vl.setValue(fixed);

            if (vl.props.onTextboxBlur)
                vl.props.onTextboxBlur(fixed);
        };
    }

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
            <textarea {...vl.state.valueHtmlAttributes} className={addClass(vl.state.valueHtmlAttributes, s.ctx.formControlClass)} value={s.ctx.value || ""}
                onChange={isIE11() ? undefined : handleTextOnChange} //https://github.com/facebook/react/issues/7211 && https://github.com/omcljs/om/issues/704
                onInput={isIE11() ? handleTextOnChange : undefined}
                onBlur={handleBlur}
                placeholder={s.ctx.placeholderLabels ? asString(s.labelText) : undefined}
                ref={elment => vl.inputElement = elment} />
        </FormGroup>
    );
};

ValueLine.renderers["Number" as ValueLineType] = (vl) => {
    return numericTextBox(vl, ValueLine.isNumber);
};

ValueLine.renderers["Decimal" as ValueLineType] = (vl) => {
    return numericTextBox(vl, ValueLine.isDecimal);
};

function numericTextBox(vl: ValueLine, validateKey: React.KeyboardEventHandler<any>) {
    const s = vl.state

    const numbroFormat = toNumbroFormat(s.formatText);

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
                {ValueLine.withItemGroup(vl,
                    <FormControlReadonly htmlAttributes={vl.state.valueHtmlAttributes} ctx={s.ctx} className="numeric">
                        {s.ctx.value == null ? "" : numbro(s.ctx.value).format(numbroFormat)}
                    </FormControlReadonly>)}
            </FormGroup>
        );

    const handleOnChange = (newValue: number | null) => {
        vl.setValue(newValue);
    };

    const htmlAttributes = {
        placeholder: s.ctx.placeholderLabels ? asString(s.labelText) : undefined,
        ...vl.props.valueHtmlAttributes
    } as React.AllHTMLAttributes<any>;

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
            {ValueLine.withItemGroup(vl,
                <NumericTextBox
                    htmlAttributes={htmlAttributes}
                    value={s.ctx.value}
                    onChange={handleOnChange}
                    formControlClass={s.ctx.formControlClass}
                    validateKey={validateKey}
                    format={numbroFormat}
                    />
            )}
        </FormGroup>
    );
}

export interface NumericTextBoxProps {
    value: number;
    onChange: (newValue: number | null) => void;
    validateKey: React.KeyboardEventHandler<any>;
    format?: string;
    formControlClass?: string;
    htmlAttributes?: React.HTMLAttributes<HTMLInputElement>;
}

export class NumericTextBox extends React.Component<NumericTextBoxProps, { text?: string }> {

    constructor(props: NumericTextBoxProps) {
        super(props);
        this.state = { text: undefined };
    }

    render() {

        const value = this.state.text != undefined ? this.state.text :
            this.props.value != undefined ? numbro(this.props.value).format(this.props.format) :
                "";

        return <input {...this.props.htmlAttributes} type="text" className={addClass(this.props.htmlAttributes, classes(this.props.formControlClass, "numeric"))} value={value}
            onBlur={this.handleOnBlur}
            onChange={isIE11() ? undefined : this.handleOnChange} //https://github.com/facebook/react/issues/7211
            onInput={isIE11() ? this.handleOnChange : undefined}
            onKeyDown={this.handleKeyDown} />

    }

    handleOnBlur = (e: React.SyntheticEvent<any>) => {
        const input = e.currentTarget as HTMLInputElement;

        let value = ValueLine.autoFixString(input.value, false);

        if (this.props.format && this.props.format.endsWith("%"))
        {
            if (value && !value.endsWith("%"))
                value += "%";
        }

        const result = value == undefined || value.length == 0 ? null : numbro.unformat(value, this.props.format);
        this.setState({ text: undefined });
        this.props.onChange(result);
    }

    handleOnChange = (e: React.SyntheticEvent<any>) => {
        const input = e.currentTarget as HTMLInputElement;
        this.setState({ text: input.value });
    }

    handleKeyDown = (e: React.KeyboardEvent<any>) => {
        if (!this.props.validateKey(e))
            e.preventDefault();
    }
}

ValueLine.renderers["DateTime" as ValueLineType] = (vl) => {

    const s = vl.state;

    const momentFormat = toMomentFormat(s.formatText);

    const m = s.ctx.value ? moment(s.ctx.value, moment.ISO_8601) : undefined;
    const showTime = momentFormat != "L" && momentFormat != "LL";

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
                {ValueLine.withItemGroup(vl, <FormControlReadonly htmlAttributes={vl.state.valueHtmlAttributes} ctx={s.ctx}>{m && m.format(momentFormat)}</FormControlReadonly>)}
            </FormGroup>
        );

    const handleDatePickerOnChange = (date?: Date, str?: string) => {

        const m = showTime ? moment(date) : moment(date).startOf("day");
        vl.setValue(m.isValid() ? m.format() : null);
    };

    let currentDate = moment();
    if (!showTime)
        currentDate = currentDate.startOf("day");

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
            {ValueLine.withItemGroup(vl,
                <div className={s.ctx.rwWidgetClass}>
                <DateTimePicker value={m && m.toDate()} onChange={handleDatePickerOnChange}
                        format={momentFormat} time={showTime} defaultCurrentDate={currentDate.toDate()} />
                </div>
            )}
        </FormGroup>
    );
}


ValueLine.renderers["TimeSpan" as ValueLineType] = (vl) => {
    return durationTextBox(vl, ValueLine.isDuration);
};

function durationTextBox(vl: ValueLine, validateKey: React.KeyboardEventHandler<any>) {

    const s = vl.state;

    const durationFormat = toMomentDurationFormat(s.formatText);

    const ticksPerMillisecond = 10000;

    if (s.ctx.readOnly) {
        const d = s.ctx.value ? moment.duration(s.ctx.value / ticksPerMillisecond) : undefined;
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
                {ValueLine.withItemGroup(vl,
                    <FormControlReadonly htmlAttributes={vl.state.valueHtmlAttributes} ctx={s.ctx} className={addClass(vl.state.valueHtmlAttributes, "numeric")}>{d && d.format(durationFormat)}</FormControlReadonly>
                )}
            </FormGroup>
        );
    }

    const handleOnChange = (newValue: number | null) => {
        const d = newValue == null ? null : moment.duration(newValue);

        vl.setValue(moment.isDuration(d) ? (d.asMilliseconds() * ticksPerMillisecond) : null);
    };

    const htmlAttributes = {
        placeholder: s.ctx.placeholderLabels ? asString(s.labelText) : undefined,
        ...vl.props.valueHtmlAttributes
    } as React.AllHTMLAttributes<any>;

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
            {ValueLine.withItemGroup(vl,
                <DurationTextBox htmlAttributes={htmlAttributes}
                    value={s.ctx.value}
                    onChange={handleOnChange}
                    validateKey={validateKey}
                    formControlClass={s.ctx.formControlClass}
                    format={durationFormat} />
            )}
        </FormGroup>
    );
}

export interface DurationTextBoxProps {
    value: number;
    onChange: (newValue: number | null) => void;
    validateKey: React.KeyboardEventHandler<any>;
    formControlClass?: string;
    format?: string;
    htmlAttributes: React.HTMLAttributes<HTMLInputElement>;
}

export class DurationTextBox extends React.Component<DurationTextBoxProps, { text?: string }> {

    constructor(props: DurationTextBoxProps) {
        super(props);
        this.state = { text: undefined };
    }

    render() {
        const ticksPerMillisecond = 10000;
        const value = this.state.text != undefined ? this.state.text :
            this.props.value != undefined ? moment.duration(this.props.value / ticksPerMillisecond).format(this.props.format) :
                "";

        return <input {...this.props.htmlAttributes} type="text" className={addClass(this.props.htmlAttributes, classes(this.props.formControlClass, "numeric"))} value={value}
            onBlur={this.handleOnBlur}
            onChange={isIE11() ? undefined : this.handleOnChange} //https://github.com/facebook/react/issues/7211
            onInput={isIE11() ? this.handleOnChange : undefined}
            onKeyDown={this.handleKeyDown} />

    }

    handleOnBlur = (e: React.SyntheticEvent<any>) => {
        const input = e.currentTarget as HTMLInputElement;
        const result = input.value == undefined || input.value.length == 0 ? null : moment.duration(input.value).asMilliseconds();
        this.setState({ text: undefined });
        this.props.onChange(result);
    }

    handleOnChange = (e: React.SyntheticEvent<any>) => {
        const input = e.currentTarget as HTMLInputElement;
        this.setState({ text: input.value });
    }

    handleKeyDown = (e: React.KeyboardEvent<any>) => {
        if (!this.props.validateKey(e))
            e.preventDefault();
    }
}
