
import * as React from 'react'
import * as moment from 'moment'
import { classes, Dic } from '../Globals'
import { Input, Tab } from 'react-bootstrap'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, TypeReference} from '../Reflection'

require("!style!css!./Lines.css");

export interface FormGroupProps extends React.Props<FormGroup> {
    title?: React.ReactChild;
    controlId?: string;
    ctx: StyleContext;
    labelProps?: React.HTMLProps<HTMLLabelElement>;
}

export class FormGroup extends React.Component<FormGroupProps, {}> {

    render() {

        var ctx = this.props.ctx;

        if (ctx.formGroupStyle == FormGroupStyle.None)
            return this.props.children as React.ReactElement<any>;

        var labelClasses = classes(ctx.formGroupStyle == FormGroupStyle.SrOnly && "sr-only",
            ctx.formGroupStyle == FormGroupStyle.LabelColumns && ("control-label " + ctx.labelColumnsCss));


        var label = <label htmlFor={this.props.controlId} {...this.props.labelProps } className= { labelClasses } >
            { this.props.title }
            </label>;

        return <div className={ "form-group " + this.props.ctx.formGroupSizeCss }>
            { ctx.formGroupStyle != FormGroupStyle.BasicDown && label }
        {
        ctx.formGroupStyle == FormGroupStyle.LabelColumns ? (<div className={ this.props.ctx.valueColumnsCss } > { this.props.children } </div>) : this.props.children}
            {ctx.formGroupStyle == FormGroupStyle.BasicDown && label }
            </div>;
    }
}


export interface FormControlStaticProps extends React.Props<FormControlStatic> {
    controlId?: string;
    ctx: StyleContext;
    className?: string
}

export class FormControlStatic extends React.Component<FormControlStaticProps, {}>
{
    render() {
        var ctx = this.props.ctx;

        return <p id={ this.props.controlId }
            className = {(ctx.formControlStaticAsFormControlReadonly ? "form-control readonly" : "form-control-static") + " " + this.props.className}>
            { this.props.children }
            </p>
    }

}


export interface LineBaseProps {
    ctx?: TypeContext<any>;
    type?: TypeReference;
    labelText?: string;
    visible?: boolean;
    hideIfNull?: boolean;
    onChange?: (val: any) => void;
}

export abstract class LineBase<P extends LineBaseProps> extends React.Component<P, P> {

    constructor(props: P) {
        super(props);

        this.state = this.calculateState(props);
    }

    componentWillReceiveProps(nextProps: P, nextContext: any) {
        this.setState(this.calculateState(nextProps));
    }

    setValue(val: any) {
        this.state.ctx.value = val;
        if (this.state.onChange)
            this.state.onChange(val);
        this.forceUpdate();
    }

    render() {

        if (this.state.visible == false || this.state.hideIfNull && this.state.ctx.value == null)
            return null;

        return this.renderInternal();
    }

    calculateState(props: P): P {
        var state = { ctx: props.ctx, type: (props.type || props.ctx.propertyRoute.member.type) } as LineBaseProps as P;
        this.calculateDefaultState(state);
        runTasks(this, state);
        Dic.extend(state, props);
        return state;
    }

    calculateDefaultState(state: P) {

    }

    abstract renderInternal(): JSX.Element;
}


export var Tasks: ((lineBase: LineBase<LineBaseProps>, state: LineBaseProps) => void)[] = [];

export function runTasks(lineBase: LineBase<LineBaseProps>, state: LineBaseProps) {
    Tasks.forEach(t=> t(lineBase, state));
}