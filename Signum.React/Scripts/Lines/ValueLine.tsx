import * as React from 'react'
import * as moment from 'moment'
import { Input, Tab } from 'react-bootstrap'
import { DatePicker } from 'react-widgets'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from 'Framework/Signum.React/Scripts/TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, TypeReference, toMomentFormat } from 'Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, runTasks, FormGroup, FormControlStatic } from 'Framework/Signum.React/Scripts/Lines/LineBase'


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

    handleInputOnChange = (e: React.SyntheticEvent) => {

        var input = e.currentTarget as HTMLInputElement;
        var val = input.type == "checkbox" || input.type == "radio" ? input.checked : input.value;
        this.state.ctx.value(val);
        this.forceUpdate();

    };

    handleDatePickerOnChange = (date: Date, str: string) => {
        this.state.ctx.value(str);
        this.forceUpdate();
    };

    renderInternal() {

        if (this.state.visible == false || this.state.hideIfNull && this.state.ctx.value == null)
            return null;

        return ValueLine.renderers[this.state.valueLineType](this);

    }

    static withUnit(unit: string, input: JSX.Element): JSX.Element {
        if (!unit)
            return input;

        return <div className="input-group">
            {input}
            <span className="input-group-addon">{unit}</span>
            </div>;
    }

    static isNumber(e: React.KeyboardEvent) {
        var c = e.keyCode;
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
        var c = e.keyCode;
        return (this.isNumber(e) ||
            (c == 110) /*NumPad Decimal*/ ||
            (c == 190) /*.*/ ||
            (c == 188) /*,*/);
    }

}

ValueLine.renderers[ValueLineType.Boolean as any] = (vl) => {
    var s = vl.state;

    if (s.type.isNullable) {
        return internalComboBox(vl, getTypeInfo("BooleanEnum"));
    }


    if (s.inlineCheckBox) {
        return <div className="checkbox">
            <label>
                <input type="checkbox" checked={s.ctx.value } onChange={vl.handleInputOnChange} disabled={s.ctx.readOnly}/>
                {s.labelText}
                </label>
            </div>;
    }
    else {
        return <FormGroup ctx={s.ctx} title={s.labelText}>
            <input type="checkbox" checked={s.ctx.value } onChange={vl.handleInputOnChange} className="form-control" disabled={s.ctx.readOnly}/>
            </FormGroup>
    }
};

ValueLine.renderers[ValueLineType.Enum as any] = (vl) => {
    return internalComboBox(vl, getTypeInfo(vl.state.type.name));
};


function internalComboBox(vl: ValueLine, typeInfo: TypeInfo) {

    var s = vl.state;

    var items = s.comboBoxItems || Dic.getValues(typeInfo.members);


    if (s.type.isNullable || s.ctx.value == null)
        items.splice(0, 0, { name: null, niceName: " - " } as MemberInfo);

    if (s.ctx.readOnly)
        return <FormGroup ctx={s.ctx} title={s.labelText}>
            { ValueLine.withUnit(s.unitText,
                <FormControlStatic ctx={s.ctx}>
                    {s.ctx.value == null ? null : items.filter(a=> a.name == s.ctx.value).single().niceName}
                    </FormControlStatic>) }
            </FormGroup>;

    return <FormGroup ctx={s.ctx} title={s.labelText}>
        { ValueLine.withUnit(s.unitText,
            <select value= { s.ctx.value } className= "form-control" onChange={vl.handleInputOnChange}>
                {items.map(mi=> <option key= {mi.name} value={mi.name}>{mi.niceName}</option>) }
                </select>)
        }
        </FormGroup>;

}

ValueLine.renderers[ValueLineType.TextBox as any] = (vl) => {

    var s = vl.state;

    if (s.ctx.readOnly)
        return <FormGroup ctx={s.ctx} title={s.labelText}>
             { ValueLine.withUnit(s.unitText, <FormControlStatic ctx={s.ctx}>{s.ctx.value}</FormControlStatic>) }
            </FormGroup>;

    return <FormGroup ctx={s.ctx} title={s.labelText}>
        { ValueLine.withUnit(s.unitText,
            <input type="text" className="form-control" value={s.ctx.value} onChange={vl.handleInputOnChange}
                placeholder={s.ctx.placeholderLabels ? s.labelText : null}/>
        ) }
        </FormGroup>;
};

ValueLine.renderers[ValueLineType.TextArea as any] = (vl) => {

    var s = vl.state;

    if (s.ctx.readOnly)
        return <FormGroup ctx={s.ctx} title={s.labelText}>
             { ValueLine.withUnit(s.unitText, <FormControlStatic ctx={s.ctx}>{s.ctx.value}</FormControlStatic>) }
            </FormGroup>;

    return <FormGroup ctx={s.ctx} title={s.labelText}>
            <input type="textarea" className="form-control" value={s.ctx.value} onChange={vl.handleInputOnChange}
                placeholder={s.ctx.placeholderLabels ? s.labelText : null}/>
        </FormGroup>;
};

ValueLine.renderers[ValueLineType.Number as any] = (vl) => {
    return numericTextBox(vl, ValueLine.isNumber);
};

ValueLine.renderers[ValueLineType.Decimal as any] = (vl) => {
    return numericTextBox(vl, ValueLine.isDecimal);
};


function numericTextBox(vl: ValueLine, handleKeyDown: React.KeyboardEventHandler) {

    var s = vl.state;

    if (s.ctx.readOnly)
        return <FormGroup ctx={s.ctx} title={s.labelText}>
             { ValueLine.withUnit(s.unitText, <FormControlStatic ctx={s.ctx}>{s.ctx.value}</FormControlStatic>) }
            </FormGroup>;

    return <FormGroup ctx={s.ctx} title={s.labelText}>
        { ValueLine.withUnit(s.unitText,
            <input type="textarea" className="form-control numeric" value={s.ctx.value} onChange={vl.handleInputOnChange}
                placeholder={s.ctx.placeholderLabels ? s.labelText : null} onKeyDown={handleKeyDown}/>
        ) }
        </FormGroup>;
}

ValueLine.renderers[ValueLineType.DateTime as any] = (vl) => {

    var s = vl.state;

    if (s.ctx.readOnly)
        return <FormGroup ctx={s.ctx} title={s.labelText}>
             { ValueLine.withUnit(s.unitText, <FormControlStatic ctx={s.ctx}>{moment(s.ctx.value).format(toMomentFormat(s.formatText)) }</FormControlStatic>) }
            </FormGroup>;

    return <FormGroup ctx={s.ctx} title={s.labelText}>
         { ValueLine.withUnit(s.unitText,
             <DatePicker value={s.ctx.value} onChange={vl.handleDatePickerOnChange} format={toMomentFormat(s.formatText) }/>
             //<input type="text" className="form-control" value={s.ctx.value} onChange={vl.handleInputOnChange}
             //    placeholder={s.ctx.placeholderLabels ? s.labelText : null}/>
         ) }
        </FormGroup>;
};