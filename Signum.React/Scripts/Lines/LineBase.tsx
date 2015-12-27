
import * as React from 'react'
import * as moment from 'moment'
import { Input, Tab } from 'react-bootstrap'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from 'Framework/Signum.React/Scripts/TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo} from 'Framework/Signum.React/Scripts/Reflection'



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
    text?: React.ReactChild;
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
            { this.props.text }
            </p>
    }

}


export interface LineBaseProps {
    ctx: TypeContext<any>;
    labelText?: string;
}

export class LineBase<P extends LineBaseProps, S> extends React.Component<P, S> {
}


export var Tasks: ((lineBase: LineBase<any, any>, lineBaseProps: LineBaseProps) => void)[] = [];

export function runTasks(lineBase: LineBase<any, any>, lineBaseProps: LineBaseProps) {
    Tasks.forEach(t=> t(lineBase, lineBaseProps));
}