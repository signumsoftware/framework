import * as React from 'react'
import { StyleContext, TypeContext } from '../Lines';
import { classes, addClass } from '../Globals';

import "./Lines.css"

export interface FormControlReadonlyProps extends React.Props<FormControlReadonly> {
    ctx: StyleContext;
    htmlAttributes?: React.HTMLAttributes<any>;
    className?: string
}

export class FormControlReadonly extends React.Component<FormControlReadonlyProps>
{
    render() {
        const ctx = this.props.ctx;

        var attrs = this.props.htmlAttributes;


        var formControlClasses = ctx.readonlyAsPlainText ? ctx.formControlPlainTextClass : classes(ctx.formControlClass, "readonly");

        return (
            <div {...attrs} className={classes(formControlClasses, attrs && attrs.className, this.props.className)}>
                {this.props.children || "\u00A0" /*To get min height*/}
            </div>
        );
    }
}