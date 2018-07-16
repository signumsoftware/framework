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
        
        if (ctx.readonlyAsPlainText) {
            return (
                <p {...attrs} className={classes(ctx.formControlPlainTextClass, attrs && attrs.className, this.props.className)}>
                    {this.props.children}
                </p>
            );

        } else {
            return (
                <span {...attrs} {...{ readOnly: true }} className={classes(ctx.formControlClass, attrs && attrs.className, this.props.className)}>
                    {this.props.children}
                </span>
            );
        }
    }
}