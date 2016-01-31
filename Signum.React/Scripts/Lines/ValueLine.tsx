import * as React from 'react'
import * as moment from 'moment'
import { classes, Dic } from '../Globals'
import { DateTimePicker } from 'react-widgets'
import 'react-widgets/lib/less/react-widgets.less';
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, TypeReference, toMomentFormat } from '../Reflection'
import { LineBase, LineBaseProps, runTasks, FormGroup, FormControlStatic } from '../Lines/LineBase'


export interface ValueLineProps extends LineBaseProps, React.Props<ValueLine> {
    valueLineType?: ValueLineType;
    unitText?: string;
    formatText?: string;
    inlineCheckBox?: boolean;
    comboBoxItems?: MemberInfo[];
}


export enum ValueLineType {
    Boolean = "Boolean" as any,
    Enum = "Enum" as any,
    DateTime = "DateTime" as any,
    TimeSpan = "TimeSpan" as any,
    TextBox = "TextBox" as any,
    TextArea = "TextArea" as any,
    Number = "Number" as any,
    Decimal = "Decimal" as any,
    Color = "Color" as any,
}


export class ValueLine extends LineBase<ValueLineProps> {

    calculateDefaultState(state: ValueLineProps) {
        state.valueLineType = this.calculateValueLineType(state.type);
    }

    calculateValueLineType(t: TypeReference): ValueLineType {

        if (t.isCollection || t.isLite)
            throw new Error("not implemented");

        if (t.isEnum)
            return ValueLineType.Enum;

        if (t.name == "boolean")
            return ValueLineType.Boolean;

        if (t.name == "datetime")
            return ValueLineType.DateTime;

        if (t.name == "string")
            return ValueLineType.TextBox;

        if (t.name == "number")
            return ValueLineType.Number;

        if (t.name == "decimal")
            return ValueLineType.Decimal;

        throw new Error(`No value line found for '${t}' (property route = ${this.state.ctx.propertyRoute.propertyPath()})`);
    }

    static renderers: {
        [valueLineType: string]: (vl: ValueLine) => JSX.Element;
    } = {};

   

    renderInternal() {

        if (this.state.visible == false || this.state.hideIfNull && this.state.ctx.value == null)
            return null;

        return ValueLine.renderers[this.state.valueLineType](this);

    }

    static withUnit(unit: string, input: JSX.Element): JSX.Element {
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
        return (this.isNumber(e) ||
            (c == 110) /*NumPad Decimal*/ ||
            (c == 190) /*.*/ ||
            (c == 188) /*,*/);
    }

}

ValueLine.renderers[ValueLineType.Boolean as any] = (vl) => {
    const s = vl.state;

    if (s.type.isNullable) {
        return internalComboBox(vl, getTypeInfo("BooleanEnum"));
    }

    const handleCheckboxOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.checked);
    };

    if (s.inlineCheckBox) {
        return (
            <div className="checkbox">
                <label>
                    <input type="checkbox" checked={s.ctx.value } onChange={handleCheckboxOnChange} disabled={s.ctx.readOnly}/>
                    {s.labelText}
                </label>
            </div>
        );
    }
    else {
        return (
            <FormGroup ctx={s.ctx} title={s.labelText}>
                <input type="checkbox" checked={s.ctx.value } onChange={handleCheckboxOnChange} className="form-control" disabled={s.ctx.readOnly}/>
            </FormGroup>
        );
    }
};


ValueLine.renderers[ValueLineType.Enum as any] = (vl) => {
    return internalComboBox(vl, getTypeInfo(vl.state.type.name));
};


function internalComboBox(vl: ValueLine, typeInfo: TypeInfo) {

    const s = vl.state;

    let items = s.comboBoxItems || Dic.getValues(typeInfo.members);


    if (s.type.isNullable || s.ctx.value == null)
        items = [{ name: "", niceName: " - " } as MemberInfo].concat(items);

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} title={s.labelText}>
                { ValueLine.withUnit(s.unitText,
                    <FormControlStatic ctx={s.ctx}>
                        {s.ctx.value == null ? null : items.filter(a => a.name == s.ctx.value).single().niceName}
                    </FormControlStatic>) }
            </FormGroup>
        );


    const handleEnumOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        const val = input.value;
        vl.setValue(val == "" ? null : val);
    };

    return (
        <FormGroup ctx={s.ctx} title={s.labelText}>
            { ValueLine.withUnit(s.unitText,
                <select value= { s.ctx.value } className= "form-control" onChange={handleEnumOnChange}>
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
            <FormGroup ctx={s.ctx} title={s.labelText}>
                { ValueLine.withUnit(s.unitText, <FormControlStatic ctx={s.ctx}>{s.ctx.value}</FormControlStatic>) }
            </FormGroup>
        );

    const handleTextOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.value);
    };

    return (
        <FormGroup ctx={s.ctx} title={s.labelText}>
            { ValueLine.withUnit(s.unitText,
                <input type="text" className="form-control" value={s.ctx.value} onChange={handleTextOnChange}
                    placeholder={s.ctx.placeholderLabels ? s.labelText : null}/>)
            }
        </FormGroup>
    );
};

ValueLine.renderers[ValueLineType.TextArea as any] = (vl) => {

    const s = vl.state;

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} title={s.labelText}>
                { ValueLine.withUnit(s.unitText, <FormControlStatic ctx={s.ctx}>{s.ctx.value}</FormControlStatic>) }
            </FormGroup>
        );

    const handleTextOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(input.value);
    };

    return (
        <FormGroup ctx={s.ctx} title={s.labelText}>
            <input type="textarea" className="form-control" value={s.ctx.value} onChange={handleTextOnChange}
                placeholder={s.ctx.placeholderLabels ? s.labelText : null}/>
        </FormGroup>
    );
};

ValueLine.renderers[ValueLineType.Number as any] = (vl) => {
    return numericTextBox(vl, ValueLine.isNumber);
};

ValueLine.renderers[ValueLineType.Decimal as any] = (vl) => {
    return numericTextBox(vl, ValueLine.isDecimal);
};


function numericTextBox(vl: ValueLine, handleKeyDown: React.KeyboardEventHandler) {

    const s = vl.state;

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} title={s.labelText}>
                { ValueLine.withUnit(s.unitText, <FormControlStatic ctx={s.ctx}>{s.ctx.value}</FormControlStatic>) }
            </FormGroup>
        );

    const handleOnChange = (e: React.SyntheticEvent) => {
        const input = e.currentTarget as HTMLInputElement;
        vl.setValue(parseFloat(input.value));
    };

    return (
        <FormGroup ctx={s.ctx} title={s.labelText}>
            { ValueLine.withUnit(s.unitText,
                <input type="textarea" className="form-control numeric" value={s.ctx.value} onChange={handleOnChange}
                    placeholder={s.ctx.placeholderLabels ? s.labelText : null} onKeyDown={handleKeyDown}/>
            ) }
        </FormGroup>
    );
}

ValueLine.renderers[ValueLineType.DateTime as any] = (vl) => {

    const s = vl.state;

    const momentFormat = toMomentFormat(s.formatText);

    const m = s.ctx.value ? moment(s.ctx.value, moment.ISO_8601()) : null;
    const showTime = momentFormat != "L" && momentFormat != "LL";

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} title={s.labelText}>
                { ValueLine.withUnit(s.unitText, <FormControlStatic ctx={s.ctx}>{m && m.format(momentFormat) }</FormControlStatic>) }
            </FormGroup>
        );

    const handleDatePickerOnChange = (date: Date, str: string) => {

        const m = moment(date);
        vl.state.ctx.value = m.isValid() ? m.format(moment.ISO_8601()) : null;
        vl.forceUpdate();
    };

    return (
        <FormGroup ctx={s.ctx} title={s.labelText}>
            { ValueLine.withUnit(s.unitText,
                <DateTimePicker value={m && m.toDate() } onChange={handleDatePickerOnChange} format={momentFormat} time={showTime}/>
            ) }
        </FormGroup>
    );
};