
import * as React from 'react'
import * as moment from 'moment'
import { classes, Dic, addClass } from '../Globals'
import { Tab } from 'react-bootstrap'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, TypeReference } from '../Reflection'
import { ValidationMessage } from '../Signum.Entities'

import "./Lines.css"

export interface FormGroupProps extends React.Props<FormGroup> {
    labelText?: React.ReactChild;
    controlId?: string;
    ctx: StyleContext;
    labelHtmlAttributes?: React.HTMLAttributes<HTMLLabelElement>;
    htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
    helpBlock?: React.ReactChild;
}

export class FormGroup extends React.Component<FormGroupProps> {

    render() {

        const ctx = this.props.ctx;

        const tCtx = ctx as TypeContext<any>;

        const errorClass = tCtx.errorClass;

        if (ctx.formGroupStyle == "None") {

            const c = this.props.children as React.ReactElement<any>;

            return (
                <span {...this.props.htmlAttributes} className={classes(this.props.ctx.formGroupSizeCss, errorClass)}>
                    {c}
                </span>
            );
        }

        const labelClasses = classes(ctx.formGroupStyle == "SrOnly" && "sr-only", ctx.formGroupStyle == "LabelColumns" && ("control-label " + ctx.labelColumnsCss));
        let pr = tCtx.propertyRoute;
        const label = (
            <label htmlFor={this.props.controlId} {...this.props.labelHtmlAttributes } className={addClass(this.props.labelHtmlAttributes, labelClasses)} >
                {this.props.labelText || (pr && pr.member && pr.member.niceName)}
            </label>
        );

        const formGroupClasses = classes("form-group", this.props.ctx.formGroupSizeCss, errorClass);
        return <div {...this.props.htmlAttributes} className={addClass(this.props.htmlAttributes, formGroupClasses)}>
            {ctx.formGroupStyle != "BasicDown" && label}
            {
                ctx.formGroupStyle != "LabelColumns" ? this.props.children :
                    (
                        <div className={this.props.ctx.valueColumnsCss} >
                            {this.props.children}
                            {this.props.helpBlock && ctx.formGroupStyle == "LabelColumns" && <span className="help-block">{this.props.helpBlock}</span>}
                        </div>
                    )
            }
            {ctx.formGroupStyle == "BasicDown" && label}
            {this.props.helpBlock && ctx.formGroupStyle != "LabelColumns" && <span className="help-block">{this.props.helpBlock}</span>}
        </div>;
    }
}


export interface FormControlStaticProps extends React.Props<FormControlStatic> {
    ctx: StyleContext;
    htmlAttributes?: React.HTMLAttributes<HTMLParagraphElement>;
    className?: string
}

export class FormControlStatic extends React.Component<FormControlStaticProps>
{
    render() {
        const ctx = this.props.ctx;

        var p = this.props.htmlAttributes;

        return (
            <p {...p} className={classes(ctx.formControlClassReadonly, p && p.className, this.props.className)} >
                {this.props.children}
            </p>
        );
    }
}

export interface ChangeEvent {
    newValue: any;
    oldValue: any;
}

export interface LineBaseProps extends StyleOptions {
    ctx: TypeContext<any>;
    type?: TypeReference;
    labelText?: React.ReactChild;
    visible?: boolean;
    hideIfNull?: boolean;
    onChange?: (e: ChangeEvent) => void;
    onValidate?: (val: any) => string;
    labelHtmlAttributes?: React.LabelHTMLAttributes<HTMLLabelElement>;
    formGroupHtmlAttributes?: React.HTMLAttributes<any>;
    helpBlock?: React.ReactChild;
}

export abstract class LineBase<P extends LineBaseProps, S extends LineBaseProps> extends React.Component<P, S> {

    constructor(props: P) {
        super(props);

        this.state = this.calculateState(props);
    }

    shouldComponentUpdate(nextProps: LineBaseProps, nextState: LineBaseProps) {
        if (Dic.equals(this.state, nextState, true))
            return false; //For Debugging

        return true;
    }

    componentWillReceiveProps(nextProps: P, nextContext: any) {
        this.state = this.calculateState(nextProps);
        this.forceUpdate();
    }

    changes = 0;
    setValue(val: any) {
        var oldValue = this.state.ctx.value;
        this.state.ctx.value = val;
        this.changes++;
        this.validate();
        this.forceUpdate();
        if (this.state.onChange)
            this.state.onChange({ oldValue: oldValue, newValue: val });
    }

    validate() {
        const error = this.state.onValidate ? this.state.onValidate(this.state.ctx.value) : this.defaultValidate(this.state.ctx.value);
        this.state.ctx.error = error;
        if (this.state.ctx.frame)
            this.state.ctx.frame.revalidate();
    }

    defaultValidate(val: any) {
        if (this.state.type!.isNotNullable && val == undefined)
            return ValidationMessage._0IsNotSet.niceToString(this.state.ctx.niceName());

        return undefined;
    }

    render() {

        if (this.state.visible == false || this.state.hideIfNull && this.state.ctx.value == undefined)
            return null;

        return this.renderInternal();
    }

    calculateState(props: P): S {

        const { type, ctx,
            formControlClassReadonly, formGroupSize, formGroupStyle, labelColumns, placeholderLabels, readOnly, valueColumns,
            ...otherProps
        } = props as LineBaseProps;

        const so: StyleOptions = { formControlClassReadonly, formGroupSize, formGroupStyle, labelColumns, placeholderLabels, readOnly, valueColumns };

        const state = { ctx: ctx.subCtx(so), type: (type || ctx.propertyRoute.typeReference() ) } as LineBaseProps as S;

        this.calculateDefaultState(state);
        runTasks(this, state);

        this.overrideProps(state, otherProps as S);
        return state;
    }

    overrideProps(state: S, overridenProps: S) {
        const labelHtmlAttributes = { ...state.labelHtmlAttributes, ...Dic.simplify(overridenProps.labelHtmlAttributes) };
        Dic.assign(state, Dic.simplify(overridenProps))
        state.labelHtmlAttributes = labelHtmlAttributes;
    }

    baseHtmlAttributes(): React.HTMLAttributes<any> {
        return {
            'data-propertyPath': this.state.ctx.propertyPath,
            'data-changes': this.changes
        } as any;
    }

    calculateDefaultState(state: S) {
    }

    abstract renderInternal(): JSX.Element | null;
}


export const tasks: ((lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) => void)[] = [];

export function runTasks(lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) {
    tasks.forEach(t => t(lineBase, state));
}