import * as React from 'react'
import { StyleContext } from '../Lines';
import { classes } from '../Globals';
import "./Lines.css"

export interface FormControlReadonlyProps extends React.Props<FormControlReadonly> {
  ctx: StyleContext;
  htmlAttributes?: React.HTMLAttributes<any>;
  className?: string;
  innerRef?: React.LegacyRef<any>;
}

export class FormControlReadonly extends React.Component<FormControlReadonlyProps>
{
  render() {
    const ctx = this.props.ctx;
    const innerRef = this.props.innerRef;
    const attrs = this.props.htmlAttributes;
    const array = React.Children.toArray(this.props.children);
    const onlyText = array.length == 1 && typeof array[0] == "string" ? array[0] as string : undefined;

    if (onlyText) { //Text is scrollable in inputs
      if (ctx.readonlyAsPlainText) {
        return (
          <input {...attrs} className={classes(ctx.formControlPlainTextClass, attrs && attrs.className, this.props.className)} tabIndex={-1} value={onlyText} ref={innerRef}/>
        );
      } else {
        return (
          <input {...attrs} {...{ readOnly: true }} className={classes(ctx.formControlClass, attrs && attrs.className, this.props.className)} tabIndex={-1} value={onlyText} ref={innerRef} />
        );
      }
    }
    else {
      if (ctx.readonlyAsPlainText) {
        return (
          <div {...attrs} className={classes(ctx.formControlPlainTextClass, attrs && attrs.className, this.props.className)} ref={innerRef}>
            {this.props.children || <span>&nbsp;</span>}
          </div>
        );
      } else {
        return (
          <div {...attrs} {...{ readOnly: true }} className={classes(ctx.formControlClass, attrs && attrs.className, this.props.className)} ref={innerRef}>
            {this.props.children || <span>&nbsp;</span>}
          </div>
        );
      }
    }


  }
}
