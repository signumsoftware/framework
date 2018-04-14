import * as React from 'react'
import { StyleContext, TypeContext } from '../Lines';
import { classes, addClass } from '../Globals';

import "./Lines.css"

export interface FormGroupProps extends React.Props<FormGroup> {
    labelText?: React.ReactChild;
    controlId?: string;
    ctx: StyleContext;
    labelHtmlAttributes?: React.HTMLAttributes<HTMLLabelElement>;
    htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
    helpText?: React.ReactChild;
}

export class FormGroup extends React.Component<FormGroupProps> {

    render() {

        const ctx = this.props.ctx;

        const tCtx = ctx as TypeContext<any>;

        const errorClass = tCtx.errorClass;

        if (ctx.formGroupStyle == "None") {

            const c = this.props.children as React.ReactElement<any>;

            return (
                <span {...this.props.htmlAttributes} className={errorClass}>
                    {c}
                </span>
            );
        }

        const labelClasses = classes(
            ctx.formGroupStyle == "SrOnly" && "sr-only",
            ctx.formGroupStyle == "LabelColumns" && ctx.labelColumnsCss,
            ctx.formGroupStyle == "LabelColumns" ? ctx.colFormLabelClass : ctx.labelClass,
        );

        let pr = tCtx.propertyRoute;
        const label = (
            <label htmlFor={this.props.controlId} {...this.props.labelHtmlAttributes } className={addClass(this.props.labelHtmlAttributes, labelClasses)} >
                {this.props.labelText || (pr && pr.member && pr.member.niceName)}
            </label>
        );

        const formGroupClasses = classes(this.props.ctx.formGroupClass,  this.props.ctx.formGroupStyle == "LabelColumns" ? "row" : undefined, errorClass);
        return <div {...this.props.htmlAttributes} className={addClass(this.props.htmlAttributes, formGroupClasses)}>
            {ctx.formGroupStyle != "BasicDown" && label}
            {
                ctx.formGroupStyle != "LabelColumns" ? this.props.children :
                    (
                        <div className={this.props.ctx.valueColumnsCss} >
                            {this.props.children}
                            {this.props.helpText && ctx.formGroupStyle == "LabelColumns" && <span className="form-text text-muted">{this.props.helpText}</span>}
                        </div>
                    )
            }
            {ctx.formGroupStyle == "BasicDown" && label}
            {this.props.helpText && ctx.formGroupStyle != "LabelColumns" && <span className="form-text text-muted">{this.props.helpText}</span>}
        </div>;
    }
}
