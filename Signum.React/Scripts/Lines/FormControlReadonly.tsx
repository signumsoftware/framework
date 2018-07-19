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

        var array = React.Children.toArray(this.props.children);

        var onlyText = array.length == 1 && typeof array[0] == "string" ? array[0] as string : undefined;

        if (onlyText) { //Text is scrollable in inputs
            if (ctx.readonlyAsPlainText) {
                return (
                    <input {...attrs} className={classes(ctx.formControlPlainTextClass, attrs && attrs.className, this.props.className)} value={onlyText}/>
                );
            } else {
                return (
                    <input {...attrs} {...{ readOnly: true }} className={classes(ctx.formControlClass, attrs && attrs.className, this.props.className)} value={onlyText} />
                );
            }
        }
        else {
            if (ctx.readonlyAsPlainText) {
                return (
                    <div {...attrs} className={classes(ctx.formControlPlainTextClass, attrs && attrs.className, this.props.className)}>
                        {this.props.children || <span>&nbsp;</span>}
                    </div>
                );
            } else {
                return (
                    <div {...attrs} {...{ readOnly: true }} className={classes(ctx.formControlClass, attrs && attrs.className, this.props.className)}>
                        {this.props.children || <span>&nbsp;</span>}
                    </div>
                );
            }
        }

    
    }
}