import * as React from 'react'
import * as moment from 'moment'
import { Input, Tab } from 'react-bootstrap'
//import { DatePicker } from 'react-widgets'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from 'Framework/Signum.React/Scripts/TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo} from 'Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, runTasks, FormGroup, FormControlStatic} from 'Framework/Signum.React/Scripts/Lines/LineBase'


export interface ValueLineProps extends LineBaseProps, React.Props<ValueLine> {
    valueLineType?: ValueLineType;
    unitText?: string;
    formatText?: string;
    inlineCheckBox?: string;
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


export class ValueLine extends LineBase<ValueLineProps, {}> {

    constructor(props) {
        super(props);
    }

    static renderers: {
        [valueLineType: string]: (vl: ValueLine, valueLineProps: ValueLineProps) => JSX.Element;
    } = {};

    handleInputOnChange = (e: React.SyntheticEvent) => {

        var input = e.currentTarget as HTMLInputElement;
        var val = input.type == "checkbox" || input.type == "radio" ? input.checked : input.value;
        this.props.ctx.setValue(val);
        this.forceUpdate();

    };

    handleDatePickerOnChange = (date: Date, str: string) => {
        this.props.ctx.setValue(str);
        this.forceUpdate();
    };

    render() {

        var props = Dic.extend({}, this.props);

        props.valueLineType = props.valueLineType || this.calculateValueLineType();

        runTasks(this, props);

        return ValueLine.renderers[props.valueLineType](this, props);

    }


    calculateValueLineType(): ValueLineType {
        var t = this.props.ctx.propertyRoute.member.type;

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

        throw new Error(`No value line found for '${t}' (property route = ${this.props.ctx.propertyRoute.propertyPath()})`);
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

ValueLine.renderers[ValueLineType.Boolean as any] = (vl, props) => {
    if (props.ctx.propertyRoute.member.type.isNullable) {
        return internalComboBox(vl, props, getTypeInfo("BooleanEnum"));
    }


    if (vl.props.inlineCheckBox) {
        return <div className="checkbox">
            <label>
                <input type="checkbox" checked={props.ctx.value } onChange={vl.handleInputOnChange} disabled={props.ctx.readOnly}/>
                {props.labelText}
                </label>
            </div>;
    }
    else {
        return <FormGroup ctx={props.ctx} title={props.labelText}>
            <input type="checkbox" checked={props.ctx.value } onChange={vl.handleInputOnChange} className="form-control" disabled={props.ctx.readOnly}/>
            </FormGroup>
    }
};

ValueLine.renderers[ValueLineType.Enum as any] = (vl, props) => {
    return internalComboBox(vl, props, getTypeInfo(props.ctx.propertyRoute.member.type.name));
};


function internalComboBox(vl: ValueLine, props: ValueLineProps, typeInfo: TypeInfo) {

    var items = vl.props.comboBoxItems || Dic.getValues(typeInfo.members);


    if (props.ctx.propertyRoute.member.type.isNullable || props.ctx.value == null)
        items.splice(0, 0, { name: null, niceName: " - " } as MemberInfo);

    if (props.ctx.readOnly)
        return <FormGroup ctx={props.ctx} title={props.labelText}>
            { ValueLine.withUnit(props.unitText,
                <FormControlStatic ctx={props.ctx} text={props.ctx.value == null ? null : items.filter(a=> a.name == props.ctx.value).single().niceName}/>) }
            </FormGroup>;

    return <FormGroup ctx={props.ctx} title={props.labelText}>
        { ValueLine.withUnit(props.unitText,
            <select value= { props.ctx.value } className= "form-control" onChange={vl.handleInputOnChange}>
                {items.map(mi=> <option key= {mi.name} value={mi.name}>{mi.niceName}</option>) }
                </select>)
        }
        </FormGroup>;

}

ValueLine.renderers[ValueLineType.TextBox as any] = (vl, props) => {
    if (props.ctx.readOnly)
        return <FormGroup ctx={props.ctx} title={props.labelText}>
             { ValueLine.withUnit(props.unitText, <FormControlStatic ctx={props.ctx} text={props.ctx.value}/>) }
            </FormGroup>;

    return <FormGroup ctx={props.ctx} title={props.labelText}>
        { ValueLine.withUnit(props.unitText,
            <input type="text" className="form-control" value={props.ctx.value} onChange={vl.handleInputOnChange}
                placeholder={props.ctx.placeholderLabels ? props.labelText : null}/>
        ) }
        </FormGroup>;
};

ValueLine.renderers[ValueLineType.TextArea as any] = (vl, props) => {

    if (props.ctx.readOnly)
        return <FormGroup ctx={props.ctx} title={props.labelText}>
             { ValueLine.withUnit(props.unitText, <FormControlStatic ctx={props.ctx} text={moment(props.ctx.value).format(props.formatText) }/>) }
            </FormGroup>;

    return <FormGroup ctx={props.ctx} title={props.labelText}>
            <input type="textarea" className="form-control" value={props.ctx.value} onChange={vl.handleInputOnChange}
                placeholder={props.ctx.placeholderLabels ? props.labelText : null}/>
        </FormGroup>;
};

ValueLine.renderers[ValueLineType.Number as any] = (vl, props) => {
    return numericTextBox(vl, props, ValueLine.isNumber);
};

ValueLine.renderers[ValueLineType.Decimal as any] = (vl, props) => {
    return numericTextBox(vl, props, ValueLine.isDecimal);
};


function numericTextBox(vl: ValueLine, props: ValueLineProps, handleKeyDown: React.KeyboardEventHandler) {
    if (props.ctx.readOnly)
        return <FormGroup ctx={props.ctx} title={props.labelText}>
             { ValueLine.withUnit(props.unitText, <FormControlStatic ctx={props.ctx} text={props.ctx.value} />) }
            </FormGroup>;

    return <FormGroup ctx={props.ctx} title={props.labelText}>
        { ValueLine.withUnit(props.unitText,
            <input type="textarea" className="form-control numeric" value={props.ctx.value} onChange={vl.handleInputOnChange}
                placeholder={props.ctx.placeholderLabels ? props.labelText : null} onKeyDown={handleKeyDown}/>
        ) }
        </FormGroup>;
}

ValueLine.renderers[ValueLineType.DateTime as any] = (vl, props) => {

    if (props.ctx.readOnly)
        return <FormGroup ctx={props.ctx} title={props.labelText}>
             { ValueLine.withUnit(props.unitText, <FormControlStatic ctx={props.ctx} text={moment(props.ctx.value).format(props.formatText) }/>) }
            </FormGroup>;

    return <FormGroup ctx={props.ctx} title={props.labelText}>
         { ValueLine.withUnit(props.unitText,
             <input type="text" className="form-control" value={props.ctx.value} onChange={vl.handleInputOnChange}
                 placeholder={props.ctx.placeholderLabels ? props.labelText : null}/>
         ) }
        {/*<DatePicker value={props.ctx.value} onChange={vl.handleDatePickerOnChange} />*/}
        </FormGroup>;
};