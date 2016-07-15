import * as React from 'react'
import * as moment from 'moment'
import * as numbro from 'numbro'

import { Dic, addClass } from '../Globals'
import { DateTimePicker } from 'react-widgets'
import 'react-widgets/dist/css/react-widgets.css';
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, TypeReference, toMomentFormat, toMomentDurationFormat } from '../Reflection'
import { LineBase, LineBaseProps, runTasks, FormGroup, FormControlStatic } from '../Lines/LineBase'


export interface ValueLineProps extends LineBaseProps, React.Props<ValueLine> {
    valueLineType?: ValueLineType;
    unitText?: string;
    formatText?: string;
    autoTrim?: boolean;
    inlineCheckbox?: boolean;
    comboBoxItems?: { name: string, niceName: string }[];
    valueHtmlProps?: React.HTMLAttributes;
}


export enum ValueLineType {
    Boolean = "Boolean" as any,
    Enum = "Enum" as any,
    DateTime = "DateTime" as any,
    TextBox = "TextBox" as any,
    TextArea = "TextArea" as any,
    Number = "Number" as any,
    Decimal = "Decimal" as any,
    Color = "Color" as any,
    TimeSpan = "TimeSpan" as any,
}


export class ValueLine extends LineBase<ValueLineProps, ValueLineProps> {

    calculateDefaultState(state: ValueLineProps) {
        state.valueLineType = this.calculateValueLineType(state);
    }

    calculateValueLineType(state : ValueLineProps): ValueLineType {

        const t = state.type;

        if (t.isCollection || t.isLite)
            throw new Error("ValueLine not implemented for " + JSON.stringify(t));

        if (t.isEnum || t.name == "boolean" && !t.isNotNullable)
            return ValueLineType.Enum;

        if (t.name == "boolean")
            return ValueLineType.Boolean;

        if (t.name == "datetime")
            return ValueLineType.DateTime;

        if (t.name == "string" || t.name == "TimeSpan")
            return ValueLineType.TextBox;

        if (t.name == "number")
            return ValueLineType.Number;

        if (t.name == "decimal")
            return ValueLineType.Decimal;

        if (t.name == "timespan")
            return ValueLineType.TimeSpan;

        throw new Error(`No value line found for '${t.name}' (property route = ${state.ctx.propertyRoute ? state.ctx.propertyRoute.propertyPath() : "??"})`);
    }

    overrideProps(state: ValueLineProps, overridenProps: ValueLineProps) {
        const valueHtmlProps = Dic.extend(state.valueHtmlProps, overridenProps.valueHtmlProps);
        super.overrideProps(state, overridenProps);
        state.valueHtmlProps = valueHtmlProps;
    }

    static renderers: {
        [valueLineType: string]: (vl: ValueLine) => JSX.Element;
    } = {};
    

    renderInternal() {

        if (this.state.visible == false || this.state.hideIfNull && this.state.ctx.value == undefined)
            return null;

        return ValueLine.renderers[this.state.valueLineType!](this);

    }

    static withUnit(unit: string | undefined, input: JSX.Element): JSX.Element {
        if (!unit)
            return input;

        return (
            <div className="input-group">
                {input}
                <span className="input-group-addon">{unit}</span>
            </div>
        );
    }

    static isNumber(e: React.KeyboardEvent) {
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
            (e.ctrlKey && c == 67) /*Ctrl + v*/);
    }

    static isDecimal(e: React.KeyboardEvent) {
        const c = e.keyCode;
        return (ValueLine.isNumber(e) ||
            (c == 110) /*NumPad Decimal*/ ||
            (c == 190) /*.*/ ||
            (c == 188) /*,*/);
    }

    static isDuration(e: React.KeyboardEvent) {
        const c = e.keyCode;
        return (ValueLine.isNumber(e) ||
            (c == 186) /*Colon*/);
    }
}

ValueLine.renderers[ValueLineType.Boolean as any] = (vl) => {
    const s = vl.state;
    
    const handleCheckboxOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.checked);
    };

    if (s.inlineCheckbox) {
        return (
            <label className={vl.state.ctx.error} {...vl.baseHtmlProps() }>
                <input type="checkbox" {...vl.state.valueHtmlProps} checked={s.ctx.value || false} onChange={handleCheckboxOnChange} disabled={s.ctx.readOnly}/>
                { " " + s.labelText}
            </label>
        );
    }
    else {
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) }>
                <input type="checkbox" {...vl.state.valueHtmlProps} checked={s.ctx.value || false} onChange={handleCheckboxOnChange}
                    className={addClass(vl.state.valueHtmlProps, "form-control")} disabled={s.ctx.readOnly}/>
            </FormGroup>
        );
    }
};

ValueLine.renderers[ValueLineType.Enum as any] = (vl) => {

    if (vl.state.type.name == "boolean")
        return internalComboBox(vl, getTypeInfo("BooleanEnum"),
            str => str == "True" ? true : false,
            val => val == true ? "True" : "False");

    return internalComboBox(vl, getTypeInfo(vl.state.type.name), str => str, str => str);
};

function internalComboBox(vl: ValueLine, typeInfo: TypeInfo, parseValue: (str: string) => any, toStringValue: (val: any) => string) {

    const s = vl.state;
    let items = s.comboBoxItems || Dic.getValues(typeInfo.members);

    if (!s.type.isNotNullable || s.ctx.value == undefined)
        items = [{ name: "", niceName: " - " }].concat(items);

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
                { ValueLine.withUnit(s.unitText,
                    <FormControlStatic {...vl.state.valueHtmlProps} ctx={s.ctx}>
                           {s.ctx.value == undefined ? undefined : items.filter(a => a.name == toStringValue(s.ctx.value)).single().niceName}
                    </FormControlStatic>) }
            </FormGroup>
        );

    const handleEnumOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        const val = input.value;
        vl.setValue(val == "" ? undefined : parseValue(val));
    };

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
            { ValueLine.withUnit(s.unitText,
                <select {...vl.state.valueHtmlProps} value={s.ctx.value == undefined ? "" : toStringValue(s.ctx.value)} className={addClass(vl.state.valueHtmlProps, "form-control") } onChange={ handleEnumOnChange } >
                        {items.map((mi, i) => <option key={i} value={mi.name}>{mi.niceName}</option>) } 
                </select>)
            }
        </FormGroup>
    );

}

ValueLine.renderers[ValueLineType.TextBox as any] = (vl) => {

    const s = vl.state;

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
                { ValueLine.withUnit(s.unitText, <FormControlStatic {...vl.state.valueHtmlProps} ctx={s.ctx}>{s.ctx.value}</FormControlStatic>) }
            </FormGroup>
        );

    const handleTextOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.value);
    };

    let handleBlur: ((e: React.SyntheticEvent) => void) | undefined = undefined;
    if (s.autoTrim == undefined || s.autoTrim == true) {
        handleBlur = (e: React.SyntheticEvent) => {
            const input = e.currentTarget as HTMLInputElement;
            if (input.value && input.value.trim() != input.value)
                vl.setValue(input.value.trim());
        };
    }

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
            { ValueLine.withUnit(s.unitText,
                <input type="text" {...vl.state.valueHtmlProps}
                    className={addClass(vl.state.valueHtmlProps, "form-control") }
                    value={s.ctx.value || ""}
                    onBlur={handleBlur}
                    onChange={handleTextOnChange}
                    placeholder={s.ctx.placeholderLabels ? asString(s.labelText) : undefined}/>)
            }
        </FormGroup>
    );
};

function asString(reactChild: React.ReactChild | undefined): string | undefined{
    if (typeof reactChild == "string")
        return reactChild as string;

    return undefined;
}

ValueLine.renderers[ValueLineType.TextArea as any] = (vl) => {

    const s = vl.state;

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
                { ValueLine.withUnit(s.unitText, <FormControlStatic {...vl.state.valueHtmlProps} ctx={s.ctx}>{s.ctx.value}</FormControlStatic>) }
            </FormGroup>
        );

    const handleTextOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.value);
    };

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
            <textarea {...vl.state.valueHtmlProps} className={addClass(vl.state.valueHtmlProps, "form-control") } value={s.ctx.value || ""} onChange={handleTextOnChange}
                placeholder={s.ctx.placeholderLabels ? asString(s.labelText) : undefined}/>
        </FormGroup>
    );
};

ValueLine.renderers[ValueLineType.Number as any] = (vl) => {
    return numericTextBox(vl, ValueLine.isNumber);
};

ValueLine.renderers[ValueLineType.Decimal as any] = (vl) => {
    return numericTextBox(vl, ValueLine.isDecimal);
};

function numericTextBox(vl: ValueLine, validateKey: React.KeyboardEventHandler) {
    const s = vl.state

    const numeralFormat = toNumeralFormat(s.formatText); 

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
                { ValueLine.withUnit(s.unitText,
                    <FormControlStatic {...vl.state.valueHtmlProps} ctx={s.ctx} className={addClass(vl.state.valueHtmlProps, "numeric") }>
                        {numbro(s.ctx.value).format(numeralFormat) }
                    </FormControlStatic>) }
            </FormGroup>
        );

    const handleOnChange = (newValue: number) => {
        vl.setValue(newValue);
    };

    const htmlProps = Dic.extend(
        { placeholder: s.ctx.placeholderLabels ? asString(s.labelText) : undefined } as React.HTMLAttributes,
        vl.props.valueHtmlProps);

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
            { ValueLine.withUnit(s.unitText,
                <NumericTextBox
                    htmlProps={htmlProps}
                    value={s.ctx.value}
                    onChange={handleOnChange}
                    validateKey={validateKey}
                    format={ numeralFormat}
                    />
            ) }
        </FormGroup>
    );
}

function toNumeralFormat(format: string | undefined) {

    if (format == undefined)
        return undefined;

    if (format.startsWith("C"))
        return "0." + "0".repeat(parseInt(format.after("C")));

    if (format.startsWith("N"))
        return "0." + "0".repeat(parseInt(format.after("N")));

    if (format.startsWith("D"))
        return "0".repeat(parseInt(format.after("D")));

    if (format.startsWith("E"))
        return "0." + "0".repeat(parseInt(format.after("E")));

    if (format.startsWith("P"))
        return "0." + "0".repeat(parseInt(format.after("P"))) + "%";

    return format;
}

export interface NumericTextBoxProps {
    value: number;
    onChange: (newValue?: number) => void;
    validateKey: React.KeyboardEventHandler;
    format?: string;
    htmlProps: React.HTMLAttributes;
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

        return <input {...this.props.htmlProps} type="text" className={addClass(this.props.htmlProps, "form-control numeric") } value={value}
            onBlur={this.handleOnBlur}
            onChange={this.handleOnChange}
            onKeyDown={this.handleKeyDown}/>

    }

    handleOnBlur = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        const result = input.value == undefined || input.value.length == 0 ? undefined : numbro(input.value).value();
        this.setState({ text: undefined });
        this.props.onChange(result);
    }

    handleOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        this.setState({ text: input.value });
    }

    handleKeyDown = (e: React.KeyboardEvent) => {
        if (!this.props.validateKey(e))
            e.preventDefault();
    }
}

ValueLine.renderers[ValueLineType.DateTime as any] = (vl) => {

    const s = vl.state;

    const momentFormat = toMomentFormat(s.formatText);

    const m = s.ctx.value ? moment(s.ctx.value, moment.ISO_8601()) : undefined;
    const showTime = momentFormat != "L" && momentFormat != "LL";

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
                { ValueLine.withUnit(s.unitText, <FormControlStatic {...vl.state.valueHtmlProps} ctx={s.ctx}>{m && m.format(momentFormat) }</FormControlStatic>) }
            </FormGroup>
        );

    const handleDatePickerOnChange = (date: Date, str: string) => {

        const m = moment(date);
        vl.setValue(m.isValid() ? m.format(moment.ISO_8601()) : undefined);
    };

    let currentDate = moment();
    if (!showTime)
        currentDate = currentDate.startOf("day");
    
    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
            { ValueLine.withUnit(s.unitText,
                <DateTimePicker value={m && m.toDate() } onChange={handleDatePickerOnChange} format={momentFormat} time={showTime} defaultCurrentDate={currentDate.toDate() }  />
            ) }
        </FormGroup>
    );
}

ValueLine.renderers[ValueLineType.TimeSpan as any] = (vl) => {
    return durationTextBox(vl, ValueLine.isDuration);
};

function durationTextBox(vl: ValueLine, validateKey: React.KeyboardEventHandler) {

    const s = vl.state;

    const durationFormat = toMomentDurationFormat(s.formatText);

    const ticksPerMillisecond = 10000;

    if (s.ctx.readOnly) {
        const d = s.ctx.value ? moment.duration(s.ctx.value / ticksPerMillisecond) : undefined;
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
                <FormControlStatic {...vl.state.valueHtmlProps} ctx={s.ctx} className={addClass(vl.state.valueHtmlProps, "numeric") }>{d && d.format(durationFormat) }</FormControlStatic>
            </FormGroup>
        );
    }

    const handleOnChange = (newValue: number) => {
        const d = moment.duration(newValue);

        vl.setValue(moment.isDuration(d) ? (d.asMilliseconds() * ticksPerMillisecond) : undefined);
    };

    const htmlProps = Dic.extend({ placeholder: s.ctx.placeholderLabels ? asString(s.labelText) : undefined } as React.HTMLAttributes, vl.props.valueHtmlProps);

    return (
        <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(vl.baseHtmlProps(), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
            <DurationTextBox htmlProps={htmlProps} value={s.ctx.value} onChange={handleOnChange} validateKey={validateKey} format={durationFormat} />
        </FormGroup>
    );
}

export interface DurationTextBoxProps {
    value: number;
    onChange: (newValue?: number) => void;
    validateKey: React.KeyboardEventHandler;
    format?: string;
    htmlProps: React.HTMLAttributes;
}

export class DurationTextBox extends React.Component<DurationTextBoxProps, { text?: string }> {

    constructor(props: NumericTextBoxProps) {
        super(props);
        this.state = { text: undefined };
    }

    render() {
        const ticksPerMillisecond = 10000;
        const value = this.state.text != undefined ? this.state.text :
            this.props.value != undefined ? moment.duration(this.props.value / ticksPerMillisecond).format(this.props.format) :
                "";

        return <input {...this.props.htmlProps} type="text" className={addClass(this.props.htmlProps, "form-control numeric") } value={value}
            onBlur={this.handleOnBlur}
            onChange={this.handleOnChange}
            onKeyDown={this.handleKeyDown}/>

    }

    handleOnBlur = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        const result = input.value == undefined || input.value.length == 0 ? undefined : moment.duration(input.value).asMilliseconds();
        this.setState({ text: undefined });
        this.props.onChange(result);
    }

    handleOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        this.setState({ text: input.value });
    }

    handleKeyDown = (e: React.KeyboardEvent) => {
        if (!this.props.validateKey(e))
            e.preventDefault();
    }
}
