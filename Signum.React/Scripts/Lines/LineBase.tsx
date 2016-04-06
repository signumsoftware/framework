
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

        const ctx = this.props.ctx;

        const tCtx = ctx as TypeContext<any>;

        var errorClass = tCtx.binding && tCtx.binding.errorClass;

        if (ctx.formGroupStyle == FormGroupStyle.None) {
           
            var c = this.props.children as React.ReactElement<any>;

            if (errorClass == null)
                return c;

            return React.cloneElement(c, { className: classes(c.props.className, errorClass) });
        }

        const labelClasses = classes(ctx.formGroupStyle == FormGroupStyle.SrOnly && "sr-only",
            ctx.formGroupStyle == FormGroupStyle.LabelColumns && ("control-label " + ctx.labelColumnsCss));


        const label = (
            <label htmlFor={this.props.controlId} {...this.props.labelProps } className= { labelClasses } >
                { this.props.title || tCtx.propertyRoute && tCtx.propertyRoute.member.niceName }
            </label>
        );

        return <div className={ classes("form-group", this.props.ctx.formGroupSizeCss, errorClass) }>
            { ctx.formGroupStyle != FormGroupStyle.BasicDown && label }
            {
                ctx.formGroupStyle == FormGroupStyle.LabelColumns ? (<div className={ this.props.ctx.valueColumnsCss } > { this.props.children } </div>) : this.props.children}
            {ctx.formGroupStyle == FormGroupStyle.BasicDown && label
            }
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
        const ctx = this.props.ctx;

        return (
            <p id={ this.props.controlId }
                className ={classes(ctx.formControlStaticAsFormControlReadonly ? "form-control readonly" : "form-control-static", this.props.className) }>
                { this.props.children }
            </p>
        );
    }

}

export interface LineBaseProps extends StyleOptions {
    ctx?: TypeContext<any>;
    type?: TypeReference;
    labelText?: React.ReactChild;
    visible?: boolean;
    hideIfNull?: boolean;
    onChange?: (val: any) => void;
}

export abstract class LineBase<P extends LineBaseProps, S extends LineBaseProps> extends React.Component<P, S> {

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

    calculateState(props: P): S {

        var so = {
            formControlStaticAsFormControlReadonly: null,
            formGroupSize: null,
            formGroupStyle: null,
            labelColumns: null,
            placeholderLabels: null,
            readOnly: null,
            valueColumns: null,
        } as StyleOptions;

        var cleanProps = Dic.without(props, so);
        
        const state = { ctx: cleanProps.ctx.subCtx(so), type: (cleanProps.type || cleanProps.ctx.propertyRoute.member.type) } as LineBaseProps as S;
        this.calculateDefaultState(state);
        runTasks(this, state);
        var overridenProps = Dic.without(cleanProps, { ctx: null, type: null }) as LineBaseProps as S;
        this.overrideProps(state, overridenProps);
        return state;
    }

    overrideProps(state: S, overridenProps: S) {
        Dic.extend(state, overridenProps);
    }

    calculateDefaultState(state: S) {
    }

    abstract renderInternal(): JSX.Element;
}


export const Tasks: ((lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) => void)[] = [];

export function runTasks(lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) {
    Tasks.forEach(t => t(lineBase, state));
}