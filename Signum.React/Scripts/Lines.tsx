/// <reference path="globals.ts" />

import * as React from 'react'
import { Input, Tab } from 'react-bootstrap'
import { TypeContext, StyleOptions, FormGroupStyle } from 'Framework/Signum.React/Scripts/TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo} from 'Framework/Signum.React/Scripts/Reflection'

export interface FormGroupProps extends React.Props<FormGroup> {
    title?: React.ReactChild;
    controlId?: string;
    ctx?: TypeContext<any>;
    labelProps?: React.HTMLProps<HTMLLabelElement>;
    addon?: string
}

export class FormGroup extends React.Component<FormGroupProps, {}> {

    render() {

        var ctx = this.props.ctx;

        if (ctx.formGroupStyle == FormGroupStyle.None)
            return this.props.children as React.ReactElement<any>;

        var labelClasses = classes(ctx.formGroupStyle == FormGroupStyle.SrOnly && "sr-only",
            ctx.formGroupStyle == FormGroupStyle.LabelColumns && ("control-label " + ctx.labelColumnsCss));


        var label = <label htmlFor={this.props.controlId} {...this.props.labelProps} className={labelClasses}>
                {this.props.title}
            </label>;

        return <div className={"form-group " + this.props.ctx.formGroupSizeCss}>
            {ctx.formGroupStyle != FormGroupStyle.BasicDown && label }
            {ctx.formGroupStyle == FormGroupStyle.LabelColumns ? <div className={this.props.ctx.valueColumnsCss}>{this.props.children}</div> : this.props.children}
            {ctx.formGroupStyle == FormGroupStyle.BasicDown && label }
          {this.props.children}
            </div>;
    }
}


export interface LineBaseProps {
    ctx: TypeContext<any>;
    labelText?: string;
}

export class LineBase<P extends LineBaseProps, S> extends React.Component<P, S> {
}





export interface ValueLineProps extends LineBaseProps, React.Props<ValueLine> {
    valueLineType?: ValueLineType;
    unitText?: string;
    formatText?: string;
    intlineCheckBox?: string;
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
    };

    handleInputOnChange = (e: React.SyntheticEvent) => {

        var input = e.currentTarget as HTMLInputElement;
        var val = input.type == "checkbox" || input.type == "radio" ? input.checked : input.value;
        this.props.ctx.setValue(val);
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

        throw new Error("not implemented");
    }

    static withUnit(unit: string, input: JSX.Element): JSX.Element {
        if (!unit)
            return input;

        return <div className="input-group">
            {input}
            <span className="input-group-addon"/>
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
    if (vl.props.intlineCheckBox) {
        return <div className="checkbox">
            <label>
                <input type="checkbox" checked={props.ctx.value } onChange={vl.handleInputOnChange} />
                {props.labelText}
                </label>
            </div>;
    }
    else {
        return <FormGroup ctx={props.ctx} title={props.labelText}>
            <input type="checkbox" checked={props.ctx.value } onChange={vl.handleInputOnChange} className="form-control"/>
            </FormGroup>
    }
};

ValueLine.renderers[ValueLineType.Enum as any] = (vl, props) => {
    var items = vl.props.comboBoxItems || Dic.getValues(getTypeInfo(props.ctx.propertyRoute.member.type.name).members);

    if (props.ctx.propertyRoute.member.type.isNullable || props.ctx.value == null)
        items.splice(0, 0, { name: null, niceName: " - " } as MemberInfo);

    return <FormGroup ctx={props.ctx} title={props.valueLineType}>
        { ValueLine.withUnit(props.unitText,
            <select value= { props.ctx.value } className= "form-control" >
                {items.map(mi=> <option key= {mi.name} value={mi.name}>{mi.niceName}</option>) }
                </select>)
        }
        </FormGroup>;
};





ValueLine.renderers[ValueLineType.TextBox as any] = (vl, props) => {
    return <FormGroup ctx={props.ctx} title={props.valueLineType}>
        { ValueLine.withUnit(props.unitText,
            <input type="text" className="form-control" value={props.ctx.value} placeholder={props.ctx.placeholderLabels ? props.labelText : null}/>
        ) }
        </FormGroup>;
};

ValueLine.renderers[ValueLineType.TextArea as any] = (vl, props) => {
    return <FormGroup ctx={props.ctx} title={props.valueLineType}>
            <input type="textarea" className="form-control" value={props.ctx.value} placeholder={props.ctx.placeholderLabels ? props.labelText : null}/>
        </FormGroup>;
};

ValueLine.renderers[ValueLineType.Number as any] = (vl, props) => {
    return <FormGroup ctx={props.ctx} title={props.valueLineType}>
        { ValueLine.withUnit(props.unitText,
            <input type="textarea" className="form-control" value={props.ctx.value} placeholder={props.ctx.placeholderLabels ? props.labelText : null}  onKeyDown={ValueLine.isNumber}/>
        ) }
        </FormGroup>;
};

ValueLine.renderers[ValueLineType.Decimal as any] = (vl, props) => {
    return <FormGroup ctx={props.ctx} title={props.valueLineType}>
        { ValueLine.withUnit(props.unitText,
            <input type="textarea" className="form-control" value={props.ctx.value} placeholder={props.ctx.placeholderLabels ? props.labelText : null} onKeyDown={ValueLine.isDecimal}/>
        ) }
        </FormGroup>;
};


export interface EntityLineProps extends LineBaseProps {

}

export class EntityLine extends LineBase<EntityLineProps, {}> {

}

export class EntityComponent<T> extends React.Component<{ typeContext: TypeContext<T> }, {}>{


    subContext<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R> {
        return this.props.typeContext.subCtx(property, styleOptions);
    }
}




export var Tasks: ((lineBase: LineBase<any, any>, lineBaseProps: LineBaseProps) => void)[] = [
    taskSetNiceName,
    taskSetUnit,
    taskSetFormat
];

export function runTasks(lineBase: LineBase<any, any>, lineBaseProps: LineBaseProps) {
    Tasks.forEach(t=> t(lineBase, lineBaseProps));
}

export function taskSetNiceName(lineBase: LineBase<any, any>, props: LineBaseProps) {
    if (!props.labelText && props.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
        props.labelText = props.ctx.propertyRoute.member.niceName;
    }
}

export function taskSetUnit(lineBase: LineBase<any, any>, props: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        var vProps = props as ValueLineProps;

        if (!vProps.unitText && props.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.unitText = props.ctx.propertyRoute.member.unit;
        }
    }
}

export function taskSetFormat(lineBase: LineBase<any, any>, props: LineBaseProps) {
    if (lineBase instanceof ValueLine) {
        var vProps = props as ValueLineProps;

        if (!vProps.unitText && props.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {
            vProps.formatText = props.ctx.propertyRoute.member.unit;
        }
    }
}